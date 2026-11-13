using Ludex.Library.Enums;
using Ludex.Library.Services;
using Ludex.WebApi.Extensions.Globalization;
using Ludex.WebApi.Features.Catalog;
using Ludex.WebApi.Features.Imports.Models.DTOs;
using Ludex.WebApi.Features.Imports.Services;
using Ludex.WebApi.Features.Library.Repositories;
using Ludex.WebApi.Features.Playnite.Models.DTOs;
using Ludex.WebApi.Features.Profile.Repositories;

namespace Ludex.WebApi.Features.Playnite.Services;

/// <summary>
/// Importing a Playnite library (every launcher Playnite reads: Epic, GOG, EA app, Ubisoft Connect, Battle.net,
/// Xbox, emulators…) from the CSV of its Library Exporter Advanced add-on.
/// </summary>
internal interface IPlayniteService
{
    /// <summary>Reads the export and matches it to the catalog (the matched games are saved to dbo.Game).</summary>
    /// <param name="item">The account and the CSV.</param>
    /// <returns>The games with a suggested status, or what's in the way.</returns>
    Task<ServiceApiResponse<PlaynitePreviewDTO>> PreviewAsync(PlaynitePreviewRequest item);

    /// <summary>Adds the games ticked (and more hours to the ones already there).</summary>
    /// <param name="item">The account and the games.</param>
    /// <returns>How many were added and updated.</returns>
    Task<ServiceApiResponse<ImportResultDTO>> ImportAsync(PlayniteImportRequest item);
}

/// <summary>
/// Importing a Playnite library: Steam games are left to the Steam import when Steam is connected (it keeps them in
/// sync); the rest is matched by name.
/// </summary>
/// <param name="matcher">Names → games.</param>
/// <param name="catalog">The catalog (whether IGDB is on).</param>
/// <param name="libraryRepository">Libraries.</param>
/// <param name="profileRepository">Profiles (whether Steam is connected).</param>
/// <param name="globalizationHelper">Messages.</param>
/// <param name="timeProvider">The clock.</param>
/// <param name="logger">Logger.</param>
internal sealed class PlayniteService(
    GameMatcher matcher,
    IGameCatalog catalog,
    ILibraryRepository libraryRepository,
    IProfileRepository profileRepository,
    IGlobalizationHelper globalizationHelper,
    TimeProvider timeProvider,
    ILogger<PlayniteService> logger) : IPlayniteService
{
    /// <inheritdoc />
    public async Task<ServiceApiResponse<PlaynitePreviewDTO>> PreviewAsync(PlaynitePreviewRequest item)
    {
        if (!Guid.TryParse(item.UserId, out var user))
        {
            return ServiceApiResponse<PlaynitePreviewDTO>.NotFound(await globalizationHelper.TranslateAsync(nameof(TranslationKeys.NoRecordsFound), item.UserId ?? string.Empty));
        }

        if (catalog.Source != CatalogSource.Igdb)
        {
            return Answer(new PlaynitePreviewDTO { Problem = "The Playnite import needs IGDB to match the games: set IGDB_CLIENT_ID and IGDB_CLIENT_SECRET in Ludex's .env." });
        }

        if (string.IsNullOrWhiteSpace(item.Csv))
        {
            return Answer(new PlaynitePreviewDTO { Available = true });
        }

        var file = PlayniteCsv.Read(item.Csv);
        var page = new PlaynitePreviewDTO { Available = true, GameCount = file.Games.Count, Missing = file.Missing, Problem = file.Problem };
        if (file.Problem is not null)
        {
            return Answer(page);
        }

        // Steam games come with the Steam import (and its daily sync) when Steam is connected.
        var games = file.Games;
        if ((await profileRepository.GetAsync(user))?.SteamID is { Length: > 0 })
        {
            games = [.. file.Games.Where(static g => g.Store != "Steam")];
            page.SteamSkipped = file.Games.Count - games.Count;
        }

        try
        {
            var library = await libraryRepository.ListAsync(user);
            var matched = await matcher.MatchAsync([.. games.Select(static g => (g.Name, (string?)g.Platform))], library);
            var mine = library.ToDictionary(static r => r.GameID);
            var now = timeProvider.GetUtcNow();

            // The same game twice (two launchers, or a game and its edition): one row, the one with most time.
            page.Games = [.. games
                .Where(g => matched.ContainsKey(g.Name))
                .GroupBy(g => matched[g.Name].GameID)
                .Select(group =>
                {
                    var g = group.OrderByDescending(static x => x.Minutes).First();
                    var game = matched[g.Name];
                    var last = group.Select(static x => x.LastPlayed).Max();
                    var status = group.Select(static x => x.Status).FirstOrDefault(static s => s is not null);
                    return ImportRules.Row(g.Name, g.Source, game, mine.GetValueOrDefault(game.GameID), g.Platform, g.Store, g.Format, g.Minutes, last, status, now);
                })
                .OrderByDescending(static g => g.LastPlayedAt ?? DateTimeOffset.MinValue)
                .ThenByDescending(static g => g.Hours)
                .ThenBy(static g => g.Game.Name, StringComparer.OrdinalIgnoreCase)];
            page.Unmatched = [.. games.Where(g => !matched.ContainsKey(g.Name)).Select(static g => g.Name).Distinct(StringComparer.Ordinal).Order(StringComparer.OrdinalIgnoreCase)];
            logger.LogInformation("Playnite preview: {Games} games, {Matched} matched, {Steam} Steam games left out", file.Games.Count, page.Games.Count, page.SteamSkipped);
            return Answer(page);
        }
        catch (CatalogUnavailableException exception)
        {
            logger.LogWarning("IGDB failed during a Playnite import: {Problem}", exception.Message);
            page.Problem = await globalizationHelper.TranslateAsync(nameof(TranslationKeys.CatalogUnreachable), exception.Message);
            return Answer(page);
        }
    }

    /// <inheritdoc />
    public async Task<ServiceApiResponse<ImportResultDTO>> ImportAsync(PlayniteImportRequest item)
    {
        if (!Guid.TryParse(item.UserId, out var user))
        {
            return ServiceApiResponse<ImportResultDTO>.NotFound(await globalizationHelper.TranslateAsync(nameof(TranslationKeys.NoRecordsFound), item.UserId ?? string.Empty));
        }

        var counts = await libraryRepository.ImportAsync(user, ImportRules.Rows(item.Games, withTrophies: false));
        logger.LogInformation("Playnite import: {Added} added, {Updated} updated", counts.Added, counts.Updated);
        return ServiceApiResponse<ImportResultDTO>.Success(new ImportResultDTO { Added = counts.Added, Updated = counts.Updated });
    }

    private static ServiceApiResponse<PlaynitePreviewDTO> Answer(PlaynitePreviewDTO page) => ServiceApiResponse<PlaynitePreviewDTO>.Success(page);
}
