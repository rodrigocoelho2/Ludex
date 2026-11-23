using Ludex.Library.Enums;

namespace Ludex.Sdk.Models;

/// <summary>What "Log a game" opens with: the game and what is already known about it.</summary>
public sealed class LogFormDTO
{
    /// <summary>Gets or sets the game.</summary>
    public GameSummaryDTO Game { get; set; } = new();

    /// <summary>Gets or sets a value indicating whether it is in the library.</summary>
    public bool InLibrary { get; set; }

    /// <summary>Gets or sets the copy being logged.</summary>
    public long? EntryId { get; set; }

    /// <summary>Gets or sets the status.</summary>
    public GameStatus? Status { get; set; }

    /// <summary>Gets or sets the rating (1–10: half stars).</summary>
    public int? Rating { get; set; }

    /// <summary>Gets or sets the platform.</summary>
    public string? Platform { get; set; }

    /// <summary>Gets or sets the hours.</summary>
    public decimal? Hours { get; set; }

    /// <summary>Gets or sets how it ended.</summary>
    public GameEnding? Ending { get; set; }

    /// <summary>Gets or sets the day it was started.</summary>
    public DateOnly? StartedOn { get; set; }

    /// <summary>Gets or sets the day it was finished.</summary>
    public DateOnly? FinishedOn { get; set; }

    /// <summary>Gets or sets the review written already.</summary>
    public string? Review { get; set; }

    /// <summary>Gets or sets a value indicating whether the review has spoilers.</summary>
    public bool Spoilers { get; set; }
}

/// <summary>Saves "Log a game": the library entry, how it ended, the review and, if asked, a session for the journal.</summary>
public sealed class LogSaveRequest
{
    /// <summary>Gets or sets the account.</summary>
    public string? UserId { get; set; }

    /// <summary>Gets or sets the game.</summary>
    public int GameId { get; set; }

    /// <summary>Gets or sets the copy (null: the first one, or a new entry).</summary>
    public long? EntryId { get; set; }

    /// <summary>Gets or sets the status.</summary>
    public GameStatus Status { get; set; } = GameStatus.Playing;

    /// <summary>Gets or sets the rating (1–10; null: none).</summary>
    public int? Rating { get; set; }

    /// <summary>Gets or sets the platform.</summary>
    public string? Platform { get; set; }

    /// <summary>Gets or sets the hours.</summary>
    public decimal? Hours { get; set; }

    /// <summary>Gets or sets how it ended.</summary>
    public GameEnding? Ending { get; set; }

    /// <summary>Gets or sets the day it was started.</summary>
    public DateOnly? StartedOn { get; set; }

    /// <summary>Gets or sets the day it was finished.</summary>
    public DateOnly? FinishedOn { get; set; }

    /// <summary>Gets or sets the review (empty: no review; an existing one is deleted).</summary>
    public string? Review { get; set; }

    /// <summary>Gets or sets a value indicating whether the review has spoilers.</summary>
    public bool Spoilers { get; set; }

    /// <summary>Gets or sets the minutes of a session to add to the journal (null or 0: none).</summary>
    public int? SessionMinutes { get; set; }

    /// <summary>Gets or sets the day of that session (null: today).</summary>
    public DateOnly? SessionDay { get; set; }

    /// <summary>Gets or sets a note for that session.</summary>
    public string? SessionNote { get; set; }

    /// <summary>Gets or sets a value indicating whether to leave the friends' feed out of it.</summary>
    public bool Quiet { get; set; }
}

/// <summary>What "Log a game" saved.</summary>
public sealed class LogResultDTO
{
    /// <summary>Gets or sets the library entry.</summary>
    public long EntryId { get; set; }

    /// <summary>Gets or sets the review (null: none).</summary>
    public long? ReviewId { get; set; }

    /// <summary>Gets or sets the session added (null: none).</summary>
    public long? SessionId { get; set; }

    /// <summary>Gets or sets the status now.</summary>
    public GameStatus Status { get; set; }

    /// <summary>Gets or sets the rating now.</summary>
    public int? Rating { get; set; }
}
