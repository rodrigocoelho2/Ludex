using Ludex.Library.Enums;
using Ludex.Sdk.Models;

namespace Ludex.Website.Model;

/// <summary>The screens of the app (picks the highlighted tab).</summary>
public enum AppSection
{
    /// <summary>Home: the hero, the numbers, the last games and the friends' activity.</summary>
    Home,

    /// <summary>The library.</summary>
    Library,

    /// <summary>Discover: search the catalog and add games.</summary>
    Discover,

    /// <summary>Friends.</summary>
    Friends,

    /// <summary>Settings.</summary>
    Settings,
}

/// <summary>
/// What every signed-in screen needs for the shell: who's signed in and which tab is on.
/// </summary>
public class AppPageModel
{
    /// <summary>The product name.</summary>
    public const string ProductName = "Ludex";

    /// <summary>The version shown in the app.</summary>
    public const string CurrentVersion = "v1.0";

    private readonly HashSet<int> _transitions = [];

    /// <summary>Gets or sets the signed-in user name.</summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>Gets or sets a value indicating whether the signed-in account is an admin.</summary>
    public bool IsAdmin { get; set; }

    /// <summary>Gets or sets the screen.</summary>
    public AppSection Section { get; set; }

    /// <summary>Gets or sets a message when the WebApi can't be reached (the screen shows it instead of data).</summary>
    public string? Unavailable { get; set; }

    /// <summary>Gets or sets a one-off message at the top of the page (after a form was sent).</summary>
    public string? Notice { get; set; }

    /// <summary>Gets or sets a one-off problem at the top of the page (after a form was sent).</summary>
    public string? Problem { get; set; }

    /// <summary>Gets the initials shown in the avatar.</summary>
    public string Initials => Helpers.Fmt.Initials(UserName);

    /// <summary>
    /// The view-transition name of a game's cover: the cover flies to the same game's cover on the next page. Each name
    /// may appear once per page, so a game shown twice (the hero and the recent row) only names its first cover.
    /// </summary>
    /// <param name="gameId">The game.</param>
    /// <returns>"cover-1942", or "none" when this page already named that game.</returns>
    public string Transition(int gameId) => _transitions.Add(gameId) ? $"cover-{gameId}" : "none";
}

/// <summary>The home page.</summary>
public sealed class HomePageModel : AppPageModel
{
    /// <summary>Gets or sets the data.</summary>
    public HomeDTO Home { get; set; } = new();
}

/// <summary>The library.</summary>
public sealed class LibraryPageModel : AppPageModel
{
    /// <summary>Gets or sets the library.</summary>
    public LibraryDTO Library { get; set; } = new();

    /// <summary>Gets or sets the status shown (null: all).</summary>
    public GameStatus? Status { get; set; }

    /// <summary>Gets the games shown.</summary>
    public IEnumerable<LibraryEntryDTO> Shown => Status is { } only ? Library.Entries.Where(e => e.Status == only) : Library.Entries;

    /// <summary>Gets the platform families present, for the filter.</summary>
    public IEnumerable<string> Families =>
        new[] { "PC", "PlayStation", "Nintendo", "Xbox", "Mobile", "Other" }.Where(f => Library.Entries.Any(e => e.PlatformFamily == f));
}

/// <summary>Discover: the search and the friends' favourites.</summary>
public sealed class DiscoverPageModel : AppPageModel
{
    /// <summary>Gets or sets the text searched.</summary>
    public string Query { get; set; } = string.Empty;

    /// <summary>Gets or sets the results (null: nothing searched yet).</summary>
    public SearchResultDTO? Result { get; set; }

    /// <summary>Gets or sets why the search failed.</summary>
    public string? SearchError { get; set; }

    /// <summary>Gets or sets the games the friends have.</summary>
    public List<PopularGameDTO> Popular { get; set; } = [];

    /// <summary>Gets or sets the catalog in use.</summary>
    public CatalogSource Source { get; set; }
}

/// <summary>A game's page.</summary>
public sealed class GamePageModel : AppPageModel
{
    /// <summary>Gets or sets the game.</summary>
    public GameDetailDTO Detail { get; set; } = new();

    /// <summary>Gets the platforms offered in the "played on" list: the game's, plus the one saved if it's not among them.</summary>
    public IEnumerable<string> PlatformChoices
    {
        get
        {
            var list = Detail.Game.Platforms.ToList();
            if (Detail.Mine?.Platform is { Length: > 0 } saved && !list.Contains(saved, StringComparer.OrdinalIgnoreCase))
            {
                list.Insert(0, saved);
            }

            return list;
        }
    }
}

/// <summary>Friends.</summary>
public sealed class FriendsPageModel : AppPageModel
{
    /// <summary>Gets or sets the data.</summary>
    public FriendsDTO Friends { get; set; } = new();

    /// <summary>Gets or sets the user name typed in the request form (kept after an error).</summary>
    public string RequestName { get; set; } = string.Empty;
}

/// <summary>Settings.</summary>
public sealed class SettingsPageModel : AppPageModel
{
    /// <summary>Gets or sets the WebApi's health (null: unreachable).</summary>
    public HealthDTO? Health { get; set; }

    /// <summary>Gets or sets the accounts (admins only).</summary>
    public List<UserDTO> Users { get; set; } = [];

    /// <summary>Gets or sets the signed-in account id.</summary>
    public string UserId { get; set; } = string.Empty;
}
