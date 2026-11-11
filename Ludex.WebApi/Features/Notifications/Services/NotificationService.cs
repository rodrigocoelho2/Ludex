using Ludex.Library.Enums;
using Ludex.Library.Services;
using Ludex.WebApi.Extensions.Globalization;
using Ludex.WebApi.Features.Games.Services;
using Ludex.WebApi.Features.Notifications.Models.DTOs;
using Ludex.WebApi.Features.Notifications.Repositories;

namespace Ludex.WebApi.Features.Notifications.Services;

/// <summary>
/// The bell: price alerts and games the Steam sync added.
/// </summary>
internal interface INotificationService
{
    /// <summary>The latest notifications and how many are new.</summary>
    /// <param name="userId">The account.</param>
    /// <returns>The bell.</returns>
    Task<ServiceApiResponse<NotificationsDTO>> ListAsync(string userId);

    /// <summary>How many are new.</summary>
    /// <param name="userId">The account.</param>
    /// <returns>The count.</returns>
    Task<ServiceApiResponse<int>> UnreadAsync(string userId);

    /// <summary>Marks them all as seen.</summary>
    /// <param name="item">The account.</param>
    /// <returns>The bell.</returns>
    Task<ServiceApiResponse<NotificationsDTO>> ReadAsync(NotificationReadRequest item);
}

/// <summary>
/// The bell.
/// </summary>
/// <param name="notificationRepository">Notifications.</param>
/// <param name="globalizationHelper">Messages.</param>
internal sealed class NotificationService(INotificationRepository notificationRepository, IGlobalizationHelper globalizationHelper) : INotificationService
{
    /// <inheritdoc />
    public async Task<ServiceApiResponse<NotificationsDTO>> ListAsync(string userId)
    {
        if (!Guid.TryParse(userId, out var user))
        {
            return await NotFoundAsync<NotificationsDTO>(userId);
        }

        var rows = await notificationRepository.ListAsync(user, 50);
        return ServiceApiResponse<NotificationsDTO>.Success(new NotificationsDTO
        {
            Unread = rows.Count(static r => r.ReadAt is null),
            Items = [.. rows.Select(static r => new NotificationDTO
            {
                Id = r.NotificationID,
                Kind = Enum.IsDefined((NotificationKind)r.Kind) ? (NotificationKind)r.Kind : NotificationKind.PriceAlert,
                GameId = r.GameID,
                Text = r.Text,
                Url = r.Url,
                CreatedAt = GameMapper.Utc(r.CreatedAt),
                Read = r.ReadAt is not null,
            })],
        });
    }

    /// <inheritdoc />
    public async Task<ServiceApiResponse<int>> UnreadAsync(string userId) =>
        Guid.TryParse(userId, out var user)
            ? ServiceApiResponse<int>.Success(await notificationRepository.UnreadAsync(user))
            : await NotFoundAsync<int>(userId);

    /// <inheritdoc />
    public async Task<ServiceApiResponse<NotificationsDTO>> ReadAsync(NotificationReadRequest item)
    {
        var page = await ListAsync(item.UserId ?? string.Empty);
        if (Guid.TryParse(item.UserId, out var user))
        {
            await notificationRepository.ReadAsync(user);
        }

        return page;
    }

    private async Task<ServiceApiResponse<T>> NotFoundAsync<T>(string? identifier) =>
        ServiceApiResponse<T>.NotFound(await globalizationHelper.TranslateAsync(nameof(TranslationKeys.NoRecordsFound), identifier ?? string.Empty));
}
