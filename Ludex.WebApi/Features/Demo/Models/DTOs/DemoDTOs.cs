namespace Ludex.WebApi.Features.Demo.Models.DTOs;

/// <summary>The made-up demo accounts and their reviews.</summary>
public sealed class DemoStatusDTO
{
    /// <summary>Gets or sets how many demo accounts there are.</summary>
    public int Accounts { get; set; }

    /// <summary>Gets or sets how many reviews they wrote.</summary>
    public int Reviews { get; set; }

    /// <summary>Gets or sets a value indicating whether Seed:DemoReviews is on (they come back at the next start).</summary>
    public bool SeedOnStart { get; set; }

    /// <summary>Gets or sets a value indicating whether they can be added now (a real account exists).</summary>
    public bool CanSeed { get; set; }

    /// <summary>Gets or sets what the last add or removal did.</summary>
    public string? Message { get; set; }
}
