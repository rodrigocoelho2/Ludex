using Ludex.Library.Enums;

namespace Ludex.Sdk.Models;

/// <summary>A review on a card or its own page: who wrote it, about which game, how they played it, and its likes.</summary>
public sealed class ReviewDTO
{
    /// <summary>Gets or sets the review id.</summary>
    public long Id { get; set; }

    /// <summary>Gets or sets the game.</summary>
    public GameSummaryDTO Game { get; set; } = new();

    /// <summary>Gets or sets the author's account id.</summary>
    public string AuthorId { get; set; } = string.Empty;

    /// <summary>Gets or sets the author's user name.</summary>
    public string AuthorName { get; set; } = string.Empty;

    /// <summary>Gets or sets the version of the author's photo (null: none).</summary>
    public int? AuthorAvatarVersion { get; set; }

    /// <summary>Gets or sets a value indicating whether the author is a made-up demo account.</summary>
    public bool AuthorIsDemo { get; set; }

    /// <summary>Gets or sets the rating given with the review (1–10: half stars).</summary>
    public int? Rating { get; set; }

    /// <summary>Gets or sets the status the game had when the review was written.</summary>
    public GameStatus? Status { get; set; }

    /// <summary>Gets or sets how it ended.</summary>
    public GameEnding? Ending { get; set; }

    /// <summary>Gets or sets the platform it was played on.</summary>
    public string? Platform { get; set; }

    /// <summary>Gets or sets the text.</summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>Gets or sets a value indicating whether the text gives things away (hidden until asked).</summary>
    public bool Spoilers { get; set; }

    /// <summary>Gets or sets when it was written.</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Gets or sets when it last changed.</summary>
    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>Gets or sets how many likes it has.</summary>
    public int Likes { get; set; }

    /// <summary>Gets or sets how many comments it has.</summary>
    public int Comments { get; set; }

    /// <summary>Gets or sets a value indicating whether the reader liked it.</summary>
    public bool LikedByMe { get; set; }

    /// <summary>Gets or sets a value indicating whether the reader wrote it.</summary>
    public bool IsMine { get; set; }
}

/// <summary>A comment under a review.</summary>
public sealed class ReviewCommentDTO
{
    /// <summary>Gets or sets the comment id.</summary>
    public long Id { get; set; }

    /// <summary>Gets or sets the writer's account id.</summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>Gets or sets the writer's user name.</summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>Gets or sets the version of the writer's photo.</summary>
    public int? AvatarVersion { get; set; }

    /// <summary>Gets or sets a value indicating whether the writer is a made-up demo account.</summary>
    public bool IsDemo { get; set; }

    /// <summary>Gets or sets the text.</summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>Gets or sets when it was written.</summary>
    public DateTimeOffset At { get; set; }

    /// <summary>Gets or sets a value indicating whether the reader may delete it (theirs, or under their review).</summary>
    public bool CanDelete { get; set; }
}

/// <summary>A review's own page: the review, its comments, who liked it, more by the same author, and the game's rating here.</summary>
public sealed class ReviewPageDTO
{
    /// <summary>Gets or sets the review.</summary>
    public ReviewDTO Review { get; set; } = new();

    /// <summary>Gets or sets the comments, oldest first.</summary>
    public List<ReviewCommentDTO> Comments { get; set; } = [];

    /// <summary>Gets or sets the names of the last few who liked it.</summary>
    public List<string> LikedBy { get; set; } = [];

    /// <summary>Gets or sets other reviews by the author, most liked first.</summary>
    public List<ReviewDTO> MoreByAuthor { get; set; } = [];

    /// <summary>Gets or sets the game's average rating on this Ludex (1–10).</summary>
    public decimal? GameAverageRating { get; set; }

    /// <summary>Gets or sets how many accounts rated the game.</summary>
    public int GameRatingCount { get; set; }
}

/// <summary>A page of reviews.</summary>
public sealed class ReviewListDTO
{
    /// <summary>Gets or sets the reviews.</summary>
    public List<ReviewDTO> Reviews { get; set; } = [];

    /// <summary>Gets or sets a value indicating whether there are more after these.</summary>
    public bool More { get; set; }

    /// <summary>Gets or sets the order.</summary>
    public ReviewSort Sort { get; set; } = ReviewSort.Popular;
}

/// <summary>Likes a review, or takes the like back.</summary>
public sealed class ReviewLikeRequest
{
    /// <summary>Gets or sets the account.</summary>
    public string? UserId { get; set; }

    /// <summary>Gets or sets the review.</summary>
    public long ReviewId { get; set; }

    /// <summary>Gets or sets a value indicating whether to like it (false: take the like back).</summary>
    public bool Like { get; set; } = true;
}

/// <summary>A review's likes after a like.</summary>
public sealed class ReviewLikeDTO
{
    /// <summary>Gets or sets the review.</summary>
    public long ReviewId { get; set; }

    /// <summary>Gets or sets how many likes it has now.</summary>
    public int Likes { get; set; }

    /// <summary>Gets or sets a value indicating whether the account likes it now.</summary>
    public bool Liked { get; set; }
}

/// <summary>Writes a comment under a review.</summary>
public sealed class ReviewCommentRequest
{
    /// <summary>Gets or sets the account.</summary>
    public string? UserId { get; set; }

    /// <summary>Gets or sets the review.</summary>
    public long ReviewId { get; set; }

    /// <summary>Gets or sets the text.</summary>
    public string? Text { get; set; }
}

/// <summary>Deletes a comment.</summary>
public sealed class ReviewCommentDeleteRequest
{
    /// <summary>Gets or sets the account.</summary>
    public string? UserId { get; set; }

    /// <summary>Gets or sets the review the comment is under.</summary>
    public long ReviewId { get; set; }

    /// <summary>Gets or sets the comment.</summary>
    public long CommentId { get; set; }
}
