using Ludex.WebApi.Features.Games.Models.Entities;

namespace Ludex.WebApi.Features.Library.Models.Entities;

/// <summary>
/// One game in an account's library (dbo.LibraryEntry joined with dbo.Game).
/// </summary>
public sealed class LibraryRow : GameRow
{
    /// <summary>Gets or sets the status (<see cref="Ludex.Library.Enums.GameStatus"/>).</summary>
    public byte Status { get; set; }

    /// <summary>Gets or sets the rating (1–10).</summary>
    public byte? Rating { get; set; }

    /// <summary>Gets or sets the hours played.</summary>
    public decimal? Hours { get; set; }

    /// <summary>Gets or sets where it was played.</summary>
    public string? Platform { get; set; }

    /// <summary>Gets or sets the PC store the copy came from.</summary>
    public string? Store { get; set; }

    /// <summary>Gets or sets the kind of copy (<see cref="Ludex.Library.Enums.GameFormat"/>).</summary>
    public byte? Format { get; set; }

    /// <summary>Gets or sets the notes.</summary>
    public string? Notes { get; set; }

    /// <summary>Gets or sets when it was added (UTC).</summary>
    public DateTime AddedAt { get; set; }

    /// <summary>Gets or sets when it was last changed (UTC).</summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>Gets or sets when it was first finished (UTC).</summary>
    public DateTime? FinishedAt { get; set; }

    /// <summary>Gets or sets how many friends have it too.</summary>
    public int FriendCount { get; set; }
}
