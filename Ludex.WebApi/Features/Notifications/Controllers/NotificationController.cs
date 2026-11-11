using System.Net;
using System.Net.Mime;
using Ludex.Library.Validation;
using Ludex.WebApi.Extensions.Mvc;
using Ludex.WebApi.Extensions.Services;
using Ludex.WebApi.Features.Notifications.Models.DTOs;
using Ludex.WebApi.Features.Notifications.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Ludex.WebApi.Features.Notifications.Controllers;

/// <summary>
/// The bell.
/// </summary>
/// <param name="notificationService">Notifications.</param>
[Authorize]
[Route("notifications")]
[ApiController]
[Produces(MediaTypeNames.Application.Json)]
[ProducesErrorResponseType(typeof(ApiValidationProblemDetails))]
[ProducesResponseType((int)HttpStatusCode.BadRequest)]
[ProducesResponseType((int)HttpStatusCode.Unauthorized)]
internal sealed class NotificationController(INotificationService notificationService) : InternalControllerBase
{
    /// <summary>The latest notifications and how many are new.</summary>
    /// <param name="userId">The account.</param>
    /// <returns>The bell.</returns>
    [HttpGet]
    [ProducesResponseType<NotificationsDTO>((int)HttpStatusCode.OK)]
    [SwaggerOperation(Summary = "NotificationsList", OperationId = "NotificationsList")]
    public async Task<ActionResult<NotificationsDTO>> NotificationsListAsync(string userId) => (await notificationService.ListAsync(userId)).ToActionResult();

    /// <summary>How many are new.</summary>
    /// <param name="userId">The account.</param>
    /// <returns>The count.</returns>
    [HttpGet("unread")]
    [ProducesResponseType<int>((int)HttpStatusCode.OK)]
    [SwaggerOperation(Summary = "NotificationsUnread", OperationId = "NotificationsUnread")]
    public async Task<ActionResult<int>> NotificationsUnreadAsync(string userId) => (await notificationService.UnreadAsync(userId)).ToActionResult();

    /// <summary>Marks them all as seen.</summary>
    /// <param name="item">The account.</param>
    /// <returns>The bell (as it was before: the new ones still marked new).</returns>
    [HttpPost("read")]
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType<NotificationsDTO>((int)HttpStatusCode.OK)]
    [SwaggerOperation(Summary = "NotificationsRead", OperationId = "NotificationsRead")]
    public async Task<ActionResult<NotificationsDTO>> NotificationsReadAsync(NotificationReadRequest item) => (await notificationService.ReadAsync(item)).ToActionResult();
}
