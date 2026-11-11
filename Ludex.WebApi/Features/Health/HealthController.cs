using System.Net;
using Ludex.Library.Enums;
using Ludex.WebApi.Extensions.Mvc;
using Ludex.WebApi.Features.Catalog;
using Ludex.WebApi.Features.Users.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Ludex.WebApi.Features.Health;

/// <summary>Whether the WebApi and its database answer, and which catalog is in use.</summary>
public sealed class HealthDTO
{
    /// <summary>Gets or sets a value indicating whether everything works.</summary>
    public bool Ok { get; set; }

    /// <summary>Gets or sets the catalog in use (IGDB, or the built-in list).</summary>
    public CatalogSource Source { get; set; }

    /// <summary>Gets or sets what went wrong, when something did.</summary>
    public string? Problem { get; set; }
}

/// <summary>Health check for the Website and Docker.</summary>
/// <param name="userRepository">Accounts (a cheap database call).</param>
/// <param name="catalog">The catalog in use.</param>
[Authorize]
[Route("health")]
[ApiController]
internal sealed class HealthController(IUserRepository userRepository, IGameCatalog catalog) : InternalControllerBase
{
    /// <summary>Checks the WebApi and its database.</summary>
    /// <returns>The health.</returns>
    [HttpGet]
    [ProducesResponseType<HealthDTO>((int)HttpStatusCode.OK)]
    [SwaggerOperation(Summary = "HealthGet", OperationId = "HealthGet")]
    public async Task<ActionResult<HealthDTO>> HealthGetAsync()
    {
        try
        {
            await userRepository.CountAsync();
            return Ok(new HealthDTO { Ok = true, Source = catalog.Source });
        }
        catch (Exception exception) when (exception is Microsoft.Data.SqlClient.SqlException or InvalidOperationException)
        {
            return Ok(new HealthDTO { Ok = false, Source = catalog.Source, Problem = "The database doesn't answer: " + exception.Message });
        }
    }
}
