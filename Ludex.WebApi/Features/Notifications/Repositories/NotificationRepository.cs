using Ludex.Library.Enums;
using Ludex.WebApi.Extensions.Data;

namespace Ludex.WebApi.Features.Notifications.Repositories;

/// <summary>A notification (dbo.NotificationList).</summary>
public sealed class NotificationRow
{
    /// <summary>Gets or sets the id.</summary>
    public long NotificationID { get; set; }

    /// <summary>Gets or sets the game.</summary>
    public int? GameID { get; set; }

    /// <summary>Gets or sets the kind.</summary>
    public byte Kind { get; set; }

    /// <summary>Gets or sets the words.</summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>Gets or sets where it leads.</summary>
    public string? Url { get; set; }

    /// <summary>Gets or sets when (UTC).</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Gets or sets when it was seen (UTC).</summary>
    public DateTime? ReadAt { get; set; }
}

/// <summary>
/// The bell (dbo.Notification).
/// </summary>
internal interface INotificationRepository
{
    /// <summary>The latest notifications.</summary>
    /// <param name="userId">The account.</param>
    /// <param name="take">At most this many.</param>
    /// <returns>Newest first.</returns>
    Task<List<NotificationRow>> ListAsync(Guid userId, int take);

    /// <summary>How many are unread.</summary>
    /// <param name="userId">The account.</param>
    /// <returns>The count.</returns>
    Task<int> UnreadAsync(Guid userId);

    /// <summary>Marks every notification as seen.</summary>
    /// <param name="userId">The account.</param>
    /// <returns>A task.</returns>
    Task ReadAsync(Guid userId);

    /// <summary>Adds a notification.</summary>
    /// <param name="userId">The account.</param>
    /// <param name="gameId">The game.</param>
    /// <param name="kind">What it is about.</param>
    /// <param name="text">The words.</param>
    /// <param name="url">Where it leads.</param>
    /// <returns>A task.</returns>
    Task AddAsync(Guid userId, int? gameId, NotificationKind kind, string text, string? url);
}

/// <summary>
/// The bell in SQL Server (stored procedures in Ludex.Database/StoredProcedures/Notifications).
/// </summary>
/// <param name="connectionFactory">Opens connections.</param>
internal sealed class NotificationRepository(ISqlConnectionFactory connectionFactory) : RepositoryBase(connectionFactory), INotificationRepository
{
    /// <inheritdoc />
    public Task<List<NotificationRow>> ListAsync(Guid userId, int take) =>
        QueryAsync<NotificationRow>("[dbo].[NotificationList]", new { UserUniqueID = userId, Take = take });

    /// <inheritdoc />
    public Task<int> UnreadAsync(Guid userId) => ExecuteScalarAsync<int>("[dbo].[NotificationUnread]", new { UserUniqueID = userId });

    /// <inheritdoc />
    public Task ReadAsync(Guid userId) => ExecuteAsync("[dbo].[NotificationRead]", new { UserUniqueID = userId });

    /// <inheritdoc />
    public Task AddAsync(Guid userId, int? gameId, NotificationKind kind, string text, string? url) =>
        ExecuteAsync("[dbo].[NotificationAdd]", new { UserUniqueID = userId, GameID = gameId, Kind = (byte)kind, Text = text.Length > 300 ? text[..300] : text, Url = url });
}
