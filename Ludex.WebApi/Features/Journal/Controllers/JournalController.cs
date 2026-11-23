using System.Net;
using System.Net.Mime;
using Ludex.Library.Enums;
using Ludex.Library.Validation;
using Ludex.WebApi.Extensions.Mvc;
using Ludex.WebApi.Extensions.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Ludex.WebApi.Features.Journal.Models.DTOs;
using Ludex.WebApi.Features.Journal.Services;

namespace Ludex.WebApi.Features.Journal.Controllers;

/// <summary>
/// The journal.
/// </summary>
/// <param name="journalService">The journal.</param>
[Authorize]
[ApiController]
[Produces(MediaTypeNames.Application.Json)]
[ProducesErrorResponseType(typeof(ApiValidationProblemDetails))]
[ProducesResponseType((int)HttpStatusCode.BadRequest)]
[ProducesResponseType((int)HttpStatusCode.Unauthorized)]
[Route("journal")]
internal sealed class JournalController(IJournalService journalService) : InternalControllerBase
{
    /// <summary>A month of the journal.</summary>
    /// <param name="userId">The account.</param>
    /// <param name="year">The year (0: this one).</param>
    /// <param name="month">The month (0: this one).</param>
    /// <param name="today">Today where the person is.</param>
    /// <returns>The month.</returns>
    [HttpGet]
    [ProducesResponseType<JournalDTO>((int)HttpStatusCode.OK)]
    [SwaggerOperation(Summary = "JournalMonth", OperationId = "JournalMonth")]
    public async Task<ActionResult<JournalDTO>> JournalMonthAsync(string userId, int year = 0, int month = 0, DateOnly? today = null) =>
        (await journalService.MonthAsync(userId, year, month, today)).ToActionResult();

    /// <summary>Writes down a session.</summary>
    /// <param name="item">The session.</param>
    /// <returns>Its id.</returns>
    [HttpPost("session")]
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType<long>((int)HttpStatusCode.OK)]
    [SwaggerOperation(Summary = "JournalAdd", OperationId = "JournalAdd")]
    public async Task<ActionResult<long>> JournalAddAsync(SessionAddRequest item) => (await journalService.AddAsync(item)).ToActionResult();

    /// <summary>Takes a session out.</summary>
    /// <param name="item">The account and the session.</param>
    /// <returns>Its id.</returns>
    [HttpPost("session/delete")]
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType<long>((int)HttpStatusCode.OK)]
    [SwaggerOperation(Summary = "JournalDelete", OperationId = "JournalDelete")]
    public async Task<ActionResult<long>> JournalDeleteAsync(SessionDeleteRequest item) => (await journalService.DeleteAsync(item)).ToActionResult();
}
