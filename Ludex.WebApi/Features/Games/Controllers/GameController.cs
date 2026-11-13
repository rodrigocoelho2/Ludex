using System.Net;
using System.Net.Mime;
using Ludex.Library.Validation;
using Ludex.WebApi.Extensions.Mvc;
using Ludex.WebApi.Extensions.Services;
using Ludex.WebApi.Features.Games.Models.DTOs;
using Ludex.WebApi.Features.Games.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Ludex.WebApi.Features.Games.Controllers;

/// <summary>
/// The catalog: search, a game's page and the covers of the sign-in page.
/// </summary>
/// <param name="gameService">Games.</param>
[Authorize]
[Route("games")]
[ApiController]
[Produces(MediaTypeNames.Application.Json)]
[ProducesErrorResponseType(typeof(ApiValidationProblemDetails))]
[ProducesResponseType((int)HttpStatusCode.BadRequest)]
[ProducesResponseType((int)HttpStatusCode.Unauthorized)]
internal sealed class GameController(IGameService gameService) : InternalControllerBase
{
    /// <summary>Searches the catalog.</summary>
    /// <param name="userId">The account (marks the games it and its friends have).</param>
    /// <param name="query">The text typed.</param>
    /// <returns>The games.</returns>
    [HttpGet("search")]
    [ProducesResponseType<SearchResultDTO>((int)HttpStatusCode.OK)]
    [SwaggerOperation(Summary = "GamesSearch", OperationId = "GamesSearch")]
    public async Task<ActionResult<SearchResultDTO>> GamesSearchAsync(string userId, string? query) =>
        (await gameService.SearchAsync(userId, query)).ToActionResult();

    /// <summary>Everything the game page shows.</summary>
    /// <param name="userId">The account.</param>
    /// <param name="gameId">The game.</param>
    /// <param name="entryId">The account's copy to show (null: the first one).</param>
    /// <returns>The game.</returns>
    [HttpGet("detail")]
    [ProducesResponseType<GameDetailDTO>((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    [SwaggerOperation(Summary = "GamesDetail", OperationId = "GamesDetail")]
    public async Task<ActionResult<GameDetailDTO>> GamesDetailAsync(string userId, int gameId, long? entryId = null) =>
        (await gameService.DetailAsync(userId, gameId, entryId)).ToActionResult();

    /// <summary>Covers for the sign-in page (no account data).</summary>
    /// <returns>The covers.</returns>
    [HttpGet("wall")]
    [ProducesResponseType<CoverWallDTO>((int)HttpStatusCode.OK)]
    [SwaggerOperation(Summary = "GamesWall", OperationId = "GamesWall")]
    public async Task<ActionResult<CoverWallDTO>> GamesWallAsync() => (await gameService.WallAsync()).ToActionResult();
}
