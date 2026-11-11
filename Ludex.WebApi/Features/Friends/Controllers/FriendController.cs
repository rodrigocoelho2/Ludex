using System.Net;
using System.Net.Mime;
using Ludex.Library.Validation;
using Ludex.WebApi.Extensions.Mvc;
using Ludex.WebApi.Extensions.Services;
using Ludex.WebApi.Features.Friends.Models.DTOs;
using Ludex.WebApi.Features.Friends.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Ludex.WebApi.Features.Friends.Controllers;

/// <summary>
/// Friends and friend requests.
/// </summary>
/// <param name="friendService">Friends.</param>
[Authorize]
[Route("friends")]
[ApiController]
[Produces(MediaTypeNames.Application.Json)]
[ProducesErrorResponseType(typeof(ApiValidationProblemDetails))]
[ProducesResponseType((int)HttpStatusCode.BadRequest)]
[ProducesResponseType((int)HttpStatusCode.Unauthorized)]
internal sealed class FriendController(IFriendService friendService) : InternalControllerBase
{
    /// <summary>The friends page.</summary>
    /// <param name="userId">The account.</param>
    /// <returns>Friends, requests, activity and favourite games.</returns>
    [HttpGet]
    [ProducesResponseType<FriendsDTO>((int)HttpStatusCode.OK)]
    [SwaggerOperation(Summary = "FriendsList", OperationId = "FriendsList")]
    public async Task<ActionResult<FriendsDTO>> FriendsListAsync(string userId) => (await friendService.ListAsync(userId)).ToActionResult();

    /// <summary>Asks an account to be friends, by user name.</summary>
    /// <param name="item">The account asking and the user name.</param>
    /// <returns>The friends page.</returns>
    [HttpPost("request")]
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType<FriendsDTO>((int)HttpStatusCode.OK)]
    [SwaggerOperation(Summary = "FriendsRequest", OperationId = "FriendsRequest")]
    public async Task<ActionResult<FriendsDTO>> FriendsRequestAsync(FriendRequestRequest item) => (await friendService.RequestAsync(item)).ToActionResult();

    /// <summary>Accepts a request.</summary>
    /// <param name="item">The account and the one that asked.</param>
    /// <returns>The friends page.</returns>
    [HttpPost("accept")]
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType<FriendsDTO>((int)HttpStatusCode.OK)]
    [SwaggerOperation(Summary = "FriendsAccept", OperationId = "FriendsAccept")]
    public async Task<ActionResult<FriendsDTO>> FriendsAcceptAsync(FriendActionRequest item) => (await friendService.AcceptAsync(item)).ToActionResult();

    /// <summary>Declines or cancels a request, or ends a friendship.</summary>
    /// <param name="item">The account and the other one.</param>
    /// <returns>The friends page.</returns>
    [HttpPost("remove")]
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType<FriendsDTO>((int)HttpStatusCode.OK)]
    [SwaggerOperation(Summary = "FriendsRemove", OperationId = "FriendsRemove")]
    public async Task<ActionResult<FriendsDTO>> FriendsRemoveAsync(FriendActionRequest item) => (await friendService.RemoveAsync(item)).ToActionResult();
}
