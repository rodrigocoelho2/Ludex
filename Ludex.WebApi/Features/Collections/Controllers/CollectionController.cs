using System.Net;
using System.Net.Mime;
using Ludex.Library.Validation;
using Ludex.WebApi.Extensions.Mvc;
using Ludex.WebApi.Extensions.Services;
using Ludex.WebApi.Features.Collections.Services;
using Ludex.WebApi.Features.Library.Models.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Ludex.WebApi.Features.Collections.Controllers;

/// <summary>
/// An account's collections.
/// </summary>
/// <param name="collectionService">Collections.</param>
[Authorize]
[Route("collections")]
[ApiController]
[Produces(MediaTypeNames.Application.Json)]
[ProducesErrorResponseType(typeof(ApiValidationProblemDetails))]
[ProducesResponseType((int)HttpStatusCode.BadRequest)]
[ProducesResponseType((int)HttpStatusCode.Unauthorized)]
internal sealed class CollectionController(ICollectionService collectionService) : InternalControllerBase
{
    /// <summary>The collections.</summary>
    /// <param name="userId">The account.</param>
    /// <returns>The collections.</returns>
    [HttpGet]
    [ProducesResponseType<List<CollectionDTO>>((int)HttpStatusCode.OK)]
    [SwaggerOperation(Summary = "CollectionsList", OperationId = "CollectionsList")]
    public async Task<ActionResult<List<CollectionDTO>>> CollectionsListAsync(string userId) => (await collectionService.ListAsync(userId)).ToActionResult();

    /// <summary>Creates or renames a collection.</summary>
    /// <param name="item">The account, the collection and the name.</param>
    /// <returns>The collections.</returns>
    [HttpPost("save")]
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType<List<CollectionDTO>>((int)HttpStatusCode.OK)]
    [SwaggerOperation(Summary = "CollectionsSave", OperationId = "CollectionsSave")]
    public async Task<ActionResult<List<CollectionDTO>>> CollectionsSaveAsync(CollectionSaveRequest item) => (await collectionService.SaveAsync(item)).ToActionResult();

    /// <summary>Deletes a collection.</summary>
    /// <param name="item">The account and the collection.</param>
    /// <returns>The collections.</returns>
    [HttpPost("delete")]
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType<List<CollectionDTO>>((int)HttpStatusCode.OK)]
    [SwaggerOperation(Summary = "CollectionsDelete", OperationId = "CollectionsDelete")]
    public async Task<ActionResult<List<CollectionDTO>>> CollectionsDeleteAsync(CollectionDeleteRequest item) => (await collectionService.DeleteAsync(item)).ToActionResult();

    /// <summary>Puts a game in a collection or takes it out.</summary>
    /// <param name="item">The account, the collection and the game.</param>
    /// <returns>The collections.</returns>
    [HttpPost("game")]
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType<List<CollectionDTO>>((int)HttpStatusCode.OK)]
    [SwaggerOperation(Summary = "CollectionsGame", OperationId = "CollectionsGame")]
    public async Task<ActionResult<List<CollectionDTO>>> CollectionsGameAsync(CollectionGameRequest item) => (await collectionService.SetGameAsync(item)).ToActionResult();
}
