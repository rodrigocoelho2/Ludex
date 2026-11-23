using System.Net;
using System.Net.Mime;
using Ludex.Library.Validation;
using Ludex.WebApi.Extensions.Mvc;
using Ludex.WebApi.Features.Demo.Models.DTOs;
using Ludex.WebApi.Features.Demo.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Ludex.WebApi.Features.Demo.Controllers;

/// <summary>
/// The made-up demo accounts and their reviews (Settings, for the admin).
/// </summary>
/// <param name="seeder">The seeder.</param>
[Authorize]
[Route("demo")]
[ApiController]
[Produces(MediaTypeNames.Application.Json)]
[ProducesErrorResponseType(typeof(ApiValidationProblemDetails))]
[ProducesResponseType((int)HttpStatusCode.Unauthorized)]
internal sealed class DemoController(DemoSeeder seeder) : InternalControllerBase
{
    /// <summary>How many demo accounts and reviews there are.</summary>
    /// <returns>The status.</returns>
    [HttpGet]
    [ProducesResponseType<DemoStatusDTO>((int)HttpStatusCode.OK)]
    [SwaggerOperation(Summary = "DemoStatus", OperationId = "DemoStatus")]
    public async Task<ActionResult<DemoStatusDTO>> DemoStatusAsync() => Ok(await seeder.StatusAsync());

    /// <summary>Adds the demo accounts.</summary>
    /// <returns>The status, with what happened.</returns>
    [HttpPost("seed")]
    [ProducesResponseType<DemoStatusDTO>((int)HttpStatusCode.OK)]
    [SwaggerOperation(Summary = "DemoSeed", OperationId = "DemoSeed")]
    public async Task<ActionResult<DemoStatusDTO>> DemoSeedAsync() => Ok(await seeder.SeedAsync(HttpContext.RequestAborted));

    /// <summary>Removes the demo accounts and everything they wrote.</summary>
    /// <returns>The status, with what happened.</returns>
    [HttpPost("remove")]
    [ProducesResponseType<DemoStatusDTO>((int)HttpStatusCode.OK)]
    [SwaggerOperation(Summary = "DemoRemove", OperationId = "DemoRemove")]
    public async Task<ActionResult<DemoStatusDTO>> DemoRemoveAsync() => Ok(await seeder.RemoveAsync());
}
