using Ludex.WebApi.Extensions.Data;
using Ludex.WebApi.Features.Library.Models.Entities;
using Ludex.WebApi.Features.Library.Services;

namespace Ludex.WebApi.Features.Library.Repositories;

/// <summary>
/// The libraries (dbo.LibraryEntry) and what their owners did (dbo.Activity).
/// </summary>
internal interface ILibraryRepository
{
    /// <summary>Every entry of a library (a game on two platforms is two), most recently changed first.</summary>
    /// <param name="userId">The account.</param>
    /// <returns>The entries.</returns>
    Task<List<LibraryRow>> ListAsync(Guid userId);

    /// <summary>One entry of a game in a library.</summary>
    /// <param name="userId">The account.</param>
    /// <param name="gameId">The game.</param>
    /// <param name="entryId">The entry (null, or one taken out: the game's first entry).</param>
    /// <returns>The entry, or null when the game isn't there.</returns>
    Task<LibraryRow?> GetAsync(Guid userId, int gameId, long? entryId = null);

    /// <summary>The entries of one game, or of every game, without the games' data.</summary>
    /// <param name="userId">The account.</param>
    /// <param name="gameId">The game (null: every game).</param>
    /// <returns>The entries, by game and then oldest first.</returns>
    Task<List<LibraryCopyRow>> CopiesAsync(Guid userId, int? gameId = null);

    /// <summary>Empties the library (entries, activity, hours history, collections' contents, what imports remembered).</summary>
    /// <param name="userId">The account.</param>
    /// <returns>How many entries went.</returns>
    Task<int> ClearAsync(Guid userId);

    /// <summary>Adds a game or changes one of its entries (the game must be in dbo.Game).</summary>
    /// <param name="userId">The account.</param>
    /// <param name="gameId">The game.</param>
    /// <param name="status">The status.</param>
    /// <param name="rating">The rating.</param>
    /// <param name="hours">The hours.</param>
    /// <param name="platform">The platform.</param>
    /// <param name="notes">The notes.</param>
    /// <param name="store">The PC store.</param>
    /// <param name="format">The kind of copy (1 physical, 2 digital).</param>
    /// <param name="entryId">The entry (null: the game's first one, or a new one).</param>
    /// <param name="newCopy">Whether to add another entry (the game on another platform).</param>
    /// <returns>The entry's id.</returns>
    Task<long> SaveAsync(Guid userId, int gameId, byte status, byte? rating, decimal? hours, string? platform, string? notes, string? store, byte? format, long? entryId = null, bool newCopy = false);

    /// <summary>Takes a game, or one of its entries, out of a library.</summary>
    /// <param name="userId">The account.</param>
    /// <param name="gameId">The game.</param>
    /// <param name="entryId">The entry (null: every entry of the game).</param>
    /// <returns>A task.</returns>
    Task DeleteAsync(Guid userId, int gameId, long? entryId = null);

    /// <summary>Records something the account did.</summary>
    /// <param name="userId">The account.</param>
    /// <param name="gameId">The game.</param>
    /// <param name="kind">What happened.</param>
    /// <param name="rating">The rating, for a Rated line.</param>
    /// <returns>A task.</returns>
    Task AddActivityAsync(Guid userId, int gameId, byte kind, byte? rating);

    /// <summary>
    /// Adds many games at once (the imports and syncs): each goes into the entry of the same platform family (or the
    /// one with no platform), otherwise into a new entry; entries already there only get more hours (and a copy,
    /// trophies). Nothing goes into the friends' feed.
    /// </summary>
    /// <param name="userId">The account.</param>
    /// <param name="games">The games (already in dbo.Game).</param>
    /// <returns>How many were added and how many got more hours.</returns>
    Task<LibraryImportCounts> ImportAsync(Guid userId, IReadOnlyCollection<LibraryImportRow> games);

    /// <summary>Sets the price under which a wishlisted game raises an alert.</summary>
    /// <param name="userId">The account.</param>
    /// <param name="gameId">The game.</param>
    /// <param name="target">The price (null: no alert).</param>
    /// <returns>A task.</returns>
    Task SetPriceTargetAsync(Guid userId, int gameId, decimal? target);

    /// <summary>The Steam games whose achievement counts need reading.</summary>
    /// <param name="userId">The account.</param>
    /// <returns>Game and Steam app.</returns>
    Task<List<AchievementTarget>> AchievementTargetsAsync(Guid userId);

    /// <summary>Keeps how many achievements of a game the account has.</summary>
    /// <param name="userId">The account.</param>
    /// <param name="gameId">The game.</param>
    /// <param name="unlocked">Unlocked.</param>
    /// <param name="total">The game's achievements.</param>
    /// <returns>A task.</returns>
    Task SaveAchievementsAsync(Guid userId, int gameId, int unlocked, int total);
}

/// <summary>A Steam game whose achievements to read (dbo.AchievementTargets).</summary>
public sealed class AchievementTarget
{
    /// <summary>Gets or sets the game.</summary>
    public int GameID { get; set; }

    /// <summary>Gets or sets its Steam app.</summary>
    public int SteamAppID { get; set; }
}

/// <summary>A game for <see cref="ILibraryRepository.ImportAsync"/> (sent as JSON to dbo.LibraryImport).</summary>
/// <param name="GameID">The game.</param>
/// <param name="Status">The status for a new entry.</param>
/// <param name="Hours">The hours played.</param>
/// <param name="PlayedAt">When it was last played (UTC).</param>
/// <param name="AppID">Its Steam app.</param>
/// <param name="RecentMinutes">Minutes played on Steam in the last two weeks.</param>
/// <param name="Platform">The platform of the copy (null: a Steam game, PC / Steam / Digital).</param>
/// <param name="Store">The store of the copy (with a platform; only filled where the entry has no copy yet).</param>
/// <param name="Format">Physical or digital (with a platform).</param>
/// <param name="AchUnlocked">Trophies or achievements earned (games that aren't Steam games).</param>
/// <param name="AchTotal">Trophies or achievements the game has.</param>
internal sealed record LibraryImportRow(
    int GameID,
    byte Status,
    decimal? Hours,
    DateTime? PlayedAt,
    int? AppID = null,
    int? RecentMinutes = null,
    string? Platform = null,
    string? Store = null,
    byte? Format = null,
    short? AchUnlocked = null,
    short? AchTotal = null)
{
    /// <summary>Gets the entry it goes into (null: a new one); set by the repository.</summary>
    public long? EntryID { get; init; }

    /// <summary>Gets the platform family, which tells new entries of one game apart.</summary>
    public string? Slot { get; init; }
}

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
    public Task<LibraryRow?> GetAsync(Guid userId, int gameId, long? entryId = null) =>
        QuerySingleOrDefaultAsync<LibraryRow>("[dbo].[LibraryGet]", new { UserUniqueID = userId, GameID = gameId, EntryID = entryId });

    /// <inheritdoc />
    public Task<List<LibraryCopyRow>> CopiesAsync(Guid userId, int? gameId = null) =>
        QueryAsync<LibraryCopyRow>("[dbo].[LibraryCopyList]", new { UserUniqueID = userId, GameID = gameId });

    /// <inheritdoc />
    public Task<int> ClearAsync(Guid userId) => ExecuteScalarAsync<int>("[dbo].[LibraryClear]", new { UserUniqueID = userId });

    /// <inheritdoc />
    public Task<long> SaveAsync(Guid userId, int gameId, byte status, byte? rating, decimal? hours, string? platform, string? notes, string? store, byte? format, long? entryId = null, bool newCopy = false) =>
        ExecuteScalarAsync<long>("[dbo].[LibrarySave]", new
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
            EntryID = entryId,
            NewCopy = newCopy,
        });

    /// <inheritdoc />
    public Task DeleteAsync(Guid userId, int gameId, long? entryId = null) =>
        ExecuteAsync("[dbo].[LibraryDelete]", new { UserUniqueID = userId, GameID = gameId, EntryID = entryId });

    /// <inheritdoc />
    public Task AddActivityAsync(Guid userId, int gameId, byte kind, byte? rating) =>
        ExecuteAsync("[dbo].[ActivityAdd]", new { UserUniqueID = userId, GameID = gameId, Kind = kind, Rating = rating });

    /// <inheritdoc />
    public async Task<LibraryImportCounts> ImportAsync(Guid userId, IReadOnlyCollection<LibraryImportRow> games)
    {
        if (games.Count == 0)
        {
            return new LibraryImportCounts();
        }

        // Each game goes into its copy on the same platform family (Steam → PC, PlayStation → PlayStation).
        var copies = (await CopiesAsync(userId)).ToLookup(static c => c.GameID);
        var rows = games.Select(row =>
        {
            var platform = row.Platform ?? "PC";
            return row with { EntryID = CopyRules.Pick(copies[row.GameID], platform, row.AppID)?.EntryID, Slot = CopyRules.Family(platform) };
        }).ToList();
        return await QuerySingleOrDefaultAsync<LibraryImportCounts>("[dbo].[LibraryImport]", new { UserUniqueID = userId, Games = Json(rows) }) ?? new LibraryImportCounts();
    }

    /// <inheritdoc />
    public Task SetPriceTargetAsync(Guid userId, int gameId, decimal? target) =>
        ExecuteAsync("[dbo].[PriceTargetSet]", new { UserUniqueID = userId, GameID = gameId, Target = target });

    /// <inheritdoc />
    public Task<List<AchievementTarget>> AchievementTargetsAsync(Guid userId) =>
        QueryAsync<AchievementTarget>("[dbo].[AchievementTargets]", new { UserUniqueID = userId });

    /// <inheritdoc />
    public Task SaveAchievementsAsync(Guid userId, int gameId, int unlocked, int total) =>
        ExecuteAsync("[dbo].[AchievementSave]", new
        {
            UserUniqueID = userId,
            GameID = gameId,
            Unlocked = (short)Math.Clamp(unlocked, 0, short.MaxValue),
            Total = (short)Math.Clamp(total, 0, short.MaxValue),
        });
}
