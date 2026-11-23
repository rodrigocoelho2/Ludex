using Ludex.Library.Enums;
using Ludex.WebApi.Features.Games.Services;
using Ludex.WebApi.Features.Journal.Models.DTOs;
using Ludex.WebApi.Features.Journal.Models.Entities;

namespace Ludex.WebApi.Features.Journal.Services;

/// <summary>
/// The journal: limits, a month's totals and the streak.
/// </summary>
internal static class JournalRules
{
    /// <summary>Longest session (a whole day).</summary>
    public const int MaxMinutes = 24 * 60;

    /// <summary>Longest note.</summary>
    public const int MaxNoteLength = 500;

    /// <summary>
    /// A line of the journal as the Website shows it.
    /// </summary>
    /// <param name="row">The row.</param>
    /// <returns>The line.</returns>
    public static JournalEntryDTO Entry(JournalRow row) => new()
    {
        Id = row.SessionID,
        Source = row.Source == (byte)SessionSource.Sync ? SessionSource.Sync : SessionSource.Manual,
        Day = DateOnly.FromDateTime(row.Day),
        Minutes = row.Minutes,
        Platform = row.Platform,
        Note = row.Note,
        Game = GameMapper.Summary(row),
    };

    /// <summary>
    /// The time per game, most first.
    /// </summary>
    /// <param name="entries">The lines.</param>
    /// <returns>The games.</returns>
    public static List<JournalGameDTO> Games(IEnumerable<JournalEntryDTO> entries) =>
        [.. entries
            .GroupBy(static e => e.Game.Id)
            .Select(static g => new JournalGameDTO { Game = g.First().Game, Minutes = g.Sum(static e => e.Minutes), Days = g.Select(static e => e.Day).Distinct().Count() })
            .OrderByDescending(static g => g.Minutes)
            .ThenBy(static g => g.Game.Name, StringComparer.CurrentCultureIgnoreCase)];

    /// <summary>
    /// How many days in a row, ending today (or yesterday, when today has nothing yet), have a line.
    /// </summary>
    /// <param name="days">The days with lines.</param>
    /// <param name="today">Today.</param>
    /// <returns>The streak, in days.</returns>
    public static int Streak(IEnumerable<DateOnly> days, DateOnly today)
    {
        var set = days.ToHashSet();
        var day = set.Contains(today) ? today : today.AddDays(-1);
        var streak = 0;
        while (set.Contains(day))
        {
            streak++;
            day = day.AddDays(-1);
        }

        return streak;
    }
}
