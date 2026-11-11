using System.Globalization;
using System.Net;
using Microsoft.AspNetCore.Html;

namespace Ludex.Website.Helpers;

/// <summary>
/// An account's avatar: its photo over its initials and colour (the initials stay when there is no photo, or while it loads).
/// </summary>
public static class Avatars
{
    /// <summary>
    /// Where a photo is served. The version is in the address, so a new photo is a new address and the old one can be cached forever.
    /// </summary>
    /// <param name="userId">The account.</param>
    /// <param name="version">The photo's version.</param>
    /// <returns>"/avatars/{id}?v=3".</returns>
    public static string Url(string userId, int version) => $"/avatars/{Uri.EscapeDataString(userId)}?v={version.ToString(CultureInfo.InvariantCulture)}";

    /// <summary>
    /// An avatar.
    /// </summary>
    /// <param name="userId">The account (null: initials only).</param>
    /// <param name="userName">The user name (initials and colour).</param>
    /// <param name="version">The photo's version (null: no photo).</param>
    /// <param name="cssClass">Extra classes (big, huge, ring…).</param>
    /// <returns>The HTML (decorative: hidden from screen readers, the name is always next to it).</returns>
    public static IHtmlContent Get(string? userId, string? userName, int? version, string? cssClass = null)
    {
        var css = cssClass is null ? "avatar" : "avatar " + cssClass;
        var photo = userId is { Length: > 0 } id && version is { } v
            ? $"<img src=\"{WebUtility.HtmlEncode(Url(id, v))}\" alt=\"\" loading=\"lazy\" decoding=\"async\" />"
            : string.Empty;
        return new HtmlString(
            $"<span class=\"{WebUtility.HtmlEncode(css)}\" style=\"--av:{Fmt.AvatarColour(userName)}\" aria-hidden=\"true\">"
            + $"<span class=\"avatar-initials\">{WebUtility.HtmlEncode(Fmt.Initials(userName))}</span>{photo}</span>");
    }
}
