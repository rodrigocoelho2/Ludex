using Ludex.Library.Enums;
using Ludex.Library.Services;
using Ludex.WebApi.Extensions.Globalization;
using Ludex.WebApi.Features.Catalog;
using Ludex.WebApi.Features.Games.Models.Entities;
using Ludex.WebApi.Features.Games.Repositories;
using Ludex.WebApi.Features.Games.Services;
using Ludex.WebApi.Features.Library.Repositories;
using Ludex.WebApi.Features.Profile.Repositories;
using Ludex.WebApi.Features.Steam.Models.DTOs;
using Microsoft.Extensions.Options;

namespace Ludex.WebApi.Features.Steam.Services;

/// <summary>
/// Importing a Steam library: read it, match it to the catalog, and add the games ticked.
/// </summary>
internal interface ISteamService
{
    /// <summary>Reads a Steam library and matches it to the catalog (the matched games are saved to dbo.Game).</summary>
    /// <param name="userId">The account.</param>
    /// <param name="profile">The Steam profile typed (empty: the one imported from last time).</param>
    /// <returns>The games with a suggested status, or what's in the way.</returns>
    Task<ServiceApiResponse<SteamPreviewDTO>> PreviewAsync(string userId, string? profile);

    /// <summary>Adds the games ticked (and more hours to the ones already there).</summary>
    /// <param name="item">The account, the Steam account and the games.</param>
    /// <returns>How many were added and updated.</returns>
    Task<ServiceApiResponse<SteamImportResultDTO>> ImportAsync(SteamImportRequest item);
}

/// <summary>
/// Importing a Steam library: read it, match it to the catalog, and add the games ticked.
/// </summary>
/// <param name="steam">The Steam Web API.</param>
/// <param name="catalog">The catalog (Steam app → game).</param>
/// <param name="gameRepository">Games (the matched ones are saved).</param>
/// <param name="libraryRepository">Libraries.</param>
/// <param name="profileRepository">Profiles (the Steam account imported from).</param>
/// <param name="options">Whether a Steam key is set.</param>
/// <param name="globalizationHelper">Messages.</param>
/// <param name="timeProvider">The clock.</param>
/// <param name="logger">Logger.</param>
internal sealed class SteamService(
    SteamClient steam,
    IGameCatalog catalog,
    IGameRepository gameRepository,
    ILibraryRepository libraryRepository,
    IProfileRepository profileRepository,
    IOptions<LudexOptions> options,
    IGlobalizationHelper globalizationHelper,
    TimeProvider timeProvider,
    ILogger<SteamService> logger) : ISteamService
{
    /// <summary>Most hours a game may have (as in the game page).</summary>
    private const decimal MaxHours = 99999m;

    /// <inheritdoc />
    public async Task<ServiceApiResponse<SteamPreviewDTO>> PreviewAsync(string userId, string? profile)
    {
        if (!Guid.TryParse(userId, out var user))
        {
            return ServiceApiResponse<SteamPreviewDTO>.NotFound(await globalizationHelper.TranslateAsync(nameof(TranslationKeys.NoRecordsFound), userId ?? string.Empty));
        }

        if (!options.Value.HasSteam)
        {
            return Answer(new SteamPreviewDTO { Problem = "The Steam import is off: set STEAM_API_KEY in Ludex's .env (a key from steamcommunity.com/dev/apikey) and update Ludex." });
        }

        if (catalog.Source != CatalogSource.Igdb)
        {
            return Answer(new SteamPreviewDTO { Problem = "The Steam import needs IGDB to match the games: set IGDB_CLIENT_ID and IGDB_CLIENT_SECRET in Ludex's .env." });
        }

        var typed = (profile ?? string.Empty).Trim();
        if (typed.Length == 0)
        {
            typed = (await profileRepository.GetAsync(user))?.SteamID ?? string.Empty;
            if (typed.Length == 0)
            {
                return Answer(new SteamPreviewDTO { Available = true });
            }
        }

        try
        {
            var (steamId, vanity) = SteamRules.ParseProfile(typed);
            if (steamId is null && vanity is not null)
            {
                steamId = await steam.ResolveAsync(vanity);
                if (steamId is null)
                {
                    return Problem($"There's no Steam profile called '{vanity}'. Paste the address of your Steam profile instead.");
                }
            }

            if (steamId is null)
            {
                return Problem("That isn't a Steam profile. Paste its address (steamcommunity.com/id/… or /profiles/…) or the custom name in it.");
            }

            var player = await steam.PlayerAsync(steamId);
            if (player is null)
            {
                return Problem("Steam has no account with that id.");
            }

            var page = new SteamPreviewDTO
            {
                Available = true,
                SteamId = steamId,
                PersonaName = player.PersonaName,
                AvatarUrl = player.AvatarFull,
                ProfileUrl = player.ProfileUrl,
            };

            var owned = await steam.OwnedGamesAsync(steamId);
            if (owned is null || owned.Count == 0)
            {
                page.Problem = player.CommunityVisibilityState == 3
                    ? "Steam lists no games for this account. If it has games, set Game details to Public (Steam → Edit profile → Privacy settings) and try again."
                    : "This Steam profile is private. Set the profile and Game details to Public (Steam → Edit profile → Privacy settings) and try again.";
                return Answer(page);
            }

            page.OwnedCount = owned.Count;
            var matched = await catalog.SteamAsync([.. owned.Select(static g => g.AppId)]);
            foreach (var game in matched.Values.DistinctBy(static g => g.Id))
            {
                await gameRepository.UpsertAsync(GameRow.From(game));
            }

            var library = (await libraryRepository.ListAsync(user)).ToDictionary(static r => r.GameID);
            var now = timeProvider.GetUtcNow();

            // Two apps can be the same game (an old version and a remaster): one row, with the most hours.
            page.Games = [.. owned
                .Where(g => matched.ContainsKey(g.AppId))
                .GroupBy(g => matched[g.AppId].Id)
                .Select(group =>
                {
                    var game = matched[group.First().AppId];
                    var minutes = group.Max(static g => g.PlaytimeForever);
                    var last = group.Select(static g => SteamRules.LastPlayed(g.LastPlayed)).Max();
                    var hours = SteamRules.Hours(minutes);
                    var (status, selected) = SteamRules.Suggest(minutes, last, now);
                    var mine = library.GetValueOrDefault(game.Id);
                    return new SteamGameDTO
                    {
                        AppId = group.OrderByDescending(static g => g.PlaytimeForever).First().AppId,
                        Game = GameMapper.Summary(game, mine is null ? null : GameMapper.Status(mine.Status)),
                        Hours = hours,
                        RecentHours = SteamRules.Hours(group.Max(static g => g.Playtime2Weeks ?? 0)),
                        LastPlayedAt = last,
                        LibraryHours = mine?.Hours,
                        SuggestedStatus = status,
                        Selected = mine is null ? selected : hours > (mine.Hours ?? 0m),
                    };
                })
                .OrderByDescending(static g => g.LastPlayedAt ?? DateTimeOffset.MinValue)
                .ThenByDescending(static g => g.Hours)];
            page.Unmatched = [.. owned
                .Where(g => !matched.ContainsKey(g.AppId))
                .Select(static g => string.IsNullOrWhiteSpace(g.Name) ? $"App {g.AppId}" : g.Name.Trim())
                .Order(StringComparer.OrdinalIgnoreCase)];
            return Answer(page);
        }
        catch (SteamUnavailableException exception)
        {
            return Problem(exception.Message);
        }
        catch (CatalogUnavailableException exception)
        {
            logger.LogWarning("IGDB failed during a Steam import: {Problem}", exception.Message);
            return Problem(await globalizationHelper.TranslateAsync(nameof(TranslationKeys.CatalogUnreachable), exception.Message));
        }
    }

    /// <inheritdoc />
    public async Task<ServiceApiResponse<SteamImportResultDTO>> ImportAsync(SteamImportRequest item)
    {
        if (!Guid.TryParse(item.UserId, out var user))
        {
            return ServiceApiResponse<SteamImportResultDTO>.NotFound(await globalizationHelper.TranslateAsync(nameof(TranslationKeys.NoRecordsFound), item.UserId ?? string.Empty));
        }

        if (!SteamRules.IsSteamId(item.SteamId))
        {
            const string message = "That isn't a Steam account: load your Steam games again.";
            return ServiceApiResponse<SteamImportResultDTO>.ValidationProblems(message).AddError("SteamId", message);
        }

        var rows = item.Games
            .Where(static g => g.GameId > 0 && Enum.IsDefined(g.Status) && g.Hours is null or (>= 0 and <= MaxHours))
            .GroupBy(static g => g.GameId)
            .Select(static group =>
            {
                var game = group.OrderByDescending(static g => g.Hours ?? 0m).First();
                return new LibraryImportRow(
                    game.GameId,
                    (byte)game.Status,
                    game.Hours is { } hours ? Math.Round(hours, 1, MidpointRounding.AwayFromZero) : null,
                    game.LastPlayedAt?.UtcDateTime is { } at ? DateTime.SpecifyKind(at, DateTimeKind.Unspecified) : null);
            })
            .ToList();

        var counts = await libraryRepository.ImportAsync(user, rows);
        await profileRepository.SetSteamAsync(user, item.SteamId!);
        logger.LogInformation("Steam import: {Added} added, {Updated} with more hours", counts.Added, counts.Updated);
        return ServiceApiResponse<SteamImportResultDTO>.Success(new SteamImportResultDTO { Added = counts.Added, Updated = counts.Updated });
    }

    private static ServiceApiResponse<SteamPreviewDTO> Answer(SteamPreviewDTO page) => ServiceApiResponse<SteamPreviewDTO>.Success(page);

    private static ServiceApiResponse<SteamPreviewDTO> Problem(string message) =>
        ServiceApiResponse<SteamPreviewDTO>.ValidationProblems(message).AddError("Profile", message);
}
