using Ludex.Library.Enums;
using Ludex.WebApi.Features.Catalog;
using Ludex.WebApi.Features.Games.Services;
using Ludex.WebApi.Features.Library.Models.DTOs;
using Ludex.WebApi.Features.Library.Models.Entities;

namespace Ludex.WebApi.Features.Library.Services;

/// <summary>
/// The rules of a library, kept apart from the database so the tests can check them: what goes into the friends' feed
/// when something changes, and the numbers on the home tiles.
/// </summary>
internal static class LibraryRules
{
    /// <summary>
    /// The feed lines a change produces.
    /// A new game: on the wishlist → Wishlisted; playing → Started; finished or dropped → Added. Then a rating → Rated.
    /// A game already there: a new status → Started / Finished / Wishlisted / Dropped; a new rating → Rated.
    /// Taking a rating away, or saving hours and notes only, adds nothing.
    /// </summary>
    /// <param name="statusBefore">The status before (null: the game wasn't in the library).</param>
    /// <param name="ratingBefore">The rating before.</param>
    /// <param name="status">The status now.</param>
    /// <param name="rating">The rating now.</param>
    /// <returns>The lines, in order.</returns>
    public static List<(ActivityKind Kind, int? Rating)> Activities(GameStatus? statusBefore, int? ratingBefore, GameStatus status, int? rating)
    {
        var lines = new List<(ActivityKind, int?)>();
        if (statusBefore is null)
        {
            lines.Add((status switch
            {
                GameStatus.Wishlist => ActivityKind.Wishlisted,
                GameStatus.Playing => ActivityKind.Started,
                _ => ActivityKind.Added,
            }, null));
        }
        else if (statusBefore != status && status != GameStatus.Backlog)
        {
            lines.Add((status switch
            {
                GameStatus.Playing => ActivityKind.Started,
                GameStatus.Finished => ActivityKind.Finished,
                GameStatus.Wishlist => ActivityKind.Wishlisted,
                _ => ActivityKind.Dropped,
            }, null));
        }

        if (rating is not null && rating != ratingBefore)
        {
            lines.Add((ActivityKind.Rated, rating));
        }

        return lines;
    }

    /// <summary>
    /// The counts per status.
    /// </summary>
    /// <param name="rows">The library.</param>
    /// <returns>The counts.</returns>
    public static LibraryCountsDTO Counts(IReadOnlyCollection<LibraryRow> rows) => new()
    {
        All = rows.Count,
        Playing = rows.Count(static r => r.Status == (byte)GameStatus.Playing),
        Finished = rows.Count(static r => r.Status == (byte)GameStatus.Finished),
        Wishlist = rows.Count(static r => r.Status == (byte)GameStatus.Wishlist),
        Dropped = rows.Count(static r => r.Status == (byte)GameStatus.Dropped),
        Backlog = rows.Count(static r => r.Status == (byte)GameStatus.Backlog),
    };

    /// <summary>
    /// The numbers on the home tiles.
    /// </summary>
    /// <param name="rows">The library.</param>
    /// <param name="year">The current year.</param>
    /// <returns>The numbers.</returns>
    public static LibraryStatsDTO Stats(IReadOnlyCollection<LibraryRow> rows, int year)
    {
        var ratings = rows.Where(static r => r.Rating is not null).Select(static r => (decimal)r.Rating!.Value).ToList();
        return new LibraryStatsDTO
        {
            GameCount = rows.Count,
            FinishedThisYear = rows.Count(r => r.Status == (byte)GameStatus.Finished && r.FinishedAt?.Year == year),
            Year = year,
            AverageRating = ratings.Count == 0 ? null : Math.Round(ratings.Average(), 1),
            WishlistCount = rows.Count(static r => r.Status == (byte)GameStatus.Wishlist),
            TotalHours = rows.Sum(static r => r.Hours ?? 0m),
        };
    }

    /// <summary>
    /// One game of the library as the Website shows it.
    /// </summary>
    /// <param name="row">The entry.</param>
    /// <param name="friendNames">The friends who have it.</param>
    /// <param name="collectionIds">The collections it is in.</param>
    /// <param name="withNotes">Include the notes (the owner's own list only).</param>
    /// <returns>The entry.</returns>
    public static LibraryEntryDTO Entry(LibraryRow row, List<string>? friendNames = null, List<int>? collectionIds = null, bool withNotes = false)
    {
        var status = GameMapper.Status(row.Status) ?? GameStatus.Playing;
        return new LibraryEntryDTO
        {
            Game = GameMapper.Summary(row, status, friendNames),
            Status = status,
            Rating = row.Rating,
            Hours = row.Hours,
            Platform = row.Platform,
            PlatformFamily = PlatformNames.Family(row.Platform ?? GameRowFirstPlatform(row)),
            Store = row.Store,
            Format = row.Format is 1 or 2 ? (GameFormat)row.Format.Value : null,
            Notes = withNotes ? row.Notes : null,
            AddedAt = GameMapper.Utc(row.AddedAt),
            UpdatedAt = GameMapper.Utc(row.UpdatedAt),
            FinishedAt = row.FinishedAt is { } finished ? GameMapper.Utc(finished) : null,
            SteamAppId = row.SteamAppID,
            RecentHours = row.RecentMinutes is > 0 ? Math.Round(row.RecentMinutes.Value / 60m, 1, MidpointRounding.AwayFromZero) : null,
            AchievementsUnlocked = row.AchievementsUnlocked,
            AchievementsTotal = row.AchievementsTotal,
            CollectionIds = collectionIds ?? [],
            Price = Price(row),
        };
    }

    /// <summary>
    /// A game's price and alert, when there's either.
    /// </summary>
    /// <param name="row">The entry.</param>
    /// <returns>The price, or null.</returns>
    public static PriceDTO? Price(LibraryRow row) => row.PriceCheckedAt is null && row.PriceTarget is null
        ? null
        : new PriceDTO
        {
            Amount = row.PriceAmount,
            Regular = row.PriceRegular,
            Cut = row.PriceCut,
            Currency = row.PriceCurrency,
            Shop = row.PriceShop,
            Url = row.PriceUrl,
            Low = row.PriceLow,
            CheckedAt = row.PriceCheckedAt is { } checkedAt ? GameMapper.Utc(checkedAt) : null,
            Target = row.PriceTarget,
        };

    private static string? GameRowFirstPlatform(LibraryRow row) =>
        Ludex.WebApi.Features.Games.Models.Entities.GameRow.Split(row.Platforms).FirstOrDefault();
}
