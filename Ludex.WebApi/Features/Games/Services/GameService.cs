using Ludex.Library.Enums;
using Ludex.Library.Services;
using Ludex.WebApi.Extensions.Globalization;
using Ludex.WebApi.Features.Catalog;
using Ludex.WebApi.Features.Collections.Repositories;
using Ludex.WebApi.Features.Collections.Services;
using Ludex.WebApi.Features.Friends.Repositories;
using Ludex.WebApi.Features.Games.Models.DTOs;
using Ludex.WebApi.Features.Games.Models.Entities;
using Ludex.WebApi.Features.Games.Repositories;
using Ludex.WebApi.Features.Library.Models.Entities;
using Ludex.WebApi.Features.Library.Repositories;
using Ludex.WebApi.Features.Library.Services;
using Ludex.WebApi.Features.Profile.Repositories;

namespace Ludex.WebApi.Features.Games.Services;

/// <summary>
/// Searching the catalog, a game's page and the covers behind the sign-in form.
/// </summary>
internal interface IGameService
{
    /// <summary>Searches the catalog, marking the games the account and its friends have.</summary>
    /// <param name="userId">The account.</param>
    /// <param name="query">The text typed.</param>
    /// <returns>The games, or a problem (too short, catalog unreachable).</returns>
    Task<ServiceApiResponse<SearchResultDTO>> SearchAsync(string userId, string? query);

    /// <summary>Everything the game page shows.</summary>
    /// <param name="userId">The account.</param>
    /// <param name="gameId">The game.</param>
    /// <returns>The game, or not found.</returns>
    /// <param name="entryId">The account's copy to show (null: the first one).</param>
    Task<ServiceApiResponse<GameDetailDTO>> DetailAsync(string userId, int gameId, long? entryId = null);

    /// <summary>Covers for the sign-in page.</summary>
    /// <returns>The covers.</returns>
    Task<ServiceApiResponse<CoverWallDTO>> WallAsync();
}

/// <summary>
/// Searching the catalog, a game's page and the covers behind the sign-in form.
/// </summary>
/// <param name="catalog">IGDB, or the built-in list.</param>
/// <param name="gameStore">Saved games, refreshed from the catalog.</param>
/// <param name="gameRepository">Saved games.</param>
/// <param name="libraryRepository">The libraries.</param>
/// <param name="friendRepository">Friends.</param>
/// <param name="globalizationHelper">Messages.</param>
/// <param name="timeProvider">The clock (how long the wall's games are kept).</param>
/// <param name="logger">Logger.</param>
internal sealed class GameService(
    IGameCatalog catalog,
    GameStore gameStore,
    IGameRepository gameRepository,
    ILibraryRepository libraryRepository,
    IFriendRepository friendRepository,
    IProfileRepository profileRepository,
    ICollectionRepository collectionRepository,
    IGlobalizationHelper globalizationHelper,
    TimeProvider timeProvider,
    ILogger<GameService> logger) : IGameService
{
    /// <summary>Most results a search returns.</summary>
    internal const int SearchLimit = 24;

    /// <summary>How many covers the sign-in wall shows.</summary>
    internal const int WallSize = 42;

    private static readonly TimeSpan WallRefresh = TimeSpan.FromHours(12);
    private static readonly TimeSpan WallRetry = TimeSpan.FromMinutes(5);
    private readonly SemaphoreSlim _wallGate = new(1, 1);
    private List<CatalogGame> _famous = [];
    private DateTimeOffset _famousAt = DateTimeOffset.MinValue;

    /// <inheritdoc />
    public async Task<ServiceApiResponse<SearchResultDTO>> SearchAsync(string userId, string? query)
    {
        if (!Guid.TryParse(userId, out var user))
        {
            return ServiceApiResponse<SearchResultDTO>.NotFound(await globalizationHelper.TranslateAsync(nameof(TranslationKeys.NoRecordsFound), userId ?? string.Empty));
        }

        var text = (query ?? string.Empty).Trim();
        var result = new SearchResultDTO { Query = text, Source = catalog.Source };
        if (text.Length < 2)
        {
            return ServiceApiResponse<SearchResultDTO>.Success(result);
        }

        List<CatalogGame> games;
        try
        {
            games = await catalog.SearchAsync(text, SearchLimit);
        }
        catch (CatalogUnavailableException exception)
        {
            var message = await globalizationHelper.TranslateAsync(nameof(TranslationKeys.CatalogUnreachable), exception.Message);
            return ServiceApiResponse<SearchResultDTO>.ValidationProblems(message).AddError("Query", message);
        }

        var mine = (await libraryRepository.CopiesAsync(user)).GroupBy(static row => row.GameID).ToDictionary(static g => g.Key, static g => GameMapper.Status(g.First().Status));
        var friends = await FriendNamesAsync(user, games.Select(static g => g.Id).ToList());
        result.Results = [.. games.Select(game => GameMapper.Summary(
            game,
            mine.TryGetValue(game.Id, out var status) ? status : null,
            friends.TryGetValue(game.Id, out var names) ? names : null))];
        return ServiceApiResponse<SearchResultDTO>.Success(result);
    }

    /// <inheritdoc />
    public async Task<ServiceApiResponse<GameDetailDTO>> DetailAsync(string userId, int gameId, long? entryId = null)
    {
        if (!Guid.TryParse(userId, out var user))
        {
            return ServiceApiResponse<GameDetailDTO>.NotFound(await globalizationHelper.TranslateAsync(nameof(TranslationKeys.NoRecordsFound), userId ?? string.Empty));
        }

        GameRow? game;
        try
        {
            game = await gameStore.GetAsync(gameId);
        }
        catch (CatalogUnavailableException exception)
        {
            var message = await globalizationHelper.TranslateAsync(nameof(TranslationKeys.CatalogUnreachable), exception.Message);
            return ServiceApiResponse<GameDetailDTO>.ValidationProblems(message).AddError("GameId", message);
        }

        if (game is null)
        {
            return ServiceApiResponse<GameDetailDTO>.NotFound(await globalizationHelper.TranslateAsync(nameof(TranslationKeys.GameNotFound)));
        }

        var mine = await libraryRepository.GetAsync(user, gameId, entryId);
        var copies = mine is null ? new List<LibraryCopyRow>() : await libraryRepository.CopiesAsync(user, gameId);
        var friends = await friendRepository.GameAsync(user, gameId);
        var favorite = mine is null ? null : (await profileRepository.GetAsync(user))?.FavoriteGameID;
        var collections = CollectionService.ToDtos(await collectionRepository.ListAsync(user));
        var rated = friends.Where(static f => f.Rating is not null).Select(static f => (decimal)f.Rating!.Value).ToList();

        return ServiceApiResponse<GameDetailDTO>.Success(new GameDetailDTO
        {
            Game = GameMapper.Summary(game, mine is null ? null : GameMapper.Status(mine.Status), [.. friends.Select(static f => f.Username)]),
            Summary = game.Summary,
            HeroImageId = game.HeroImageID,
            Screenshots = GameRow.Split(game.Screenshots, ','),
            ReleaseDate = game.FirstReleaseDate is { } date ? DateOnly.FromDateTime(date) : null,
            Publisher = game.Publisher,
            Genres = GameRow.Split(game.Genres),
            Modes = GameRow.Split(game.Modes),
            Mine = mine is null ? null : new MyGameDTO
            {
                EntryId = mine.EntryID,
                Copies = [.. copies.Select(static c => new MyCopyDTO
                {
                    EntryId = c.EntryID,
                    Platform = c.Platform,
                    Store = c.Store,
                    Status = GameMapper.Status(c.Status) ?? GameStatus.Playing,
                    Hours = c.Hours,
                    AchievementsUnlocked = c.AchievementsUnlocked,
                    AchievementsTotal = c.AchievementsTotal,
                })],
                Status = GameMapper.Status(mine.Status) ?? GameStatus.Playing,
                Rating = mine.Rating,
                Hours = mine.Hours,
                Platform = mine.Platform,
                Store = mine.Store,
                Format = mine.Format is 1 or 2 ? (GameFormat)mine.Format.Value : null,
                Notes = mine.Notes,
                AddedAt = GameMapper.Utc(mine.AddedAt),
                UpdatedAt = GameMapper.Utc(mine.UpdatedAt),
                FinishedAt = mine.FinishedAt is { } finished ? GameMapper.Utc(finished) : null,
                IsFavorite = favorite == gameId,
                SteamAppId = mine.SteamAppID,
                RecentHours = mine.RecentMinutes is > 0 ? Math.Round(mine.RecentMinutes.Value / 60m, 1, MidpointRounding.AwayFromZero) : null,
                AchievementsUnlocked = mine.AchievementsUnlocked,
                AchievementsTotal = mine.AchievementsTotal,
                CollectionIds = [.. collections.Where(c => c.GameIds.Contains(gameId)).Select(static c => c.Id)],
            },
            Collections = collections,
            Price = mine is null ? null : LibraryRules.Price(mine),
            Friends = [.. friends.Select(static f => new FriendGameDTO
            {
                Id = f.UserUniqueID.ToString("N"),
                UserName = f.Username,
                Status = GameMapper.Status(f.Status) ?? GameStatus.Playing,
                Rating = f.Rating,
                Hours = f.Hours,
                Platform = f.Platform,
                UpdatedAt = GameMapper.Utc(f.UpdatedAt),
                AvatarVersion = f.AvatarVersion,
            })],
            FriendsAverageRating = rated.Count == 0 ? null : Math.Round(rated.Average(), 1),
            Source = catalog.Source,
        });
    }

    /// <inheritdoc />
    public async Task<ServiceApiResponse<CoverWallDTO>> WallAsync()
    {
        // The games people here have, then (with IGDB) well-known ones to fill the wall; a new order on every visit.
        var games = (await gameRepository.CoverWallAsync(WallSize)).Select(static row => GameMapper.Summary(row)).ToList();
        if (games.Count < WallSize && catalog.Source == CatalogSource.Igdb)
        {
            var seen = games.Select(static g => g.Id).ToHashSet();
            games.AddRange((await FamousAsync()).Where(g => seen.Add(g.Id) && g.CoverImageId is not null).Select(static g => GameMapper.Summary(g)));
        }

        var wall = games.Take(WallSize).ToArray();
        Random.Shared.Shuffle(wall);
        return ServiceApiResponse<CoverWallDTO>.Success(new CoverWallDTO { Games = [.. wall], Source = catalog.Source });
    }

    private async Task<List<CatalogGame>> FamousAsync()
    {
        await _wallGate.WaitAsync();
        try
        {
            var now = timeProvider.GetUtcNow();
            if (now - _famousAt > (_famous.Count > 0 ? WallRefresh : WallRetry))
            {
                // A failure is tried again after a few minutes, not on every sign-in page and not only in 12 hours.
                _famousAt = now;
                try
                {
                    _famous = await catalog.PopularAsync(WallSize);
                    logger.LogInformation("Sign-in wall: {Count} well-known games from the catalog", _famous.Count);
                }
                catch (Exception exception) when (exception is CatalogUnavailableException or System.Text.Json.JsonException)
                {
                    logger.LogWarning("No well-known games for the sign-in wall: {Problem}", exception.Message);
                }
            }

            return _famous;
        }
        finally
        {
            _wallGate.Release();
        }
    }

    private async Task<Dictionary<int, List<string>>> FriendNamesAsync(Guid user, List<int> gameIds) =>
        GameMapper.NamesByGame((await friendRepository.OwnershipAsync(user, gameIds)).Select(static r => (r.GameID, r.Username)));
}
