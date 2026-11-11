namespace Ludex.WebApi.Features.Catalog;

/// <summary>
/// Short names for platforms ("PlayStation 5" → "PS5"), as the library cards and filters show them.
/// </summary>
internal static class PlatformNames
{
    private static readonly Dictionary<string, string> Short = new(StringComparer.OrdinalIgnoreCase)
    {
        ["PC (Microsoft Windows)"] = "PC",
        ["Windows"] = "PC",
        ["Mac"] = "Mac",
        ["Linux"] = "Linux",
        ["PlayStation 5"] = "PS5",
        ["PlayStation 4"] = "PS4",
        ["PlayStation 3"] = "PS3",
        ["PlayStation 2"] = "PS2",
        ["PlayStation"] = "PS1",
        ["PlayStation Portable"] = "PSP",
        ["PlayStation Vita"] = "PS Vita",
        ["PlayStation VR2"] = "PS VR2",
        ["Nintendo Switch 2"] = "Switch 2",
        ["Nintendo Switch"] = "Switch",
        ["Wii U"] = "Wii U",
        ["Wii"] = "Wii",
        ["Nintendo GameCube"] = "GameCube",
        ["Nintendo 64"] = "N64",
        ["Super Nintendo Entertainment System"] = "SNES",
        ["Nintendo Entertainment System"] = "NES",
        ["Nintendo 3DS"] = "3DS",
        ["New Nintendo 3DS"] = "3DS",
        ["Nintendo DS"] = "DS",
        ["Game Boy Advance"] = "GBA",
        ["Game Boy Color"] = "GBC",
        ["Game Boy"] = "Game Boy",
        ["Xbox Series X|S"] = "Xbox Series",
        ["Xbox One"] = "Xbox One",
        ["Xbox 360"] = "Xbox 360",
        ["Xbox"] = "Xbox",
        ["iOS"] = "iOS",
        ["Android"] = "Android",
        ["Google Stadia"] = "Stadia",
        ["Meta Quest 3"] = "Quest 3",
        ["Meta Quest 2"] = "Quest 2",
        ["Sega Mega Drive/Genesis"] = "Mega Drive",
        ["Dreamcast"] = "Dreamcast",
    };

    /// <summary>
    /// The short name of a platform.
    /// </summary>
    /// <param name="name">IGDB's name.</param>
    /// <param name="abbreviation">IGDB's abbreviation, when it has one.</param>
    /// <returns>The short name (never contains ';', which separates platforms in the database).</returns>
    public static string Friendly(string? name, string? abbreviation)
    {
        var text = name is not null && Short.TryGetValue(name.Trim(), out var known)
            ? known
            : !string.IsNullOrWhiteSpace(name) ? name.Trim() : (abbreviation ?? string.Empty).Trim();
        return text.Replace(';', ',');
    }

    /// <summary>
    /// The family a platform belongs to, for the library's platform filter.
    /// </summary>
    /// <param name="platform">A short name, or what someone typed ("PS5", "Steam Deck", "Switch").</param>
    /// <returns>PC, PlayStation, Nintendo, Xbox, Mobile or Other.</returns>
    public static string Family(string? platform)
    {
        var text = (platform ?? string.Empty).Trim();
        if (text.Length == 0)
        {
            return "Other";
        }

        bool Has(string part) => text.Contains(part, StringComparison.OrdinalIgnoreCase);
        if (Has("xbox"))
        {
            return "Xbox";
        }

        if (Has("ps") || Has("playstation"))
        {
            return "PlayStation";
        }

        if (Has("switch") || Has("nintendo") || Has("wii") || Has("3ds") || Has("gamecube") || Has("n64") || Has("snes")
            || Has("nes") || Has("game boy") || Has("gba") || Has("gbc") || text.Equals("DS", StringComparison.OrdinalIgnoreCase))
        {
            return "Nintendo";
        }

        if (Has("pc") || Has("windows") || Has("steam") || Has("mac") || Has("linux"))
        {
            return "PC";
        }

        if (Has("ios") || Has("android") || Has("iphone") || Has("ipad") || Has("mobile"))
        {
            return "Mobile";
        }

        return "Other";
    }
}
