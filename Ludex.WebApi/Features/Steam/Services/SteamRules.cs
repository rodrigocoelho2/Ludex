using System.Text.RegularExpressions;
using Ludex.Library.Enums;
using Ludex.WebApi.Features.Steam.Models.DTOs;

namespace Ludex.WebApi.Features.Steam.Services;

/// <summary>
/// The rules of the Steam import, kept apart from Steam so the tests can check them: what a typed profile is, hours
/// from minutes, and the status a game is suggested with.
/// Played in the last 30 days → Playing. Played 2 hours or more, not lately → Played (Steam can't tell if you reached the end). Under 2 hours → Dropped.
/// Never played → Backlog, and left unticked (the daily sync adds games bought later as Backlog).
/// </summary>
internal static partial class SteamRules
{
    /// <summary>Played within this many days: Playing.</summary>
    public const int RecentDays = 30;

    /// <summary>At least this many minutes (not lately): Played; fewer: Dropped.</summary>
    public const int PlayedMinutes = 120;

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
            return (GameStatus.Backlog, false);
        }

        if (lastPlayed is { } at && now - at <= TimeSpan.FromDays(RecentDays))
        {
            return (GameStatus.Playing, true);
        }

        return (minutes >= PlayedMinutes ? GameStatus.Played : GameStatus.Dropped, true);
    }

    /// <summary>
    /// When a game was last played.
    /// </summary>
    /// <param name="unixSeconds">Steam's rtime_last_played.</param>
    /// <returns>The time, or null for never.</returns>
    public static DateTimeOffset? LastPlayed(long? unixSeconds) => unixSeconds is > 0 ? DateTimeOffset.FromUnixTimeSeconds(unixSeconds.Value) : null;

    /// <summary>
    /// A game's achievements as the game page shows them: names and icons from the game, unlocked from the account,
    /// rarity from everyone. Unlocked first (latest first), then the locked ones, most common first.
    /// </summary>
    /// <param name="appId">The Steam app.</param>
    /// <param name="schema">The game's achievements.</param>
    /// <param name="mine">The account's (null: Steam wouldn't say).</param>
    /// <param name="global">The % of players per achievement.</param>
    /// <returns>The achievements.</returns>
    public static AchievementsDTO Achievements(
        int appId,
        IReadOnlyCollection<SteamSchemaAchievement> schema,
        IReadOnlyCollection<SteamPlayerAchievement>? mine,
        IReadOnlyDictionary<string, double> global)
    {
        var unlocked = (mine ?? [])
            .Where(static a => a.Achieved == 1 && a.ApiName is not null)
            .GroupBy(static a => a.ApiName!, StringComparer.Ordinal)
            .ToDictionary(static g => g.Key, static g => g.First().UnlockTime, StringComparer.Ordinal);
        var items = schema.Where(static a => a.Name is not null).Select(a =>
        {
            var has = unlocked.TryGetValue(a.Name!, out var at);
            var hidden = a.Hidden == 1;
            return new AchievementDTO
            {
                Name = a.DisplayName ?? a.Name!,
                Description = has || !hidden ? a.Description : null,
                IconUrl = has ? a.Icon : a.IconGray ?? a.Icon,
                Unlocked = has,
                UnlockedAt = has && at > 0 ? DateTimeOffset.FromUnixTimeSeconds(at) : null,
                GlobalPercent = global.TryGetValue(a.Name!, out var percent) ? Math.Round(percent, 1) : null,
                Hidden = hidden,
            };
        }).ToList();

        return new AchievementsDTO
        {
            AppId = appId,
            Unlocked = items.Count(static i => i.Unlocked),
            Total = items.Count,
            Private = mine is null,
            Items = [.. items
                .OrderByDescending(static i => i.Unlocked)
                .ThenByDescending(static i => i.UnlockedAt ?? DateTimeOffset.MinValue)
                .ThenByDescending(static i => i.GlobalPercent ?? -1)],
        };
    }

    private static bool IsVanity(string value) =>
        value.Length is >= 2 and <= 64 && value.All(static c => char.IsAsciiLetterOrDigit(c) || c is '_' or '-');

    [GeneratedRegex(@"steamcommunity\.com/(profiles|id)/([^/?#\s]+)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ProfileUrl();
}
