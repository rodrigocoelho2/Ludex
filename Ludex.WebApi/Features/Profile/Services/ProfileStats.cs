using System.Globalization;
using Ludex.Library.Enums;
using Ludex.WebApi.Features.Games.Models.Entities;
using Ludex.WebApi.Features.Library.Models.Entities;
using Ludex.WebApi.Features.Profile.Models.DTOs;

namespace Ludex.WebApi.Features.Profile.Services;

/// <summary>
/// The Backloggd-style numbers of a profile: the four favourites, the spread of ratings, games finished per year and
/// the genres played most. Pure functions over the library, so they are unit tested.
/// </summary>
internal static class ProfileStats
{
    /// <summary>How many favourites the top of a profile shows.</summary>
    public const int MaxFavorites = 4;

    /// <summary>
    /// The favourite game ids saved, in order: known numbers only, each once, at most four.
    /// </summary>
    /// <param name="saved">The saved value (comma separated), or null.</param>
    /// <returns>The ids.</returns>
    public static List<int> FavoriteIds(string? saved) =>
        [.. (saved ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(static s => int.TryParse(s, NumberStyles.None, CultureInfo.InvariantCulture, out var id) ? id : 0)
            .Where(static id => id > 0)
            .Distinct()
            .Take(MaxFavorites)];

    /// <summary>
    /// The value to save for the favourites chosen: games of the library only, each once, at most four.
    /// </summary>
    /// <param name="chosen">The ids chosen, in order.</param>
    /// <param name="library">The ids of the library's games.</param>
    /// <returns>The comma separated ids, or null for none.</returns>
    public static string? FavoritesValue(IEnumerable<int> chosen, IReadOnlySet<int> library)
    {
        var ids = chosen.Where(library.Contains).Distinct().Take(MaxFavorites).ToList();
        return ids.Count == 0 ? null : string.Join(',', ids.Select(static id => id.ToString(CultureInfo.InvariantCulture)));
    }

    /// <summary>
    /// How many games got each rating (a game owned twice counts once, with its first rated copy).
    /// </summary>
    /// <param name="rows">The library.</param>
    /// <returns>Ten counts: ratings 1 to 10.</returns>
    public static List<int> RatingCounts(IEnumerable<LibraryRow> rows)
    {
        var counts = new int[10];
        foreach (var rating in rows.Where(static r => r.Rating is >= 1 and <= 10).GroupBy(static r => r.GameID).Select(static g => g.OrderBy(static r => r.EntryID).First().Rating!.Value))
        {
            counts[rating - 1]++;
        }

        return [.. counts];
    }

    /// <summary>
    /// How many games were finished each year (the day given in the log, else the day the status became Finished).
    /// </summary>
    /// <param name="rows">The library.</param>
    /// <param name="take">At most this many years, newest first.</param>
    /// <returns>The years.</returns>
    public static List<YearCountDTO> FinishedByYear(IEnumerable<LibraryRow> rows, int take = 5) =>
        [.. rows
            .Where(static r => r.Status == (byte)GameStatus.Finished && (r.FinishedOn ?? r.FinishedAt) is not null)
            .GroupBy(static r => r.GameID)
            .Select(static g => (g.Select(static r => r.FinishedOn ?? r.FinishedAt).Min())!.Value.Year)
            .GroupBy(static year => year)
            .OrderByDescending(static g => g.Key)
            .Take(take)
            .Select(static g => new YearCountDTO { Year = g.Key, Count = g.Count() })];

    /// <summary>
    /// The genres with most games in the library (wishlist left out).
    /// </summary>
    /// <param name="rows">The library.</param>
    /// <param name="take">At most this many.</param>
    /// <returns>The genres, most first.</returns>
    public static List<NameCountDTO> TopGenres(IEnumerable<LibraryRow> rows, int take = 6) =>
        [.. rows
            .Where(static r => r.Status != (byte)GameStatus.Wishlist)
            .DistinctBy(static r => r.GameID)
            .SelectMany(static r => GameRow.Split(r.Genres))
            .GroupBy(static genre => genre, StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(static g => g.Count())
            .ThenBy(static g => g.Key, StringComparer.OrdinalIgnoreCase)
            .Take(take)
            .Select(static g => new NameCountDTO { Name = g.Key, Count = g.Count() })];
}
