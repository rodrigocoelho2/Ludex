using System.Globalization;
using Ludex.Library.Enums;

namespace Ludex.Website.Helpers;

/// <summary>
/// How numbers, dates and statuses read on the pages.
/// </summary>
public static class Fmt
{
    private static readonly CultureInfo English = CultureInfo.GetCultureInfo("en-IE");

    /// <summary>The words of a status.</summary>
    /// <param name="status">The status.</param>
    /// <returns>Playing, Finished, Wishlist or Dropped.</returns>
    public static string Status(GameStatus status) => status switch
    {
        GameStatus.Playing => "Playing",
        GameStatus.Finished => "Finished",
        GameStatus.Wishlist => "Wishlist",
        GameStatus.Dropped => "Dropped",
        GameStatus.Backlog => "Backlog",
        GameStatus.Played => "Played",
        _ => status.ToString(),
    };

    /// <summary>The CSS name of a status (chip colours).</summary>
    /// <param name="status">The status.</param>
    /// <returns>playing, finished, wishlist or dropped.</returns>
    public static string StatusCss(GameStatus status) => status.ToString().ToLowerInvariant();

    /// <summary>A rating as the cards show it: half stars (each point of the 1–10 rating is half a star).</summary>
    /// <param name="rating">The rating.</param>
    /// <returns>"★★★★½", or "—" when there is none.</returns>
    public static string Rating(int? rating) => rating is >= 1 and <= 10 ? Stars(rating) : "—";

    /// <summary>An average rating, in stars.</summary>
    /// <param name="rating">The average (1–10).</param>
    /// <returns>"★ 4.2", or "—".</returns>
    public static string Average(decimal? rating) => rating is { } value ? "★ " + (value / 2m).ToString("0.0", English) : "—";

    /// <summary>What an average rating from friends reads as, Steam style.</summary>
    /// <param name="rating">The average (1–10).</param>
    /// <returns>"Overwhelmingly positive", "Very positive", "Positive", "Mixed" or "Negative".</returns>
    public static string Reception(decimal rating) => rating switch
    {
        >= 9.0m => "Overwhelmingly positive",
        >= 8.0m => "Very positive",
        >= 6.5m => "Positive",
        >= 5.0m => "Mixed",
        _ => "Negative",
    };

    /// <summary>The CSS name of <see cref="Reception"/>'s colour.</summary>
    /// <param name="rating">The average.</param>
    /// <returns>good, mixed or bad.</returns>
    public static string ReceptionCss(decimal rating) => rating >= 6.5m ? "good" : rating >= 5.0m ? "mixed" : "bad";

    /// <summary>Hours played.</summary>
    /// <param name="hours">The hours.</param>
    /// <returns>"38 h", "12.5 h", or "—".</returns>
    public static string Hours(decimal? hours) => hours is { } value ? value.ToString("#,0.#", English) + " h" : "—";

    /// <summary>A price.</summary>
    /// <param name="amount">The amount.</param>
    /// <param name="currency">The currency (EUR, USD…).</param>
    /// <returns>"€19.99", "$5.00", "19.99 PLN", or "—".</returns>
    public static string Money(decimal? amount, string? currency)
    {
        if (amount is not { } value)
        {
            return "—";
        }

        var number = value.ToString("0.00", English);
        return (currency ?? string.Empty).ToUpperInvariant() switch
        {
            "EUR" => "€" + number,
            "USD" => "$" + number,
            "GBP" => "£" + number,
            "" => number,
            var other => $"{number} {other}",
        };
    }

    /// <summary>A share of achievements.</summary>
    /// <param name="unlocked">Unlocked.</param>
    /// <param name="total">The game's.</param>
    /// <returns>The % (0–100), or null when the game has none or they weren't read.</returns>
    public static int? AchievementPercent(int? unlocked, int? total) =>
        total is > 0 ? (int)Math.Floor(100.0 * Math.Min(unlocked ?? 0, total.Value) / total.Value) : null;

    /// <summary>A number for an input (dot decimals, whatever the culture).</summary>
    /// <param name="value">The number.</param>
    /// <returns>The text.</returns>
    public static string Input(decimal? value) => value?.ToString("0.#", CultureInfo.InvariantCulture) ?? string.Empty;

    /// <summary>A whole number with thousands separators.</summary>
    /// <param name="value">The number.</param>
    /// <returns>The text.</returns>
    public static string Count(decimal value) => value.ToString("#,0", English);

    /// <summary>A date.</summary>
    /// <param name="date">The date.</param>
    /// <returns>"25 February 2022".</returns>
    public static string LongDate(DateOnly date) => date.ToString("d MMMM yyyy", English);

    /// <summary>A short date: the year only when it isn't this one.</summary>
    /// <param name="at">The time.</param>
    /// <returns>"12 Mar" or "12 Mar 2024".</returns>
    public static string ShortDate(DateTimeOffset at)
    {
        var local = TimeZoneInfo.ConvertTime(at, Clock.Zone);
        return local.Year == Clock.Now.Year ? local.ToString("d MMM", English) : local.ToString("d MMM yyyy", English);
    }

    /// <summary>A month and year.</summary>
    /// <param name="at">The time.</param>
    /// <returns>"Nov 2026".</returns>
    public static string MonthYear(DateTimeOffset at) => TimeZoneInfo.ConvertTime(at, Clock.Zone).ToString("MMM yyyy", English);

    /// <summary>When someone was last around.</summary>
    /// <param name="at">The time (null: never).</param>
    /// <returns>"Online now" (the last 5 minutes), "Last seen 2 h ago"… or "Not seen yet".</returns>
    public static string Seen(DateTimeOffset? at) => at is not { } when
        ? "Not seen yet"
        : DateTimeOffset.UtcNow - when < TimeSpan.FromMinutes(5) ? "Online now" : "Last seen " + Ago(when);

    /// <summary>Whether someone was around in the last 5 minutes.</summary>
    /// <param name="at">The time.</param>
    /// <returns>True when online.</returns>
    public static bool Online(DateTimeOffset? at) => at is { } when && DateTimeOffset.UtcNow - when < TimeSpan.FromMinutes(5);

    /// <summary>How long ago.</summary>
    /// <param name="at">The time.</param>
    /// <returns>"just now", "5 min ago", "2 h ago", "yesterday", "3 days ago", or the date.</returns>
    public static string Ago(DateTimeOffset at)
    {
        var span = DateTimeOffset.UtcNow - at;
        return span.TotalMinutes < 1 ? "just now"
            : span.TotalMinutes < 60 ? $"{(int)span.TotalMinutes} min ago"
            : span.TotalHours < 24 ? $"{(int)span.TotalHours} h ago"
            : span.TotalDays < 2 ? "yesterday"
            : span.TotalDays < 7 ? $"{(int)span.TotalDays} days ago"
            : ShortDate(at);
    }

    /// <summary>What a feed line says a friend did.</summary>
    /// <param name="kind">What happened.</param>
    /// <returns>"finished", "started playing"…</returns>
    public static string Did(ActivityKind kind) => kind switch
    {
        ActivityKind.Added => "added",
        ActivityKind.Started => "started playing",
        ActivityKind.Finished => "finished",
        ActivityKind.Rated => "rated",
        ActivityKind.Wishlisted => "wishlisted",
        ActivityKind.Dropped => "dropped",
        ActivityKind.Played => "played",
        ActivityKind.Reviewed => "reviewed",
        _ => "played",
    };

    /// <summary>Who of the friends have a game.</summary>
    /// <param name="names">Their user names.</param>
    /// <returns>"Tiago has it", "Tiago and Inês have it", "Tiago, Inês and 2 more have it", or empty.</returns>
    public static string Owners(IReadOnlyList<string> names) => names.Count switch
    {
        0 => string.Empty,
        1 => $"{names[0]} has it",
        2 => $"{names[0]} and {names[1]} have it",
        3 => $"{names[0]}, {names[1]} and {names[2]} have it",
        _ => $"{names[0]}, {names[1]} and {names.Count - 2} more have it",
    };

    /// <summary>"1 friend" / "3 friends".</summary>
    /// <param name="count">How many.</param>
    /// <param name="one">The singular.</param>
    /// <param name="many">The plural.</param>
    /// <returns>The words.</returns>
    public static string Plural(int count, string one, string many) => $"{count.ToString(English)} {(count == 1 ? one : many)}";

    /// <summary>The initials of a user name (the avatar).</summary>
    /// <param name="userName">The user name.</param>
    /// <returns>Two letters.</returns>
    public static string Initials(string? userName) =>
        string.IsNullOrEmpty(userName) ? "L" : userName.Length == 1 ? userName.ToUpperInvariant() : userName[..2].ToUpperInvariant();

    /// <summary>A colour per user name (the avatar), always the same for the same name.</summary>
    /// <param name="userName">The user name.</param>
    /// <returns>A hex colour that passes 4.5:1 under white text.</returns>
    public static string AvatarColour(string? userName)
    {
        string[] palette = ["#2E5E7E", "#7E4A2E", "#5B3E7E", "#2E6E5A", "#7E2E4A", "#4A5A2E", "#2E4A7E", "#6E3A2E"];
        var hash = 0;
        foreach (var c in (userName ?? string.Empty).ToLowerInvariant())
        {
            hash = unchecked((hash * 31) + c);
        }

        return palette[(int)((uint)hash % (uint)palette.Length)];
    }

    /// <summary>A 1–10 rating as stars ("★★★★½").</summary>
    /// <param name="rating">The rating.</param>
    /// <returns>The stars, or empty for none.</returns>
    public static string Stars(int? rating) => rating is >= 1 and <= 10 ? new string('★', rating.Value / 2) + (rating.Value % 2 == 1 ? "½" : string.Empty) : string.Empty;

    /// <summary>A 1–10 rating as a number of stars ("4.5").</summary>
    /// <param name="rating">The rating.</param>
    /// <returns>The number, or "—".</returns>
    public static string StarsValue(decimal? rating) => rating is { } value ? (value / 2m).ToString("0.#", English) : "—";

    /// <summary>A 1–10 rating for a screen reader ("4.5 out of 5 stars").</summary>
    /// <param name="rating">The rating.</param>
    /// <returns>The words.</returns>
    public static string StarsLabel(int? rating) => rating is { } value ? $"{StarsValue(value)} out of 5 stars" : "Not rated";

    /// <summary>How a game ended, in words.</summary>
    /// <param name="ending">The ending.</param>
    /// <returns>The words.</returns>
    public static string Ending(GameEnding ending) => ending switch
    {
        GameEnding.Completed => "Completed",
        GameEnding.Mastered => "Mastered (100%)",
        GameEnding.Shelved => "Shelved for now",
        GameEnding.Abandoned => "Abandoned",
        _ => ending.ToString(),
    };

    /// <summary>How a review says the game was played ("Finished on PC · 100%").</summary>
    /// <param name="status">The status.</param>
    /// <param name="platform">The platform.</param>
    /// <param name="ending">How it ended.</param>
    /// <returns>The words, or empty.</returns>
    public static string PlayedLine(GameStatus? status, string? platform, GameEnding? ending)
    {
        var line = status is { } s ? Status(s) + (platform is { Length: > 0 } p ? " on " + p : string.Empty) : platform ?? string.Empty;
        return ending is GameEnding.Mastered ? line + " · 100%" : ending is GameEnding.Shelved ? line + " · shelved" : line;
    }

    /// <summary>Minutes as "1 h 40" or "55 min".</summary>
    /// <param name="minutes">The minutes.</param>
    /// <returns>The time.</returns>
    public static string Minutes(int minutes) => minutes >= 60
        ? (minutes / 60).ToString(English) + " h" + (minutes % 60 > 0 ? " " + (minutes % 60).ToString("00", English) : string.Empty)
        : minutes.ToString(English) + " min";

    /// <summary>A day as "18 Nov".</summary>
    /// <param name="day">The day.</param>
    /// <returns>The day.</returns>
    public static string DayMonth(DateOnly day) => day.ToString("d MMM", English);

    /// <summary>A day as "18 Nov 2026" (the year only when it isn't this one).</summary>
    /// <param name="at">When.</param>
    /// <returns>The day.</returns>
    public static string Day(DateTimeOffset at)
    {
        var local = TimeZoneInfo.ConvertTime(at, Clock.Zone);
        return local.ToString(local.Year == Clock.Now.Year ? "d MMM" : "d MMM yyyy", English);
    }

    /// <summary>Whether a status counts as played for a list's progress (finished, played or dropped).</summary>
    /// <param name="status">The status.</param>
    /// <returns>True when played.</returns>
    public static bool IsPlayed(GameStatus? status) => status is GameStatus.Finished or GameStatus.Played or GameStatus.Dropped;

    /// <summary>Who can see a list, in words.</summary>
    /// <param name="visibility">The visibility.</param>
    /// <returns>The words.</returns>
    public static string Visibility(ListVisibility visibility) => visibility switch
    {
        ListVisibility.Friends => "Friends",
        ListVisibility.Private => "Only you",
        _ => "Public",
    };
}
