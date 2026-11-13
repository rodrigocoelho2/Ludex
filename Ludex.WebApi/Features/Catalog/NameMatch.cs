using System.Globalization;
using System.Text;

namespace Ludex.WebApi.Features.Catalog;

/// <summary>
/// Matching a game by its name (the Playnite and PlayStation imports, which have no store id IGDB knows): the names are
/// folded the same way ("Marvel's Spider-Man™ 2" and "Marvel’s Spider-Man 2" agree), and only a confident match is
/// taken — a wrong game in the library is worse than one left for the person to add.
/// </summary>
internal static class NameMatch
{
    // Words a store adds to a name that don't make it another game.
    private static readonly string[] Editions =
    [
        "game of the year edition", "goty edition", "game of the year", "definitive edition", "deluxe edition",
        "complete edition", "ultimate edition", "gold edition", "standard edition", "digital deluxe edition",
        "enhanced edition", "director s cut", "directors cut", "anniversary edition", "special edition", "premium edition",
        "complete", "goty", "ps4", "ps5", "ps4 ps5", "ps4 and ps5", "for windows", "pc", "windows edition",
        "playstation5", "playstation 5", "playstation4", "playstation 4", "ps4 edition", "ps5 edition", "trophy", "trophies",
    ];

    /// <summary>
    /// A name folded for comparing: lower case, no accents, no ™/®, "&amp;" as "and", punctuation as spaces.
    /// </summary>
    /// <param name="name">The name.</param>
    /// <returns>The folded name ("" for nothing).</returns>
    public static string Key(string? name)
    {
        var text = (name ?? string.Empty).Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(text.Length);
        var space = false;
        foreach (var raw in text)
        {
            var c = raw;
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark || c is '™' or '®' or '©' or '\'' or '’' or '`')
            {
                continue;
            }

            if (c == '&')
            {
                builder.Append(space || builder.Length == 0 ? "and " : " and ");
                space = true;
                continue;
            }

            if (char.IsLetterOrDigit(c))
            {
                builder.Append(char.ToLowerInvariant(c));
                space = false;
            }
            else if (!space && builder.Length > 0)
            {
                builder.Append(' ');
                space = true;
            }
        }

        return builder.ToString().Trim();
    }

    /// <summary>
    /// The folded name without an edition at its end ("Witcher 3 Wild Hunt Game of the Year Edition" → "witcher 3 wild hunt").
    /// </summary>
    /// <param name="key">A folded name.</param>
    /// <returns>The base name.</returns>
    public static string Base(string key)
    {
        var text = key;
        var changed = true;
        while (changed)
        {
            changed = false;
            foreach (var edition in Editions)
            {
                if (text.Length > edition.Length + 1 && text.EndsWith(" " + edition, StringComparison.Ordinal))
                {
                    text = text[..^(edition.Length + 1)].TrimEnd();
                    changed = true;
                }
            }
        }

        return text;
    }

    /// <summary>
    /// The candidate that is the game named, or null when none is close enough.
    /// </summary>
    /// <param name="name">The name in the other store.</param>
    /// <param name="candidates">The catalog's candidates, best first.</param>
    /// <param name="platformHint">The platform the copy is on ("PS5", "PC"…), to break ties.</param>
    /// <returns>The match, or null.</returns>
    public static CatalogGame? Best(string name, IReadOnlyList<CatalogGame> candidates, string? platformHint = null)
    {
        var key = Key(name);
        if (key.Length == 0 || candidates.Count == 0)
        {
            return null;
        }

        var stem = Base(key);
        bool OnPlatform(CatalogGame game) => platformHint is not null && game.Platforms.Contains(platformHint, StringComparer.OrdinalIgnoreCase);
        CatalogGame? Pick(Func<CatalogGame, bool> rule)
        {
            var hits = candidates.Where(rule).ToList();
            return hits.FirstOrDefault(OnPlatform) ?? hits.FirstOrDefault();
        }

        // 1. The same name. 2. The same name once editions are put aside on either side. 3. One name is the other plus a
        //    subtitle ("Hades" for "Hades II" is refused: only a separated subtitle counts), when that's the only such
        //    candidate.
        return Pick(g => Key(g.Name) == key)
               ?? Pick(g => Base(Key(g.Name)) == stem)
               ?? Subtitle(stem, candidates);
    }

    private static CatalogGame? Subtitle(string stem, IReadOnlyList<CatalogGame> candidates)
    {
        if (stem.Length < 4)
        {
            return null;
        }

        var hits = candidates.Where(g =>
        {
            var other = Base(Key(g.Name));
            var (shorter, longer) = other.Length <= stem.Length ? (other, stem) : (stem, other);
            return shorter.Length >= 4
                   && longer.StartsWith(shorter + " ", StringComparison.Ordinal)
                   && !char.IsDigit(longer[shorter.Length + 1])
                   && !IsRoman(longer[(shorter.Length + 1)..].Split(' ')[0])
                   && shorter.Length * 2 >= longer.Length;
        }).ToList();
        return hits.Count == 1 ? hits[0] : null;
    }

    private static bool IsRoman(string word) => word.Length is > 0 and <= 4 && word.All(static c => c is 'i' or 'v' or 'x');
}
