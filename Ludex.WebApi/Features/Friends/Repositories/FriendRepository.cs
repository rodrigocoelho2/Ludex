using Ludex.WebApi.Extensions.Data;
using Ludex.WebApi.Features.Friends.Models.Entities;

namespace Ludex.WebApi.Features.Friends.Repositories;

/// <summary>
/// Friendships (dbo.Friendship) and what friends have and do.
/// </summary>
internal interface IFriendRepository
{
    /// <summary>The friends and the requests waiting either way.</summary>
    /// <param name="userId">The account.</param>
    /// <returns>The links.</returns>
    Task<List<FriendRow>> ListAsync(Guid userId);

    /// <summary>Asks another account to be friends (accepts at once when it had asked first).</summary>
    /// <param name="userId">The account asking.</param>
    /// <param name="friendId">The other account.</param>
    /// <returns>The state afterwards: 1 friends, 2 request sent, 0 nothing.</returns>
    Task<byte> RequestAsync(Guid userId, Guid friendId);

    /// <summary>Accepts the other account's request.</summary>
    /// <param name="userId">The account answering.</param>
    /// <param name="friendId">The account that asked.</param>
    /// <returns>A task.</returns>
    Task AcceptAsync(Guid userId, Guid friendId);

    /// <summary>Ends a friendship or a request, both ways.</summary>
    /// <param name="userId">The account.</param>
    /// <param name="friendId">The other account.</param>
    /// <returns>A task.</returns>
    Task RemoveAsync(Guid userId, Guid friendId);

    /// <summary>The friends who have a game.</summary>
    /// <param name="userId">The account.</param>
    /// <param name="gameId">The game.</param>
    /// <returns>Their entries.</returns>
    Task<List<FriendGameRow>> GameAsync(Guid userId, int gameId);

    /// <summary>Which friends have which of these games.</summary>
    /// <param name="userId">The account.</param>
    /// <param name="gameIds">The games.</param>
    /// <returns>One row per friend and game.</returns>
    Task<List<OwnershipRow>> OwnershipAsync(Guid userId, IReadOnlyCollection<int> gameIds);

    /// <summary>The games most friends have that the account doesn't.</summary>
    /// <param name="userId">The account.</param>
    /// <param name="take">At most this many.</param>
    /// <returns>The games.</returns>
    Task<List<PopularRow>> PopularAsync(Guid userId, int take);

    /// <summary>What the friends did lately.</summary>
    /// <param name="userId">The account.</param>
    /// <param name="take">At most this many lines.</param>
    /// <returns>The lines, newest first.</returns>
    Task<List<ActivityRow>> FeedAsync(Guid userId, int take);
}

/// <summary>
/// Friendships in SQL Server (stored procedures in Ludex.Database/StoredProcedures/Friends and Activity).
/// </summary>
/// <param name="connectionFactory">Opens connections.</param>
internal sealed class FriendRepository(ISqlConnectionFactory connectionFactory) : RepositoryBase(connectionFactory), IFriendRepository
{
    /// <inheritdoc />
    public Task<List<FriendRow>> ListAsync(Guid userId) => QueryAsync<FriendRow>("[dbo].[FriendList]", new { UserUniqueID = userId });

    /// <inheritdoc />
    public Task<byte> RequestAsync(Guid userId, Guid friendId) =>
        ExecuteScalarAsync<byte>("[dbo].[FriendRequest]", new { UserUniqueID = userId, FriendUniqueID = friendId });

    /// <inheritdoc />
    public Task AcceptAsync(Guid userId, Guid friendId) => ExecuteAsync("[dbo].[FriendAccept]", new { UserUniqueID = userId, FriendUniqueID = friendId });

    /// <inheritdoc />
    public Task RemoveAsync(Guid userId, Guid friendId) => ExecuteAsync("[dbo].[FriendRemove]", new { UserUniqueID = userId, FriendUniqueID = friendId });

    /// <inheritdoc />
    public Task<List<FriendGameRow>> GameAsync(Guid userId, int gameId) =>
        QueryAsync<FriendGameRow>("[dbo].[FriendGameList]", new { UserUniqueID = userId, GameID = gameId });

    /// <inheritdoc />
    public async Task<List<OwnershipRow>> OwnershipAsync(Guid userId, IReadOnlyCollection<int> gameIds) =>
        gameIds.Count == 0 ? [] : await QueryAsync<OwnershipRow>("[dbo].[FriendOwnership]", new { UserUniqueID = userId, GameIDs = Json(gameIds.Distinct()) });

    /// <inheritdoc />
    public Task<List<PopularRow>> PopularAsync(Guid userId, int take) => QueryAsync<PopularRow>("[dbo].[FriendPopular]", new { UserUniqueID = userId, Take = take });

    /// <inheritdoc />
    public Task<List<ActivityRow>> FeedAsync(Guid userId, int take) => QueryAsync<ActivityRow>("[dbo].[ActivityFeed]", new { UserUniqueID = userId, Take = take });
}
