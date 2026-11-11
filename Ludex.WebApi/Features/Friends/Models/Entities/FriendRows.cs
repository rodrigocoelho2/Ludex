using Ludex.WebApi.Features.Games.Models.Entities;

namespace Ludex.WebApi.Features.Friends.Models.Entities;

/// <summary>Another account linked to the signed-in one (dbo.FriendList).</summary>
public sealed class FriendRow
{
    /// <summary>Gets or sets the other account's public id.</summary>
    public Guid UserUniqueID { get; set; }

    /// <summary>Gets or sets the other account's user name.</summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>Gets or sets the link: 1 friends, 2 request sent, 3 request received.</summary>
    public byte State { get; set; }

    /// <summary>Gets or sets since when (UTC).</summary>
    public DateTime Since { get; set; }

    /// <summary>Gets or sets how many games the other account has.</summary>
    public int GameCount { get; set; }

    /// <summary>Gets or sets the version of the friend's photo (null: none).</summary>
    public int? AvatarVersion { get; set; }
}

/// <summary>A friend's entry for one game (dbo.FriendGameList).</summary>
public sealed class FriendGameRow
{
    /// <summary>Gets or sets the friend's public id.</summary>
    public Guid UserUniqueID { get; set; }

    /// <summary>Gets or sets the friend's user name.</summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>Gets or sets the status.</summary>
    public byte Status { get; set; }

    /// <summary>Gets or sets the rating.</summary>
    public byte? Rating { get; set; }

    /// <summary>Gets or sets the hours.</summary>
    public decimal? Hours { get; set; }

    /// <summary>Gets or sets the platform.</summary>
    public string? Platform { get; set; }

    /// <summary>Gets or sets when it last changed (UTC).</summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>Gets or sets the version of the friend's photo (null: none).</summary>
    public int? AvatarVersion { get; set; }
}

/// <summary>A friend who has one of the games asked about (dbo.FriendOwnership).</summary>
public sealed class OwnershipRow
{
    /// <summary>Gets or sets the game.</summary>
    public int GameID { get; set; }

    /// <summary>Gets or sets the friend's user name.</summary>
    public string Username { get; set; } = string.Empty;
}

/// <summary>A game friends have and the account doesn't (dbo.FriendPopular).</summary>
public sealed class PopularRow : GameRow
{
    /// <summary>Gets or sets how many friends have it.</summary>
    public int FriendCount { get; set; }

    /// <summary>Gets or sets their average rating.</summary>
    public decimal? AverageRating { get; set; }
}

/// <summary>One line of the friends' feed (dbo.ActivityFeed).</summary>
public sealed class ActivityRow
{
    /// <summary>Gets or sets the line's id.</summary>
    public long ActivityID { get; set; }

    /// <summary>Gets or sets what happened (<see cref="Ludex.Library.Enums.ActivityKind"/>).</summary>
    public byte Kind { get; set; }

    /// <summary>Gets or sets the rating, for a Rated line.</summary>
    public byte? Rating { get; set; }

    /// <summary>Gets or sets when (UTC).</summary>
    public DateTime At { get; set; }

    /// <summary>Gets or sets the friend's public id.</summary>
    public Guid UserUniqueID { get; set; }

    /// <summary>Gets or sets the friend's user name.</summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>Gets or sets the game.</summary>
    public int GameID { get; set; }

    /// <summary>Gets or sets the game's name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the game's cover.</summary>
    public string? CoverImageID { get; set; }

    /// <summary>Gets or sets the signed-in account's status for the game (null: not in its library).</summary>
    public byte? MyStatus { get; set; }

    /// <summary>Gets or sets the version of the friend's photo (null: none).</summary>
    public int? AvatarVersion { get; set; }
}
