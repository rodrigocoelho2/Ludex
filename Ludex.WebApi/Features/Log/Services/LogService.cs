using Ludex.Library.Enums;
using Ludex.Library.Services;
using Ludex.Library.Validation;
using Ludex.WebApi.Extensions.Globalization;
using Ludex.WebApi.Features.Catalog;
using Ludex.WebApi.Features.Games.Services;
using Ludex.WebApi.Features.Journal.Repositories;
using Ludex.WebApi.Features.Journal.Services;
using Ludex.WebApi.Features.Library.Models.DTOs;
using Ludex.WebApi.Features.Library.Repositories;
using Ludex.WebApi.Features.Library.Services;
using Ludex.WebApi.Features.Log.Models.DTOs;
using Ludex.WebApi.Features.Reviews.Repositories;
using Ludex.WebApi.Features.Reviews.Services;

namespace Ludex.WebApi.Features.Log.Services;

/// <summary>
/// "Log a game": status, rating, platform, hours, how it ended and when, the review, and a session for the journal,
/// in one go.
/// </summary>
internal interface ILogService
{
    /// <summary>What the form opens with.</summary>
    /// <param name="userId">The account.</param>
    /// <param name="gameId">The game.</param>
    /// <param name="entryId">The copy (null: the first one).</param>
    /// <returns>The form, or a problem.</returns>
    Task<ServiceApiResponse<LogFormDTO>> FormAsync(string userId, int gameId, long? entryId);

    /// <summary>Saves the form.</summary>
    /// <param name="item">Everything on it.</param>
    /// <returns>What was saved, or the problems.</returns>
    Task<ServiceApiResponse<LogResultDTO>> SaveAsync(LogSaveRequest item);
}

/// <summary>
/// "Log a game".
/// </summary>
/// <param name="libraryService">The library (its own checks and the friends' feed).</param>
/// <param name="libraryRepository">Library entries.</param>
/// <param name="reviewRepository">Reviews.</param>
/// <param name="journalRepository">The journal.</param>
/// <param name="gameStore">Games, from the database or the catalog.</param>
/// <param name="globalizationHelper">Messages.</param>
/// <param name="timeProvider">The clock.</param>
internal sealed class LogService(
    ILibraryService libraryService,
    ILibraryRepository libraryRepository,
    IReviewRepository reviewRepository,
    IJournalRepository journalRepository,
    GameStore gameStore,
    IGlobalizationHelper globalizationHelper,
    TimeProvider timeProvider) : ILogService
{
    /// <inheritdoc />
    public async Task<ServiceApiResponse<LogFormDTO>> FormAsync(string userId, int gameId, long? entryId)
    {
        if (!Guid.TryParse(userId, out var user))
        {
            return await NotFoundAsync<LogFormDTO>(userId);
        }

        Games.Models.Entities.GameRow? game;
        try
        {
            game = await gameStore.GetAsync(gameId);
        }
        catch (CatalogUnavailableException exception)
        {
            var message = await globalizationHelper.TranslateAsync(nameof(TranslationKeys.CatalogUnreachable), exception.Message);
            return ServiceApiResponse<LogFormDTO>.ValidationProblems(message).AddError("GameId", message);
        }

        if (game is null)
        {
            return ServiceApiResponse<LogFormDTO>.NotFound(await globalizationHelper.TranslateAsync(nameof(TranslationKeys.GameNotFound)));
        }

        var entry = await libraryRepository.GetAsync(user, gameId, entryId);
        var review = (await reviewRepository.ListAsync(user, gameId, user, (byte)ReviewSort.Recent, ReviewService.PopularDays, 0, 1)).FirstOrDefault();
        return ServiceApiResponse<LogFormDTO>.Success(new LogFormDTO
        {
            Game = GameMapper.Summary(game, GameMapper.Status(entry?.Status)),
            InLibrary = entry is not null,
            EntryId = entry?.EntryID,
            Status = GameMapper.Status(entry?.Status),
            Rating = entry?.Rating,
            Platform = entry?.Platform,
            Hours = entry?.Hours,
            Ending = entry?.Ending is >= 1 and <= 4 ? (GameEnding)entry.Ending.Value : null,
            StartedOn = entry?.StartedOn is { } started ? DateOnly.FromDateTime(started) : null,
            FinishedOn = entry?.FinishedOn is { } finished ? DateOnly.FromDateTime(finished) : null,
            Review = review?.Text,
            Spoilers = review?.Spoilers ?? false,
        });
    }

    /// <inheritdoc />
    public async Task<ServiceApiResponse<LogResultDTO>> SaveAsync(LogSaveRequest item)
    {
        if (!Guid.TryParse(item.UserId, out var user))
        {
            return await NotFoundAsync<LogResultDTO>(item.UserId);
        }

        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        var text = ReviewRules.Clean(item.Review);
        var sessionNote = ReviewRules.Clean(item.SessionNote);
        var minutes = item.SessionMinutes ?? 0;
        var errors = new List<ApiValidationError>();
        if (text is { Length: > ReviewRules.MaxReviewLength })
        {
            errors.Add(await globalizationHelper.ValidationErrorAsync("Review", nameof(TranslationKeys.TooLong), ReviewRules.MaxReviewLength));
        }

        if (minutes is < 0 or > JournalRules.MaxMinutes)
        {
            errors.Add(await globalizationHelper.ValidationErrorAsync("SessionMinutes", nameof(TranslationKeys.OutOfRange), 0, JournalRules.MaxMinutes));
        }

        if (sessionNote is { Length: > JournalRules.MaxNoteLength })
        {
            errors.Add(await globalizationHelper.ValidationErrorAsync("SessionNote", nameof(TranslationKeys.TooLong), JournalRules.MaxNoteLength));
        }

        if (item.StartedOn is { } startedOn && item.FinishedOn is { } finishedOn && finishedOn < startedOn)
        {
            errors.Add(await globalizationHelper.ValidationErrorAsync("FinishedOn", nameof(TranslationKeys.FinishedBeforeStarted)));
        }

        if (item.StartedOn > today.AddDays(1) || item.FinishedOn > today.AddDays(1) || item.SessionDay > today.AddDays(1))
        {
            errors.Add(await globalizationHelper.ValidationErrorAsync(item.SessionDay > today.AddDays(1) ? "Day" : item.FinishedOn > today.AddDays(1) ? "FinishedOn" : "StartedOn", nameof(TranslationKeys.DayInFuture)));
        }

        if (errors.Count > 0)
        {
            return ServiceApiResponse<LogResultDTO>.ValidationProblems(await globalizationHelper.TranslateAsync(nameof(TranslationKeys.ValidationProblems))).AddErrorRange(errors);
        }

        // The library entry, through the library's own checks (rating, hours, platform) and its feed lines. What the
        // form doesn't show (notes, store, format) stays as it was; a store that no longer fits a new platform goes.
        var before = await libraryRepository.GetAsync(user, item.GameId, item.EntryId);
        var platform = string.IsNullOrWhiteSpace(item.Platform) ? before?.Platform : item.Platform.Trim();
        var samePlatform = string.Equals(platform, before?.Platform, StringComparison.OrdinalIgnoreCase);
        var saved = await libraryService.SaveAsync(new LibrarySaveRequest
        {
            UserId = item.UserId,
            EntryId = before?.EntryID ?? item.EntryId,
            GameId = item.GameId,
            Status = item.Status,
            Rating = item.Rating,
            Hours = item.Hours ?? before?.Hours,
            Platform = platform,
            Store = samePlatform ? before?.Store : null,
            Format = samePlatform && before?.Format is 1 or 2 ? (GameFormat)before.Format!.Value : null,
            Notes = before?.Notes,
            Quiet = item.Quiet,
        });
        if (!saved.IsSuccess || saved.Result is not { } entry)
        {
            return saved.Status switch
            {
                ServiceApiResponseStatus.NotFound => ServiceApiResponse<LogResultDTO>.NotFound(saved.Problems?.Title ?? string.Empty),
                _ => ServiceApiResponse<LogResultDTO>.ValidationProblems(saved.Problems?.Title ?? string.Empty).AddErrorRange(saved.Problems?.Errors ?? []),
            };
        }

        var ending = ReviewRules.EndingFor(item.Status, item.Ending);
        await libraryRepository.SaveLogAsync(user, entry.EntryId, (byte?)ending, item.StartedOn, item.FinishedOn);

        long? reviewId = null;
        if (text is not null)
        {
            reviewId = await reviewRepository.SaveAsync(user, item.GameId, text, item.Spoilers, (byte?)item.Rating, (byte)item.Status, (byte?)ending, platform);
        }
        else
        {
            await reviewRepository.DeleteAsync(user, item.GameId);
        }

        long? sessionId = null;
        if (minutes > 0)
        {
            sessionId = await journalRepository.AddAsync(user, item.GameId, item.SessionDay ?? today, minutes, platform, sessionNote);
        }

        return ServiceApiResponse<LogResultDTO>.Success(new LogResultDTO
        {
            EntryId = entry.EntryId,
            ReviewId = reviewId,
            SessionId = sessionId,
            Status = entry.Status,
            Rating = entry.Rating,
        });
    }

    private async Task<ServiceApiResponse<T>> NotFoundAsync<T>(string? identifier) =>
        ServiceApiResponse<T>.NotFound(await globalizationHelper.TranslateAsync(nameof(TranslationKeys.NoRecordsFound), identifier ?? string.Empty));
}
