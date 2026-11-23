using System.Net;
using System.Net.Mime;
using Ludex.Library.Enums;
using Ludex.Library.Validation;
using Ludex.WebApi.Extensions.Mvc;
using Ludex.WebApi.Extensions.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Ludex.WebApi.Features.Log.Models.DTOs;
using Ludex.WebApi.Features.Log.Services;

namespace Ludex.WebApi.Features.Log.Controllers;

/// <summary>
/// "Log a game".
/// </summary>
/// <param name="logService">The log.</param>
[Authorize]
[ApiController]
[Produces(MediaTypeNames.Application.Json)]
[ProducesErrorResponseType(typeof(ApiValidationProblemDetails))]
[ProducesResponseType((int)HttpStatusCode.BadRequest)]
[ProducesResponseType((int)HttpStatusCode.Unauthorized)]
[Route("log")]
internal sealed class LogController(ILogService logService) : InternalControllerBase
{
    /// <summary>What the form opens with.</summary>
    /// <param name="userId">The account.</param>
    /// <param name="gameId">The game.</param>
    /// <param name="entryId">The copy.</param>
    /// <returns>The form.</returns>
    [HttpGet]
    [ProducesResponseType<LogFormDTO>((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    [SwaggerOperation(Summary = "LogForm", OperationId = "LogForm")]
    public async Task<ActionResult<LogFormDTO>> LogFormAsync(string userId, int gameId, long? entryId = null) => (await logService.FormAsync(userId, gameId, entryId)).ToActionResult();

    /// <summary>Saves the form.</summary>
    /// <param name="item">Everything on it.</param>
    /// <returns>What was saved.</returns>
    [HttpPost]
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType<LogResultDTO>((int)HttpStatusCode.OK)]
    [SwaggerOperation(Summary = "LogSave", OperationId = "LogSave")]
    public async Task<ActionResult<LogResultDTO>> LogSaveAsync(LogSaveRequest item) => (await logService.SaveAsync(item)).ToActionResult();
}
