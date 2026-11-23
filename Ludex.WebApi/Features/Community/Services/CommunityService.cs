using Ludex.Library.Enums;
using Ludex.Library.Services;
using Ludex.WebApi.Extensions.Globalization;
using Ludex.WebApi.Features.Catalog;
using Ludex.WebApi.Features.Community.Models.DTOs;
using Ludex.WebApi.Features.Community.Repositories;
using Ludex.WebApi.Features.Games.Services;
using Ludex.WebApi.Features.Journal.Services;
using Ludex.WebApi.Features.Lists.Repositories;
using Ludex.WebApi.Features.Lists.Services;
using Ludex.WebApi.Features.Reviews.Models.DTOs;
using Ludex.WebApi.Features.Reviews.Repositories;
using Ludex.WebApi.Features.Reviews.Services;

namespace Ludex.WebApi.Features.Community.Services;

/// <summary>
/// What everyone on this Ludex plays and thinks: home's community shelves, and a game's ratings and reviews.
/// </summary>
internal interface ICommunityService
{
    /// <summary>Home's community part: trending, popular reviews, lists, coming soon, sleeper hits.</summary>
    /// <param name="userId">The reader.</param>
    /// <returns>The shelves.</returns>
    Task<ServiceApiResponse<CommunityHomeDTO>> HomeAsync(string userId);

    /// <summary>A game's ratings, statuses, reviews and lists on this Ludex, with the reader's own review and sessions.</summary>
    /// <param name="userId">The reader.</param>
    /// <param name="gameId">The game.</param>
    /// <param name="sort">How the reviews are ordered.</param>
    /// <returns>The game's community block.</returns>
    Task<ServiceApiResponse<GameCommunityDTO>> GameAsync(string userId, int gameId, ReviewSort sort);
}

/// <summary>
/// The community shelves and a game's community block.
/// </summary>
/// <param name="communityRepository">The numbers.</param>
/// <param name="reviewRepository">Reviews.</param>
/// <param name="listService">Lists.</param>
/// <param name="listRepository">The reader's lists.</param>
/// <param name="journalService">The reader's sessions.</param>
/// <param name="catalog">Games coming out.</param>
/// <param name="globalizationHelper">Messages.</param>
/// <param name="timeProvider">The clock.</param>
/// <param name="logger">Logger.</param>
internal sealed class CommunityService(
    ICommunityRepository communityRepository,
    IReviewRepository reviewRepository,
    IListService listService,
    IListRepository listRepository,
    IJournalService journalService,
    IGameCatalog catalog,
    IGlobalizationHelper globalizationHelper,
    TimeProvider timeProvider,
    ILogger<CommunityService> logger) : ICommunityService
{
    /// <summary>How many games a shelf has.</summary>
    internal const int ShelfSize = 12;

    /// <summary>The fewest trending games for the week to be enough (else the last 30 days).</summary>
    internal const int TrendingEnough = 4;

    private static readonly TimeSpan UpcomingRefresh = TimeSpan.FromHours(6);
    private static readonly TimeSpan UpcomingRetry = TimeSpan.FromMinutes(10);
    private readonly SemaphoreSlim _upcomingGate = new(1, 1);
    private List<CatalogGame> _upcoming = [];
    private DateTimeOffset _upcomingAt = DateTimeOffset.MinValue;

    /// <inheritdoc />
    public async Task<ServiceApiResponse<CommunityHomeDTO>> HomeAsync(string userId)
    {
        if (!Guid.TryParse(userId, out var user))
        {
            return ServiceApiResponse<CommunityHomeDTO>.NotFound(await globalizationHelper.TranslateAsync(nameof(TranslationKeys.NoRecordsFound), userId ?? string.Empty));
        }

        var home = new CommunityHomeDTO();
        var trending = await communityRepository.TrendingAsync(7, ShelfSize);
        if (trending.Count < TrendingEnough)
        {
            trending = await communityRepository.TrendingAsync(30, ShelfSize);
            home.TrendingDays = 30;
        }

        home.Trending = [.. trending.Select(CommunityRules.Game)];
        home.PopularReviews = await ReviewsAsync(user, ReviewSort.Popular, 6);
        home.FriendReviews = await ReviewsAsync(user, ReviewSort.Friends, 6);
        home.AllTimeReviews = await ReviewsAsync(user, ReviewSort.AllTime, 6);
        home.PopularLists = await listService.PopularAsync(user, null, 4);

        var counts = await communityRepository.CountsAsync();
        home.Sleepers = [.. (await communityRepository.SleepersAsync(8m, 2, CommunityRules.SleeperMaxOwners(counts.All ?? 0), 6)).Select(CommunityRules.Game)];
        home.ComingSoon = await ComingSoonAsync(6);
        return ServiceApiResponse<CommunityHomeDTO>.Success(home);
    }

    /// <inheritdoc />
    public async Task<ServiceApiResponse<GameCommunityDTO>> GameAsync(string userId, int gameId, ReviewSort sort)
    {
        if (!Guid.TryParse(userId, out var user))
        {
            return ServiceApiResponse<GameCommunityDTO>.NotFound(await globalizationHelper.TranslateAsync(nameof(TranslationKeys.NoRecordsFound), userId ?? string.Empty));
        }

        var block = new GameCommunityDTO();
        CommunityRules.Fill(await communityRepository.RatingsAsync(gameId), block);

        var order = Enum.IsDefined(sort) ? sort : ReviewSort.Popular;
        var mine = await reviewRepository.ListAsync(user, gameId, user, (byte)ReviewSort.Recent, ReviewService.PopularDays, 0, 1);
        block.MyReview = mine.Count == 0 ? null : ReviewRules.Review(mine[0]);
        var rows = await reviewRepository.ListAsync(user, gameId, null, (byte)order, ReviewService.PopularDays, 0, 9);
        var others = rows.Where(static r => !r.IsMine).ToList();
        block.Reviews = new ReviewListDTO { Reviews = [.. others.Take(8).Select(ReviewRules.Review)], More = others.Count > 8, Sort = order };
        block.Lists = await listService.PopularAsync(user, gameId, 4);
        block.MyLists = ListService.Mine(await listRepository.ForGameAsync(user, gameId));
        var sessions = await journalService.GameAsync(user, gameId);
        block.MySessions = [.. sessions.Take(5)];
        block.MyMinutes = sessions.Sum(static s => s.Minutes);
        return ServiceApiResponse<GameCommunityDTO>.Success(block);
    }

    private async Task<List<ReviewDTO>> ReviewsAsync(Guid user, ReviewSort sort, int take) =>
        [.. (await reviewRepository.ListAsync(user, null, null, (byte)sort, ReviewService.PopularDays, 0, take)).Select(ReviewRules.Review)];

    private async Task<List<CommunityGameDTO>> ComingSoonAsync(int take)
    {
        var games = await UpcomingAsync();
        if (games.Count == 0)
        {
            return [];
        }

        var waiting = (await communityRepository.WishlistCountsAsync([.. games.Select(static g => g.Id)])).ToDictionary(static c => c.GameID, static c => c.Count);
        return [.. games
            .Select((game, i) => (Game: game, Order: i, Waiting: waiting.GetValueOrDefault(game.Id)))
            .OrderByDescending(static g => g.Waiting)
            .ThenBy(static g => g.Order)
            .Take(take)
            .Select(static g => new CommunityGameDTO
            {
                Game = GameMapper.Summary(g.Game),
                Waiting = g.Waiting,
                ReleaseDate = g.Game.ReleaseDate,
            })];
    }

    private async Task<List<CatalogGame>> UpcomingAsync()
    {
        await _upcomingGate.WaitAsync();
        try
        {
            var now = timeProvider.GetUtcNow();
            if (now - _upcomingAt > (_upcoming.Count > 0 ? UpcomingRefresh : UpcomingRetry))
            {
                _upcomingAt = now;
                try
                {
                    _upcoming = await catalog.UpcomingAsync(24);
                }
                catch (Exception exception) when (exception is CatalogUnavailableException or System.Text.Json.JsonException)
                {
                    logger.LogWarning("No games coming soon from the catalog: {Problem}", exception.Message);
                }
            }

            var today = DateOnly.FromDateTime(now.UtcDateTime);
            return [.. _upcoming.Where(g => g.ReleaseDate is null || g.ReleaseDate > today)];
        }
        finally
        {
            _upcomingGate.Release();
        }
    }
}
