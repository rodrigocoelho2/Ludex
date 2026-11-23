using Ludex.WebApi.Features.Games.Models.Entities;

namespace Ludex.WebApi.Features.Reviews.Models.Entities;

/// <summary>A review with its author and game (dbo.ReviewGet, dbo.ReviewList).</summary>
public sealed class ReviewRow : GameRow
{
    /// <summary>Gets or sets the review id.</summary>
    public long ReviewID { get; set; }

    /// <summary>Gets or sets the rating given (1–10).</summary>
    public byte? Rating { get; set; }

    /// <summary>Gets or sets the status at the time.</summary>
    public byte? Status { get; set; }

    /// <summary>Gets or sets how it ended.</summary>
    public byte? Ending { get; set; }

    /// <summary>Gets or sets the platform.</summary>
    public string? Platform { get; set; }

    /// <summary>Gets or sets the text.</summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>Gets or sets a value indicating whether it has spoilers.</summary>
    public bool Spoilers { get; set; }

    /// <summary>Gets or sets when it was written.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Gets or sets when it last changed.</summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>Gets or sets the author's public id.</summary>
    public Guid AuthorUniqueID { get; set; }

    /// <summary>Gets or sets the author's user name.</summary>
    public string AuthorName { get; set; } = string.Empty;

    /// <summary>Gets or sets a value indicating whether the author is a demo account.</summary>
    public bool AuthorIsDemo { get; set; }

    /// <summary>Gets or sets the version of the author's photo.</summary>
    public int? AuthorAvatarVersion { get; set; }

    /// <summary>Gets or sets how many likes it has.</summary>
    public int Likes { get; set; }

    /// <summary>Gets or sets how many comments it has.</summary>
    public int Comments { get; set; }

    /// <summary>Gets or sets a value indicating whether the reader liked it.</summary>
    public bool LikedByMe { get; set; }

    /// <summary>Gets or sets a value indicating whether the reader wrote it.</summary>
    public bool IsMine { get; set; }
}

/// <summary>A comment under a review (dbo.ReviewCommentList).</summary>
public sealed class ReviewCommentRow
{
    /// <summary>Gets or sets the comment id.</summary>
    public long CommentID { get; set; }

    /// <summary>Gets or sets the text.</summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>Gets or sets when it was written.</summary>
    public DateTime At { get; set; }

    /// <summary>Gets or sets the writer's public id.</summary>
    public Guid UserUniqueID { get; set; }

    /// <summary>Gets or sets the writer's user name.</summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>Gets or sets a value indicating whether the writer is a demo account.</summary>
    public bool IsDemo { get; set; }

    /// <summary>Gets or sets the version of the writer's photo.</summary>
    public int? AvatarVersion { get; set; }

    /// <summary>Gets or sets a value indicating whether the reader may delete it.</summary>
    public bool CanDelete { get; set; }
}
