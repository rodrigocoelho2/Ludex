using Ludex.Library.Enums;
using Ludex.WebApi.Features.Library.Models.DTOs;

namespace Ludex.WebApi.Features.Profile.Models.DTOs;

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

    /// <summary>Gets or sets the games changed last.</summary>
    public List<LibraryEntryDTO> Recent { get; set; } = [];

    /// <summary>Gets or sets the friends, playing first.</summary>
    public List<ProfileFriendDTO> Friends { get; set; } = [];

    /// <summary>Gets or sets the games of the library to choose the favourite from (own profile only).</summary>
    public List<LibraryEntryDTO> FavoriteChoices { get; set; } = [];
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
