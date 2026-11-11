using Ludex.Library.Enums;
using Ludex.Library.Services;
using Ludex.Library.Validation;
using Ludex.WebApi.Extensions.Globalization;
using Ludex.WebApi.Features.Catalog;
using Ludex.WebApi.Features.Friends.Repositories;
using Ludex.WebApi.Features.Friends.Services;
using Ludex.WebApi.Features.Games.Models.Entities;
using Ludex.WebApi.Features.Games.Services;
using Ludex.WebApi.Features.Library.Models.DTOs;
using Ludex.WebApi.Features.Library.Models.Entities;
using Ludex.WebApi.Features.Library.Repositories;

namespace Ludex.WebApi.Features.Library.Services;

/// <summary>
/// An account's library and its home page.
/// </summary>
internal interface ILibraryService
{
    /// <summary>The library.</summary>
    /// <param name="userId">The account.</param>
    /// <returns>Every game and the counts.</returns>
    Task<ServiceApiResponse<LibraryDTO>> ListAsync(string userId);

    /// <summary>Adds a game or changes it (status, rating, hours, platform, notes).</summary>
    /// <param name="item">The game and its values.</param>
    /// <returns>The entry, or validation problems.</returns>
    Task<ServiceApiResponse<LibraryEntryDTO>> SaveAsync(LibrarySaveRequest item);

    /// <summary>Sets only the status, adding the game when it isn't there.</summary>
    /// <param name="item">The game and its status.</param>
    /// <returns>The entry, or validation problems.</returns>
    Task<ServiceApiResponse<LibraryEntryDTO>> SetStatusAsync(LibraryStatusRequest item);

    /// <summary>Takes a game out of the library.</summary>
    /// <param name="userId">The account.</param>
    /// <param name="gameId">The game.</param>
    /// <returns>The game id.</returns>
    Task<ServiceApiResponse<int>> DeleteAsync(string userId, int gameId);

    /// <summary>The home page: the game being played, the numbers, the last games added and the friends' activity.</summary>
    /// <param name="userId">The account.</param>
    /// <returns>The home page.</returns>
    Task<ServiceApiResponse<HomeDTO>> HomeAsync(string userId);
}

/// <summary>
/// An account's library and its home page.
/// </summary>
/// <param name="libraryRepository">The libraries.</param>
/// <param name="friendRepository">Friends.</param>
/// <param name="gameStore">Games, from the database or the catalog.</param>
/// <param name="catalog">The catalog (which one is in use).</param>
/// <param name="globalizationHelper">Messages.</param>
/// <param name="timeProvider">The clock (this year's numbers).</param>
internal sealed class LibraryService(
    ILibraryRepository libraryRepository,
    IFriendRepository friendRepository,
    GameStore gameStore,
    IGameCatalog catalog,
    IGlobalizationHelper globalizationHelper,
    TimeProvider timeProvider) : ILibraryService
{
    /// <summary>Most hours a game can have.</summary>
    internal const decimal MaxHours = 99_999m;

    /// <summary>Longest platform name.</summary>
    internal const int MaxPlatformLength = 60;

    /// <summary>Longest notes.</summary>
    internal const int MaxNotesLength = 2000;

    /// <inheritdoc />
    public async Task<ServiceApiResponse<LibraryDTO>> ListAsync(string userId)
    {
        if (!Guid.TryParse(userId, out var user))
        {
            return await NotFoundAsync<LibraryDTO>(userId);
        }

        var rows = await libraryRepository.ListAsync(user);
        var names = await FriendNamesAsync(user, rows.Select(static r => r.GameID).ToList());
        return ServiceApiResponse<LibraryDTO>.Success(new LibraryDTO
        {
            Entries = [.. rows.Select(row => LibraryRules.Entry(row, names.GetValueOrDefault(row.GameID)))],
            Counts = LibraryRules.Counts(rows),
        });
    }

    /// <inheritdoc />
    public async Task<ServiceApiResponse<LibraryEntryDTO>> SaveAsync(LibrarySaveRequest item)
    {
        if (!Guid.TryParse(item.UserId, out var user))
        {
            return await NotFoundAsync<LibraryEntryDTO>(item.UserId);
        }

        var platform = Clean(item.Platform);
        var notes = Clean(item.Notes);
        var errors = new List<ApiValidationError>();
        if (!Enum.IsDefined(item.Status))
        {
            errors.Add(await globalizationHelper.ValidationErrorAsync("Status", nameof(TranslationKeys.RequiredField)));
        }

        if (item.Rating is < 1 or > 10)
        {
            errors.Add(await globalizationHelper.ValidationErrorAsync("Rating", nameof(TranslationKeys.OutOfRange), 1, 10));
        }

        if (item.Hours is < 0 or > MaxHours)
        {
            errors.Add(await globalizationHelper.ValidationErrorAsync("Hours", nameof(TranslationKeys.OutOfRange), 0, MaxHours));
        }

        if (platform is { Length: > MaxPlatformLength })
        {
            errors.Add(await globalizationHelper.ValidationErrorAsync("Platform", nameof(TranslationKeys.TooLong), MaxPlatformLength));
        }

        if (notes is { Length: > MaxNotesLength })
        {
            errors.Add(await globalizationHelper.ValidationErrorAsync("Notes", nameof(TranslationKeys.TooLong), MaxNotesLength));
        }

        if (errors.Count > 0)
        {
            return ServiceApiResponse<LibraryEntryDTO>.ValidationProblems(await globalizationHelper.TranslateAsync(nameof(TranslationKeys.ValidationProblems)))
                .AddErrorRange(errors);
        }

        var hours = item.Hours is { } value ? Math.Round(value, 1, MidpointRounding.AwayFromZero) : (decimal?)null;
        return await StoreAsync(user, item.GameId, item.Status, item.Rating, hours, platform, notes, keepDetails: false);
    }

    /// <inheritdoc />
    public async Task<ServiceApiResponse<LibraryEntryDTO>> SetStatusAsync(LibraryStatusRequest item)
    {
        if (!Guid.TryParse(item.UserId, out var user))
        {
            return await NotFoundAsync<LibraryEntryDTO>(item.UserId);
        }

        if (!Enum.IsDefined(item.Status))
        {
            return ServiceApiResponse<LibraryEntryDTO>.ValidationProblems(await globalizationHelper.TranslateAsync(nameof(TranslationKeys.ValidationProblems)))
                .AddError("Status", (await globalizationHelper.ValidationErrorAsync("Status", nameof(TranslationKeys.RequiredField))).Message);
        }

        return await StoreAsync(user, item.GameId, item.Status, null, null, null, null, keepDetails: true);
    }

    /// <inheritdoc />
    public async Task<ServiceApiResponse<int>> DeleteAsync(string userId, int gameId)
    {
        if (!Guid.TryParse(userId, out var user))
        {
            return await NotFoundAsync<int>(userId);
        }

        await libraryRepository.DeleteAsync(user, gameId);
        return ServiceApiResponse<int>.Success(gameId);
    }

    /// <inheritdoc />
    public async Task<ServiceApiResponse<HomeDTO>> HomeAsync(string userId)
    {
        if (!Guid.TryParse(userId, out var user))
        {
            return await NotFoundAsync<HomeDTO>(userId);
        }

        var rows = await libraryRepository.ListAsync(user);
        var playing = rows.Where(static r => r.Status == (byte)GameStatus.Playing).ToList();
        var hero = playing.FirstOrDefault() ?? rows.FirstOrDefault(static r => r.Status != (byte)GameStatus.Wishlist) ?? rows.FirstOrDefault();
        var alsoPlaying = playing.Where(r => r != hero).Take(3).ToList();
        var recent = rows.OrderByDescending(static r => r.AddedAt).Take(6).ToList();

        var shown = new List<int>();
        if (hero is not null)
        {
            shown.Add(hero.GameID);
        }

        shown.AddRange(alsoPlaying.Select(static r => r.GameID));
        shown.AddRange(recent.Select(static r => r.GameID));
        var names = await FriendNamesAsync(user, shown);

        var friends = await friendRepository.ListAsync(user);
        var popular = await FriendService.PopularAsync(friendRepository, user, 4);

        return ServiceApiResponse<HomeDTO>.Success(new HomeDTO
        {
            Hero = hero is null ? null : LibraryRules.Entry(hero, names.GetValueOrDefault(hero.GameID)),
            HeroImageId = hero?.HeroImageID,
            AlsoPlaying = [.. alsoPlaying.Select(row => LibraryRules.Entry(row, names.GetValueOrDefault(row.GameID)))],
            Stats = LibraryRules.Stats(rows, timeProvider.GetUtcNow().Year),
            Recent = [.. recent.Select(row => LibraryRules.Entry(row, names.GetValueOrDefault(row.GameID)))],
            Feed = [.. (await friendRepository.FeedAsync(user, 8)).Select(FriendService.Activity)],
            Popular = popular,
            FriendCount = friends.Count(static f => f.State == (byte)FriendState.Friends),
            Source = catalog.Source,
        });
    }

    private async Task<ServiceApiResponse<LibraryEntryDTO>> StoreAsync(
        Guid user, int gameId, GameStatus status, int? rating, decimal? hours, string? platform, string? notes, bool keepDetails)
    {
        GameRow? game;
        try
        {
            game = await gameStore.GetAsync(gameId);
        }
        catch (CatalogUnavailableException exception)
        {
            var message = await globalizationHelper.TranslateAsync(nameof(TranslationKeys.CatalogUnreachable), exception.Message);
            return ServiceApiResponse<LibraryEntryDTO>.ValidationProblems(message).AddError("GameId", message);
        }

        if (game is null)
        {
            var message = await globalizationHelper.TranslateAsync(nameof(TranslationKeys.GameNotFound));
            return ServiceApiResponse<LibraryEntryDTO>.NotFound(message);
        }

        var before = await libraryRepository.GetAsync(user, gameId);
        if (keepDetails && before is not null)
        {
            rating = before.Rating;
            hours = before.Hours;
            platform = before.Platform;
            notes = before.Notes;
        }

        await libraryRepository.SaveAsync(user, gameId, (byte)status, (byte?)rating, hours, platform, notes);
        foreach (var (kind, lineRating) in LibraryRules.Activities(GameMapper.Status(before?.Status), before?.Rating, status, rating))
        {
            await libraryRepository.AddActivityAsync(user, gameId, (byte)kind, (byte?)lineRating);
        }

        var after = await libraryRepository.GetAsync(user, gameId);
        if (after is null)
        {
            return await NotFoundAsync<LibraryEntryDTO>(user.ToString("N"));
        }

        var names = await FriendNamesAsync(user, [gameId]);
        return ServiceApiResponse<LibraryEntryDTO>.Success(LibraryRules.Entry(after, names.GetValueOrDefault(gameId)));
    }

    private async Task<Dictionary<int, List<string>>> FriendNamesAsync(Guid user, List<int> gameIds) =>
        GameMapper.NamesByGame((await friendRepository.OwnershipAsync(user, gameIds)).Select(static r => (r.GameID, r.Username)));

    private async Task<ServiceApiResponse<T>> NotFoundAsync<T>(string? identifier) =>
        ServiceApiResponse<T>.NotFound(await globalizationHelper.TranslateAsync(nameof(TranslationKeys.NoRecordsFound), identifier ?? string.Empty));

    private static string? Clean(string? text) => string.IsNullOrWhiteSpace(text) ? null : text.Trim();
}
