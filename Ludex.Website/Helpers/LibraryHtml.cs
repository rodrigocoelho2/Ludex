using System.Net;
using Ludex.Sdk.Models;
using Microsoft.AspNetCore.Html;

namespace Ludex.Website.Helpers;

/// <summary>
/// Small repeated bits of the library sidebar, written once.
/// </summary>
public static class LibraryHtml
{
    /// <summary>
    /// A game in the sidebar: its tiny cover and its name; draggable onto a collection.
    /// </summary>
    /// <param name="entry">The game.</param>
    /// <param name="current">The game open (it is highlighted).</param>
    /// <returns>The list item.</returns>
    public static IHtmlContent SideItem(LibraryEntryDTO entry, int? current)
    {
        var game = entry.Game;
        var name = WebUtility.HtmlEncode(game.Name);
        var thumb = game.CoverImageId is { Length: > 0 } cover
            ? $"<img src=\"{WebUtility.HtmlEncode(Igdb.Url(cover, Igdb.CoverSmall))}\" alt=\"\" loading=\"lazy\" decoding=\"async\" />"
            : string.Empty;
        var here = current == game.Id ? " aria-current=\"page\"" : string.Empty;
        return new HtmlString(
            $"<li data-side-name=\"{WebUtility.HtmlEncode(game.Name.ToLowerInvariant())}\">"
            + $"<a href=\"/games/{game.Id}\"{here} draggable=\"true\" data-drag-game=\"{game.Id}\">"
            + $"<span class=\"lib-thumb\" style=\"--tint:{WebUtility.HtmlEncode(game.Tint)}\">{thumb}</span>"
            + $"<span class=\"lib-side-name\">{name}</span></a></li>");
    }
}
