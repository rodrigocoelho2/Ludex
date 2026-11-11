using Ludex.Library.Enums;

namespace Ludex.WebApi.Features.Catalog;

/// <summary>
/// A game as the catalog describes it (IGDB, or the list built into Ludex).
/// </summary>
internal sealed class CatalogGame
{
    /// <summary>Gets or sets the id (IGDB's; the built-in list uses ids from <see cref="OfflineCatalog.FirstId"/>).</summary>
    public int Id { get; set; }

    /// <summary>Gets or sets the name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the URL-friendly name.</summary>
    public string? Slug { get; set; }

    /// <summary>Gets or sets what the game is about.</summary>
    public string? Summary { get; set; }

    /// <summary>Gets or sets the IGDB image id of the cover.</summary>
    public string? CoverImageId { get; set; }

    /// <summary>Gets or sets the IGDB image id of the wide art behind the title (an artwork, or a screenshot).</summary>
    public string? HeroImageId { get; set; }

    /// <summary>Gets or sets the IGDB image ids of the screenshots.</summary>
    public List<string> Screenshots { get; set; } = [];

    /// <summary>Gets or sets the first release.</summary>
    public DateOnly? ReleaseDate { get; set; }

    /// <summary>Gets or sets the developer.</summary>
    public string? Developer { get; set; }

    /// <summary>Gets or sets the publisher.</summary>
    public string? Publisher { get; set; }

    /// <summary>Gets or sets the genres.</summary>
    public List<string> Genres { get; set; } = [];

    /// <summary>Gets or sets the platforms (short names: PC, PS5, Switch…).</summary>
    public List<string> Platforms { get; set; } = [];

    /// <summary>Gets or sets the modes (Single player, Co-operative…).</summary>
    public List<string> Modes { get; set; } = [];

    /// <summary>Gets or sets the critics' rating (0–100).</summary>
    public int? CriticScore { get; set; }

    /// <summary>Gets or sets the hours to finish the main story.</summary>
    public decimal? TimeToBeatMain { get; set; }

    /// <summary>Gets or sets the hours to finish everything (100%).</summary>
    public decimal? TimeToBeatFull { get; set; }
}

/// <summary>
/// Thrown when the catalog can't be reached or refuses the call.
/// </summary>
/// <param name="message">What went wrong (shown to the user).</param>
/// <param name="inner">The cause.</param>
internal sealed class CatalogUnavailableException(string message, Exception? inner = null) : Exception(message, inner);

/// <summary>
/// Where games come from: search and one game's full data.
/// </summary>
internal interface IGameCatalog
{
    /// <summary>Gets which catalog this is.</summary>
    CatalogSource Source { get; }

    /// <summary>
    /// Finds games by name.
    /// </summary>
    /// <param name="query">The text typed.</param>
    /// <param name="take">At most this many.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The games, best match first (search fields only: no summary or screenshots).</returns>
    /// <exception cref="CatalogUnavailableException">The catalog can't be reached.</exception>
    Task<List<CatalogGame>> SearchAsync(string query, int take, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets one game with everything the game page shows.
    /// </summary>
    /// <param name="id">The game id.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The game, or null when the catalog has no such game.</returns>
    /// <exception cref="CatalogUnavailableException">The catalog can't be reached.</exception>
    Task<CatalogGame?> GetAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Well-known games with a cover (the wall behind the sign-in form).
    /// </summary>
    /// <param name="take">At most this many.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The games (search fields only).</returns>
    /// <exception cref="CatalogUnavailableException">The catalog can't be reached.</exception>
    Task<List<CatalogGame>> PopularAsync(int take, CancellationToken cancellationToken = default);

    /// <summary>
    /// The catalog's games for Steam apps (the Steam import). Editions and versions are given as their main game.
    /// </summary>
    /// <param name="appIds">Steam app ids.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The game of each app the catalog knows (apps it doesn't know are left out), with everything the game page shows.</returns>
    /// <exception cref="CatalogUnavailableException">The catalog can't be reached.</exception>
    Task<Dictionary<int, CatalogGame>> SteamAsync(IReadOnlyCollection<int> appIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Several games at once, with everything the game page shows (the daily refresh).
    /// </summary>
    /// <param name="ids">The game ids.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The games the catalog knows.</returns>
    /// <exception cref="CatalogUnavailableException">The catalog can't be reached.</exception>
    Task<List<CatalogGame>> GetManyAsync(IReadOnlyCollection<int> ids, CancellationToken cancellationToken = default);
}
