using Ludex.WebApi.Extensions.Data;

namespace Ludex.WebApi.Features.Collections.Repositories;

/// <summary>A collection and one of its games (dbo.CollectionList; GameID null for an empty collection).</summary>
public sealed class CollectionRow
{
    /// <summary>Gets or sets the collection.</summary>
    public int CollectionID { get; set; }

    /// <summary>Gets or sets its name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets a game in it.</summary>
    public int? GameID { get; set; }
}

/// <summary>
/// An account's collections (dbo.Collection, dbo.CollectionGame).
/// </summary>
internal interface ICollectionRepository
{
    /// <summary>The collections with their games.</summary>
    /// <param name="userId">The account.</param>
    /// <returns>One row per collection and game.</returns>
    Task<List<CollectionRow>> ListAsync(Guid userId);

    /// <summary>Creates or renames a collection.</summary>
    /// <param name="userId">The account.</param>
    /// <param name="collectionId">The collection (null: a new one).</param>
    /// <param name="name">The name.</param>
    /// <returns>Its id; 0 when it isn't the account's; -1 when the name is taken.</returns>
    Task<int> SaveAsync(Guid userId, int? collectionId, string name);

    /// <summary>Deletes a collection.</summary>
    /// <param name="userId">The account.</param>
    /// <param name="collectionId">The collection.</param>
    /// <returns>A task.</returns>
    Task DeleteAsync(Guid userId, int collectionId);

    /// <summary>Puts a game in a collection or takes it out.</summary>
    /// <param name="userId">The account.</param>
    /// <param name="collectionId">The collection.</param>
    /// <param name="gameId">The game (of the library).</param>
    /// <param name="inside">True: in; false: out.</param>
    /// <returns>A task.</returns>
    Task SetGameAsync(Guid userId, int collectionId, int gameId, bool inside);
}

/// <summary>
/// Collections in SQL Server (stored procedures in Ludex.Database/StoredProcedures/Collections).
/// </summary>
/// <param name="connectionFactory">Opens connections.</param>
internal sealed class CollectionRepository(ISqlConnectionFactory connectionFactory) : RepositoryBase(connectionFactory), ICollectionRepository
{
    /// <inheritdoc />
    public Task<List<CollectionRow>> ListAsync(Guid userId) => QueryAsync<CollectionRow>("[dbo].[CollectionList]", new { UserUniqueID = userId });

    /// <inheritdoc />
    public Task<int> SaveAsync(Guid userId, int? collectionId, string name) =>
        ExecuteScalarAsync<int>("[dbo].[CollectionSave]", new { UserUniqueID = userId, CollectionID = collectionId, Name = name });

    /// <inheritdoc />
    public Task DeleteAsync(Guid userId, int collectionId) => ExecuteAsync("[dbo].[CollectionDelete]", new { UserUniqueID = userId, CollectionID = collectionId });

    /// <inheritdoc />
    public Task SetGameAsync(Guid userId, int collectionId, int gameId, bool inside) =>
        ExecuteAsync("[dbo].[CollectionSetGame]", new { UserUniqueID = userId, CollectionID = collectionId, GameID = gameId, In = inside });
}
