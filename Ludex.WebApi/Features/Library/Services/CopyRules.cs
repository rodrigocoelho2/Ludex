using Ludex.Library.Platforms;
using Ludex.WebApi.Features.Catalog;
using Ludex.WebApi.Features.Library.Models.Entities;

namespace Ludex.WebApi.Features.Library.Services;

/// <summary>
/// A game owned on two platforms is two entries, each with its own status, hours and achievements or trophies. When
/// an import brings a game, it goes into the entry of the same platform family (Steam into the PC copy, PlayStation
/// into the PlayStation copy), or the entry whose platform was never said; otherwise it is a new entry.
/// </summary>
internal static class CopyRules
{
    /// <summary>
    /// The family copies are told apart by: PC (Windows, Mac, Linux, Steam Deck), PlayStation, Xbox, Nintendo, Mobile,
    /// or the platform itself.
    /// </summary>
    /// <param name="platform">The platform's short name.</param>
    /// <returns>The family ("" when no platform is said).</returns>
    public static string Family(string? platform)
    {
        if (string.IsNullOrWhiteSpace(platform))
        {
            return string.Empty;
        }

        if (GameCopies.UsesStores(platform))
        {
            return "PC";
        }

        var family = PlatformNames.Family(platform);
        return family == "Other" ? platform.Trim().ToUpperInvariant() : family;
    }

    /// <summary>
    /// The entry a game from an import goes into.
    /// </summary>
    /// <typeparam name="T">The entries' type.</typeparam>
    /// <param name="copies">The game's entries.</param>
    /// <param name="platform">The platform of the copy imported (null: a Steam game, PC).</param>
    /// <param name="appId">Its Steam app (Steam imports).</param>
    /// <returns>The entry, or null for a new one.</returns>
    public static T? Pick<T>(IEnumerable<T> copies, string? platform, int? appId = null)
        where T : class, ILibraryCopy
    {
        var list = copies.OrderBy(static c => c.EntryID).ToList();
        if (list.Count == 0)
        {
            return null;
        }

        if (appId is { } app && list.FirstOrDefault(c => c.SteamAppID == app) is { } steam)
        {
            return steam;
        }

        var wanted = platform is { Length: > 0 } ? platform.Trim() : "PC";
        var family = Family(wanted);
        var same = list.Where(c => Family(c.Platform) == family).ToList();
        return same.FirstOrDefault(c => string.Equals(c.Platform?.Trim(), wanted, StringComparison.OrdinalIgnoreCase))
               ?? same.FirstOrDefault()
               ?? list.FirstOrDefault(static c => string.IsNullOrWhiteSpace(c.Platform));
    }
}
