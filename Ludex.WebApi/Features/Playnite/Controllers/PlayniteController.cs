using System.Net;
using System.Net.Mime;
using Ludex.Library.Validation;
using Ludex.WebApi.Extensions.Mvc;
using Ludex.WebApi.Extensions.Services;
using Ludex.WebApi.Features.Imports.Models.DTOs;
using Ludex.WebApi.Features.Playnite.Models.DTOs;
using Ludex.WebApi.Features.Playnite.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Ludex.WebApi.Features.Playnite.Controllers;

/// <summary>
/// Importing a Playnite library (the CSV of Library Exporter Advanced).
/// </summary>
/// <param name="playniteService">The import.</param>
[Authorize]
[Route("playnite")]
[ApiController]
[Produces(MediaTypeNames.Application.Json)]
[ProducesErrorResponseType(typeof(ApiValidationProblemDetails))]
[ProducesResponseType((int)HttpStatusCode.BadRequest)]
[ProducesResponseType((int)HttpStatusCode.Unauthorized)]
internal sealed class PlayniteController(IPlayniteService playniteService) : InternalControllerBase
{
    /// <summary>Reads Playnite's export and matches it to the catalog.</summary>
    /// <param name="item">The account and the CSV.</param>
    /// <returns>The games with a suggested status.</returns>
    [HttpPost("preview")]
    [Consumes(MediaTypeNames.Application.Json)]
    [RequestSizeLimit(32 * 1024 * 1024)]
    [ProducesResponseType<PlaynitePreviewDTO>((int)HttpStatusCode.OK)]
    [SwaggerOperation(Summary = "PlaynitePreview", OperationId = "PlaynitePreview")]
    public async Task<ActionResult<PlaynitePreviewDTO>> PlaynitePreviewAsync(PlaynitePreviewRequest item) =>
        (await playniteService.PreviewAsync(item)).ToActionResult();

    /// <summary>Adds the games ticked.</summary>
    /// <param name="item">The account and the games.</param>
    /// <returns>How many were added and updated.</returns>
    [HttpPost("import")]
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType<ImportResultDTO>((int)HttpStatusCode.OK)]
    [SwaggerOperation(Summary = "PlayniteImport", OperationId = "PlayniteImport")]
    public async Task<ActionResult<ImportResultDTO>> PlayniteImportAsync(PlayniteImportRequest item) =>
        (await playniteService.ImportAsync(item)).ToActionResult();
}
