using Ludex.WebApi.Features.Catalog;

namespace Ludex.WebApi.Features.Games.Models.Entities;

/// <summary>
/// A game copied from the catalog (dbo.Game).
/// </summary>
public class GameRow
{
    /// <summary>Separates the genres, platforms and modes in their columns.</summary>
    public const char ListSeparator = ';';

    /// <summary>Gets or sets the id (IGDB's, or the built-in list's).</summary>
    public int GameID { get; set; }

    /// <summary>Gets or sets the name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the URL-friendly name.</summary>
    public string? Slug { get; set; }

    /// <summary>Gets or sets the summary.</summary>
    public string? Summary { get; set; }

    /// <summary>Gets or sets the IGDB image id of the cover.</summary>
    public string? CoverImageID { get; set; }

    /// <summary>Gets or sets the IGDB image id of the wide art.</summary>
    public string? HeroImageID { get; set; }

    /// <summary>Gets or sets the IGDB image ids of the screenshots, separated by commas.</summary>
    public string? Screenshots { get; set; }

    /// <summary>Gets or sets the first release (a DATE).</summary>
    public DateTime? FirstReleaseDate { get; set; }

    /// <summary>Gets or sets the developer.</summary>
    public string? Developer { get; set; }

    /// <summary>Gets or sets the publisher.</summary>
    public string? Publisher { get; set; }

    /// <summary>Gets or sets the genres, separated by <see cref="ListSeparator"/>.</summary>
    public string? Genres { get; set; }

    /// <summary>Gets or sets the platforms, separated by <see cref="ListSeparator"/>.</summary>
    public string? Platforms { get; set; }

    /// <summary>Gets or sets the modes, separated by <see cref="ListSeparator"/>.</summary>
    public string? Modes { get; set; }

    /// <summary>Gets or sets when the data was copied from the catalog (UTC).</summary>
    public DateTime FetchedAt { get; set; }

    /// <summary>
    /// Splits a list column.
    /// </summary>
    /// <param name="value">The column.</param>
    /// <param name="separator">The separator.</param>
    /// <returns>The items.</returns>
    public static List<string> Split(string? value, char separator = ListSeparator) =>
        [.. (value ?? string.Empty).Split(separator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)];

    /// <summary>
    /// Joins a list for its column.
    /// </summary>
    /// <param name="items">The items.</param>
    /// <param name="separator">The separator.</param>
    /// <param name="maxLength">The column's length; items that don't fit are left out.</param>
    /// <returns>The column value, or null when empty.</returns>
    public static string? Join(IEnumerable<string> items, char separator, int maxLength)
    {
        var result = string.Empty;
        foreach (var item in items.Select(static i => i.Trim()).Where(static i => i.Length > 0))
        {
            var next = result.Length == 0 ? item : result + separator + item;
            if (next.Length > maxLength)
            {
                break;
            }

            result = next;
        }

        return result.Length == 0 ? null : result;
    }

    /// <summary>
    /// The row for a catalog game (what GameUpsert saves).
    /// </summary>
    /// <param name="game">The catalog game.</param>
    /// <returns>The row (FetchedAt is set by the database).</returns>
    internal static GameRow From(CatalogGame game) => new()
    {
        GameID = game.Id,
        Name = game.Name.Length > 300 ? game.Name[..300] : game.Name,
        Slug = game.Slug is { Length: > 300 } slug ? slug[..300] : game.Slug,
        Summary = game.Summary,
        CoverImageID = game.CoverImageId,
        HeroImageID = game.HeroImageId,
        Screenshots = Join(game.Screenshots, ',', 1000),
        FirstReleaseDate = game.ReleaseDate?.ToDateTime(TimeOnly.MinValue),
        Developer = Cut(game.Developer, 200),
        Publisher = Cut(game.Publisher, 200),
        Genres = Join(game.Genres, ListSeparator, 400),
        Platforms = Join(game.Platforms, ListSeparator, 1000),
        Modes = Join(game.Modes, ListSeparator, 400),
    };

    private static string? Cut(string? value, int length) => value is { } text && text.Length > length ? text[..length] : value;
}
