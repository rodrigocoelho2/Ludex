using System.Net;
using System.Net.Mime;
using Ludex.Library.Validation;
using Ludex.WebApi.Extensions.Mvc;
using Ludex.WebApi.Extensions.Services;
using Ludex.WebApi.Features.Steam.Models.DTOs;
using Ludex.WebApi.Features.Steam.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Ludex.WebApi.Features.Steam.Controllers;

/// <summary>
/// Importing a Steam library.
/// </summary>
/// <param name="steamService">The import.</param>
[Authorize]
[Route("steam")]
[ApiController]
[Produces(MediaTypeNames.Application.Json)]
[ProducesErrorResponseType(typeof(ApiValidationProblemDetails))]
[ProducesResponseType((int)HttpStatusCode.BadRequest)]
[ProducesResponseType((int)HttpStatusCode.Unauthorized)]
internal sealed class SteamController(ISteamService steamService) : InternalControllerBase
{
    /// <summary>Reads a Steam library and matches it to the catalog.</summary>
    /// <param name="userId">The account.</param>
    /// <param name="profile">The Steam profile (empty: the one imported from last time).</param>
    /// <returns>The games with a suggested status.</returns>
    [HttpGet]
    [ProducesResponseType<SteamPreviewDTO>((int)HttpStatusCode.OK)]
    [SwaggerOperation(Summary = "SteamPreview", OperationId = "SteamPreview")]
    public async Task<ActionResult<SteamPreviewDTO>> SteamPreviewAsync(string userId, string? profile) =>
        (await steamService.PreviewAsync(userId, profile)).ToActionResult();

    /// <summary>Adds the games ticked.</summary>
    /// <param name="item">The account, the Steam account and the games.</param>
    /// <returns>How many were added and updated.</returns>
    [HttpPost("import")]
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType<SteamImportResultDTO>((int)HttpStatusCode.OK)]
    [SwaggerOperation(Summary = "SteamImport", OperationId = "SteamImport")]
    public async Task<ActionResult<SteamImportResultDTO>> SteamImportAsync(SteamImportRequest item) =>
        (await steamService.ImportAsync(item)).ToActionResult();
}
