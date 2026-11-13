using Ludex.Library.Enums;
using Ludex.WebApi.Features.Games.Models.DTOs;

namespace Ludex.WebApi.Features.Imports.Models.DTOs;

/// <summary>One game found in another library (Playnite, PlayStation), matched to the catalog.</summary>
public sealed class ImportGameDTO
{
    /// <summary>Gets or sets the game's id in the other library (PlayStation: its title; null when it has none).</summary>
    public string? Key { get; set; }

    /// <summary>Gets or sets the name in the other library.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets where it came from there ("Epic", "GOG", "PS5"…).</summary>
    public string? Source { get; set; }

    /// <summary>Gets or sets the game (MyStatus: where it is in the library already).</summary>
    public GameSummaryDTO Game { get; set; } = new();

    /// <summary>Gets or sets the platform of the copy (Ludex's short name: "PC", "PS5"…).</summary>
    public string Platform { get; set; } = string.Empty;

    /// <summary>Gets or sets the PC store of the copy (null on consoles).</summary>
    public string? Store { get; set; }

    /// <summary>Gets or sets physical or digital (null: unknown).</summary>
    public GameFormat? Format { get; set; }

    /// <summary>Gets or sets the hours played there.</summary>
    public decimal Hours { get; set; }

    /// <summary>Gets or sets when it was last played there.</summary>
    public DateTimeOffset? LastPlayedAt { get; set; }

    /// <summary>Gets or sets the hours in the library now (null: none, or not in the library).</summary>
    public decimal? LibraryHours { get; set; }

    /// <summary>Gets or sets the status suggested for a game that isn't in the library.</summary>
    public GameStatus SuggestedStatus { get; set; }

    /// <summary>Gets or sets a value indicating whether it starts ticked (new and played, or more hours than the library has).</summary>
    public bool Selected { get; set; }

    /// <summary>Gets or sets the trophies earned (PlayStation; null: unknown).</summary>
    public int? TrophiesEarned { get; set; }

    /// <summary>Gets or sets the trophies the game has (PlayStation; null: unknown).</summary>
    public int? TrophiesTotal { get; set; }
}

/// <summary>One game to import from another library.</summary>
public sealed class ImportGameRequest
{
    /// <summary>Gets or sets the game's id in the other library (PlayStation: its title, remembered for the daily sync).</summary>
    public string? Key { get; set; }

    /// <summary>Gets or sets the game (already saved by the preview).</summary>
    public int GameId { get; set; }

    /// <summary>Gets or sets the status (only used when the game isn't in the library).</summary>
    public GameStatus Status { get; set; }

    /// <summary>Gets or sets the hours played there.</summary>
    public decimal? Hours { get; set; }

    /// <summary>Gets or sets when it was last played there.</summary>
    public DateTimeOffset? LastPlayedAt { get; set; }

    /// <summary>Gets or sets the platform of the copy.</summary>
    public string? Platform { get; set; }

    /// <summary>Gets or sets the PC store of the copy.</summary>
    public string? Store { get; set; }

    /// <summary>Gets or sets physical or digital.</summary>
    public GameFormat? Format { get; set; }

    /// <summary>Gets or sets the trophies earned (PlayStation).</summary>
    public int? TrophiesEarned { get; set; }

    /// <summary>Gets or sets the trophies the game has (PlayStation).</summary>
    public int? TrophiesTotal { get; set; }
}

/// <summary>What an import did.</summary>
public sealed class ImportResultDTO
{
    /// <summary>Gets or sets how many games were added.</summary>
    public int Added { get; set; }

    /// <summary>Gets or sets how many games got more hours (or their copy, or trophies).</summary>
    public int Updated { get; set; }
}
