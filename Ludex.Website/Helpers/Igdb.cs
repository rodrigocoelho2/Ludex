namespace Ludex.Website.Helpers;

/// <summary>
/// Addresses of IGDB's images (https://api-docs.igdb.com/#images). The browser loads them straight from
/// images.igdb.com; Ludex only keeps the image ids.
/// </summary>
public static class Igdb
{
    /// <summary>A small cover (90×128): the blurred placeholder of the blur-up.</summary>
    public const string CoverSmall = "t_cover_small";

    /// <summary>A card cover (264×374).</summary>
    public const string CoverBig = "t_cover_big";

    /// <summary>A sharp cover for big screens (528×748).</summary>
    public const string CoverBig2x = "t_cover_big_2x";

    /// <summary>A screenshot in the grid (569×320).</summary>
    public const string ScreenshotMed = "t_screenshot_med";

    /// <summary>A full-width image (1920×1080).</summary>
    public const string Full = "t_1080p";

    /// <summary>
    /// The address of an image.
    /// </summary>
    /// <param name="imageId">IGDB's image id.</param>
    /// <param name="size">One of the size constants.</param>
    /// <returns>The URL.</returns>
    public static string Url(string imageId, string size) => $"https://images.igdb.com/igdb/image/upload/{size}/{Uri.EscapeDataString(imageId)}.jpg";
}
