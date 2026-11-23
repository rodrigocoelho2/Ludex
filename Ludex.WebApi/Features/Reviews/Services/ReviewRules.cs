using Ludex.Library.Enums;
using Ludex.WebApi.Features.Games.Services;
using Ludex.WebApi.Features.Reviews.Models.DTOs;
using Ludex.WebApi.Features.Reviews.Models.Entities;

namespace Ludex.WebApi.Features.Reviews.Services;

/// <summary>
/// Reviews: how long they may be, how text is tidied, and how rows become what the Website shows.
/// </summary>
internal static class ReviewRules
{
    /// <summary>Longest review.</summary>
    public const int MaxReviewLength = 4000;

    /// <summary>Longest comment.</summary>
    public const int MaxCommentLength = 1000;

    /// <summary>
    /// Tidies a review or comment: line ends made \n, no spaces around it, no more than one empty line in a row.
    /// </summary>
    /// <param name="text">The text typed.</param>
    /// <returns>The text, or null when nothing is left.</returns>
    public static string? Clean(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var lines = text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n').Split('\n').Select(static l => l.TrimEnd()).ToList();
        var kept = new List<string>();
        foreach (var line in lines)
        {
            if (line.Length == 0 && (kept.Count == 0 || kept[^1].Length == 0))
            {
                continue;
            }

            kept.Add(line);
        }

        while (kept.Count > 0 && kept[^1].Length == 0)
        {
            kept.RemoveAt(kept.Count - 1);
        }

        var result = string.Join('\n', kept).Trim();
        return result.Length == 0 ? null : result;
    }

    /// <summary>
    /// The ending that goes with a status: only a game finished, played or dropped has one.
    /// </summary>
    /// <param name="status">The status.</param>
    /// <param name="ending">The ending chosen.</param>
    /// <returns>The ending to keep, or null.</returns>
    public static GameEnding? EndingFor(GameStatus status, GameEnding? ending) =>
        ending is { } value && Enum.IsDefined(value) && status is GameStatus.Finished or GameStatus.Played or GameStatus.Dropped ? value : null;

    /// <summary>
    /// A review as the Website shows it.
    /// </summary>
    /// <param name="row">The row.</param>
    /// <returns>The review.</returns>
    public static ReviewDTO Review(ReviewRow row) => new()
    {
        Id = row.ReviewID,
        Game = GameMapper.Summary(row),
        AuthorId = row.AuthorUniqueID.ToString("N"),
        AuthorName = row.AuthorName,
        AuthorAvatarVersion = row.AuthorAvatarVersion,
        AuthorIsDemo = row.AuthorIsDemo,
        Rating = row.Rating,
        Status = GameMapper.Status(row.Status),
        Ending = row.Ending is >= 1 and <= 4 ? (GameEnding)row.Ending.Value : null,
        Platform = row.Platform,
        Text = row.Text,
        Spoilers = row.Spoilers,
        CreatedAt = GameMapper.Utc(row.CreatedAt),
        UpdatedAt = GameMapper.Utc(row.UpdatedAt),
        Likes = row.Likes,
        Comments = row.Comments,
        LikedByMe = row.LikedByMe,
        IsMine = row.IsMine,
    };

    /// <summary>
    /// A comment as the Website shows it.
    /// </summary>
    /// <param name="row">The row.</param>
    /// <returns>The comment.</returns>
    public static ReviewCommentDTO Comment(ReviewCommentRow row) => new()
    {
        Id = row.CommentID,
        UserId = row.UserUniqueID.ToString("N"),
        UserName = row.Username,
        AvatarVersion = row.AvatarVersion,
        IsDemo = row.IsDemo,
        Text = row.Text,
        At = GameMapper.Utc(row.At),
        CanDelete = row.CanDelete,
    };
}
