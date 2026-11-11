using Ludex.Library.Enums;
using Ludex.WebApi.Features.Games.Models.DTOs;

namespace Ludex.WebApi.Features.Steam.Models.DTOs;

/// <summary>A Steam library ready to import: each game matched to the catalog, with a suggested status.</summary>
public sealed class SteamPreviewDTO
{
    /// <summary>Gets or sets a value indicating whether the import can run here (a Steam key and IGDB are set).</summary>
    public bool Available { get; set; }

    /// <summary>Gets or sets why it can't run, or why this profile's games can't be read.</summary>
    public string? Problem { get; set; }

    /// <summary>Gets or sets the SteamID64 read (null: nothing read yet).</summary>
    public string? SteamId { get; set; }

    /// <summary>Gets or sets the Steam profile's name.</summary>
    public string? PersonaName { get; set; }

    /// <summary>Gets or sets the Steam profile's picture.</summary>
    public string? AvatarUrl { get; set; }

    /// <summary>Gets or sets the Steam profile's address.</summary>
    public string? ProfileUrl { get; set; }

    /// <summary>Gets or sets how many games the Steam account owns.</summary>
    public int OwnedCount { get; set; }

    /// <summary>Gets or sets the games matched to the catalog, the ones played last first.</summary>
    public List<SteamGameDTO> Games { get; set; } = [];

    /// <summary>Gets or sets the Steam names the catalog doesn't know (tools, soundtracks, demos…).</summary>
    public List<string> Unmatched { get; set; } = [];
}

/// <summary>One Steam game, matched to the catalog.</summary>
public sealed class SteamGameDTO
{
    /// <summary>Gets or sets the Steam app id.</summary>
    public int AppId { get; set; }

    /// <summary>Gets or sets the game (MyStatus: where it is in the library already).</summary>
    public GameSummaryDTO Game { get; set; } = new();

    /// <summary>Gets or sets the hours played on Steam.</summary>
    public decimal Hours { get; set; }

    /// <summary>Gets or sets the hours played on Steam in the last two weeks.</summary>
    public decimal RecentHours { get; set; }

    /// <summary>Gets or sets when it was last played on Steam.</summary>
    public DateTimeOffset? LastPlayedAt { get; set; }

    /// <summary>Gets or sets the hours in the library now (null: none, or not in the library).</summary>
    public decimal? LibraryHours { get; set; }

    /// <summary>Gets or sets the status suggested for a game that isn't in the library.</summary>
    public GameStatus SuggestedStatus { get; set; }

    /// <summary>Gets or sets a value indicating whether it starts ticked (new and played, or more hours than the library has).</summary>
    public bool Selected { get; set; }
}

/// <summary>Imports the games ticked.</summary>
public sealed class SteamImportRequest
{
    /// <summary>Gets or sets the account.</summary>
    public string? UserId { get; set; }

    /// <summary>Gets or sets the SteamID64 they came from (remembered for "Sync again").</summary>
    public string? SteamId { get; set; }

    /// <summary>Gets or sets the games.</summary>
    public List<SteamImportGameRequest> Games { get; set; } = [];
}

/// <summary>One game to import.</summary>
public sealed class SteamImportGameRequest
{
    /// <summary>Gets or sets the game (already saved by the preview).</summary>
    public int GameId { get; set; }

    /// <summary>Gets or sets the status (only used when the game isn't in the library).</summary>
    public GameStatus Status { get; set; }

    /// <summary>Gets or sets the hours played on Steam.</summary>
    public decimal? Hours { get; set; }

    /// <summary>Gets or sets when it was last played on Steam.</summary>
    public DateTimeOffset? LastPlayedAt { get; set; }
}

/// <summary>What an import did.</summary>
public sealed class SteamImportResultDTO
{
    /// <summary>Gets or sets how many games were added.</summary>
    public int Added { get; set; }

    /// <summary>Gets or sets how many games got more hours.</summary>
    public int Updated { get; set; }
}
