using Ludex.WebApi.Features.Games.Models.Entities;

namespace Ludex.WebApi.Features.Journal.Models.Entities;

/// <summary>A line of the journal with its game (dbo.JournalList).</summary>
public sealed class JournalRow : GameRow
{
    /// <summary>Gets or sets the session id (null: time a sync saw).</summary>
    public long? SessionID { get; set; }

    /// <summary>Gets or sets where it comes from (1 written down, 2 a sync).</summary>
    public byte Source { get; set; }

    /// <summary>Gets or sets the day.</summary>
    public DateTime Day { get; set; }

    /// <summary>Gets or sets the minutes.</summary>
    public int Minutes { get; set; }

    /// <summary>Gets or sets the platform.</summary>
    public string? Platform { get; set; }

    /// <summary>Gets or sets the note.</summary>
    public string? Note { get; set; }

    /// <summary>Gets or sets when it was written down.</summary>
    public DateTime CreatedAt { get; set; }
}
