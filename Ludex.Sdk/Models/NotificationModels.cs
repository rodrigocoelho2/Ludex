using Ludex.Library.Enums;

namespace Ludex.Sdk.Models;

/// <summary>The bell: the latest notifications and how many are new.</summary>
public sealed class NotificationsDTO
{
    /// <summary>Gets or sets how many are unread.</summary>
    public int Unread { get; set; }

    /// <summary>Gets or sets the latest, newest first.</summary>
    public List<NotificationDTO> Items { get; set; } = [];
}

/// <summary>One notification.</summary>
public sealed class NotificationDTO
{
    /// <summary>Gets or sets the id.</summary>
    public long Id { get; set; }

    /// <summary>Gets or sets what it is about.</summary>
    public NotificationKind Kind { get; set; }

    /// <summary>Gets or sets the game (null: none).</summary>
    public int? GameId { get; set; }

    /// <summary>Gets or sets the words.</summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>Gets or sets where it leads.</summary>
    public string? Url { get; set; }

    /// <summary>Gets or sets when.</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Gets or sets a value indicating whether it was seen.</summary>
    public bool Read { get; set; }
}

/// <summary>Marks an account's notifications as seen.</summary>
public sealed class NotificationReadRequest
{
    /// <summary>Gets or sets the account.</summary>
    public string? UserId { get; set; }
}
