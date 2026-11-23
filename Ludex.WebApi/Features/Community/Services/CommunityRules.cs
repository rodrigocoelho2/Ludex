using Ludex.Library.Enums;
using Ludex.WebApi.Features.Community.Models.DTOs;
using Ludex.WebApi.Features.Community.Models.Entities;
using Ludex.WebApi.Features.Games.Services;

namespace Ludex.WebApi.Features.Community.Services;

/// <summary>
/// The community numbers: a game's spread of ratings and statuses, and how a game shows on a shelf.
/// </summary>
internal static class CommunityRules
{
    /// <summary>
    /// Fills a game's ratings and statuses from the counts.
    /// </summary>
    /// <param name="rows">The counts (Kind 1 rating, 2 status).</param>
    /// <param name="into">The game's community block.</param>
    public static void Fill(IEnumerable<GameRatingRow> rows, GameCommunityDTO into)
    {
        var histogram = new int[10];
        foreach (var row in rows)
        {
            if (row.Kind == 1 && row.Value is >= 1 and <= 10)
            {
                histogram[row.Value - 1] += row.Count;
            }
            else if (row.Kind == 2)
            {
                switch ((GameStatus)row.Value)
                {
                    case GameStatus.Finished:
                    case GameStatus.Played:
                        into.Played += row.Count;
                        break;
                    case GameStatus.Playing:
                        into.Playing += row.Count;
                        break;
                    case GameStatus.Backlog:
                        into.Backlog += row.Count;
                        break;
                    case GameStatus.Wishlist:
                        into.Wishlist += row.Count;
                        break;
                    case GameStatus.Dropped:
                        into.Dropped += row.Count;
                        break;
                }
            }
        }

        into.Histogram = [.. histogram];
        into.RatingCount = histogram.Sum();
        into.AverageRating = into.RatingCount == 0
            ? null
            : Math.Round(histogram.Select(static (count, i) => (decimal)count * (i + 1)).Sum() / into.RatingCount, 1, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// A game on a community shelf.
    /// </summary>
    /// <param name="row">The row.</param>
    /// <returns>The game.</returns>
    public static CommunityGameDTO Game(CommunityGameRow row) => new()
    {
        Game = GameMapper.Summary(row),
        Active = row.Active,
        Owners = row.Owners,
        Ratings = row.Ratings,
        AverageRating = row.AverageRating is { } average ? Math.Round(average, 1, MidpointRounding.AwayFromZero) : null,
        ReleaseDate = row.FirstReleaseDate is { } date ? DateOnly.FromDateTime(date) : null,
    };

    /// <summary>
    /// The most owners a sleeper hit may have: a quarter of the accounts, and never fewer than two.
    /// </summary>
    /// <param name="accounts">How many accounts there are.</param>
    /// <returns>The most owners.</returns>
    public static int SleeperMaxOwners(int accounts) => Math.Max(2, (int)Math.Ceiling(accounts / 4.0));
}
