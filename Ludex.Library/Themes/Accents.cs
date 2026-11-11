namespace Ludex.Library.Themes;

/// <summary>
/// The colour themes an account can pick in Settings. The WebApi only saves these keys; the Website puts the key on
/// the page (<c>&lt;html data-accent&gt;</c>) and app.css holds the colours.
/// </summary>
public static class Accents
{
    /// <summary>The default (no key saved).</summary>
    public const string Default = "cyan";

    /// <summary>Every theme: its key, its name and its main colour (for the swatch), in the order offered.</summary>
    public static readonly IReadOnlyList<Accent> All =
    [
        new("cyan", "Cyan", "#4FC3F7"),
        new("blue", "Deep blue", "#5A8DFF"),
        new("emerald", "Emerald", "#34D399"),
        new("lime", "Lime", "#A3E635"),
        new("gold", "Gold", "#FBBF24"),
        new("ember", "Ember", "#FB923C"),
        new("crimson", "Crimson", "#F87171"),
        new("rose", "Rose", "#F472B6"),
        new("violet", "Violet", "#A78BFA"),
    ];

    /// <summary>Whether a key is a theme.</summary>
    /// <param name="key">The key.</param>
    /// <returns>True when it is one of <see cref="All"/>.</returns>
    public static bool IsKnown(string? key) => key is not null && All.Any(a => string.Equals(a.Key, key, StringComparison.Ordinal));

    /// <summary>The key to use for what was saved (unknown or missing: the default).</summary>
    /// <param name="key">What was saved.</param>
    /// <returns>A known key.</returns>
    public static string Normalize(string? key) => IsKnown(key) ? key! : Default;
}

/// <summary>A colour theme.</summary>
/// <param name="Key">What's saved and put on the page.</param>
/// <param name="Name">What Settings shows.</param>
/// <param name="Color">The main colour.</param>
public sealed record Accent(string Key, string Name, string Color);
