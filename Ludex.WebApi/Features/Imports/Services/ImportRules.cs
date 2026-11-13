using Ludex.Library.Enums;
using Ludex.Library.Platforms;
using Ludex.WebApi.Features.Games.Models.Entities;
using Ludex.WebApi.Features.Games.Services;
using Ludex.WebApi.Features.Imports.Models.DTOs;
using Ludex.WebApi.Features.Library.Models.Entities;
using Ludex.WebApi.Features.Library.Repositories;
using Ludex.WebApi.Features.Library.Services;
using Ludex.WebApi.Features.Steam.Services;

namespace Ludex.WebApi.Features.Imports.Services;

/// <summary>
/// What the imports from other libraries (Playnite, PlayStation) share: a suggested status from how a game was played,
/// the preview row, and the rows dbo.LibraryImport takes.
/// </summary>
internal static class ImportRules
{
    /// <summary>Most hours a game may have (as in the game page).</summary>
    public const decimal MaxHours = 99999m;

    /// <summary>Longest platform name kept.</summary>
    public const int MaxPlatform = 60;

    /// <summary>
    /// The preview row of a game.
    /// </summary>
    /// <param name="name">The name in the other library.</param>
    /// <param name="source">Where it came from there.</param>
    /// <param name="game">The game matched.</param>
    /// <param name="mine">The library's entry of that game on the same platform family (null: a new copy).</param>
    /// <param name="platform">The platform of the copy.</param>
    /// <param name="store">The PC store.</param>
    /// <param name="format">Physical or digital.</param>
    /// <param name="minutes">Minutes played.</param>
    /// <param name="lastPlayed">When it was last played.</param>
    /// <param name="status">A status the other library says (null: suggested from how it was played).</param>
    /// <param name="now">Now.</param>
    /// <returns>The row.</returns>
    public static ImportGameDTO Row(
        string name,
        string? source,
        GameRow game,
        LibraryRow? mine,
        string platform,
        string? store,
        GameFormat? format,
        int minutes,
        DateTimeOffset? lastPlayed,
        GameStatus? status,
        DateTimeOffset now)
    {
        var hours = SteamRules.Hours(minutes);
        var (suggested, selected) = SteamRules.Suggest(minutes, lastPlayed, now);
        if (status is { } said)
        {
            suggested = said;
            selected = true;
        }

        var copy = GameCopies.Normalize(platform, store, format);
        return new ImportGameDTO
        {
            Name = name,
            Source = source,
            Game = GameMapper.Summary(game, mine is null ? null : GameMapper.Status(mine.Status)),
            Platform = platform,
            Store = copy.Problem is null ? copy.Store : null,
            Format = copy.Problem is null ? copy.Format : null,
            Hours = hours,
            LastPlayedAt = lastPlayed,
            LibraryHours = mine?.Hours,
            SuggestedStatus = suggested,
            Selected = mine is null ? selected : hours > (mine.Hours ?? 0m),
        };
    }

    /// <summary>
    /// The rows for dbo.LibraryImport from the games ticked: one per game and platform family (the one with most
    /// hours), hours to one decimal, a copy only as the platform allows. The repository picks the entry each goes into.
    /// </summary>
    /// <param name="games">The games sent.</param>
    /// <param name="withTrophies">Whether trophy counts are kept (PlayStation).</param>
    /// <returns>The rows.</returns>
    public static List<LibraryImportRow> Rows(IEnumerable<ImportGameRequest> games, bool withTrophies) =>
    [
        .. games
            .Where(static g => g.GameId > 0 && Enum.IsDefined(g.Status) && g.Hours is null or (>= 0 and <= MaxHours))
            .GroupBy(static g => (g.GameId, CopyRules.Family(string.IsNullOrWhiteSpace(g.Platform) ? "PC" : g.Platform)))
            .Select(group =>
            {
                var game = group.OrderByDescending(static g => g.Hours ?? 0m).First();
                var platform = string.IsNullOrWhiteSpace(game.Platform) ? "PC" : game.Platform.Trim();
                if (platform.Length > MaxPlatform)
                {
                    platform = platform[..MaxPlatform];
                }

                var copy = GameCopies.Normalize(platform, game.Store, game.Format);
                var trophies = withTrophies && game.TrophiesTotal is > 0 and <= 32000 && game.TrophiesEarned is { } earned && earned >= 0 && earned <= game.TrophiesTotal;
                return new LibraryImportRow(
                    game.GameId,
                    (byte)game.Status,
                    game.Hours is { } hours ? Math.Round(hours, 1, MidpointRounding.AwayFromZero) : null,
                    game.LastPlayedAt?.UtcDateTime is { } at ? DateTime.SpecifyKind(at, DateTimeKind.Unspecified) : null,
                    Platform: platform,
                    Store: copy.Problem is null ? copy.Store : null,
                    Format: copy.Problem is null && copy.Format is { } f ? (byte)f : null,
                    AchUnlocked: trophies ? (short)game.TrophiesEarned!.Value : null,
                    AchTotal: trophies ? (short)game.TrophiesTotal!.Value : null);
            }),
    ];
}
