using System.Net;
using System.Net.Mime;
using Ludex.Library.Validation;
using Ludex.WebApi.Extensions.Mvc;
using Ludex.WebApi.Extensions.Services;
using Ludex.WebApi.Features.Imports.Models.DTOs;
using Ludex.WebApi.Features.Psn.Models.DTOs;
using Ludex.WebApi.Features.Psn.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Ludex.WebApi.Features.Psn.Controllers;

/// <summary>
/// The PlayStation connection: connect, import, sync, disconnect.
/// </summary>
/// <param name="psnService">The connection.</param>
[Authorize]
[Route("psn")]
[ApiController]
[Produces(MediaTypeNames.Application.Json)]
[ProducesErrorResponseType(typeof(ApiValidationProblemDetails))]
[ProducesResponseType((int)HttpStatusCode.BadRequest)]
[ProducesResponseType((int)HttpStatusCode.Unauthorized)]
internal sealed class PsnController(IPsnService psnService) : InternalControllerBase
{
    /// <summary>The account's connection.</summary>
    /// <param name="userId">The account.</param>
    /// <returns>The connection.</returns>
    [HttpGet]
    [ProducesResponseType<PsnStatusDTO>((int)HttpStatusCode.OK)]
    [SwaggerOperation(Summary = "PsnStatus", OperationId = "PsnStatus")]
    public async Task<ActionResult<PsnStatusDTO>> PsnStatusAsync(string userId) => (await psnService.StatusAsync(userId)).ToActionResult();

    /// <summary>Connects PlayStation with an NPSSO.</summary>
    /// <param name="item">The account and the NPSSO.</param>
    /// <returns>The connection.</returns>
    [HttpPost("connect")]
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType<PsnStatusDTO>((int)HttpStatusCode.OK)]
    [SwaggerOperation(Summary = "PsnConnect", OperationId = "PsnConnect")]
    public async Task<ActionResult<PsnStatusDTO>> PsnConnectAsync(PsnConnectRequest item) => (await psnService.ConnectAsync(item)).ToActionResult();

    /// <summary>Reads the games played and matches them to the catalog.</summary>
    /// <param name="userId">The account.</param>
    /// <returns>The games with a suggested status.</returns>
    [HttpGet("preview")]
    [ProducesResponseType<PsnPreviewDTO>((int)HttpStatusCode.OK)]
    [SwaggerOperation(Summary = "PsnPreview", OperationId = "PsnPreview")]
    public async Task<ActionResult<PsnPreviewDTO>> PsnPreviewAsync(string userId) => (await psnService.PreviewAsync(userId)).ToActionResult();

    /// <summary>Adds the games ticked.</summary>
    /// <param name="item">The account and the games.</param>
    /// <returns>How many were added and updated.</returns>
    [HttpPost("import")]
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType<ImportResultDTO>((int)HttpStatusCode.OK)]
    [SwaggerOperation(Summary = "PsnImport", OperationId = "PsnImport")]
    public async Task<ActionResult<ImportResultDTO>> PsnImportAsync(PsnImportRequest item) => (await psnService.ImportAsync(item)).ToActionResult();

    /// <summary>Syncs the library with PlayStation now (the daily job does it on its own).</summary>
    /// <param name="item">The account.</param>
    /// <returns>What changed.</returns>
    [HttpPost("sync")]
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType<ImportResultDTO>((int)HttpStatusCode.OK)]
    [SwaggerOperation(Summary = "PsnSync", OperationId = "PsnSync")]
    public async Task<ActionResult<ImportResultDTO>> PsnSyncAsync(PsnAccountRequest item) => (await psnService.SyncNowAsync(item)).ToActionResult();

    /// <summary>The account's trophies in a game it played on PlayStation.</summary>
    /// <param name="userId">The account.</param>
    /// <param name="gameId">The game.</param>
    /// <returns>The trophies (404: no trophy list known).</returns>
    [HttpGet("trophies")]
    [ProducesResponseType<PsnTrophiesDTO>((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    [SwaggerOperation(Summary = "PsnTrophies", OperationId = "PsnTrophies")]
    public async Task<ActionResult<PsnTrophiesDTO>> PsnTrophiesAsync(string userId, int gameId) => (await psnService.TrophiesAsync(userId, gameId)).ToActionResult();

    /// <summary>Disconnects PlayStation (the games imported stay).</summary>
    /// <param name="item">The account.</param>
    /// <returns>The connection (none).</returns>
    [HttpPost("disconnect")]
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType<PsnStatusDTO>((int)HttpStatusCode.OK)]
    [SwaggerOperation(Summary = "PsnDisconnect", OperationId = "PsnDisconnect")]
    public async Task<ActionResult<PsnStatusDTO>> PsnDisconnectAsync(PsnAccountRequest item) => (await psnService.DisconnectAsync(item)).ToActionResult();
}
