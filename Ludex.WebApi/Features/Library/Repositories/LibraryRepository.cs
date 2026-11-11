using Ludex.WebApi.Extensions.Data;
using Ludex.WebApi.Features.Library.Models.Entities;

namespace Ludex.WebApi.Features.Library.Repositories;

/// <summary>
/// The libraries (dbo.LibraryEntry) and what their owners did (dbo.Activity).
/// </summary>
internal interface ILibraryRepository
{
    /// <summary>Every game of a library, most recently changed first.</summary>
    /// <param name="userId">The account.</param>
    /// <returns>The games.</returns>
    Task<List<LibraryRow>> ListAsync(Guid userId);

    /// <summary>One game of a library.</summary>
    /// <param name="userId">The account.</param>
    /// <param name="gameId">The game.</param>
    /// <returns>The entry, or null when the game isn't there.</returns>
    Task<LibraryRow?> GetAsync(Guid userId, int gameId);

    /// <summary>Adds a game or changes it (the game must be in dbo.Game).</summary>
    /// <param name="userId">The account.</param>
    /// <param name="gameId">The game.</param>
    /// <param name="status">The status.</param>
    /// <param name="rating">The rating.</param>
    /// <param name="hours">The hours.</param>
    /// <param name="platform">The platform.</param>
    /// <param name="notes">The notes.</param>
    /// <param name="store">The PC store.</param>
    /// <param name="format">The kind of copy (1 physical, 2 digital).</param>
    /// <returns>A task.</returns>
    Task SaveAsync(Guid userId, int gameId, byte status, byte? rating, decimal? hours, string? platform, string? notes, string? store, byte? format);

    /// <summary>Takes a game out of a library.</summary>
    /// <param name="userId">The account.</param>
    /// <param name="gameId">The game.</param>
    /// <returns>A task.</returns>
    Task DeleteAsync(Guid userId, int gameId);

    /// <summary>Records something the account did.</summary>
    /// <param name="userId">The account.</param>
    /// <param name="gameId">The game.</param>
    /// <param name="kind">What happened.</param>
    /// <param name="rating">The rating, for a Rated line.</param>
    /// <returns>A task.</returns>
    Task AddActivityAsync(Guid userId, int gameId, byte kind, byte? rating);

    /// <summary>
    /// Adds many games at once (the Steam import): new ones with their status, hours and platform PC; ones already
    /// there only get more hours. Nothing goes into the friends' feed.
    /// </summary>
    /// <param name="userId">The account.</param>
    /// <param name="games">The games (each once; already in dbo.Game).</param>
    /// <returns>How many were added and how many got more hours.</returns>
    Task<LibraryImportCounts> ImportAsync(Guid userId, IReadOnlyCollection<LibraryImportRow> games);
}

/// <summary>A game for <see cref="ILibraryRepository.ImportAsync"/> (sent as JSON to dbo.LibraryImport).</summary>
/// <param name="GameID">The game.</param>
/// <param name="Status">The status for a new entry.</param>
/// <param name="Hours">The hours played.</param>
/// <param name="PlayedAt">When it was last played (UTC).</param>
internal sealed record LibraryImportRow(int GameID, byte Status, decimal? Hours, DateTime? PlayedAt);

/// <summary>What dbo.LibraryImport did.</summary>
public sealed class LibraryImportCounts
{
    /// <summary>Gets or sets how many games were added.</summary>
    public int Added { get; set; }

    /// <summary>Gets or sets how many games got more hours.</summary>
    public int Updated { get; set; }
}

/// <summary>
/// The libraries in SQL Server (stored procedures in Ludex.Database/StoredProcedures/Library and Activity).
/// </summary>
/// <param name="connectionFactory">Opens connections.</param>
internal sealed class LibraryRepository(ISqlConnectionFactory connectionFactory) : RepositoryBase(connectionFactory), ILibraryRepository
{
    /// <inheritdoc />
    public Task<List<LibraryRow>> ListAsync(Guid userId) => QueryAsync<LibraryRow>("[dbo].[LibraryList]", new { UserUniqueID = userId });

    /// <inheritdoc />
    public Task<LibraryRow?> GetAsync(Guid userId, int gameId) =>
        QuerySingleOrDefaultAsync<LibraryRow>("[dbo].[LibraryGet]", new { UserUniqueID = userId, GameID = gameId });

    /// <inheritdoc />
    public Task SaveAsync(Guid userId, int gameId, byte status, byte? rating, decimal? hours, string? platform, string? notes, string? store, byte? format) =>
        ExecuteAsync("[dbo].[LibrarySave]", new
        {
            UserUniqueID = userId,
            GameID = gameId,
            Status = status,
            Rating = rating,
            Hours = hours,
            Platform = platform,
            Notes = notes,
            Store = store,
            Format = format,
        });

    /// <inheritdoc />
    public Task DeleteAsync(Guid userId, int gameId) => ExecuteAsync("[dbo].[LibraryDelete]", new { UserUniqueID = userId, GameID = gameId });

    /// <inheritdoc />
    public Task AddActivityAsync(Guid userId, int gameId, byte kind, byte? rating) =>
        ExecuteAsync("[dbo].[ActivityAdd]", new { UserUniqueID = userId, GameID = gameId, Kind = kind, Rating = rating });

    /// <inheritdoc />
    public async Task<LibraryImportCounts> ImportAsync(Guid userId, IReadOnlyCollection<LibraryImportRow> games) =>
        games.Count == 0
            ? new LibraryImportCounts()
            : await QuerySingleOrDefaultAsync<LibraryImportCounts>("[dbo].[LibraryImport]", new { UserUniqueID = userId, Games = Json(games) }) ?? new LibraryImportCounts();
}
