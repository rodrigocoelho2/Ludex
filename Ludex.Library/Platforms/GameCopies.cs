using Ludex.Library.Enums;

namespace Ludex.Library.Platforms;

/// <summary>
/// Which copies a platform can have, shared by the WebApi (it checks what's saved) and the Website (it only offers
/// those). A PC copy comes from a store (Steam, Epic Games…); a console copy is physical or digital, and consoles from
/// before digital stores only had discs and cartridges; phones, standalone VR and streaming are digital only.
/// </summary>
public static class GameCopies
{
    /// <summary>The PC stores, in the order offered.</summary>
    public static readonly IReadOnlyList<string> PcStores =
    [
        "Steam", "Epic Games", "GOG", "EA app", "Ubisoft Connect", "Battle.net", "Microsoft Store", "Rockstar", "itch.io", "Other store",
    ];

    // Short names as Ludex writes them (PlatformNames), plus what people type. Compared ignoring case.
    private static readonly HashSet<string> Pc = new(StringComparer.OrdinalIgnoreCase)
    {
        "PC", "Windows", "Mac", "macOS", "Linux", "Steam Deck",
    };

    // No digital store for games (or none worth the name) while these were sold.
    private static readonly HashSet<string> PhysicalOnly = new(StringComparer.OrdinalIgnoreCase)
    {
        "NES", "SNES", "N64", "GameCube", "Game Boy", "GBC", "GBA", "DS", "PS1", "PlayStation", "PS2", "Xbox",
        "Mega Drive", "Genesis", "Master System", "Saturn", "Dreamcast", "Game Gear", "Atari 2600", "Neo Geo",
    };

    // No discs or cartridges.
    private static readonly HashSet<string> DigitalOnly = new(StringComparer.OrdinalIgnoreCase)
    {
        "iOS", "Android", "Stadia", "Quest 2", "Quest 3", "Quest", "Meta Quest", "Apple Arcade",
    };

    /// <summary>
    /// Whether a platform's copies come from a PC store.
    /// </summary>
    /// <param name="platform">The platform's short name.</param>
    /// <returns>True for PC, Mac, Linux and Steam Deck.</returns>
    public static bool UsesStores(string? platform) => platform is not null && Pc.Contains(platform.Trim());

    /// <summary>
    /// The formats a platform can have (a PC copy is digital, from its store).
    /// </summary>
    /// <param name="platform">The platform's short name (null or unknown: both).</param>
    /// <returns>The formats, physical first.</returns>
    public static IReadOnlyList<GameFormat> Formats(string? platform)
    {
        var name = platform?.Trim() ?? string.Empty;
        if (Pc.Contains(name) || DigitalOnly.Contains(name))
        {
            return [GameFormat.Digital];
        }

        return PhysicalOnly.Contains(name) ? [GameFormat.Physical] : [GameFormat.Physical, GameFormat.Digital];
    }

    /// <summary>
    /// The PC store as Ludex spells it.
    /// </summary>
    /// <param name="store">What was sent ("steam", "Epic Games"…).</param>
    /// <returns>The store, or null when it isn't one.</returns>
    public static string? Store(string? store) =>
        store is null ? null : PcStores.FirstOrDefault(s => s.Equals(store.Trim(), StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// The choices of copy for a platform, as the game page offers them: the stores for a PC, otherwise the formats.
    /// </summary>
    /// <param name="platform">The platform's short name.</param>
    /// <returns>"Steam", "Epic Games"… or "Physical", "Digital".</returns>
    public static IReadOnlyList<string> Choices(string? platform) =>
        UsesStores(platform) ? PcStores : [.. Formats(platform).Select(static f => f.ToString())];

    /// <summary>
    /// Checks and completes a copy: a store only on a PC (and then the copy is digital); a format the platform had; a
    /// platform with a single format gets it without asking.
    /// </summary>
    /// <param name="platform">The platform's short name.</param>
    /// <param name="store">The store sent.</param>
    /// <param name="format">The format sent.</param>
    /// <returns>The store and format to save, or the problem ("store" or "format").</returns>
    public static (string? Store, GameFormat? Format, string? Problem) Normalize(string? platform, string? store, GameFormat? format)
    {
        var known = Store(store);
        if (store is { Length: > 0 } && known is null)
        {
            return (null, null, "store");
        }

        if (known is not null && !UsesStores(platform))
        {
            return (null, null, "store");
        }

        if (format is { } f && (!Enum.IsDefined(f) || !Formats(platform).Contains(f)))
        {
            return (null, null, "format");
        }

        if (known is not null)
        {
            return (known, GameFormat.Digital, null);
        }

        if (format is null && !string.IsNullOrWhiteSpace(platform) && !UsesStores(platform) && Formats(platform) is [var only])
        {
            return (null, only, null);
        }

        return (null, format, null);
    }

    /// <summary>
    /// How a copy reads ("Steam", "Physical", "Digital").
    /// </summary>
    /// <param name="store">The store.</param>
    /// <param name="format">The format.</param>
    /// <returns>The words, or null when nothing is known.</returns>
    public static string? Label(string? store, GameFormat? format) => store ?? format?.ToString();
}
