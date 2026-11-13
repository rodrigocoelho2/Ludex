using Ludex.Library.Enums;

namespace Ludex.Sdk.Models;

/// <summary>A profile page. What comes from the library (showcases, friends) is only there for the account itself and its friends.</summary>
public sealed class ProfileDTO
{
    /// <summary>Gets or sets the account id.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Gets or sets the user name.</summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>Gets or sets the version of the photo (null: none).</summary>
    public int? AvatarVersion { get; set; }

    /// <summary>Gets or sets the bio.</summary>
    public string? Bio { get; set; }

    /// <summary>Gets or sets the location.</summary>
    public string? Location { get; set; }

    /// <summary>Gets or sets when the account was created.</summary>
    public DateTimeOffset MemberSince { get; set; }

    /// <summary>Gets or sets the last time the account used the app (null: hidden or never).</summary>
    public DateTimeOffset? LastSeenAt { get; set; }

    /// <summary>Gets or sets a value indicating whether it is the signed-in account's own profile.</summary>
    public bool IsMe { get; set; }

    /// <summary>Gets or sets how the signed-in account relates to this one.</summary>
    public FriendState FriendState { get; set; }

    /// <summary>Gets or sets a value indicating whether the library parts are there (oneself or a friend).</summary>
    public bool CanSeeLibrary { get; set; }

    /// <summary>Gets or sets the level and XP.</summary>
    public ProfileLevelDTO Level { get; set; } = new();

    /// <summary>Gets or sets every badge, earned or not (earned first).</summary>
    public List<BadgeDTO> Badges { get; set; } = [];

    /// <summary>Gets or sets the game being played now (the one changed last).</summary>
    public LibraryEntryDTO? Playing { get; set; }

    /// <summary>Gets or sets the favourite game (chosen, or the best rated).</summary>
    public LibraryEntryDTO? Favorite { get; set; }

    /// <summary>Gets or sets a value indicating whether <see cref="Favorite"/> was chosen rather than guessed.</summary>
    public bool FavoriteChosen { get; set; }

    /// <summary>Gets or sets the numbers.</summary>
    public LibraryStatsDTO Stats { get; set; } = new();

    /// <summary>Gets or sets the counts per status.</summary>
    public LibraryCountsDTO Counts { get; set; } = new();

    /// <summary>Gets or sets the best rated games.</summary>
    public List<LibraryEntryDTO> TopRated { get; set; } = [];

    /// <summary>Gets or sets the games played last (the most played on Steam in the last two weeks first).</summary>
    public List<LibraryEntryDTO> Recent { get; set; } = [];

    /// <summary>Gets or sets the hours played on Steam in the last two weeks.</summary>
    public decimal RecentHours { get; set; }

    /// <summary>Gets or sets the Steam achievements unlocked in all.</summary>
    public int Achievements { get; set; }

    /// <summary>Gets or sets the friends, playing first.</summary>
    public List<ProfileFriendDTO> Friends { get; set; } = [];

    /// <summary>Gets or sets the games of the library to choose the favourite from (own profile only).</summary>
    public List<LibraryEntryDTO> FavoriteChoices { get; set; } = [];

    /// <summary>Gets or sets the colour the profile shows in (an accent key: cyan, violet…).</summary>
    public string Accent { get; set; } = "cyan";

    /// <summary>Gets or sets the banner: art, glow or plain.</summary>
    public string Banner { get; set; } = "art";

    /// <summary>Gets or sets the banner's game (its artwork, or its cover when it has none).</summary>
    public GameSummaryDTO? BannerGame { get; set; }

    /// <summary>Gets or sets the banner game's artwork (IGDB image id; null: use the cover).</summary>
    public string? BannerImageId { get; set; }

    /// <summary>Gets or sets the frame round the photo: ring, glow or none.</summary>
    public string Frame { get; set; } = "ring";

    /// <summary>Gets or sets the badge shown under the name (null: none chosen).</summary>
    public BadgeDTO? Title { get; set; }

    /// <summary>Gets or sets what the account plays (up to five).</summary>
    public List<string> Tags { get; set; } = [];

    /// <summary>Gets or sets a line about the favourite game.</summary>
    public string? FavoriteNote { get; set; }

    /// <summary>Gets or sets the showcases shown, in order (favourite, numbers, rare, year, top, playing, platinums, closest, finished, recent, collection, badges).</summary>
    public List<string> Showcases { get; set; } = [];

    /// <summary>Gets or sets who sees the library and showcases.</summary>
    public LibraryVisibility LibraryVisibility { get; set; } = LibraryVisibility.Friends;

    /// <summary>Gets or sets a value indicating whether others see what is being played.</summary>
    public bool ShowPlaying { get; set; } = true;

    /// <summary>Gets or sets the hours by platform family (PC, PlayStation…), most first.</summary>
    public List<PlatformHoursDTO> PlatformHours { get; set; } = [];

    /// <summary>Gets or sets how many games have every achievement or trophy.</summary>
    public int Completed { get; set; }

    /// <summary>Gets or sets the rarest achievements and trophies earned (Steam and PlayStation).</summary>
    public List<RareAchievementDTO> Rare { get; set; } = [];

    /// <summary>Gets or sets how long was played each day of the last year (days played only).</summary>
    public List<PlayDayDTO> PlayDays { get; set; } = [];

    /// <summary>Gets or sets the games with every achievement or trophy, the last first.</summary>
    public List<LibraryEntryDTO> Platinums { get; set; } = [];

    /// <summary>Gets or sets the games closest to every achievement (not there yet).</summary>
    public List<LibraryEntryDTO> Closest { get; set; } = [];

    /// <summary>Gets or sets the games finished last.</summary>
    public List<LibraryEntryDTO> Finished { get; set; } = [];

    /// <summary>Gets or sets the collection on the side.</summary>
    public ProfileCollectionDTO? Pinned { get; set; }

    /// <summary>Gets or sets the account's collections to pin one from (own profile only).</summary>
    public List<ProfileCollectionDTO> CollectionChoices { get; set; } = [];
}

/// <summary>Who sees a library.</summary>
public enum LibraryVisibility
{
    /// <summary>Friends (the default).</summary>
    Friends = 1,

    /// <summary>Everyone on this Ludex.</summary>
    Everyone = 2,

    /// <summary>Only the account.</summary>
    OnlyMe = 3,
}

/// <summary>Hours on one family of platforms.</summary>
public sealed class PlatformHoursDTO
{
    /// <summary>Gets or sets the family (PC, PlayStation, Xbox, Nintendo…).</summary>
    public string Family { get; set; } = string.Empty;

    /// <summary>Gets or sets the hours.</summary>
    public decimal Hours { get; set; }
}

/// <summary>A rare achievement or trophy earned.</summary>
public sealed class RareAchievementDTO
{
    /// <summary>Gets or sets its name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets its game.</summary>
    public int GameId { get; set; }

    /// <summary>Gets or sets the game's name.</summary>
    public string GameName { get; set; } = string.Empty;

    /// <summary>Gets or sets where it was earned (Steam, PS5…).</summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>Gets or sets its icon.</summary>
    public string? IconUrl { get; set; }

    /// <summary>Gets or sets the % of players who have it.</summary>
    public double Percent { get; set; }

    /// <summary>Gets or sets the trophy grade (Bronze, Silver, Gold, Platinum; null: a Steam achievement).</summary>
    public string? Grade { get; set; }
}

/// <summary>Hours played on a day.</summary>
public sealed class PlayDayDTO
{
    /// <summary>Gets or sets the day (midnight UTC).</summary>
    public DateTimeOffset Day { get; set; }

    /// <summary>Gets or sets the hours.</summary>
    public decimal Hours { get; set; }
}

/// <summary>A collection on a profile.</summary>
public sealed class ProfileCollectionDTO
{
    /// <summary>Gets or sets its id.</summary>
    public int Id { get; set; }

    /// <summary>Gets or sets its name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets how many games it has.</summary>
    public int Count { get; set; }

    /// <summary>Gets or sets its first games.</summary>
    public List<GameSummaryDTO> Games { get; set; } = [];
}

/// <summary>Everything Customize profile saves.</summary>
public sealed class ProfileCustomizeRequest
{
    /// <summary>Gets or sets the account.</summary>
    public string? UserId { get; set; }

    /// <summary>Gets or sets the bio.</summary>
    public string? Bio { get; set; }

    /// <summary>Gets or sets the location.</summary>
    public string? Location { get; set; }

    /// <summary>Gets or sets the favourite game (null: the best rated).</summary>
    public int? FavoriteGameId { get; set; }

    /// <summary>Gets or sets a line about the favourite game.</summary>
    public string? FavoriteNote { get; set; }

    /// <summary>Gets or sets the colour (an accent key).</summary>
    public string? Accent { get; set; }

    /// <summary>Gets or sets the banner: art, glow or plain.</summary>
    public string? Banner { get; set; }

    /// <summary>Gets or sets the banner's game (null: the favourite).</summary>
    public int? BannerGameId { get; set; }

    /// <summary>Gets or sets the frame: ring, glow or none.</summary>
    public string? Frame { get; set; }

    /// <summary>Gets or sets the badge shown under the name (an earned badge's id; null: none).</summary>
    public string? Title { get; set; }

    /// <summary>Gets or sets what the account plays (up to five).</summary>
    public List<string> Tags { get; set; } = [];

    /// <summary>Gets or sets the showcases, in order (up to six).</summary>
    public List<string> Showcases { get; set; } = [];

    /// <summary>Gets or sets the collection on the side.</summary>
    public int? PinnedCollectionId { get; set; }

    /// <summary>Gets or sets who sees the library.</summary>
    public LibraryVisibility LibraryVisibility { get; set; } = LibraryVisibility.Friends;

    /// <summary>Gets or sets a value indicating whether others see what is being played.</summary>
    public bool ShowPlaying { get; set; } = true;
}

/// <summary>A level: XP comes from the library (finished games, ratings, hours, games).</summary>
public sealed class ProfileLevelDTO
{
    /// <summary>Gets or sets the level (1 and up).</summary>
    public int Level { get; set; } = 1;

    /// <summary>Gets or sets the XP in all.</summary>
    public int Xp { get; set; }

    /// <summary>Gets or sets the XP the level starts at.</summary>
    public int LevelXp { get; set; }

    /// <summary>Gets or sets the XP the next level starts at.</summary>
    public int NextLevelXp { get; set; } = 100;
}

/// <summary>A badge, earned or on the way.</summary>
public sealed class BadgeDTO
{
    /// <summary>Gets or sets the id.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Gets or sets the name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets how to earn it.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Gets or sets the icon's name (the Website's icon set).</summary>
    public string Icon { get; set; } = "star";

    /// <summary>Gets or sets its colour.</summary>
    public string Color { get; set; } = "#4FC3F7";

    /// <summary>Gets or sets how far along it is.</summary>
    public int Progress { get; set; }

    /// <summary>Gets or sets what it takes.</summary>
    public int Target { get; set; } = 1;

    /// <summary>Gets or sets a value indicating whether it is earned.</summary>
    public bool Earned { get; set; }
}

/// <summary>A friend on a profile.</summary>
public sealed class ProfileFriendDTO
{
    /// <summary>Gets or sets the account id.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Gets or sets the user name.</summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>Gets or sets the version of the photo.</summary>
    public int? AvatarVersion { get; set; }

    /// <summary>Gets or sets the last time the friend used the app.</summary>
    public DateTimeOffset? LastSeenAt { get; set; }

    /// <summary>Gets or sets the game the friend is playing.</summary>
    public int? PlayingGameId { get; set; }

    /// <summary>Gets or sets that game's name.</summary>
    public string? PlayingName { get; set; }

    /// <summary>Gets or sets how many games the friend has.</summary>
    public int GameCount { get; set; }
}

/// <summary>A profile photo.</summary>
public sealed class AvatarDTO
{
    /// <summary>Gets or sets the image (empty when only the version is answered).</summary>
    public byte[] Data { get; set; } = [];

    /// <summary>Gets or sets its media type.</summary>
    public string ContentType { get; set; } = string.Empty;

    /// <summary>Gets or sets its version (null: no photo).</summary>
    public int? Version { get; set; }
}

/// <summary>Saves the bio, location and favourite game.</summary>
public sealed class ProfileSaveRequest
{
    /// <summary>Gets or sets the account.</summary>
    public string? UserId { get; set; }

    /// <summary>Gets or sets the bio.</summary>
    public string? Bio { get; set; }

    /// <summary>Gets or sets the location.</summary>
    public string? Location { get; set; }

    /// <summary>Gets or sets the favourite game (null: the best rated is shown).</summary>
    public int? FavoriteGameId { get; set; }
}

/// <summary>Makes a game the favourite (the game page's button), or clears it.</summary>
public sealed class ProfileFavoriteRequest
{
    /// <summary>Gets or sets the account.</summary>
    public string? UserId { get; set; }

    /// <summary>Gets or sets the game (null: clear).</summary>
    public int? GameId { get; set; }
}

/// <summary>Sets the colour theme.</summary>
public sealed class ProfileAccentRequest
{
    /// <summary>Gets or sets the account.</summary>
    public string? UserId { get; set; }

    /// <summary>Gets or sets the theme's key (null: the default).</summary>
    public string? Accent { get; set; }
}

/// <summary>Sets the profile photo, or removes it.</summary>
public sealed class ProfileAvatarRequest
{
    /// <summary>Gets or sets the account.</summary>
    public string? UserId { get; set; }

    /// <summary>Gets or sets the image (empty: remove the photo).</summary>
    public byte[]? Data { get; set; }
}

/// <summary>Names an account (the "last seen" heartbeat).</summary>
public sealed class ProfileUserRequest
{
    /// <summary>Gets or sets the account.</summary>
    public string? UserId { get; set; }
}
