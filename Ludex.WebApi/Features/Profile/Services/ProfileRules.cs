using Ludex.Library.Enums;
using Ludex.WebApi.Features.Catalog;
using Ludex.WebApi.Features.Games.Models.Entities;
using Ludex.WebApi.Features.Library.Models.Entities;
using Ludex.WebApi.Features.Profile.Models.DTOs;

namespace Ludex.WebApi.Features.Profile.Services;

/// <summary>
/// The rules of a profile, kept apart from the database so the tests can check them: XP and levels, badges, and
/// what a profile photo may be.
/// XP: 100 per finished game, 20 per rating, 5 per game in the library (the wishlist left out), 1 per hour played.
/// Level L starts at 100·(L−1)² XP: level 2 at 100, 3 at 400, 4 at 900, 10 at 8,100.
/// </summary>
internal static class ProfileRules
{
    /// <summary>Largest profile photo accepted, in bytes (the Website shrinks photos to 256 px first).</summary>
    public const int MaxAvatarBytes = 400 * 1024;

    /// <summary>Longest bio.</summary>
    public const int MaxBioLength = 300;

    /// <summary>Longest location.</summary>
    public const int MaxLocationLength = 80;

    /// <summary>XP for each finished game.</summary>
    public const int XpPerFinished = 100;

    /// <summary>XP for each rating.</summary>
    public const int XpPerRating = 20;

    /// <summary>XP for each game in the library (not the wishlist).</summary>
    public const int XpPerGame = 5;

    /// <summary>
    /// The XP a library is worth.
    /// </summary>
    /// <param name="rows">The library.</param>
    /// <returns>The XP.</returns>
    public static int Xp(IReadOnlyCollection<LibraryRow> rows)
    {
        var played = rows.Where(static r => r.Status != (byte)GameStatus.Wishlist).ToList();
        var hours = (int)Math.Floor(rows.Sum(static r => r.Hours ?? 0m));
        return (played.Count(static r => r.Status == (byte)GameStatus.Finished) * XpPerFinished)
            + (rows.Count(static r => r.Rating is not null) * XpPerRating)
            + (played.Count * XpPerGame)
            + hours;
    }

    /// <summary>
    /// The XP a level starts at.
    /// </summary>
    /// <param name="level">The level (1 and up).</param>
    /// <returns>100·(level−1)².</returns>
    public static int LevelStart(int level) => level <= 1 ? 0 : 100 * (level - 1) * (level - 1);

    /// <summary>
    /// The level for some XP.
    /// </summary>
    /// <param name="xp">The XP.</param>
    /// <returns>The level.</returns>
    public static ProfileLevelDTO Level(int xp)
    {
        xp = Math.Max(0, xp);
        var level = (int)Math.Floor(Math.Sqrt(xp / 100.0)) + 1;

        // Guard against rounding at the edges.
        while (LevelStart(level + 1) <= xp)
        {
            level++;
        }

        while (level > 1 && LevelStart(level) > xp)
        {
            level--;
        }

        return new ProfileLevelDTO { Level = level, Xp = xp, LevelXp = LevelStart(level), NextLevelXp = LevelStart(level + 1) };
    }

    /// <summary>
    /// Every badge, earned or not: earned first (rarest first), then the closest to being earned.
    /// </summary>
    /// <param name="rows">The library.</param>
    /// <param name="friendCount">How many friends the account has.</param>
    /// <returns>The badges.</returns>
    public static List<BadgeDTO> Badges(IReadOnlyCollection<LibraryRow> rows, int friendCount)
    {
        var finished = rows.Where(static r => r.Status == (byte)GameStatus.Finished).ToList();
        var played = rows.Where(static r => r.Status != (byte)GameStatus.Wishlist).ToList();
        var ratings = rows.Where(static r => r.Rating is not null).Select(static r => (int)r.Rating!.Value).ToList();
        var totalHours = (int)Math.Floor(rows.Sum(static r => r.Hours ?? 0m));
        var longest = (int)Math.Floor(rows.Select(static r => r.Hours ?? 0m).DefaultIfEmpty(0m).Max());
        var families = played
            .Select(static r => PlatformNames.Family(r.Platform ?? GameRow.Split(r.Platforms).FirstOrDefault()))
            .Where(static f => f != "Other")
            .Distinct(StringComparer.Ordinal)
            .Count();
        var bestYear = finished
            .Where(static r => r.FinishedAt is not null)
            .GroupBy(static r => r.FinishedAt!.Value.Year)
            .Select(static g => g.Count())
            .DefaultIfEmpty(0)
            .Max();
        var retro = finished.Count(static r => r.FirstReleaseDate is { Year: < 2000 });
        var dayOne = rows.Count(static r => r.FirstReleaseDate is { } release
            && r.AddedAt >= release.AddDays(-1)
            && r.AddedAt <= release.AddDays(30));

        var badges = new List<BadgeDTO>
        {
            Badge("first-finish", "First finish", "Finish a game.", "flag", Bronze, finished.Count, 1),
            Badge("finisher", "Finisher", "Finish 10 games.", "flag", Silver, finished.Count, 10),
            Badge("completionist", "Completionist", "Finish 50 games.", "trophy", Gold, finished.Count, 50),
            Badge("hall-of-fame", "Hall of fame", "Finish 100 games.", "crown", Gold, finished.Count, 100),
            Badge("hundred-hours", "100-hour club", "Spend 100 hours in one game.", "clock", Accent, longest, 100),
            Badge("marathoner", "Marathoner", "Play 500 hours in all.", "clock", Silver, totalHours, 500),
            Badge("thousand-hours", "Thousand hours", "Play 1,000 hours in all.", "hourglass", Gold, totalHours, 1000),
            Badge("critic", "Critic", "Rate 25 games.", "star", Accent, ratings.Count, 25),
            Badge("perfect-ten", "Perfect ten", "Give a game a 10.", "star", Gold, ratings.Count(static r => r == 10), 1),
            Badge("tough-crowd", "Tough crowd", "Give a game 3 or less.", "thumb-down", Red, ratings.Count(static r => r <= 3), 1),
            Badge("collector", "Collector", "Have 50 games in the library.", "library", Bronze, played.Count, 50),
            Badge("curator", "Curator", "Have 100 games in the library.", "library", Purple, played.Count, 100),
            Badge("wishful", "Wishful thinking", "Put 10 games on the wishlist.", "heart", Purple, rows.Count - played.Count, 10),
            Badge("platform-hopper", "Platform hopper", "Play on 4 families of platforms.", "pad", Green, families, 4),
            Badge("retro-soul", "Retro soul", "Finish a game released before 2000.", "sparkle", Bronze, retro, 1),
            Badge("day-one", "Day one", "Add a game within 30 days of its release.", "bolt", Accent, dayOne, 1),
            Badge("good-company", "Good company", "Have 5 friends.", "friends", Green, friendCount, 5),
            Badge("year-of-play", "Year of play", "Finish 12 games in one year.", "calendar", Silver, bestYear, 12),
            Badge("not-for-me", "Not for me", "Drop a game.", "x", Red, rows.Count(static r => r.Status == (byte)GameStatus.Dropped), 1),
        };

        return [.. badges
            .OrderByDescending(static b => b.Earned)
            .ThenByDescending(static b => b.Earned ? b.Target : 0)
            .ThenByDescending(static b => b.Earned ? 0 : (double)b.Progress / b.Target)];
    }

    /// <summary>
    /// The media type of a profile photo, read from its first bytes (not from what the browser said).
    /// </summary>
    /// <param name="data">The image.</param>
    /// <returns>image/jpeg, image/png or image/webp; null for anything else or a photo too big.</returns>
    public static string? AvatarType(byte[]? data)
    {
        if (data is null || data.Length < 12 || data.Length > MaxAvatarBytes)
        {
            return null;
        }

        if (data[0] == 0xFF && data[1] == 0xD8 && data[2] == 0xFF)
        {
            return "image/jpeg";
        }

        if (data[0] == 0x89 && data[1] == 0x50 && data[2] == 0x4E && data[3] == 0x47 && data[4] == 0x0D && data[5] == 0x0A && data[6] == 0x1A && data[7] == 0x0A)
        {
            return "image/png";
        }

        if (data[0] == (byte)'R' && data[1] == (byte)'I' && data[2] == (byte)'F' && data[3] == (byte)'F'
            && data[8] == (byte)'W' && data[9] == (byte)'E' && data[10] == (byte)'B' && data[11] == (byte)'P')
        {
            return "image/webp";
        }

        return null;
    }

    /// <summary>
    /// A text field as it is saved: trimmed, line breaks kept as \n, empty becomes null.
    /// </summary>
    /// <param name="value">What was typed.</param>
    /// <returns>The text, or null.</returns>
    public static string? Clean(string? value)
    {
        var text = (value ?? string.Empty).Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n').Trim();
        return text.Length == 0 ? null : text;
    }

    private const string Bronze = "#C98B5B";
    private const string Silver = "#B8C4D6";
    private const string Gold = "#F5B841";
    private const string Accent = "#4FC3F7";
    private const string Green = "#8EDB97";
    private const string Purple = "#B794F6";
    private const string Red = "#F2777A";

    private static BadgeDTO Badge(string id, string name, string description, string icon, string color, int progress, int target) => new()
    {
        Id = id,
        Name = name,
        Description = description,
        Icon = icon,
        Color = color,
        Progress = Math.Min(progress, target),
        Target = target,
        Earned = progress >= target,
    };
}
