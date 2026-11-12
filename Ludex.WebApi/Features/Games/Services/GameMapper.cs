using Ludex.Library.Enums;
using Ludex.WebApi.Features.Catalog;
using Ludex.WebApi.Features.Games.Models.DTOs;
using Ludex.WebApi.Features.Games.Models.Entities;

namespace Ludex.WebApi.Features.Games.Services;

/// <summary>
/// Turns rows and catalog games into the cards the Website draws.
/// </summary>
internal static class GameMapper
{
    /// <summary>
    /// The card of a saved game.
    /// </summary>
    /// <param name="row">The game.</param>
    /// <param name="myStatus">Its status in the signed-in account's library.</param>
    /// <param name="friendNames">The friends who have it.</param>
    /// <returns>The card.</returns>
    public static GameSummaryDTO Summary(GameRow row, GameStatus? myStatus = null, List<string>? friendNames = null) => new()
    {
        Id = row.GameID,
        Name = row.Name,
        Year = row.FirstReleaseDate?.Year,
        CoverImageId = row.CoverImageID,
        Developer = row.Developer,
        Platforms = GameRow.Split(row.Platforms),
        Tint = GameTint.For(row.GameID),
        MyStatus = myStatus,
        FriendNames = friendNames ?? [],
        CriticScore = row.CriticScore,
        TimeToBeatMain = row.TimeToBeatMain,
        TimeToBeatFull = row.TimeToBeatFull,
    };

    /// <summary>
    /// The card of a catalog game (a search result).
    /// </summary>
    /// <param name="game">The game.</param>
    /// <param name="myStatus">Its status in the signed-in account's library.</param>
    /// <param name="friendNames">The friends who have it.</param>
    /// <returns>The card.</returns>
    public static GameSummaryDTO Summary(CatalogGame game, GameStatus? myStatus = null, List<string>? friendNames = null) => new()
    {
        Id = game.Id,
        Name = game.Name,
        Year = game.ReleaseDate?.Year,
        CoverImageId = game.CoverImageId,
        Developer = game.Developer,
        Platforms = [.. game.Platforms],
        Tint = GameTint.For(game.Id),
        MyStatus = myStatus,
        FriendNames = friendNames ?? [],
        CriticScore = game.CriticScore,
        TimeToBeatMain = game.TimeToBeatMain,
        TimeToBeatFull = game.TimeToBeatFull,
    };

    /// <summary>
    /// A status read from the database.
    /// </summary>
    /// <param name="value">The stored number.</param>
    /// <returns>The status, or null when the number isn't one.</returns>
    public static GameStatus? Status(byte? value) => value is >= 1 and <= 6 ? (GameStatus)value.Value : null;

    /// <summary>
    /// A UTC time read from the database.
    /// </summary>
    /// <param name="value">The time (DATETIME2, kind unspecified).</param>
    /// <returns>The time with a zero offset.</returns>
    public static DateTimeOffset Utc(DateTime value) => new(DateTime.SpecifyKind(value, DateTimeKind.Utc));

    /// <summary>
    /// Groups friend names by game.
    /// </summary>
    /// <param name="rows">(game, user name) pairs.</param>
    /// <returns>The names of each game.</returns>
    public static Dictionary<int, List<string>> NamesByGame(IEnumerable<(int GameId, string UserName)> rows) =>
        rows.GroupBy(static r => r.GameId).ToDictionary(static g => g.Key, static g => g.Select(static r => r.UserName).ToList());
}
