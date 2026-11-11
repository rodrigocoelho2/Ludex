using Ludex.Library.Enums;
using Ludex.WebApi.Features.Games.Models.DTOs;

namespace Ludex.WebApi.Features.Friends.Models.DTOs;

/// <summary>Another account, as the signed-in one relates to it.</summary>
public sealed class FriendDTO
{
    /// <summary>Gets or sets the account id.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Gets or sets the user name.</summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>Gets or sets the link.</summary>
    public FriendState State { get; set; }

    /// <summary>Gets or sets since when (friends: accepted; requests: sent).</summary>
    public DateTimeOffset Since { get; set; }

    /// <summary>Gets or sets how many games the account has.</summary>
    public int GameCount { get; set; }

    /// <summary>Gets or sets the version of the account's photo (null: none).</summary>
    public int? AvatarVersion { get; set; }
}

/// <summary>Something a friend did with a game.</summary>
public sealed class ActivityDTO
{
    /// <summary>Gets or sets the line's id.</summary>
    public long Id { get; set; }

    /// <summary>Gets or sets the friend's account id.</summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>Gets or sets the friend's user name.</summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>Gets or sets what happened.</summary>
    public ActivityKind Kind { get; set; }

    /// <summary>Gets or sets the rating given (Rated).</summary>
    public int? Rating { get; set; }

    /// <summary>Gets or sets when.</summary>
    public DateTimeOffset At { get; set; }

    /// <summary>Gets or sets the game (MyStatus: whether the signed-in account has it too).</summary>
    public GameSummaryDTO Game { get; set; } = new();

    /// <summary>Gets or sets the version of the account's photo (null: none).</summary>
    public int? AvatarVersion { get; set; }
}

/// <summary>A game friends have and the signed-in account doesn't.</summary>
public sealed class PopularGameDTO
{
    /// <summary>Gets or sets the game (FriendNames: who has it).</summary>
    public GameSummaryDTO Game { get; set; } = new();

    /// <summary>Gets or sets the friends' average rating.</summary>
    public decimal? AverageRating { get; set; }
}

/// <summary>The friends page.</summary>
public sealed class FriendsDTO
{
    /// <summary>Gets or sets the friends.</summary>
    public List<FriendDTO> Friends { get; set; } = [];

    /// <summary>Gets or sets the requests waiting for the signed-in account.</summary>
    public List<FriendDTO> Incoming { get; set; } = [];

    /// <summary>Gets or sets the requests the signed-in account sent.</summary>
    public List<FriendDTO> Outgoing { get; set; } = [];

    /// <summary>Gets or sets what the friends did lately.</summary>
    public List<ActivityDTO> Feed { get; set; } = [];

    /// <summary>Gets or sets the games the friends have and the account doesn't.</summary>
    public List<PopularGameDTO> Popular { get; set; } = [];
}

/// <summary>Asks someone to be friends, by user name.</summary>
public sealed class FriendRequestRequest
{
    /// <summary>Gets or sets the account sending it.</summary>
    public string? UserId { get; set; }

    /// <summary>Gets or sets the other account's user name.</summary>
    public string? UserName { get; set; }
}

/// <summary>Accepts, declines or ends a friendship.</summary>
public sealed class FriendActionRequest
{
    /// <summary>Gets or sets the signed-in account.</summary>
    public string? UserId { get; set; }

    /// <summary>Gets or sets the other account.</summary>
    public string? FriendId { get; set; }
}
