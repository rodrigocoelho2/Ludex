using Ludex.WebApi.Extensions.Data;
using Ludex.WebApi.Features.Lists.Models.Entities;

namespace Ludex.WebApi.Features.Lists.Repositories;

/// <summary>
/// Lists of games (dbo.GameList, dbo.GameListItem, dbo.GameListLike).
/// </summary>
internal interface IListRepository
{
    /// <summary>Creates a list or changes one of the account's.</summary>
    /// <param name="userId">The account.</param>
    /// <param name="listId">The list (null: a new one).</param>
    /// <param name="name">The name.</param>
    /// <param name="description">The description.</param>
    /// <param name="ranked">Whether it is ranked.</param>
    /// <param name="visibility">Who can see it.</param>
    /// <param name="at">When (null: now).</param>
    /// <returns>Its id (0: not the account's).</returns>
    Task<int> SaveAsync(Guid userId, int? listId, string name, string? description, bool ranked, byte visibility, DateTime? at = null);

    /// <summary>Deletes one of the account's lists.</summary>
    /// <param name="userId">The account.</param>
    /// <param name="listId">The list.</param>
    /// <returns>A task.</returns>
    Task DeleteAsync(Guid userId, int listId);

    /// <summary>Puts a game in one of the account's lists, or takes it out.</summary>
    /// <param name="userId">The account.</param>
    /// <param name="listId">The list.</param>
    /// <param name="gameId">The game (in dbo.Game).</param>
    /// <param name="inside">True: in.</param>
    /// <param name="note">A note.</param>
    /// <returns>False when the list isn't the account's.</returns>
    Task<bool> SetItemAsync(Guid userId, int listId, int gameId, bool inside, string? note);

    /// <summary>Moves a game of one of the account's lists.</summary>
    /// <param name="userId">The account.</param>
    /// <param name="listId">The list.</param>
    /// <param name="gameId">The game.</param>
    /// <param name="position">Its new place (1 = first).</param>
    /// <returns>A task.</returns>
    Task MoveAsync(Guid userId, int listId, int gameId, int position);

    /// <summary>One list the reader may see.</summary>
    /// <param name="viewerId">The reader.</param>
    /// <param name="listId">The list.</param>
    /// <returns>The list, or null.</returns>
    Task<ListRow?> GetAsync(Guid viewerId, int listId);

    /// <summary>The games of a list the reader may see.</summary>
    /// <param name="viewerId">The reader.</param>
    /// <param name="listId">The list.</param>
    /// <returns>The games, in order.</returns>
    Task<List<ListItemRow>> ItemsAsync(Guid viewerId, int listId);

    /// <summary>Lists the reader may see.</summary>
    /// <param name="viewerId">The reader.</param>
    /// <param name="ownerId">Only this account's.</param>
    /// <param name="gameId">Only lists holding this game.</param>
    /// <param name="sort">1 popular, 2 recently changed.</param>
    /// <param name="days">How many days "popular" looks back.</param>
    /// <param name="take">How many.</param>
    /// <returns>The lists.</returns>
    Task<List<ListRow>> BrowseAsync(Guid viewerId, Guid? ownerId, int? gameId, byte sort, int days, int take);

    /// <summary>Likes a list or takes the like back.</summary>
    /// <param name="userId">The account.</param>
    /// <param name="listId">The list.</param>
    /// <param name="like">True: like.</param>
    /// <returns>The likes now.</returns>
    Task<int> LikeAsync(Guid userId, int listId, bool like);

    /// <summary>The account's lists and whether a game is in each.</summary>
    /// <param name="userId">The account.</param>
    /// <param name="gameId">The game.</param>
    /// <returns>The lists.</returns>
    Task<List<MyListRow>> ForGameAsync(Guid userId, int gameId);

    /// <summary>The first games of each list, for the covers.</summary>
    /// <param name="listIds">The lists.</param>
    /// <param name="take">How many per list.</param>
    /// <returns>The games, by list and place.</returns>
    Task<List<ListCoverRow>> CoversAsync(IReadOnlyCollection<int> listIds, int take);
}

/// <summary>
/// Lists in SQL Server (stored procedures in Ludex.Database/StoredProcedures/Lists).
/// </summary>
/// <param name="connectionFactory">Opens connections.</param>
internal sealed class ListRepository(ISqlConnectionFactory connectionFactory) : RepositoryBase(connectionFactory), IListRepository
{
    /// <inheritdoc />
    public Task<int> SaveAsync(Guid userId, int? listId, string name, string? description, bool ranked, byte visibility, DateTime? at = null) =>
        ExecuteScalarAsync<int>("[dbo].[ListSave]", new
        {
            UserUniqueID = userId,
            ListID = listId,
            Name = name,
            Description = description,
            Ranked = ranked,
            Visibility = visibility,
            At = at,
        });

    /// <inheritdoc />
    public Task DeleteAsync(Guid userId, int listId) => ExecuteAsync("[dbo].[ListDelete]", new { UserUniqueID = userId, ListID = listId });

    /// <inheritdoc />
    public Task<bool> SetItemAsync(Guid userId, int listId, int gameId, bool inside, string? note) =>
        ExecuteScalarAsync<bool>("[dbo].[ListItemSet]", new { UserUniqueID = userId, ListID = listId, GameID = gameId, In = inside, Note = note });

    /// <inheritdoc />
    public Task MoveAsync(Guid userId, int listId, int gameId, int position) =>
        ExecuteAsync("[dbo].[ListItemMove]", new { UserUniqueID = userId, ListID = listId, GameID = gameId, Position = position });

    /// <inheritdoc />
    public Task<ListRow?> GetAsync(Guid viewerId, int listId) =>
        QuerySingleOrDefaultAsync<ListRow>("[dbo].[ListGet]", new { ViewerUniqueID = viewerId, ListID = listId });

    /// <inheritdoc />
    public Task<List<ListItemRow>> ItemsAsync(Guid viewerId, int listId) =>
        QueryAsync<ListItemRow>("[dbo].[ListItems]", new { ViewerUniqueID = viewerId, ListID = listId });

    /// <inheritdoc />
    public Task<List<ListRow>> BrowseAsync(Guid viewerId, Guid? ownerId, int? gameId, byte sort, int days, int take) =>
        QueryAsync<ListRow>("[dbo].[ListBrowse]", new
        {
            ViewerUniqueID = viewerId,
            OwnerUniqueID = ownerId,
            GameID = gameId,
            Sort = sort,
            Days = days,
            Take = take,
        });

    /// <inheritdoc />
    public Task<int> LikeAsync(Guid userId, int listId, bool like) =>
        ExecuteScalarAsync<int>("[dbo].[ListLikeSet]", new { UserUniqueID = userId, ListID = listId, Like = like });

    /// <inheritdoc />
    public Task<List<MyListRow>> ForGameAsync(Guid userId, int gameId) =>
        QueryAsync<MyListRow>("[dbo].[ListsForGame]", new { UserUniqueID = userId, GameID = gameId });

    /// <inheritdoc />
    public Task<List<ListCoverRow>> CoversAsync(IReadOnlyCollection<int> listIds, int take) =>
        listIds.Count == 0 ? Task.FromResult(new List<ListCoverRow>()) : QueryAsync<ListCoverRow>("[dbo].[ListCovers]", new { ListIDs = Json(listIds), Take = take });
}
