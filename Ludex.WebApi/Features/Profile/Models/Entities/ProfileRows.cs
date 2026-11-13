namespace Ludex.WebApi.Features.Profile.Models.Entities;

/// <summary>An account and its profile (dbo.ProfileGet).</summary>
public sealed class ProfileRow
{
    /// <summary>Gets or sets the public id.</summary>
    public Guid UserUniqueID { get; set; }

    /// <summary>Gets or sets the user name.</summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>Gets or sets when the account was created (UTC).</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Gets or sets the last sign-in (UTC).</summary>
    public DateTime? LastLoginAt { get; set; }

    /// <summary>Gets or sets the bio.</summary>
    public string? Bio { get; set; }

    /// <summary>Gets or sets the location.</summary>
    public string? Location { get; set; }

    /// <summary>Gets or sets the favourite game.</summary>
    public int? FavoriteGameID { get; set; }

    /// <summary>Gets or sets the version of the photo (null: none).</summary>
    public int? AvatarVersion { get; set; }

    /// <summary>Gets or sets the last time the account used the app (UTC).</summary>
    public DateTime? LastSeenAt { get; set; }

    /// <summary>Gets or sets the SteamID64 the library was last imported from.</summary>
    public string? SteamID { get; set; }

    /// <summary>Gets or sets when the library was last synced with Steam (UTC).</summary>
    public DateTime? SteamSyncedAt { get; set; }

    /// <summary>Gets or sets the colour (null: the default).</summary>
    public string? Accent { get; set; }

    /// <summary>Gets or sets the banner: art, glow or plain (null: art).</summary>
    public string? Banner { get; set; }

    /// <summary>Gets or sets the game whose artwork is the banner (null: the favourite's).</summary>
    public int? BannerGameID { get; set; }

    /// <summary>Gets or sets the frame round the photo: ring, glow or none (null: ring).</summary>
    public string? Frame { get; set; }

    /// <summary>Gets or sets the badge shown under the name.</summary>
    public string? Title { get; set; }

    /// <summary>Gets or sets what the account plays, separated by |.</summary>
    public string? Tags { get; set; }

    /// <summary>Gets or sets a line about the favourite game.</summary>
    public string? FavoriteNote { get; set; }

    /// <summary>Gets or sets the showcases, in order, comma separated (null: the default).</summary>
    public string? Showcases { get; set; }

    /// <summary>Gets or sets the collection on the side.</summary>
    public int? PinnedCollectionID { get; set; }

    /// <summary>Gets or sets who sees the library: 1 friends, 2 everyone, 3 only me (null: friends).</summary>
    public byte? LibraryVisibility { get; set; }

    /// <summary>Gets or sets whether others see what is being played (null: yes).</summary>
    public bool? ShowPlaying { get; set; }
}

/// <summary>Hours played on a day (dbo.ProfilePlayDays).</summary>
public sealed class PlayDayRow
{
    /// <summary>Gets or sets the day.</summary>
    public DateTime Day { get; set; }

    /// <summary>Gets or sets the hours.</summary>
    public decimal Hours { get; set; }
}

/// <summary>A profile photo (dbo.ProfileAvatarGet).</summary>
public sealed class AvatarRow
{
    /// <summary>Gets or sets the image.</summary>
    public byte[] Avatar { get; set; } = [];

    /// <summary>Gets or sets its media type.</summary>
    public string AvatarContentType { get; set; } = "image/jpeg";

    /// <summary>Gets or sets its version.</summary>
    public int AvatarVersion { get; set; }
}

/// <summary>A friend on a profile (dbo.ProfileFriends).</summary>
public sealed class ProfileFriendRow
{
    /// <summary>Gets or sets the friend's public id.</summary>
    public Guid UserUniqueID { get; set; }

    /// <summary>Gets or sets the friend's user name.</summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>Gets or sets the version of the friend's photo.</summary>
    public int? AvatarVersion { get; set; }

    /// <summary>Gets or sets the last time the friend used the app (UTC).</summary>
    public DateTime? LastSeenAt { get; set; }

    /// <summary>Gets or sets the game the friend is playing.</summary>
    public int? PlayingGameID { get; set; }

    /// <summary>Gets or sets that game's name.</summary>
    public string? PlayingName { get; set; }

    /// <summary>Gets or sets how many games the friend has.</summary>
    public int GameCount { get; set; }
}

/// <summary>An account with a Steam library (dbo.SteamAccountList).</summary>
public sealed class SteamAccountRow
{
    /// <summary>Gets or sets the account.</summary>
    public Guid UserUniqueID { get; set; }

    /// <summary>Gets or sets its SteamID64.</summary>
    public string SteamID { get; set; } = string.Empty;

    /// <summary>Gets or sets when it was last synced (UTC).</summary>
    public DateTime? SteamSyncedAt { get; set; }
}
