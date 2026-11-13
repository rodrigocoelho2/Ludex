namespace Ludex.Sdk.Models;

/// <summary>Reads Playnite's export and matches it to the catalog.</summary>
public sealed class PlaynitePreviewRequest
{
    /// <summary>Gets or sets the account.</summary>
    public string? UserId { get; set; }

    /// <summary>Gets or sets the CSV that Library Exporter Advanced wrote.</summary>
    public string? Csv { get; set; }
}

/// <summary>A Playnite library ready to import: each game matched to the catalog, with a suggested status.</summary>
public sealed class PlaynitePreviewDTO
{
    /// <summary>Gets or sets a value indicating whether the import can run here (IGDB is set).</summary>
    public bool Available { get; set; }

    /// <summary>Gets or sets why it can't run, or why the file can't be read.</summary>
    public string? Problem { get; set; }

    /// <summary>Gets or sets how many games the file has.</summary>
    public int GameCount { get; set; }

    /// <summary>Gets or sets how many Steam games were left out (the Steam import keeps those).</summary>
    public int SteamSkipped { get; set; }

    /// <summary>Gets or sets the useful columns the export left out ("Time Played", "Completion Status"…).</summary>
    public List<string> Missing { get; set; } = [];

    /// <summary>Gets or sets the games matched to the catalog, the ones played last first.</summary>
    public List<ImportGameDTO> Games { get; set; } = [];

    /// <summary>Gets or sets the names the catalog doesn't know.</summary>
    public List<string> Unmatched { get; set; } = [];
}

/// <summary>Imports the Playnite games ticked.</summary>
public sealed class PlayniteImportRequest
{
    /// <summary>Gets or sets the account.</summary>
    public string? UserId { get; set; }

    /// <summary>Gets or sets the games.</summary>
    public List<ImportGameRequest> Games { get; set; } = [];
}
