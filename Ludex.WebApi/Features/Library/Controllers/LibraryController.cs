using System.Net;
using System.Net.Mime;
using Ludex.Library.Validation;
using Ludex.WebApi.Extensions.Mvc;
using Ludex.WebApi.Extensions.Services;
using Ludex.WebApi.Features.Library.Models.DTOs;
using Ludex.WebApi.Features.Library.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Ludex.WebApi.Features.Library.Controllers;

/// <summary>
/// An account's library and home page.
/// </summary>
/// <param name="libraryService">Libraries.</param>
[Authorize]
[Route("library")]
[ApiController]
[Produces(MediaTypeNames.Application.Json)]
[ProducesErrorResponseType(typeof(ApiValidationProblemDetails))]
[ProducesResponseType((int)HttpStatusCode.BadRequest)]
[ProducesResponseType((int)HttpStatusCode.Unauthorized)]
internal sealed class LibraryController(ILibraryService libraryService) : InternalControllerBase
{
    /// <summary>The library.</summary>
    /// <param name="userId">The account.</param>
    /// <returns>Every game and the counts.</returns>
    [HttpGet]
    [ProducesResponseType<LibraryDTO>((int)HttpStatusCode.OK)]
    [SwaggerOperation(Summary = "LibraryList", OperationId = "LibraryList")]
    public async Task<ActionResult<LibraryDTO>> LibraryListAsync(string userId) => (await libraryService.ListAsync(userId)).ToActionResult();

    /// <summary>Adds a game or changes it.</summary>
    /// <param name="item">The game and its values.</param>
    /// <returns>The entry.</returns>
    [HttpPost]
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType<LibraryEntryDTO>((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    [SwaggerOperation(Summary = "LibrarySave", OperationId = "LibrarySave")]
    public async Task<ActionResult<LibraryEntryDTO>> LibrarySaveAsync(LibrarySaveRequest item) => (await libraryService.SaveAsync(item)).ToActionResult();

    /// <summary>Sets only the status (adds the game when it isn't there).</summary>
    /// <param name="item">The game and its status.</param>
    /// <returns>The entry.</returns>
    [HttpPost("status")]
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType<LibraryEntryDTO>((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    [SwaggerOperation(Summary = "LibraryStatus", OperationId = "LibraryStatus")]
    public async Task<ActionResult<LibraryEntryDTO>> LibraryStatusAsync(LibraryStatusRequest item) => (await libraryService.SetStatusAsync(item)).ToActionResult();

    /// <summary>Takes a game out of the library.</summary>
    /// <param name="userId">The account.</param>
    /// <param name="gameId">The game.</param>
    /// <returns>The game id.</returns>
    [HttpDelete]
    [ProducesResponseType<int>((int)HttpStatusCode.OK)]
    [SwaggerOperation(Summary = "LibraryDelete", OperationId = "LibraryDelete")]
    public async Task<ActionResult<int>> LibraryDeleteAsync(string userId, int gameId) => (await libraryService.DeleteAsync(userId, gameId)).ToActionResult();

    /// <summary>The home page.</summary>
    /// <param name="userId">The account.</param>
    /// <returns>The home page.</returns>
    [HttpGet("home")]
    [ProducesResponseType<HomeDTO>((int)HttpStatusCode.OK)]
    [SwaggerOperation(Summary = "LibraryHome", OperationId = "LibraryHome")]
    public async Task<ActionResult<HomeDTO>> LibraryHomeAsync(string userId) => (await libraryService.HomeAsync(userId)).ToActionResult();

    /// <summary>Sets the price under which a wishlisted game raises an alert.</summary>
    /// <param name="item">The account, the game and the price.</param>
    /// <returns>The price now.</returns>
    [HttpPost("price-target")]
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType<PriceTargetRequest>((int)HttpStatusCode.OK)]
    [SwaggerOperation(Summary = "LibraryPriceTarget", OperationId = "LibraryPriceTarget")]
    public async Task<ActionResult<PriceTargetRequest>> LibraryPriceTargetAsync(PriceTargetRequest item) => (await libraryService.SetPriceTargetAsync(item)).ToActionResult();
}
