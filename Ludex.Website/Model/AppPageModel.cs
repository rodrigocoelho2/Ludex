using Ludex.Library.Enums;
using Ludex.Library.Platforms;
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

    /// <summary>A profile (one's own or someone else's).</summary>
    Profile,
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

    /// <summary>Gets or sets the signed-in account's id.</summary>
    public string MyId { get; set; } = string.Empty;

    /// <summary>Gets or sets the version of the signed-in account's photo (null: none).</summary>
    public int? MyAvatarVersion { get; set; }

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

    /// <summary>Gets or sets the collection shown (null: all).</summary>
    public int? Collection { get; set; }

    /// <summary>Gets the games shown.</summary>
    public IEnumerable<LibraryEntryDTO> Shown => Status is { } only ? Library.Entries.Where(e => e.Status == only) : Library.Entries;

    /// <summary>Gets the platform families present, each with the platforms played on, for the filter.</summary>
    public IEnumerable<(string Family, List<string> Platforms)> Families =>
        new[] { "PC", "PlayStation", "Nintendo", "Xbox", "Mobile", "Other" }
            .Select(f => (f, Library.Entries.Where(e => e.PlatformFamily == f).Select(static e => e.Platform).OfType<string>()
                .Distinct(StringComparer.OrdinalIgnoreCase).Order(StringComparer.OrdinalIgnoreCase).ToList()))
            .Where(f => Library.Entries.Any(e => e.PlatformFamily == f.Item1));

    /// <summary>Gets the kinds of copy present: physical, digital, then the PC stores.</summary>
    public IEnumerable<string> Copies =>
        new[] { "Physical", "Digital" }.Where(f => Library.Entries.Any(e => e.Format?.ToString() == f))
            .Concat(GameCopies.PcStores.Where(s => Library.Entries.Any(e => e.Store == s)));
}

/// <summary>
/// What the library sidebar draws: every game by status, the one open highlighted.
/// </summary>
/// <param name="Entries">The library.</param>
/// <param name="Current">The game open (null: the library page).</param>
/// <param name="Collections">The account's collections.</param>
public sealed record LibrarySidebarView(IReadOnlyList<LibraryEntryDTO> Entries, int? Current, IReadOnlyList<CollectionDTO> Collections);

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

    /// <summary>Gets or sets the library down the side.</summary>
    public List<LibraryEntryDTO> Sidebar { get; set; } = [];

    /// <summary>Gets or sets the collections down the side.</summary>
    public List<CollectionDTO> SidebarCollections { get; set; } = [];

    /// <summary>Gets or sets the account's Steam achievements in this game (null: not a Steam game, none, or unreachable).</summary>
    public AchievementsDTO? Achievements { get; set; }

    /// <summary>Gets the copies each platform of the "played on" list can have, for the page script.</summary>
    public Dictionary<string, IReadOnlyList<string>> CopyChoices =>
        PlatformChoices.Append("Other").Append(string.Empty).Distinct(StringComparer.OrdinalIgnoreCase)
            .ToDictionary(static p => p, static p => GameCopies.Choices(p.Length == 0 ? null : p), StringComparer.OrdinalIgnoreCase);

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

/// <summary>A profile.</summary>
public sealed class ProfilePageModel : AppPageModel
{
    /// <summary>Gets or sets the profile.</summary>
    public ProfileDTO Profile { get; set; } = new();

    /// <summary>Gets the share of the way from this level to the next (0–1).</summary>
    public double LevelProgress
    {
        get
        {
            var level = Profile.Level;
            var span = level.NextLevelXp - level.LevelXp;
            return span <= 0 ? 0 : Math.Clamp((double)(level.Xp - level.LevelXp) / span, 0, 1);
        }
    }

    /// <summary>Gets the badges earned.</summary>
    public List<BadgeDTO> Earned => [.. Profile.Badges.Where(static b => b.Earned)];

    /// <summary>Gets the colour of the level ring: it changes every ten levels, like Steam's.</summary>
    public string LevelColour => ((Profile.Level.Level - 1) / 10) switch
    {
        0 => "#9FB0C3",
        1 => "#C98B5B",
        2 => "#4FC3F7",
        3 => "#8EDB97",
        4 => "#B794F6",
        5 => "#F2777A",
        _ => "#F5B841",
    };
}

/// <summary>The Steam import.</summary>
public sealed class SteamPageModel : AppPageModel
{
    /// <summary>Gets or sets the Steam profile typed.</summary>
    public string ProfileInput { get; set; } = string.Empty;

    /// <summary>Gets or sets what was read (null: nothing yet).</summary>
    public SteamPreviewDTO? Preview { get; set; }

    /// <summary>Gets or sets why the profile couldn't be read.</summary>
    public string? ReadProblem { get; set; }

    /// <summary>Gets the games new to the library.</summary>
    public List<SteamGameDTO> New => Preview is null ? [] : [.. Preview.Games.Where(static g => g.Game.MyStatus is null)];

    /// <summary>Gets the games already in the library.</summary>
    public List<SteamGameDTO> Known => Preview is null ? [] : [.. Preview.Games.Where(static g => g.Game.MyStatus is not null)];
}

/// <summary>Editing one's profile.</summary>
public sealed class ProfileEditPageModel : AppPageModel
{
    /// <summary>Gets or sets the profile.</summary>
    public ProfileDTO Profile { get; set; } = new();
}

/// <summary>The bell's page.</summary>
public sealed class NotificationsPageModel : AppPageModel
{
    /// <summary>Gets or sets the notifications.</summary>
    public NotificationsDTO Notifications { get; set; } = new();
}
