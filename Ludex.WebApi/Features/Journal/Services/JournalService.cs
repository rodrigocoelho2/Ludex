using Ludex.Library.Enums;
using Ludex.Library.Services;
using Ludex.Library.Validation;
using Ludex.WebApi.Extensions.Globalization;
using Ludex.WebApi.Features.Catalog;
using Ludex.WebApi.Features.Games.Services;
using Ludex.WebApi.Features.Journal.Models.DTOs;
using Ludex.WebApi.Features.Journal.Repositories;

namespace Ludex.WebApi.Features.Journal.Services;

/// <summary>
/// The journal: a month of sessions, written down or seen by the syncs.
/// </summary>
internal interface IJournalService
{
    /// <summary>A month of the journal.</summary>
    /// <param name="userId">The account.</param>
    /// <param name="year">The year.</param>
    /// <param name="month">The month (1–12).</param>
    /// <param name="today">Today where the person is (null: today in UTC).</param>
    /// <returns>The month.</returns>
    Task<ServiceApiResponse<JournalDTO>> MonthAsync(string userId, int year, int month, DateOnly? today = null);

    /// <summary>The last sessions of one game.</summary>
    /// <param name="userId">The account.</param>
    /// <param name="gameId">The game.</param>
    /// <returns>Every line of the game, newest first.</returns>
    Task<List<JournalEntryDTO>> GameAsync(Guid userId, int gameId);

    /// <summary>Writes down a session.</summary>
    /// <param name="item">The session.</param>
    /// <returns>The session id.</returns>
    Task<ServiceApiResponse<long>> AddAsync(SessionAddRequest item);

    /// <summary>Takes a session out.</summary>
    /// <param name="item">The account and the session.</param>
    /// <returns>The session id.</returns>
    Task<ServiceApiResponse<long>> DeleteAsync(SessionDeleteRequest item);
}

/// <summary>
/// The journal.
/// </summary>
/// <param name="journalRepository">Sessions.</param>
/// <param name="gameStore">Games, from the database or the catalog.</param>
/// <param name="globalizationHelper">Messages.</param>
/// <param name="timeProvider">The clock.</param>
internal sealed class JournalService(
    IJournalRepository journalRepository,
    GameStore gameStore,
    IGlobalizationHelper globalizationHelper,
    TimeProvider timeProvider) : IJournalService
{
    /// <inheritdoc />
    public async Task<ServiceApiResponse<JournalDTO>> MonthAsync(string userId, int year, int month, DateOnly? today = null)
    {
        if (!Guid.TryParse(userId, out var user))
        {
            return await NotFoundAsync<JournalDTO>(userId);
        }

        var now = today ?? Today();
        if (year is < 1970 or > 9998 || month is < 1 or > 12)
        {
            (year, month) = (now.Year, now.Month);
        }

        var from = new DateOnly(year, month, 1);
        var to = from.AddMonths(1).AddDays(-1);
        var entries = (await journalRepository.ListAsync(user, from, to)).Select(JournalRules.Entry).ToList();
        var recent = from <= now && now <= to
            ? entries
            : (await journalRepository.ListAsync(user, now.AddDays(-62), now)).Select(JournalRules.Entry).ToList();

        return ServiceApiResponse<JournalDTO>.Success(new JournalDTO
        {
            Year = year,
            Month = month,
            Entries = entries,
            Games = JournalRules.Games(entries),
            Minutes = entries.Sum(static e => e.Minutes),
            Sessions = entries.Count,
            Streak = JournalRules.Streak(recent.Select(static e => e.Day), now),
        });
    }

    /// <inheritdoc />
    public async Task<List<JournalEntryDTO>> GameAsync(Guid userId, int gameId) =>
        [.. (await journalRepository.ListAsync(userId, new DateOnly(1970, 1, 1), Today().AddDays(1), gameId)).Select(JournalRules.Entry)];

    /// <inheritdoc />
    public async Task<ServiceApiResponse<long>> AddAsync(SessionAddRequest item)
    {
        if (!Guid.TryParse(item.UserId, out var user))
        {
            return await NotFoundAsync<long>(item.UserId);
        }

        var day = item.Day ?? Today();
        var note = Reviews.Services.ReviewRules.Clean(item.Note);
        var platform = string.IsNullOrWhiteSpace(item.Platform) ? null : item.Platform.Trim();
        var errors = new List<ApiValidationError>();
        if (item.Minutes is < 1 or > JournalRules.MaxMinutes)
        {
            errors.Add(await globalizationHelper.ValidationErrorAsync("Minutes", nameof(TranslationKeys.OutOfRange), 1, JournalRules.MaxMinutes));
        }

        if (day > Today().AddDays(1))
        {
            errors.Add(await globalizationHelper.ValidationErrorAsync("Day", nameof(TranslationKeys.DayInFuture)));
        }

        if (note is { Length: > JournalRules.MaxNoteLength })
        {
            errors.Add(await globalizationHelper.ValidationErrorAsync("Note", nameof(TranslationKeys.TooLong), JournalRules.MaxNoteLength));
        }

        if (platform is { Length: > 60 })
        {
            errors.Add(await globalizationHelper.ValidationErrorAsync("Platform", nameof(TranslationKeys.TooLong), 60));
        }

        if (errors.Count > 0)
        {
            return ServiceApiResponse<long>.ValidationProblems(await globalizationHelper.TranslateAsync(nameof(TranslationKeys.ValidationProblems))).AddErrorRange(errors);
        }

        try
        {
            if (await gameStore.GetAsync(item.GameId) is null)
            {
                return ServiceApiResponse<long>.NotFound(await globalizationHelper.TranslateAsync(nameof(TranslationKeys.GameNotFound)));
            }
        }
        catch (CatalogUnavailableException exception)
        {
            var message = await globalizationHelper.TranslateAsync(nameof(TranslationKeys.CatalogUnreachable), exception.Message);
            return ServiceApiResponse<long>.ValidationProblems(message).AddError("GameId", message);
        }

        return ServiceApiResponse<long>.Success(await journalRepository.AddAsync(user, item.GameId, day, item.Minutes, platform, note));
    }

    /// <inheritdoc />
    public async Task<ServiceApiResponse<long>> DeleteAsync(SessionDeleteRequest item)
    {
        if (!Guid.TryParse(item.UserId, out var user))
        {
            return await NotFoundAsync<long>(item.UserId);
        }

        await journalRepository.DeleteAsync(user, item.Id);
        return ServiceApiResponse<long>.Success(item.Id);
    }

    private DateOnly Today() => DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);

    private async Task<ServiceApiResponse<T>> NotFoundAsync<T>(string? identifier) =>
        ServiceApiResponse<T>.NotFound(await globalizationHelper.TranslateAsync(nameof(TranslationKeys.NoRecordsFound), identifier ?? string.Empty));
}
