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
    /// <summary>How many games a shelf holds.</summary>
    public const int ShelfSize = 18;

    /// <summary>Gets or sets the data.</summary>
    public HomeDTO Home { get; set; } = new();

    /// <summary>Gets or sets the whole library (the sidebar and the shelves).</summary>
    public LibraryDTO Library { get; set; } = new();

    private int? HeroId => Home.Hero?.Game.Id;

    /// <summary>Gets the games touched last (played, synced from Steam or changed), the wishlist aside.</summary>
    public List<LibraryEntryDTO> RecentGames => [.. Library.Entries
        .Where(e => e.Status != GameStatus.Wishlist && e.Game.Id != HeroId)
        .OrderByDescending(static e => e.UpdatedAt)
        .Take(ShelfSize)];

    /// <summary>Gets the games being played, latest first.</summary>
    public List<LibraryEntryDTO> ContinuePlaying => [.. Library.Entries
        .Where(e => e.Status == GameStatus.Playing && e.Game.Id != HeroId)
        .OrderByDescending(static e => e.RecentHours ?? 0)
        .ThenByDescending(static e => e.UpdatedAt)
        .Take(ShelfSize)];

    /// <summary>Gets the backlog, best reviewed first, then shortest.</summary>
    public List<LibraryEntryDTO> PlayNext => [.. Library.Entries
        .Where(static e => e.Status == GameStatus.Backlog)
        .OrderByDescending(static e => e.Game.CriticScore ?? -1)
        .ThenBy(static e => e.Game.TimeToBeatMain ?? decimal.MaxValue)
        .ThenBy(static e => e.Game.Name, StringComparer.OrdinalIgnoreCase)
        .Take(ShelfSize)];

    /// <summary>Gets the wishlisted games on sale or under their price alert, biggest cut first.</summary>
    public List<LibraryEntryDTO> Deals => [.. Library.Entries
        .Where(static e => e.Status == GameStatus.Wishlist && IsDeal(e.Price))
        .OrderByDescending(static e => e.Price!.Cut ?? 0)
        .ThenBy(static e => e.Price!.Amount)
        .Take(ShelfSize)];

    /// <summary>Gets the number of backlog games.</summary>
    public int BacklogCount => Library.Counts.Backlog;

    /// <summary>Whether a price is worth a look: on sale, or at or under the alert.</summary>
    /// <param name="price">The price.</param>
    /// <returns>True for a deal.</returns>
    public static bool IsDeal(PriceDTO? price) =>
        price?.Amount is { } amount && (price.Cut is > 0 || (price.Target is { } target && amount <= target));
}

/// <summary>A game on one of the home page's shelves.</summary>
/// <param name="Entry">The game in the library.</param>
/// <param name="Transition">Its cover's view-transition name.</param>
/// <param name="Sub">The line under the name.</param>
/// <param name="Chip">A label on the cover (null: none).</param>
/// <param name="ChipCss">The label's extra class.</param>
public sealed record ShelfCard(LibraryEntryDTO Entry, string Transition, string Sub, string? Chip = null, string? ChipCss = null);

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
/// <param name="AtHome">On the home page (nothing highlighted).</param>
public sealed record LibrarySidebarView(IReadOnlyList<LibraryEntryDTO> Entries, int? Current, IReadOnlyList<CollectionDTO> Collections, bool AtHome = false);

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

    /// <summary>Gets or sets the account's PlayStation trophies in this game (null: not a PlayStation copy, or unknown).</summary>
    public PsnTrophiesDTO? Trophies { get; set; }

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

    /// <summary>Gets or sets a value indicating whether Discord sign-in is set up on this Ludex.</summary>
    public bool DiscordEnabled { get; set; }

    /// <summary>Gets or sets a value indicating whether this account has a Discord account linked.</summary>
    public bool DiscordLinked { get; set; }

    /// <summary>Gets or sets the linked Discord account's name.</summary>
    public string? DiscordName { get; set; }

    /// <summary>Gets or sets a value indicating whether the account has a password (one made with Discord may not).</summary>
    public bool HasPassword { get; set; } = true;

    /// <summary>Gets or sets what just happened with Discord (linked, unlinked, taken, failed), for a note.</summary>
    public string? DiscordNote { get; set; }
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

/// <summary>The Playnite import page.</summary>
public sealed class PlaynitePageModel : AppPageModel
{
    /// <summary>Gets or sets the file read (null: none yet).</summary>
    public string? FileName { get; set; }

    /// <summary>Gets or sets what was read (Available: whether the import can run).</summary>
    public PlaynitePreviewDTO? Preview { get; set; }

    /// <summary>Gets or sets why the file couldn't be read.</summary>
    public string? ReadProblem { get; set; }

    /// <summary>Gets the games new to the library.</summary>
    public List<ImportGameDTO> New => Preview is null ? [] : [.. Preview.Games.Where(static g => g.Game.MyStatus is null)];

    /// <summary>Gets the games already in the library.</summary>
    public List<ImportGameDTO> Known => Preview is null ? [] : [.. Preview.Games.Where(static g => g.Game.MyStatus is not null)];
}

/// <summary>The games of an import to review (the Playnite and PlayStation pages).</summary>
/// <param name="New">The games new to the library.</param>
/// <param name="Known">The games already there.</param>
/// <param name="Unmatched">The names not matched.</param>
/// <param name="KnownNote">What happens to the games already there.</param>
public sealed record ImportListView(List<ImportGameDTO> New, List<ImportGameDTO> Known, List<string> Unmatched, string KnownNote);

/// <summary>The PlayStation page.</summary>
public sealed class PsnPageModel : AppPageModel
{
    /// <summary>Gets or sets the connection.</summary>
    public PsnStatusDTO Status { get; set; } = new();

    /// <summary>Gets or sets the games read (null: not loaded).</summary>
    public PsnPreviewDTO? Preview { get; set; }

    /// <summary>Gets the games new to the library.</summary>
    public List<ImportGameDTO> New => Preview is null ? [] : [.. Preview.Games.Where(static g => g.Game.MyStatus is null)];

    /// <summary>Gets the games already in the library.</summary>
    public List<ImportGameDTO> Known => Preview is null ? [] : [.. Preview.Games.Where(static g => g.Game.MyStatus is not null)];
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

/// <summary>The head of a home shelf.</summary>
/// <param name="Title">The title.</param>
/// <param name="Note">A short note beside it (null: none).</param>
/// <param name="SeeAll">Where "See all" goes (null: no link).</param>
public sealed record ShelfHead(string Title, string? Note = null, string? SeeAll = null);
