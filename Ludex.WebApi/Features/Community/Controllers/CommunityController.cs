using System.Net;
using System.Net.Mime;
using Ludex.Library.Enums;
using Ludex.Library.Validation;
using Ludex.WebApi.Extensions.Mvc;
using Ludex.WebApi.Extensions.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Ludex.WebApi.Features.Community.Models.DTOs;
using Ludex.WebApi.Features.Community.Services;

namespace Ludex.WebApi.Features.Community.Controllers;

/// <summary>
/// What everyone on this Ludex plays and thinks.
/// </summary>
/// <param name="communityService">The community.</param>
[Authorize]
[ApiController]
[Produces(MediaTypeNames.Application.Json)]
[ProducesErrorResponseType(typeof(ApiValidationProblemDetails))]
[ProducesResponseType((int)HttpStatusCode.BadRequest)]
[ProducesResponseType((int)HttpStatusCode.Unauthorized)]
[Route("community")]
internal sealed class CommunityController(ICommunityService communityService) : InternalControllerBase
{
    /// <summary>Home's community shelves.</summary>
    /// <param name="userId">The reader.</param>
    /// <returns>The shelves.</returns>
    [HttpGet("home")]
    [ProducesResponseType<CommunityHomeDTO>((int)HttpStatusCode.OK)]
    [SwaggerOperation(Summary = "CommunityHome", OperationId = "CommunityHome")]
    public async Task<ActionResult<CommunityHomeDTO>> CommunityHomeAsync(string userId) => (await communityService.HomeAsync(userId)).ToActionResult();

    /// <summary>A game's ratings, statuses, reviews and lists here.</summary>
    /// <param name="userId">The reader.</param>
    /// <param name="gameId">The game.</param>
    /// <param name="sort">How the reviews are ordered.</param>
    /// <returns>The game's community block.</returns>
    [HttpGet("game")]
    [ProducesResponseType<GameCommunityDTO>((int)HttpStatusCode.OK)]
    [SwaggerOperation(Summary = "CommunityGame", OperationId = "CommunityGame")]
    public async Task<ActionResult<GameCommunityDTO>> CommunityGameAsync(string userId, int gameId, ReviewSort sort = ReviewSort.Popular) =>
        (await communityService.GameAsync(userId, gameId, sort)).ToActionResult();
}
