using Ludex.WebApi.Extensions.Data;
using Ludex.WebApi.Features.Community.Models.Entities;

namespace Ludex.WebApi.Features.Community.Repositories;

/// <summary>
/// What everyone on this Ludex does with games: trending, ratings, sleeper hits, wishlists.
/// </summary>
internal interface ICommunityRepository
{
    /// <summary>The games most accounts did something with lately.</summary>
    /// <param name="days">How many days back.</param>
    /// <param name="take">How many.</param>
    /// <returns>The games.</returns>
    Task<List<CommunityGameRow>> TrendingAsync(int days, int take);

    /// <summary>Well rated games few accounts have.</summary>
    /// <param name="minRating">Lowest average (1–10).</param>
    /// <param name="minRatings">Fewest ratings.</param>
    /// <param name="maxOwners">Most owners.</param>
    /// <param name="take">How many.</param>
    /// <returns>The games.</returns>
    Task<List<CommunityGameRow>> SleepersAsync(decimal minRating, int minRatings, int maxOwners, int take);

    /// <summary>How a game was rated, and how many accounts have it in each status.</summary>
    /// <param name="gameId">The game.</param>
    /// <returns>The counts.</returns>
    Task<List<GameRatingRow>> RatingsAsync(int gameId);

    /// <summary>How many accounts wish for each game.</summary>
    /// <param name="gameIds">The games.</param>
    /// <returns>The counts (games nobody wishes for are left out).</returns>
    Task<List<GameCountRow>> WishlistCountsAsync(IReadOnlyCollection<int> gameIds);

    /// <summary>How many accounts there are.</summary>
    /// <returns>The counts.</returns>
    Task<CommunityCountsRow> CountsAsync();
}

/// <summary>
/// The community numbers in SQL Server (stored procedures in Ludex.Database/StoredProcedures/Community).
/// </summary>
/// <param name="connectionFactory">Opens connections.</param>
internal sealed class CommunityRepository(ISqlConnectionFactory connectionFactory) : RepositoryBase(connectionFactory), ICommunityRepository
{
    /// <inheritdoc />
    public Task<List<CommunityGameRow>> TrendingAsync(int days, int take) => QueryAsync<CommunityGameRow>("[dbo].[GameTrending]", new { Days = days, Take = take });

    /// <inheritdoc />
    public Task<List<CommunityGameRow>> SleepersAsync(decimal minRating, int minRatings, int maxOwners, int take) =>
        QueryAsync<CommunityGameRow>("[dbo].[GameSleepers]", new { MinRating = minRating, MinRatings = minRatings, MaxOwners = maxOwners, Take = take });

    /// <inheritdoc />
    public Task<List<GameRatingRow>> RatingsAsync(int gameId) => QueryAsync<GameRatingRow>("[dbo].[GameRatings]", new { GameID = gameId });

    /// <inheritdoc />
    public Task<List<GameCountRow>> WishlistCountsAsync(IReadOnlyCollection<int> gameIds) =>
        gameIds.Count == 0 ? Task.FromResult(new List<GameCountRow>()) : QueryAsync<GameCountRow>("[dbo].[GameWishlistCounts]", new { GameIDs = Json(gameIds) });

    /// <inheritdoc />
    public async Task<CommunityCountsRow> CountsAsync() => await QuerySingleOrDefaultAsync<CommunityCountsRow>("[dbo].[CommunityCounts]") ?? new CommunityCountsRow();
}
