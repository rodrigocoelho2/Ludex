using Ludex.WebApi.Features.Games.Models.Entities;

namespace Ludex.WebApi.Features.Lists.Models.Entities;

/// <summary>A list with its owner and numbers (dbo.ListGet, dbo.ListBrowse).</summary>
public sealed class ListRow
{
    /// <summary>Gets or sets the list id.</summary>
    public int ListID { get; set; }

    /// <summary>Gets or sets the name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the description.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets a value indicating whether it is ranked.</summary>
    public bool Ranked { get; set; }

    /// <summary>Gets or sets who can see it (1 everyone, 2 friends, 3 only the owner).</summary>
    public byte Visibility { get; set; }

    /// <summary>Gets or sets when it was made.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Gets or sets when it last changed.</summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>Gets or sets the owner's public id.</summary>
    public Guid OwnerUniqueID { get; set; }

    /// <summary>Gets or sets the owner's user name.</summary>
    public string OwnerName { get; set; } = string.Empty;

    /// <summary>Gets or sets a value indicating whether the owner is a demo account.</summary>
    public bool OwnerIsDemo { get; set; }

    /// <summary>Gets or sets the version of the owner's photo.</summary>
    public int? OwnerAvatarVersion { get; set; }

    /// <summary>Gets or sets how many games it has.</summary>
    public int GameCount { get; set; }

    /// <summary>Gets or sets how many likes it has.</summary>
    public int Likes { get; set; }

    /// <summary>Gets or sets a value indicating whether the reader liked it.</summary>
    public bool LikedByMe { get; set; }

    /// <summary>Gets or sets a value indicating whether it is the reader's.</summary>
    public bool IsMine { get; set; }

    /// <summary>Gets or sets how many of its games the reader has played.</summary>
    public int PlayedByMe { get; set; }
}

/// <summary>One of the first games of a list, for its stacked covers (dbo.ListCovers).</summary>
public sealed class ListCoverRow : GameRow
{
    /// <summary>Gets or sets the list.</summary>
    public int ListID { get; set; }

    /// <summary>Gets or sets the game's place.</summary>
    public int Position { get; set; }
}

/// <summary>A game in a list (dbo.ListItems).</summary>
public sealed class ListItemRow : GameRow
{
    /// <summary>Gets or sets its place.</summary>
    public int Position { get; set; }

    /// <summary>Gets or sets the note.</summary>
    public string? Note { get; set; }

    /// <summary>Gets or sets when it was added.</summary>
    public DateTime AddedAt { get; set; }

    /// <summary>Gets or sets the reader's status of it.</summary>
    public byte? MyStatus { get; set; }

    /// <summary>Gets or sets the reader's rating of it.</summary>
    public byte? MyRating { get; set; }
}

/// <summary>One of the account's lists, and whether a game is in it (dbo.ListsForGame).</summary>
public sealed class MyListRow
{
    /// <summary>Gets or sets the list id.</summary>
    public int ListID { get; set; }

    /// <summary>Gets or sets the name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets a value indicating whether it is ranked.</summary>
    public bool Ranked { get; set; }

    /// <summary>Gets or sets who can see it.</summary>
    public byte Visibility { get; set; }

    /// <summary>Gets or sets a value indicating whether the game is in it.</summary>
    public bool HasGame { get; set; }
}
