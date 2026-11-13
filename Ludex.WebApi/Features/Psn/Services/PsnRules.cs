using System.Globalization;
using System.Text.RegularExpressions;
using Ludex.Library.Enums;
using Ludex.WebApi.Features.Catalog;
using Ludex.WebApi.Features.Psn.Models.DTOs;

namespace Ludex.WebApi.Features.Psn.Services;

/// <summary>A PlayStation game: its versions played and its trophies, put together.</summary>
/// <param name="Key">The title's key (the concept, or the trophy list, or the title id).</param>
/// <param name="Name">The name.</param>
/// <param name="Platform">PS5, PS4, PS3 or PS Vita.</param>
/// <param name="Minutes">Minutes played (PS4 and PS5 only).</param>
/// <param name="LastPlayed">When it was last played (or last earned a trophy).</param>
/// <param name="TrophiesEarned">Trophies earned (null: no trophy list found).</param>
/// <param name="TrophiesTotal">Trophies the game has.</param>
/// <param name="Platinum">Whether the platinum is earned.</param>
/// <param name="TrophyList">Its trophy list and the service to read it from ("NPWR12345_00:trophy2"; null: none found).</param>
internal sealed record PsnGame(string Key, string Name, string Platform, int Minutes, DateTimeOffset? LastPlayed, int? TrophiesEarned, int? TrophiesTotal, bool Platinum, string? TrophyList = null);

/// <summary>
/// How PlayStation's answers become Ludex's: time played ("PT228H56M33S"), the platform, one game per concept, and
/// trophies matched to the games by name.
/// </summary>
internal static partial class PsnRules
{
    /// <summary>
    /// The NPSSO in what was pasted (the 64 characters alone, or the whole {"npsso":"…"} page).
    /// </summary>
    /// <param name="text">What was pasted.</param>
    /// <returns>The NPSSO, or null.</returns>
    public static string? Npsso(string? text)
    {
        var match = NpssoPattern().Match(text ?? string.Empty);
        return match.Success ? match.Groups[1].Value : null;
    }

    /// <summary>
    /// Minutes from an ISO 8601 duration ("PT228H56M33S", "P1DT2H").
    /// </summary>
    /// <param name="duration">The duration.</param>
    /// <returns>The minutes (0 when unknown).</returns>
    public static int Minutes(string? duration)
    {
        var match = DurationPattern().Match(duration ?? string.Empty);
        if (!match.Success)
        {
            return 0;
        }

        double Part(string name) => match.Groups[name].Success ? double.Parse(match.Groups[name].Value, CultureInfo.InvariantCulture) : 0;
        var minutes = (Part("d") * 24 * 60) + (Part("h") * 60) + Part("m") + (Part("s") / 60);
        return (int)Math.Clamp(Math.Floor(minutes), 0, 99999 * 60);
    }

    /// <summary>
    /// The platform of a game played.
    /// </summary>
    /// <param name="category">PlayStation's category (ps5_native_game, ps4_game).</param>
    /// <returns>PS5 or PS4.</returns>
    public static string Platform(string? category) =>
        category?.StartsWith("ps5", StringComparison.OrdinalIgnoreCase) == true ? "PS5" : "PS4";

    /// <summary>
    /// The platform of a trophy list ("PS5", "PS4", "PS3,PSVITA").
    /// </summary>
    /// <param name="platforms">PlayStation's platforms.</param>
    /// <returns>Ludex's short name of the newest.</returns>
    public static string TrophyPlatform(string? platforms)
    {
        var text = (platforms ?? string.Empty).ToUpperInvariant();
        return text.Contains("PS5", StringComparison.Ordinal) ? "PS5"
            : text.Contains("PS4", StringComparison.Ordinal) ? "PS4"
            : text.Contains("PS3", StringComparison.Ordinal) ? "PS3"
            : text.Contains("VITA", StringComparison.Ordinal) ? "PS Vita"
            : "PS4";
    }

    /// <summary>
    /// The account's games: the versions of one game (PS4 and PS5) put together, with their trophy list when one has
    /// the same name; trophy lists with no game played (PS3, PS Vita, or not listed) are games too.
    /// </summary>
    /// <param name="played">The games played.</param>
    /// <param name="trophies">The trophy lists.</param>
    /// <returns>The games, most recently played first.</returns>
    public static List<PsnGame> Games(IEnumerable<PsnPlayedTitle> played, IEnumerable<PsnTrophyTitle> trophies)
    {
        var lists = trophies.Where(static t => !string.IsNullOrWhiteSpace(t.TrophyTitleName) && !string.IsNullOrWhiteSpace(t.NpCommunicationId)).ToList();
        var byName = lists.GroupBy(static t => NameMatch.Base(NameMatch.Key(t.TrophyTitleName))).ToDictionary(static g => g.Key, static g => g.ToList());
        var used = new HashSet<string>(StringComparer.Ordinal);
        var games = new List<PsnGame>();
        foreach (var group in played.Where(static t => !string.IsNullOrWhiteSpace(t.Name) && (t.Concept?.Id is not null || !string.IsNullOrWhiteSpace(t.TitleId)))
                     .GroupBy(static t => t.Concept?.Id is { } id ? "c:" + id.ToString(CultureInfo.InvariantCulture) : "t:" + t.TitleId))
        {
            var name = Clean(group.Select(static t => t.Concept?.Name).FirstOrDefault(static n => !string.IsNullOrWhiteSpace(n)) ?? group.First().Name!);
            var platform = group.Any(static t => Platform(t.Category) == "PS5") ? "PS5" : "PS4";
            var minutes = (int)Math.Min(group.Sum(static t => (long)Minutes(t.PlayDuration)), 99999 * 60);
            var last = group.Select(static t => t.LastPlayedDateTime).Max();

            // The trophy lists of this game: the same name (a PS4 and a PS5 list both count; the one with most earned).
            var lists2 = group.Select(static t => NameMatch.Base(NameMatch.Key(t.Name))).Append(NameMatch.Base(NameMatch.Key(name)))
                .Distinct(StringComparer.Ordinal)
                .SelectMany(k => byName.GetValueOrDefault(k) ?? [])
                .Where(l => !used.Contains(l.NpCommunicationId!))
                .ToList();
            var best = lists2.OrderByDescending(static l => l.EarnedTrophies?.Total ?? 0).FirstOrDefault();
            foreach (var list in lists2)
            {
                used.Add(list.NpCommunicationId!);
            }

            games.Add(new PsnGame(
                group.Key,
                name,
                platform,
                minutes,
                Max(last, best?.LastUpdatedDateTime),
                best?.EarnedTrophies?.Total,
                best?.DefinedTrophies?.Total,
                lists2.Any(static l => l.EarnedTrophies?.Platinum > 0),
                best is null ? null : TrophyList(best)));
        }

        foreach (var list in lists.Where(l => !used.Contains(l.NpCommunicationId!)))
        {
            used.Add(list.NpCommunicationId!);
            games.Add(new PsnGame(
                "np:" + list.NpCommunicationId,
                Clean(list.TrophyTitleName!),
                TrophyPlatform(list.TrophyTitlePlatform),
                0,
                list.LastUpdatedDateTime,
                list.EarnedTrophies?.Total,
                list.DefinedTrophies?.Total,
                list.EarnedTrophies?.Platinum > 0,
                TrophyList(list)));
        }

        return [.. games.OrderByDescending(static g => g.LastPlayed ?? DateTimeOffset.MinValue)];
    }

    /// <summary>
    /// The status a PlayStation game is suggested with: the platinum means finished; otherwise as Steam's (played
    /// lately → Playing, 2 h or more → Played…). A trophy list with trophies earned counts as played.
    /// </summary>
    /// <param name="game">The game.</param>
    /// <param name="now">Now.</param>
    /// <returns>The status, or null to use the suggestion from the time played.</returns>
    public static GameStatus? Status(PsnGame game, DateTimeOffset now)
    {
        if (game.Platinum)
        {
            return GameStatus.Finished;
        }

        if (game.Minutes == 0 && game.TrophiesEarned is > 0)
        {
            return game.LastPlayed is { } at && now - at <= TimeSpan.FromDays(30) ? GameStatus.Playing : GameStatus.Played;
        }

        return null;
    }

    /// <summary>
    /// A trophy list as Ludex keeps it: its id and the service it is read from (PS5 lists are "trophy2").
    /// </summary>
    /// <param name="list">The trophy list.</param>
    /// <returns>"NPWR12345_00:trophy2".</returns>
    public static string TrophyList(PsnTrophyTitle list) =>
        $"{list.NpCommunicationId}:{((list.TrophyTitlePlatform ?? string.Empty).Contains("PS5", StringComparison.OrdinalIgnoreCase) ? "trophy2" : "trophy")}";

    /// <summary>
    /// A game's trophies as the game page shows them: earned first (latest first), then the rest, the most earned
    /// first; hidden ones still locked keep their secret.
    /// </summary>
    /// <param name="definitions">The trophies of the list.</param>
    /// <param name="earned">The account's progress in it.</param>
    /// <returns>The trophies.</returns>
    public static List<PsnTrophyDTO> Trophies(IEnumerable<PsnTrophyDefinition> definitions, IEnumerable<PsnTrophyProgress> earned)
    {
        var progress = earned.GroupBy(static e => e.TrophyId).ToDictionary(static g => g.Key, static g => g.First());
        return
        [
            .. definitions.Select(d =>
            {
                var mine = progress.GetValueOrDefault(d.TrophyId);
                var unlocked = mine?.Earned == true;
                var secret = d.TrophyHidden == true && !unlocked;
                return new PsnTrophyDTO
                {
                    Name = secret ? "Hidden trophy" : d.TrophyName ?? "Trophy",
                    Description = secret ? null : d.TrophyDetail,
                    IconUrl = d.TrophyIconUrl,
                    Grade = Grade(d.TrophyType),
                    Earned = unlocked,
                    EarnedAt = unlocked ? mine!.EarnedDateTime : null,
                    Rarity = double.TryParse(mine?.TrophyEarnedRate, NumberStyles.Float, CultureInfo.InvariantCulture, out var rate) ? rate : null,
                    Hidden = d.TrophyHidden == true,
                };
            })
            .OrderByDescending(static t => t.Earned)
            .ThenByDescending(static t => t.EarnedAt ?? DateTimeOffset.MinValue)
            .ThenByDescending(static t => t.Rarity ?? -1),
        ];
    }

    private static string Grade(string? type) => (type ?? string.Empty).ToLowerInvariant() switch
    {
        "platinum" => "Platinum",
        "gold" => "Gold",
        "silver" => "Silver",
        _ => "Bronze",
    };

    // ™ and ® are dropped from the names PlayStation shows.
    private static string Clean(string name) => name.Replace("™", string.Empty, StringComparison.Ordinal).Replace("®", string.Empty, StringComparison.Ordinal).Trim();

    private static DateTimeOffset? Max(DateTimeOffset? a, DateTimeOffset? b) => a is null ? b : b is null ? a : a > b ? a : b;

    [GeneratedRegex(@"(?<![A-Za-z0-9])([A-Za-z0-9]{64})(?![A-Za-z0-9])")]
    private static partial Regex NpssoPattern();

    [GeneratedRegex(@"^P(?:(?<d>\d+(?:\.\d+)?)D)?(?:T(?:(?<h>\d+(?:\.\d+)?)H)?(?:(?<m>\d+(?:\.\d+)?)M)?(?:(?<s>\d+(?:\.\d+)?)S)?)?$")]
    private static partial Regex DurationPattern();
}
