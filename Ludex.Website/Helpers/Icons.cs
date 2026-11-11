using Microsoft.AspNetCore.Html;

namespace Ludex.Website.Helpers;

/// <summary>
/// The line icons (24×24, stroke = currentColor), square-ended to match the flat Metro look. Shipped with the app.
/// </summary>
public static class Icons
{
    private static readonly Dictionary<string, string> Paths = new(StringComparer.Ordinal)
    {
        ["home"] = "<path d=\"M3.5 11 12 4l8.5 7\"/><path d=\"M6 9.5V20h12V9.5\"/><path d=\"M10 20v-5h4v5\"/>",
        ["library"] = "<rect x=\"4\" y=\"4\" width=\"7\" height=\"7\"/><rect x=\"13\" y=\"4\" width=\"7\" height=\"7\"/><rect x=\"4\" y=\"13\" width=\"7\" height=\"7\"/><rect x=\"13\" y=\"13\" width=\"7\" height=\"7\"/>",
        ["discover"] = "<circle cx=\"11\" cy=\"11\" r=\"6.5\"/><path d=\"M16 16l4.5 4.5\"/>",
        ["friends"] = "<circle cx=\"9\" cy=\"8.5\" r=\"3.5\"/><path d=\"M3 20a6 6 0 0 1 12 0\"/><path d=\"M15.5 5.2a3.5 3.5 0 0 1 0 6.6M17.5 14.5A6 6 0 0 1 21 20\"/>",
        ["settings"] = "<circle cx=\"12\" cy=\"12\" r=\"3\"/><path d=\"M12 2.5v3M12 18.5v3M2.5 12h3M18.5 12h3M5.3 5.3l2.1 2.1M16.6 16.6l2.1 2.1M5.3 18.7l2.1-2.1M16.6 7.4l2.1-2.1\"/>",
        ["search"] = "<circle cx=\"11\" cy=\"11\" r=\"6.5\"/><path d=\"M16 16l4 4\"/>",
        ["plus"] = "<path d=\"M12 5v14M5 12h14\"/>",
        ["check"] = "<path d=\"M5 12.5l4.5 4.5L19 7.5\"/>",
        ["x"] = "<path d=\"M6 6l12 12M18 6L6 18\"/>",
        ["left"] = "<path d=\"M15 5l-7 7 7 7\"/>",
        ["right"] = "<path d=\"M9 5l7 7-7 7\"/>",
        ["arrow"] = "<path d=\"M5 12h14M13 6l6 6-6 6\"/>",
        ["star"] = "<path d=\"M12 3.8l2.5 5.2 5.7.8-4.1 4 1 5.6L12 16.7l-5.1 2.7 1-5.6-4.1-4 5.7-.8z\"/>",
        ["clock"] = "<circle cx=\"12\" cy=\"12\" r=\"8.5\"/><path d=\"M12 7.5V12l3 2\"/>",
        ["pad"] = "<path d=\"M7 8h10a4 4 0 0 1 4 4v2.5a2.5 2.5 0 0 1-4.5 1.5L15 14H9l-1.5 2A2.5 2.5 0 0 1 3 14.5V12a4 4 0 0 1 4-4z\"/><path d=\"M8 10.5v3M6.5 12h3M15.5 11h.01M17.5 13h.01\"/>",
        ["trash"] = "<path d=\"M4 7h16M9 7V4h6v3M6 7l1 13h10l1-13\"/>",
        ["logout"] = "<path d=\"M15 4h5v16h-5M10 16l-4-4 4-4M6 12h10\"/>",
        ["user"] = "<circle cx=\"12\" cy=\"8.5\" r=\"4\"/><path d=\"M4.5 20a7.5 7.5 0 0 1 15 0\"/>",
        ["user-plus"] = "<circle cx=\"10\" cy=\"8.5\" r=\"4\"/><path d=\"M3 20a7 7 0 0 1 13 -3.5\"/><path d=\"M19 13v6M16 16h6\"/>",
        ["lock"] = "<rect x=\"4.5\" y=\"10.5\" width=\"15\" height=\"10\"/><path d=\"M8 10.5V8a4 4 0 0 1 8 0v2.5\"/>",
        ["eye"] = "<path d=\"M2.5 12S6 5.5 12 5.5 21.5 12 21.5 12 18 18.5 12 18.5 2.5 12 2.5 12Z\"/><circle cx=\"12\" cy=\"12\" r=\"3\"/>",
        ["key"] = "<circle cx=\"8\" cy=\"15\" r=\"4\"/><path d=\"M11 12l8-8M16 7l2 2M14 9l2 2\"/>",
        ["server"] = "<rect x=\"3.5\" y=\"4\" width=\"17\" height=\"7\"/><rect x=\"3.5\" y=\"13\" width=\"17\" height=\"7\"/><path d=\"M7.5 7.5h.01M7.5 16.5h.01\"/>",
        ["sparkle"] = "<path d=\"M12 3l1.8 5.2L19 10l-5.2 1.8L12 17l-1.8-5.2L5 10l5.2-1.8z\"/>",
        ["info"] = "<circle cx=\"12\" cy=\"12\" r=\"9\"/><path d=\"M12 11v6M12 7.5v.5\"/>",
        ["image"] = "<rect x=\"3.5\" y=\"5\" width=\"17\" height=\"14\"/><circle cx=\"9\" cy=\"10\" r=\"1.6\"/><path d=\"M20.5 16l-5-5-8 8\"/>",
        ["heart"] = "<path d=\"M12 20s-7.5-4.6-7.5-10A4.5 4.5 0 0 1 12 7.3 4.5 4.5 0 0 1 19.5 10c0 5.4-7.5 10-7.5 10z\"/>",
        ["other"] = "<circle cx=\"12\" cy=\"12\" r=\"8\"/><path d=\"M9 12l2 2 4-4\"/>",
    };

    /// <summary>
    /// An icon.
    /// </summary>
    /// <param name="name">Its name.</param>
    /// <param name="cssClass">An extra class.</param>
    /// <returns>The SVG (decorative: hidden from screen readers).</returns>
    public static IHtmlContent Get(string name, string? cssClass = null) =>
        new HtmlString($"<svg class=\"ic{(cssClass is null ? string.Empty : " " + cssClass)}\" viewBox=\"0 0 24 24\" aria-hidden=\"true\">{(Paths.TryGetValue(name, out var path) ? path : Paths["other"])}</svg>");

    /// <summary>
    /// The Ludex mark: four tiles, one in the accent colour and one faded, like a Metro start screen.
    /// </summary>
    /// <param name="size">The size in px.</param>
    /// <returns>The SVG.</returns>
    public static IHtmlContent Logo(int size = 28) => new HtmlString(
        $"<svg class=\"logo-mark\" width=\"{size}\" height=\"{size}\" viewBox=\"0 0 40 40\" aria-hidden=\"true\">"
        + "<rect width=\"18\" height=\"18\" style=\"fill:var(--accent)\"/><rect x=\"22\" width=\"18\" height=\"18\" fill=\"currentColor\"/>"
        + "<rect y=\"22\" width=\"18\" height=\"18\" fill=\"currentColor\"/><rect x=\"22\" y=\"22\" width=\"18\" height=\"18\" fill=\"currentColor\" opacity=\".35\"/></svg>");
}
