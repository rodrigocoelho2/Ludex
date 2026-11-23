using Ludex.Library.Enums;
using Ludex.WebApi.Features.Games.Models.DTOs;

namespace Ludex.WebApi.Features.Journal.Models.DTOs;

/// <summary>A line of the journal: a session written down, or time a sync saw.</summary>
public sealed class JournalEntryDTO
{
    /// <summary>Gets or sets the session id (null for time a sync saw: it can't be deleted).</summary>
    public long? Id { get; set; }

    /// <summary>Gets or sets where the line comes from.</summary>
    public SessionSource Source { get; set; } = SessionSource.Manual;

    /// <summary>Gets or sets the day.</summary>
    public DateOnly Day { get; set; }

    /// <summary>Gets or sets how long, in minutes.</summary>
    public int Minutes { get; set; }

    /// <summary>Gets or sets the platform.</summary>
    public string? Platform { get; set; }

    /// <summary>Gets or sets the note.</summary>
    public string? Note { get; set; }

    /// <summary>Gets or sets the game.</summary>
    public GameSummaryDTO Game { get; set; } = new();
}

/// <summary>One game's time in a month of the journal.</summary>
public sealed class JournalGameDTO
{
    /// <summary>Gets or sets the game.</summary>
    public GameSummaryDTO Game { get; set; } = new();

    /// <summary>Gets or sets the minutes played.</summary>
    public int Minutes { get; set; }

    /// <summary>Gets or sets how many days it was played.</summary>
    public int Days { get; set; }
}

/// <summary>A month of the journal.</summary>
public sealed class JournalDTO
{
    /// <summary>Gets or sets the year.</summary>
    public int Year { get; set; }

    /// <summary>Gets or sets the month (1–12).</summary>
    public int Month { get; set; }

    /// <summary>Gets or sets the lines, newest day first.</summary>
    public List<JournalEntryDTO> Entries { get; set; } = [];

    /// <summary>Gets or sets the time per game, most first.</summary>
    public List<JournalGameDTO> Games { get; set; } = [];

    /// <summary>Gets or sets the minutes played in the month.</summary>
    public int Minutes { get; set; }

    /// <summary>Gets or sets how many lines there are.</summary>
    public int Sessions { get; set; }

    /// <summary>Gets or sets how many days in a row, up to today, have something in the journal.</summary>
    public int Streak { get; set; }
}

/// <summary>Writes down a play session.</summary>
public sealed class SessionAddRequest
{
    /// <summary>Gets or sets the account.</summary>
    public string? UserId { get; set; }

    /// <summary>Gets or sets the game.</summary>
    public int GameId { get; set; }

    /// <summary>Gets or sets the day (null: today).</summary>
    public DateOnly? Day { get; set; }

    /// <summary>Gets or sets how long, in minutes.</summary>
    public int Minutes { get; set; }

    /// <summary>Gets or sets the platform.</summary>
    public string? Platform { get; set; }

    /// <summary>Gets or sets a note.</summary>
    public string? Note { get; set; }
}

/// <summary>Takes a session out of the journal.</summary>
public sealed class SessionDeleteRequest
{
    /// <summary>Gets or sets the account.</summary>
    public string? UserId { get; set; }

    /// <summary>Gets or sets the session.</summary>
    public long Id { get; set; }
}
