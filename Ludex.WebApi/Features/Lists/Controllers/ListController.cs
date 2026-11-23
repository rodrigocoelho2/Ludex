using System.Net;
using System.Net.Mime;
using Ludex.Library.Enums;
using Ludex.Library.Validation;
using Ludex.WebApi.Extensions.Mvc;
using Ludex.WebApi.Extensions.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Ludex.WebApi.Features.Lists.Models.DTOs;
using Ludex.WebApi.Features.Lists.Services;

namespace Ludex.WebApi.Features.Lists.Controllers;

/// <summary>
/// Lists of games.
/// </summary>
/// <param name="listService">Lists.</param>
[Authorize]
[ApiController]
[Produces(MediaTypeNames.Application.Json)]
[ProducesErrorResponseType(typeof(ApiValidationProblemDetails))]
[ProducesResponseType((int)HttpStatusCode.BadRequest)]
[ProducesResponseType((int)HttpStatusCode.Unauthorized)]
[Route("lists")]
internal sealed class ListController(IListService listService) : InternalControllerBase
{
    /// <summary>The lists page.</summary>
    /// <param name="viewerId">The reader.</param>
    /// <param name="owner">An account's user name (empty: the reader's own).</param>
    /// <returns>The lists.</returns>
    [HttpGet]
    [ProducesResponseType<ListBrowseDTO>((int)HttpStatusCode.OK)]
    [SwaggerOperation(Summary = "ListsBrowse", OperationId = "ListsBrowse")]
    public async Task<ActionResult<ListBrowseDTO>> ListsBrowseAsync(string viewerId, string? owner = null) => (await listService.BrowseAsync(viewerId, owner)).ToActionResult();

    /// <summary>A list's own page.</summary>
    /// <param name="viewerId">The reader.</param>
    /// <param name="id">The list.</param>
    /// <returns>The list.</returns>
    [HttpGet("{id:int}")]
    [ProducesResponseType<ListDetailDTO>((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    [SwaggerOperation(Summary = "ListsGet", OperationId = "ListsGet")]
    public async Task<ActionResult<ListDetailDTO>> ListsGetAsync(string viewerId, int id) => (await listService.GetAsync(viewerId, id)).ToActionResult();

    /// <summary>The account's lists and whether a game is in each.</summary>
    /// <param name="userId">The account.</param>
    /// <param name="gameId">The game.</param>
    /// <returns>The lists.</returns>
    [HttpGet("for-game")]
    [ProducesResponseType<List<MyListDTO>>((int)HttpStatusCode.OK)]
    [SwaggerOperation(Summary = "ListsForGame", OperationId = "ListsForGame")]
    public async Task<ActionResult<List<MyListDTO>>> ListsForGameAsync(string userId, int gameId) => (await listService.ForGameAsync(userId, gameId)).ToActionResult();

    /// <summary>Makes a list or changes one.</summary>
    /// <param name="item">The list.</param>
    /// <returns>The list now.</returns>
    [HttpPost("save")]
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType<ListDetailDTO>((int)HttpStatusCode.OK)]
    [SwaggerOperation(Summary = "ListsSave", OperationId = "ListsSave")]
    public async Task<ActionResult<ListDetailDTO>> ListsSaveAsync(ListSaveRequest item) => (await listService.SaveAsync(item)).ToActionResult();

    /// <summary>Deletes a list.</summary>
    /// <param name="item">The account and the list.</param>
    /// <returns>The list id.</returns>
    [HttpPost("delete")]
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType<int>((int)HttpStatusCode.OK)]
    [SwaggerOperation(Summary = "ListsDelete", OperationId = "ListsDelete")]
    public async Task<ActionResult<int>> ListsDeleteAsync(ListDeleteRequest item) => (await listService.DeleteAsync(item)).ToActionResult();

    /// <summary>Puts a game in a list or takes it out.</summary>
    /// <param name="item">The account, the list and the game.</param>
    /// <returns>The account's lists and whether the game is in each.</returns>
    [HttpPost("item")]
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType<List<MyListDTO>>((int)HttpStatusCode.OK)]
    [SwaggerOperation(Summary = "ListsItem", OperationId = "ListsItem")]
    public async Task<ActionResult<List<MyListDTO>>> ListsItemAsync(ListItemRequest item) => (await listService.SetItemAsync(item)).ToActionResult();

    /// <summary>Moves a game of a list.</summary>
    /// <param name="item">The account, the list, the game and its new place.</param>
    /// <returns>The list now.</returns>
    [HttpPost("move")]
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType<ListDetailDTO>((int)HttpStatusCode.OK)]
    [SwaggerOperation(Summary = "ListsMove", OperationId = "ListsMove")]
    public async Task<ActionResult<ListDetailDTO>> ListsMoveAsync(ListMoveRequest item) => (await listService.MoveAsync(item)).ToActionResult();

    /// <summary>Likes a list or takes the like back.</summary>
    /// <param name="item">The account and the list.</param>
    /// <returns>The likes now.</returns>
    [HttpPost("like")]
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType<ListLikeDTO>((int)HttpStatusCode.OK)]
    [SwaggerOperation(Summary = "ListsLike", OperationId = "ListsLike")]
    public async Task<ActionResult<ListLikeDTO>> ListsLikeAsync(ListLikeRequest item) => (await listService.LikeAsync(item)).ToActionResult();
}
