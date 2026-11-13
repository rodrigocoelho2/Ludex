using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Ludex.Library.Enums;
using Ludex.Library.Platforms;
using Ludex.WebApi.Features.Catalog;

namespace Ludex.WebApi.Features.Playnite.Services;

/// <summary>A game read from Playnite's export.</summary>
/// <param name="Name">The name.</param>
/// <param name="Source">Where Playnite got it ("Steam", "Epic"…; null: added by hand).</param>
/// <param name="Platform">The platform, as Ludex names it ("PC", "PS5"…).</param>
/// <param name="Store">The PC store (null on consoles, or when unknown).</param>
/// <param name="Format">Physical or digital (null: unknown).</param>
/// <param name="Minutes">Minutes played.</param>
/// <param name="LastPlayed">When it was last played.</param>
/// <param name="Status">The status Playnite's completion status says (null: suggest one from how it was played).</param>
internal sealed record PlayniteGame(string Name, string? Source, string Platform, string? Store, GameFormat? Format, int Minutes, DateTimeOffset? LastPlayed, GameStatus? Status);

/// <summary>What was read from Playnite's export.</summary>
/// <param name="Games">The games.</param>
/// <param name="Missing">The useful columns the export left out ("Time Played", "Completion Status"…).</param>
/// <param name="Problem">Why nothing could be read (null: read).</param>
internal sealed record PlayniteFile(List<PlayniteGame> Games, List<string> Missing, string? Problem);

/// <summary>
/// Reads the CSV that Playnite's Library Exporter Advanced add-on writes (Extensions → Library Exporter Advanced →
/// Export library). Its columns are named in Playnite's language, the time played is in seconds, dates are written the
/// way the computer writes dates (day or month first, read from the rows) and lists are joined with ", ".
/// </summary>
internal static partial class PlayniteCsv
{
    /// <summary>Most games read from one file.</summary>
    public const int MaxGames = 10000;

    /// <summary>
    /// Reads the export.
    /// </summary>
    /// <param name="text">The file's text.</param>
    /// <returns>The games, or what's wrong.</returns>
    public static PlayniteFile Read(string? text)
    {
        var rows = Rows(text ?? string.Empty);
        if (rows.Count == 0 || rows[0].Count == 0)
        {
            return new PlayniteFile([], [], "That file is empty. Export the library from Playnite with Library Exporter Advanced and choose that CSV.");
        }

        var header = rows[0].Select(static h => h.Trim().Trim('﻿')).ToList();
        int Column(HashSet<string> names) => header.FindIndex(names.Contains);
        var name = Column(PlayniteHeaders.Name);
        if (name < 0)
        {
            // The name is always the first column; refuse only what plainly isn't Playnite's export.
            if (header.Count < 2 || header.Any(static h => h.Contains('{') || h.Length > 80))
            {
                return new PlayniteFile([], [], "That doesn't look like Playnite's export. Use Library Exporter Advanced (Extensions → Library Exporter Advanced → Export library) and choose the CSV it writes.");
            }

            name = 0;
        }

        var source = Column(PlayniteHeaders.Sources);
        var platforms = Column(PlayniteHeaders.Platforms);
        var played = Column(PlayniteHeaders.TimePlayed);
        var completion = Column(PlayniteHeaders.CompletionStatus);
        var last = Column(PlayniteHeaders.LastPlayed);
        var missing = new List<string>();
        if (played < 0)
        {
            missing.Add("Time Played");
        }

        if (completion < 0)
        {
            missing.Add("Completion Status");
        }

        if (last < 0)
        {
            missing.Add("Last Played");
        }

        if (platforms < 0)
        {
            missing.Add("Platforms");
        }

        if (source < 0)
        {
            missing.Add("Sources");
        }

        string? Cell(List<string> row, int index) => index >= 0 && index < row.Count && row[index].Trim() is { Length: > 0 } value ? value : null;
        var dayFirst = DayFirst(rows.Skip(1).Select(r => Cell(r, last)));
        var games = new List<PlayniteGame>();
        foreach (var row in rows.Skip(1))
        {
            if (Cell(row, name) is not { } title || title.Length > 200)
            {
                continue;
            }

            var from = Cell(row, source);
            var platform = Platform(Cell(row, platforms), from);
            var store = GameCopies.UsesStores(platform) ? Store(from) : null;
            var format = GameCopies.UsesStores(platform) ? GameFormat.Digital : ConsoleFormat(from);
            var minutes = Minutes(Cell(row, played));
            games.Add(new PlayniteGame(title, from, platform, store, format, minutes, Date(Cell(row, last), dayFirst), Status(Cell(row, completion), minutes)));
            if (games.Count == MaxGames)
            {
                break;
            }
        }

        return games.Count == 0
            ? new PlayniteFile([], missing, "That file has no games.")
            : new PlayniteFile(games, missing, null);
    }

    /// <summary>
    /// The platform as Ludex names it, from Playnite's ("PC (Windows)" → "PC", "Sony PlayStation 5" → "PS5").
    /// </summary>
    /// <param name="platforms">Playnite's platforms, joined with ", ".</param>
    /// <param name="source">Where Playnite got the game (a PC store means PC when no platform is given).</param>
    /// <returns>The platform.</returns>
    public static string Platform(string? platforms, string? source)
    {
        var names = (platforms ?? string.Empty).Split([',', ';', '|'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(static p => PlatformName(p))
            .Where(static p => p.Length > 0)
            .ToList();
        if (names.Count == 0)
        {
            return "PC";
        }

        // A game on several platforms: the PC when Playnite got it from a PC store, otherwise the first one.
        return Store(source) is not null && names.FirstOrDefault(GameCopies.UsesStores) is { } pc ? pc : names[0];
    }

    /// <summary>
    /// The PC store as Ludex spells it, from Playnite's source.
    /// </summary>
    /// <param name="source">Playnite's source ("Steam", "Epic", "Ubisoft Connect"…).</param>
    /// <returns>The store, or null when the source isn't a PC store.</returns>
    public static string? Store(string? source)
    {
        var text = NameMatch.Key(source);
        if (text.Length == 0)
        {
            return null;
        }

        bool Has(string word) => text.Contains(word, StringComparison.Ordinal);
        return text switch
        {
            _ when Has("steam") => "Steam",
            _ when Has("epic") => "Epic Games",
            _ when Has("gog") => "GOG",
            _ when Has("ea app") || Has("origin") || text == "ea" => "EA app",
            _ when Has("ubisoft") || Has("uplay") => "Ubisoft Connect",
            _ when Has("battle net") || Has("blizzard") => "Battle.net",
            _ when Has("xbox") || Has("microsoft") => "Microsoft Store",
            _ when Has("rockstar") => "Rockstar",
            _ when Has("itch") => "itch.io",
            _ when Has("playstation") || Has("psn") || Has("nintendo") || Has("emulat") || text == "roms" => null,
            _ when Has("playnite") || Has("manual") => null,
            _ => "Other store",
        };
    }

    /// <summary>
    /// The status Playnite's completion status says (its defaults, in English or translated; a renamed status that
    /// says nothing known is left to the suggestion).
    /// </summary>
    /// <param name="completion">The completion status.</param>
    /// <param name="minutes">Minutes played.</param>
    /// <returns>The status, or null.</returns>
    public static GameStatus? Status(string? completion, int minutes)
    {
        var text = NameMatch.Key(completion);
        if (text.Length == 0)
        {
            return null;
        }

        bool Has(params string[] words) => words.Any(w => text.Contains(w, StringComparison.Ordinal));
        if (Has("not played", "nao jogado", "no jugado", "nicht gespielt", "non joue", "non giocato", "niet gespeeld", "nie grano"))
        {
            return null;   // Playnite's default for every game: say nothing
        }

        if (Has("plan", "hold", "pausa", "pause", "wishlist", "backlog", "quero jogar", "por jogar"))
        {
            return GameStatus.Backlog;
        }

        if (Has("abandon", "dropped", "desist"))
        {
            return GameStatus.Dropped;
        }

        if (Has("beaten", "complet", "finish", "termin", "conclu", "zakon", "ukon", "durchgespielt", "abgeschlossen", "fini", "battu"))
        {
            return GameStatus.Finished;
        }

        if (Has("playing", "a jogar", "jogando", "jugando", "en cours", "spiele gerade", "in corso", "aan het spelen"))
        {
            return GameStatus.Playing;
        }

        if (Has("played", "jogado", "jugado", "gespielt", "joue", "giocato", "gespeeld"))
        {
            return minutes > 0 ? GameStatus.Played : null;
        }

        return null;
    }

    /// <summary>
    /// Minutes from Playnite's time played (seconds).
    /// </summary>
    /// <param name="seconds">The seconds, as written.</param>
    /// <returns>The minutes (0 when unknown).</returns>
    public static int Minutes(string? seconds) =>
        long.TryParse(seconds?.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out var value) && value > 0
            ? (int)Math.Min(value / 60, ImportMaxMinutes)
            : 0;

    /// <summary>
    /// Whether the export's dates put the day first: a first number over 12 says so, a second number over 12 says the
    /// month is first; nothing either way reads day first (as most of the world writes dates).
    /// </summary>
    /// <param name="dates">The dates written.</param>
    /// <returns>True for day first.</returns>
    public static bool DayFirst(IEnumerable<string?> dates)
    {
        foreach (var date in dates)
        {
            if (Parts(date) is { } parts && parts.Count >= 3 && parts[0].Length <= 2)
            {
                var first = int.Parse(parts[0], CultureInfo.InvariantCulture);
                var second = int.Parse(parts[1], CultureInfo.InvariantCulture);
                if (first > 12)
                {
                    return true;
                }

                if (second > 12)
                {
                    return false;
                }
            }
        }

        return true;
    }

    /// <summary>
    /// A date as the computer wrote it ("13/11/2026 21:04:33", "11/13/2026 9:04:33 PM", "2026-11-13 21:04:33").
    /// </summary>
    /// <param name="text">The date.</param>
    /// <param name="dayFirst">Whether the day comes before the month (when the year is last).</param>
    /// <returns>The time (read as UTC), or null.</returns>
    public static DateTimeOffset? Date(string? text, bool dayFirst)
    {
        if (Parts(text) is not { Count: >= 3 } parts)
        {
            return null;
        }

        int N(int i) => i < parts.Count ? int.Parse(parts[i], CultureInfo.InvariantCulture) : 0;
        int year, month, day;
        if (parts[0].Length == 4)
        {
            (year, month, day) = (N(0), N(1), N(2));
        }
        else
        {
            year = N(2) < 100 ? 2000 + N(2) : N(2);
            (day, month) = dayFirst ? (N(0), N(1)) : (N(1), N(0));
        }

        var hour = N(3);
        var upper = text!.ToUpperInvariant();
        var pm = upper.Contains("PM", StringComparison.Ordinal) || upper.Contains("P.M", StringComparison.Ordinal) || text.Contains("午後", StringComparison.Ordinal) || text.Contains("下午", StringComparison.Ordinal) || text.Contains("오후", StringComparison.Ordinal);
        var am = upper.Contains("AM", StringComparison.Ordinal) || upper.Contains("A.M", StringComparison.Ordinal) || text.Contains("午前", StringComparison.Ordinal) || text.Contains("上午", StringComparison.Ordinal) || text.Contains("오전", StringComparison.Ordinal);
        if (pm && hour is >= 1 and < 12)
        {
            hour += 12;
        }
        else if (am && hour == 12)
        {
            hour = 0;
        }

        if (year is < 1990 or > 2100 || month is < 1 or > 12 || day < 1 || day > DateTime.DaysInMonth(year, month) || hour > 23 || N(4) > 59 || N(5) > 59)
        {
            return null;
        }

        return new DateTimeOffset(year, month, day, hour, N(4), N(5), TimeSpan.Zero);
    }

    // Longest time kept (the game page allows 99999 hours).
    private const int ImportMaxMinutes = 99999 * 60;

    private static List<string>? Parts(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var parts = Digits().Matches(text).Select(static m => m.Value).ToList();
        return parts.Count >= 3 && parts.All(static p => p.Length <= 4) ? parts : null;
    }

    private static string PlatformName(string name)
    {
        var text = NameMatch.Key(name);
        bool Has(string word) => text.Contains(word, StringComparison.Ordinal);
        return text switch
        {
            "" => string.Empty,
            _ when Has("linux") => "Linux",
            _ when text == "mac" || Has("macintosh") || Has("macos") || Has("os x") => "Mac",
            _ when Has("windows") || text == "pc" || text.StartsWith("pc ", StringComparison.Ordinal) || text == "dos" || Has("ms dos") => "PC",
            _ when Has("playstation 5") || Has("ps5") => "PS5",
            _ when Has("playstation 4") || Has("ps4") => "PS4",
            _ when Has("playstation 3") || Has("ps3") => "PS3",
            _ when Has("playstation 2") || Has("ps2") => "PS2",
            _ when Has("vita") => "PS Vita",
            _ when Has("psp") || Has("playstation portable") => "PSP",
            _ when Has("playstation") || Has("psx") || Has("ps1") => "PS1",
            _ when Has("switch 2") => "Switch 2",
            _ when Has("switch") => "Switch",
            _ when Has("wii u") => "Wii U",
            _ when Has("wii") => "Wii",
            _ when Has("3ds") => "3DS",
            _ when Has("gamecube") => "GameCube",
            _ when Has("nintendo 64") || Has("n64") => "N64",
            _ when Has("game boy advance") || Has("gba") => "GBA",
            _ when Has("game boy color") || Has("gbc") => "GBC",
            _ when Has("game boy") => "Game Boy",
            _ when Has("super nintendo") || Has("snes") => "SNES",
            _ when Has("nintendo ds") || text == "ds" => "DS",
            _ when Has("nintendo entertainment") || text == "nes" => "NES",
            _ when Has("xbox series") => "Xbox Series",
            _ when Has("xbox one") => "Xbox One",
            _ when Has("xbox 360") => "Xbox 360",
            _ when Has("xbox") => "Xbox",
            _ when Has("android") => "Android",
            _ when Has("ios") || Has("iphone") || Has("ipad") => "iOS",
            _ when Has("genesis") || Has("mega drive") => "Mega Drive",
            _ when Has("dreamcast") => "Dreamcast",
            _ when Has("saturn") => "Saturn",
            _ => name.Trim().Length > 60 ? name.Trim()[..60] : name.Trim(),
        };
    }

    // A console game from a console's own store is digital; anything else is left for the person to say.
    private static GameFormat? ConsoleFormat(string? source)
    {
        var text = NameMatch.Key(source);
        return text.Contains("playstation", StringComparison.Ordinal) || text.Contains("psn", StringComparison.Ordinal)
               || text.Contains("xbox", StringComparison.Ordinal) || text.Contains("eshop", StringComparison.Ordinal)
               || text.Contains("nintendo", StringComparison.Ordinal)
            ? GameFormat.Digital
            : null;
    }

    // The rows of a CSV (RFC 4180: quoted fields may hold the separator, quotes doubled, line breaks); the separator
    // is the one the header uses most, of , ; and tab.
    private static List<List<string>> Rows(string text)
    {
        var start = text.Length > 0 && text[0] == '﻿' ? 1 : 0;
        var firstLine = text.IndexOf('\n', start) is var end and >= 0 ? text[start..end] : text[start..];
        var separator = new[] { ',', ';', '\t' }.OrderByDescending(c => Outside(firstLine, c)).First();
        var rows = new List<List<string>>();
        var row = new List<string>();
        var field = new StringBuilder();
        var quoted = false;
        for (var i = start; i < text.Length; i++)
        {
            var c = text[i];
            if (quoted)
            {
                if (c == '"')
                {
                    if (i + 1 < text.Length && text[i + 1] == '"')
                    {
                        field.Append('"');
                        i++;
                    }
                    else
                    {
                        quoted = false;
                    }
                }
                else
                {
                    field.Append(c);
                }
            }
            else if (c == '"' && field.Length == 0)
            {
                quoted = true;
            }
            else if (c == separator)
            {
                row.Add(field.ToString());
                field.Clear();
            }
            else if (c is '\n' or '\r')
            {
                if (c == '\r' && i + 1 < text.Length && text[i + 1] == '\n')
                {
                    i++;
                }

                row.Add(field.ToString());
                field.Clear();
                if (row.Count > 1 || row[0].Length > 0)
                {
                    rows.Add(row);
                }

                row = [];
                if (rows.Count > MaxGames + 1)
                {
                    return rows;
                }
            }
            else
            {
                field.Append(c);
            }
        }

        row.Add(field.ToString());
        if (row.Count > 1 || row[0].Length > 0)
        {
            rows.Add(row);
        }

        return rows;
    }

    private static int Outside(string line, char separator)
    {
        var count = 0;
        var quoted = false;
        foreach (var c in line)
        {
            if (c == '"')
            {
                quoted = !quoted;
            }
            else if (c == separator && !quoted)
            {
                count++;
            }
        }

        return count;
    }

    [GeneratedRegex(@"\d+")]
    private static partial Regex Digits();
}
