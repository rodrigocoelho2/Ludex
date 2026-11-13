namespace Ludex.Sdk.Models;

/// <summary>An account's PlayStation connection.</summary>
public sealed class PsnStatusDTO
{
    /// <summary>Gets or sets a value indicating whether PlayStation can be connected here (IGDB is set).</summary>
    public bool Available { get; set; }

    /// <summary>Gets or sets why it can't, or what went wrong.</summary>
    public string? Problem { get; set; }

    /// <summary>Gets or sets a value indicating whether an account is connected and working.</summary>
    public bool Connected { get; set; }

    /// <summary>Gets or sets a value indicating whether the connection broke (connect again with a new NPSSO).</summary>
    public bool Broken { get; set; }

    /// <summary>Gets or sets the PSN name.</summary>
    public string? OnlineId { get; set; }

    /// <summary>Gets or sets when it was connected.</summary>
    public DateTimeOffset? ConnectedAt { get; set; }

    /// <summary>Gets or sets when the library was last synced (null: not imported yet).</summary>
    public DateTimeOffset? SyncedAt { get; set; }

    /// <summary>Gets or sets when the sign-in runs out (a new NPSSO is needed then).</summary>
    public DateTimeOffset? ExpiresAt { get; set; }
}

/// <summary>Connects PlayStation with an NPSSO.</summary>
public sealed class PsnConnectRequest
{
    /// <summary>Gets or sets the account.</summary>
    public string? UserId { get; set; }

    /// <summary>Gets or sets the NPSSO (or the whole page that shows it).</summary>
    public string? Npsso { get; set; }
}

/// <summary>An account's PlayStation (sync now, disconnect).</summary>
public sealed class PsnAccountRequest
{
    /// <summary>Gets or sets the account.</summary>
    public string? UserId { get; set; }
}

/// <summary>A PlayStation library ready to import: each game matched to the catalog, with a suggested status.</summary>
public sealed class PsnPreviewDTO
{
    /// <summary>Gets or sets the connection.</summary>
    public PsnStatusDTO Status { get; set; } = new();

    /// <summary>Gets or sets how many games PlayStation listed.</summary>
    public int GameCount { get; set; }

    /// <summary>Gets or sets the games matched to the catalog, the ones played last first.</summary>
    public List<ImportGameDTO> Games { get; set; } = [];

    /// <summary>Gets or sets the names the catalog doesn't know.</summary>
    public List<string> Unmatched { get; set; } = [];
}

/// <summary>Imports the PlayStation games ticked.</summary>
public sealed class PsnImportRequest
{
    /// <summary>Gets or sets the account.</summary>
    public string? UserId { get; set; }

    /// <summary>Gets or sets the games.</summary>
    public List<ImportGameRequest> Games { get; set; } = [];
}

/// <summary>An account's trophies in a game it played on PlayStation.</summary>
public sealed class PsnTrophiesDTO
{
    /// <summary>Gets or sets how many are earned.</summary>
    public int Earned { get; set; }

    /// <summary>Gets or sets how many the game has.</summary>
    public int Total { get; set; }

    /// <summary>Gets or sets the trophies: earned first (latest first), then the rest, the most earned first.</summary>
    public List<PsnTrophyDTO> Items { get; set; } = [];
}

/// <summary>One trophy.</summary>
public sealed class PsnTrophyDTO
{
    /// <summary>Gets or sets the name ("Hidden trophy" for a hidden one still locked).</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets what it takes (null for a hidden one still locked).</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets the icon.</summary>
    public string? IconUrl { get; set; }

    /// <summary>Gets or sets the grade: Bronze, Silver, Gold or Platinum.</summary>
    public string Grade { get; set; } = "Bronze";

    /// <summary>Gets or sets a value indicating whether it is earned.</summary>
    public bool Earned { get; set; }

    /// <summary>Gets or sets when it was earned.</summary>
    public DateTimeOffset? EarnedAt { get; set; }

    /// <summary>Gets or sets the % of players who have it (null: unknown).</summary>
    public double? Rarity { get; set; }

    /// <summary>Gets or sets a value indicating whether it is hidden until earned.</summary>
    public bool Hidden { get; set; }
}
