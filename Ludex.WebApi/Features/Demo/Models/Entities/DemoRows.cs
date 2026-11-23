namespace Ludex.WebApi.Features.Demo.Models.Entities;

/// <summary>How many demo accounts and reviews there are (dbo.DemoStatus).</summary>
public sealed class DemoStatusRow
{
    /// <summary>Gets or sets the demo accounts.</summary>
    public int Accounts { get; set; }

    /// <summary>Gets or sets their reviews.</summary>
    public int Reviews { get; set; }

    /// <summary>Gets or sets the real accounts.</summary>
    public int RealAccounts { get; set; }
}
