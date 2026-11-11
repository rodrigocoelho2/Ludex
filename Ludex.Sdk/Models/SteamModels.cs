using Ludex.Library.Enums;

namespace Ludex.Sdk.Models;

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

    /// <summary>Gets or sets when the library was last synced with Steam (null: never).</summary>
    public DateTimeOffset? SyncedAt { get; set; }
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

    /// <summary>Gets or sets its Steam app.</summary>
    public int? AppId { get; set; }

    /// <summary>Gets or sets the hours played on Steam in the last two weeks.</summary>
    public decimal? RecentHours { get; set; }
}

/// <summary>What an import did.</summary>
public sealed class SteamImportResultDTO
{
    /// <summary>Gets or sets how many games were added.</summary>
    public int Added { get; set; }

    /// <summary>Gets or sets how many games got more hours.</summary>
    public int Updated { get; set; }
}

/// <summary>Syncs an account's library with Steam now.</summary>
public sealed class SteamSyncRequest
{
    /// <summary>Gets or sets the account.</summary>
    public string? UserId { get; set; }
}

/// <summary>What a sync did.</summary>
public sealed class SteamSyncResultDTO
{
    /// <summary>Gets or sets how many games bought since were added (as Backlog, or by how they were played).</summary>
    public int Added { get; set; }

    /// <summary>Gets or sets how many games got more hours.</summary>
    public int Updated { get; set; }

    /// <summary>Gets or sets how many games had their achievements read.</summary>
    public int Achievements { get; set; }
}

/// <summary>An account's Steam achievements in a game.</summary>
public sealed class AchievementsDTO
{
    /// <summary>Gets or sets the Steam app.</summary>
    public int AppId { get; set; }

    /// <summary>Gets or sets how many are unlocked.</summary>
    public int Unlocked { get; set; }

    /// <summary>Gets or sets how many the game has.</summary>
    public int Total { get; set; }

    /// <summary>Gets or sets a value indicating whether Steam wouldn't say (private game details).</summary>
    public bool Private { get; set; }

    /// <summary>Gets or sets the achievements: unlocked first (latest first), then the rarest still locked.</summary>
    public List<AchievementDTO> Items { get; set; } = [];
}

/// <summary>One achievement.</summary>
public sealed class AchievementDTO
{
    /// <summary>Gets or sets the name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets what it takes (null for a hidden one still locked).</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets the icon (the grey one while locked).</summary>
    public string? IconUrl { get; set; }

    /// <summary>Gets or sets a value indicating whether it is unlocked.</summary>
    public bool Unlocked { get; set; }

    /// <summary>Gets or sets when it was unlocked.</summary>
    public DateTimeOffset? UnlockedAt { get; set; }

    /// <summary>Gets or sets the % of players who have it (null: unknown).</summary>
    public double? GlobalPercent { get; set; }

    /// <summary>Gets or sets a value indicating whether it is hidden until unlocked.</summary>
    public bool Hidden { get; set; }
}
