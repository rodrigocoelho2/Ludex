using Ludex.Library.Enums;
using Ludex.WebApi.Features.Games.Models.DTOs;

namespace Ludex.WebApi.Features.Lists.Models.DTOs;

/// <summary>A list on a card: its name, owner, first covers, likes and how much of it the reader has played.</summary>
public sealed class ListSummaryDTO
{
    /// <summary>Gets or sets the list id.</summary>
    public int Id { get; set; }

    /// <summary>Gets or sets the name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets what it is about.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets a value indicating whether the order is a ranking.</summary>
    public bool Ranked { get; set; }

    /// <summary>Gets or sets who can see it.</summary>
    public ListVisibility Visibility { get; set; } = ListVisibility.Public;

    /// <summary>Gets or sets the owner's account id.</summary>
    public string OwnerId { get; set; } = string.Empty;

    /// <summary>Gets or sets the owner's user name.</summary>
    public string OwnerName { get; set; } = string.Empty;

    /// <summary>Gets or sets the version of the owner's photo.</summary>
    public int? OwnerAvatarVersion { get; set; }

    /// <summary>Gets or sets a value indicating whether the owner is a made-up demo account.</summary>
    public bool OwnerIsDemo { get; set; }

    /// <summary>Gets or sets how many games it has.</summary>
    public int GameCount { get; set; }

    /// <summary>Gets or sets how many likes it has.</summary>
    public int Likes { get; set; }

    /// <summary>Gets or sets a value indicating whether the reader liked it.</summary>
    public bool LikedByMe { get; set; }

    /// <summary>Gets or sets a value indicating whether it is the reader's.</summary>
    public bool IsMine { get; set; }

    /// <summary>Gets or sets how many of its games the reader has played (finished, played or dropped).</summary>
    public int PlayedByMe { get; set; }

    /// <summary>Gets or sets when it last changed.</summary>
    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>Gets or sets its first games (for the stacked covers).</summary>
    public List<GameSummaryDTO> Covers { get; set; } = [];
}

/// <summary>A game in a list.</summary>
public sealed class ListItemDTO
{
    /// <summary>Gets or sets its place (1 = first).</summary>
    public int Position { get; set; }

    /// <summary>Gets or sets the owner's note about it.</summary>
    public string? Note { get; set; }

    /// <summary>Gets or sets the game (MyStatus: where the reader is with it).</summary>
    public GameSummaryDTO Game { get; set; } = new();

    /// <summary>Gets or sets the reader's rating of it.</summary>
    public int? MyRating { get; set; }
}

/// <summary>A list's own page.</summary>
public sealed class ListDetailDTO
{
    /// <summary>Gets or sets the list.</summary>
    public ListSummaryDTO List { get; set; } = new();

    /// <summary>Gets or sets its games, in order.</summary>
    public List<ListItemDTO> Items { get; set; } = [];
}

/// <summary>The lists page: the reader's own, the popular ones this week, the newest.</summary>
public sealed class ListBrowseDTO
{
    /// <summary>Gets or sets the reader's lists (or, for someone else, theirs).</summary>
    public List<ListSummaryDTO> Mine { get; set; } = [];

    /// <summary>Gets or sets the most liked lately.</summary>
    public List<ListSummaryDTO> Popular { get; set; } = [];

    /// <summary>Gets or sets the most recently changed.</summary>
    public List<ListSummaryDTO> Recent { get; set; } = [];
}

/// <summary>One of the reader's lists, and whether a game is in it (the game page's "Add to list").</summary>
public sealed class MyListDTO
{
    /// <summary>Gets or sets the list id.</summary>
    public int Id { get; set; }

    /// <summary>Gets or sets the name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets a value indicating whether it is ranked.</summary>
    public bool Ranked { get; set; }

    /// <summary>Gets or sets who can see it.</summary>
    public ListVisibility Visibility { get; set; } = ListVisibility.Public;

    /// <summary>Gets or sets a value indicating whether the game is in it.</summary>
    public bool HasGame { get; set; }
}

/// <summary>Creates a list or changes one.</summary>
public sealed class ListSaveRequest
{
    /// <summary>Gets or sets the account.</summary>
    public string? UserId { get; set; }

    /// <summary>Gets or sets the list (null: a new one).</summary>
    public int? Id { get; set; }

    /// <summary>Gets or sets the name.</summary>
    public string? Name { get; set; }

    /// <summary>Gets or sets what it is about.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets a value indicating whether the order is a ranking.</summary>
    public bool Ranked { get; set; }

    /// <summary>Gets or sets who can see it.</summary>
    public ListVisibility Visibility { get; set; } = ListVisibility.Public;

    /// <summary>Gets or sets a game to put in it straight away (a new list made from a game page).</summary>
    public int? FirstGameId { get; set; }
}

/// <summary>Deletes a list.</summary>
public sealed class ListDeleteRequest
{
    /// <summary>Gets or sets the account.</summary>
    public string? UserId { get; set; }

    /// <summary>Gets or sets the list.</summary>
    public int Id { get; set; }
}

/// <summary>Puts a game in a list or takes it out.</summary>
public sealed class ListItemRequest
{
    /// <summary>Gets or sets the account.</summary>
    public string? UserId { get; set; }

    /// <summary>Gets or sets the list.</summary>
    public int ListId { get; set; }

    /// <summary>Gets or sets the game.</summary>
    public int GameId { get; set; }

    /// <summary>Gets or sets a value indicating whether to put it in (false: take it out).</summary>
    public bool In { get; set; } = true;

    /// <summary>Gets or sets a note about it.</summary>
    public string? Note { get; set; }
}

/// <summary>Moves a game of a list to another place.</summary>
public sealed class ListMoveRequest
{
    /// <summary>Gets or sets the account.</summary>
    public string? UserId { get; set; }

    /// <summary>Gets or sets the list.</summary>
    public int ListId { get; set; }

    /// <summary>Gets or sets the game.</summary>
    public int GameId { get; set; }

    /// <summary>Gets or sets its new place (1 = first).</summary>
    public int Position { get; set; }
}

/// <summary>Likes a list, or takes the like back.</summary>
public sealed class ListLikeRequest
{
    /// <summary>Gets or sets the account.</summary>
    public string? UserId { get; set; }

    /// <summary>Gets or sets the list.</summary>
    public int ListId { get; set; }

    /// <summary>Gets or sets a value indicating whether to like it.</summary>
    public bool Like { get; set; } = true;
}

/// <summary>A list's likes after a like.</summary>
public sealed class ListLikeDTO
{
    /// <summary>Gets or sets the list.</summary>
    public int ListId { get; set; }

    /// <summary>Gets or sets how many likes it has now.</summary>
    public int Likes { get; set; }

    /// <summary>Gets or sets a value indicating whether the account likes it now.</summary>
    public bool Liked { get; set; }
}
