using System.Text.RegularExpressions;
using Ludex.Library.Enums;

namespace Ludex.WebApi.Features.Steam.Services;

/// <summary>
/// The rules of the Steam import, kept apart from Steam so the tests can check them: what a typed profile is, hours
/// from minutes, and the status a game is suggested with.
/// Played in the last 30 days → Playing. Played 2 hours or more, not lately → Finished. Under 2 hours → Dropped.
/// Never played → Wishlist, and left unticked.
/// </summary>
internal static partial class SteamRules
{
    /// <summary>Played within this many days: Playing.</summary>
    public const int RecentDays = 30;

    /// <summary>At least this many minutes (not lately): Finished; fewer: Dropped.</summary>
    public const int FinishedMinutes = 120;

    /// <summary>
    /// What was typed as a Steam profile: a SteamID64, a profile address, or a custom profile name.
    /// </summary>
    /// <param name="input">What was typed.</param>
    /// <returns>The SteamID64, or the custom name to look up; both null when it is neither.</returns>
    public static (string? SteamId, string? Vanity) ParseProfile(string? input)
    {
        var text = (input ?? string.Empty).Trim();
        if (text.Length == 0)
        {
            return (null, null);
        }

        var match = ProfileUrl().Match(text);
        if (match.Success)
        {
            var value = match.Groups[2].Value;
            return match.Groups[1].Value.Equals("profiles", StringComparison.OrdinalIgnoreCase)
                ? (IsSteamId(value) ? value : null, null)
                : (null, IsVanity(value) ? value : null);
        }

        if (IsSteamId(text))
        {
            return (text, null);
        }

        return (null, IsVanity(text) ? text : null);
    }

    /// <summary>
    /// Whether a text is a SteamID64 (17 digits starting 7656119).
    /// </summary>
    /// <param name="value">The text.</param>
    /// <returns>True when it is.</returns>
    public static bool IsSteamId(string? value) =>
        value is { Length: 17 } && value.StartsWith("7656119", StringComparison.Ordinal) && value.All(char.IsAsciiDigit);

    /// <summary>
    /// Hours from Steam's minutes, to one decimal.
    /// </summary>
    /// <param name="minutes">The minutes.</param>
    /// <returns>The hours.</returns>
    public static decimal Hours(int minutes) => Math.Round(Math.Max(0, minutes) / 60m, 1, MidpointRounding.AwayFromZero);

    /// <summary>
    /// The status a game is suggested with, and whether it starts ticked.
    /// </summary>
    /// <param name="minutes">Minutes played.</param>
    /// <param name="lastPlayed">When it was last played.</param>
    /// <param name="now">Now.</param>
    /// <returns>The status and whether to import it unless unticked.</returns>
    public static (GameStatus Status, bool Selected) Suggest(int minutes, DateTimeOffset? lastPlayed, DateTimeOffset now)
    {
        if (minutes <= 0)
        {
            return (GameStatus.Wishlist, false);
        }

        if (lastPlayed is { } at && now - at <= TimeSpan.FromDays(RecentDays))
        {
            return (GameStatus.Playing, true);
        }

        return (minutes >= FinishedMinutes ? GameStatus.Finished : GameStatus.Dropped, true);
    }

    /// <summary>
    /// When a game was last played.
    /// </summary>
    /// <param name="unixSeconds">Steam's rtime_last_played.</param>
    /// <returns>The time, or null for never.</returns>
    public static DateTimeOffset? LastPlayed(long? unixSeconds) => unixSeconds is > 0 ? DateTimeOffset.FromUnixTimeSeconds(unixSeconds.Value) : null;

    private static bool IsVanity(string value) =>
        value.Length is >= 2 and <= 64 && value.All(static c => char.IsAsciiLetterOrDigit(c) || c is '_' or '-');

    [GeneratedRegex(@"steamcommunity\.com/(profiles|id)/([^/?#\s]+)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ProfileUrl();
}
