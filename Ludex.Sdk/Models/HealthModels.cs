using Ludex.Library.Enums;

namespace Ludex.Sdk.Models;

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
