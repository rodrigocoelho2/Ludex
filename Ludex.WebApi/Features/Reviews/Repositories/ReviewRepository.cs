using Ludex.WebApi.Extensions.Data;
using Ludex.WebApi.Features.Reviews.Models.Entities;

namespace Ludex.WebApi.Features.Reviews.Repositories;

/// <summary>
/// Reviews, their likes and comments (dbo.Review, dbo.ReviewLike, dbo.ReviewComment).
/// </summary>
internal interface IReviewRepository
{
    /// <summary>Writes or changes an account's review of a game.</summary>
    /// <param name="userId">The account.</param>
    /// <param name="gameId">The game.</param>
    /// <param name="text">The text.</param>
    /// <param name="spoilers">Whether it has spoilers.</param>
    /// <param name="rating">The rating at the time.</param>
    /// <param name="status">The status at the time.</param>
    /// <param name="ending">How it ended.</param>
    /// <param name="platform">The platform.</param>
    /// <param name="at">When (null: now; the demo seed spreads its reviews over the last weeks).</param>
    /// <returns>The review id.</returns>
    Task<long> SaveAsync(Guid userId, int gameId, string text, bool spoilers, byte? rating, byte? status, byte? ending, string? platform, DateTime? at = null);

    /// <summary>Deletes an account's review of a game.</summary>
    /// <param name="userId">The account.</param>
    /// <param name="gameId">The game.</param>
    /// <returns>A task.</returns>
    Task DeleteAsync(Guid userId, int gameId);

    /// <summary>One review.</summary>
    /// <param name="viewerId">The reader.</param>
    /// <param name="reviewId">The review.</param>
    /// <returns>The review, or null.</returns>
    Task<ReviewRow?> GetAsync(Guid viewerId, long reviewId);

    /// <summary>A page of reviews.</summary>
    /// <param name="viewerId">The reader.</param>
    /// <param name="gameId">Only this game.</param>
    /// <param name="authorId">Only this account's.</param>
    /// <param name="sort">The order (see <see cref="Ludex.Library.Enums.ReviewSort"/>).</param>
    /// <param name="days">How many days "popular now" looks back.</param>
    /// <param name="skip">How many to skip.</param>
    /// <param name="take">How many.</param>
    /// <returns>The reviews.</returns>
    Task<List<ReviewRow>> ListAsync(Guid viewerId, int? gameId, Guid? authorId, byte sort, int days, int skip, int take);

    /// <summary>Likes a review or takes the like back.</summary>
    /// <param name="userId">The account.</param>
    /// <param name="reviewId">The review.</param>
    /// <param name="like">True: like.</param>
    /// <returns>The likes now.</returns>
    Task<int> LikeAsync(Guid userId, long reviewId, bool like);

    /// <summary>Who liked a review, newest first.</summary>
    /// <param name="reviewId">The review.</param>
    /// <param name="take">How many.</param>
    /// <returns>Their user names.</returns>
    Task<List<string>> LikersAsync(long reviewId, int take);

    /// <summary>Adds a comment.</summary>
    /// <param name="userId">The account.</param>
    /// <param name="reviewId">The review.</param>
    /// <param name="text">The text.</param>
    /// <param name="at">When (null: now).</param>
    /// <returns>The comment id (0: the review is gone).</returns>
    Task<long> CommentAsync(Guid userId, long reviewId, string text, DateTime? at = null);

    /// <summary>Deletes a comment (the writer's, or under the account's review).</summary>
    /// <param name="userId">The account.</param>
    /// <param name="commentId">The comment.</param>
    /// <returns>A task.</returns>
    Task DeleteCommentAsync(Guid userId, long commentId);

    /// <summary>The comments under a review.</summary>
    /// <param name="viewerId">The reader.</param>
    /// <param name="reviewId">The review.</param>
    /// <returns>The comments, oldest first.</returns>
    Task<List<ReviewCommentRow>> CommentsAsync(Guid viewerId, long reviewId);
}

/// <summary>
/// Reviews in SQL Server (stored procedures in Ludex.Database/StoredProcedures/Reviews).
/// </summary>
/// <param name="connectionFactory">Opens connections.</param>
internal sealed class ReviewRepository(ISqlConnectionFactory connectionFactory) : RepositoryBase(connectionFactory), IReviewRepository
{
    /// <inheritdoc />
    public Task<long> SaveAsync(Guid userId, int gameId, string text, bool spoilers, byte? rating, byte? status, byte? ending, string? platform, DateTime? at = null) =>
        ExecuteScalarAsync<long>("[dbo].[ReviewSave]", new
        {
            UserUniqueID = userId,
            GameID = gameId,
            Text = text,
            Spoilers = spoilers,
            Rating = rating,
            Status = status,
            Ending = ending,
            Platform = platform,
            At = at,
        });

    /// <inheritdoc />
    public Task DeleteAsync(Guid userId, int gameId) => ExecuteAsync("[dbo].[ReviewDelete]", new { UserUniqueID = userId, GameID = gameId });

    /// <inheritdoc />
    public Task<ReviewRow?> GetAsync(Guid viewerId, long reviewId) =>
        QuerySingleOrDefaultAsync<ReviewRow>("[dbo].[ReviewGet]", new { ViewerUniqueID = viewerId, ReviewID = reviewId });

    /// <inheritdoc />
    public Task<List<ReviewRow>> ListAsync(Guid viewerId, int? gameId, Guid? authorId, byte sort, int days, int skip, int take) =>
        QueryAsync<ReviewRow>("[dbo].[ReviewList]", new
        {
            ViewerUniqueID = viewerId,
            GameID = gameId,
            AuthorUniqueID = authorId,
            Sort = sort,
            Days = days,
            Skip = skip,
            Take = take,
        });

    /// <inheritdoc />
    public Task<int> LikeAsync(Guid userId, long reviewId, bool like) =>
        ExecuteScalarAsync<int>("[dbo].[ReviewLikeSet]", new { UserUniqueID = userId, ReviewID = reviewId, Like = like });

    /// <inheritdoc />
    public Task<List<string>> LikersAsync(long reviewId, int take) => QueryAsync<string>("[dbo].[ReviewLikers]", new { ReviewID = reviewId, Take = take });

    /// <inheritdoc />
    public Task<long> CommentAsync(Guid userId, long reviewId, string text, DateTime? at = null) =>
        ExecuteScalarAsync<long>("[dbo].[ReviewCommentAdd]", new { UserUniqueID = userId, ReviewID = reviewId, Text = text, At = at });

    /// <inheritdoc />
    public Task DeleteCommentAsync(Guid userId, long commentId) =>
        ExecuteAsync("[dbo].[ReviewCommentDelete]", new { UserUniqueID = userId, CommentID = commentId });

    /// <inheritdoc />
    public Task<List<ReviewCommentRow>> CommentsAsync(Guid viewerId, long reviewId) =>
        QueryAsync<ReviewCommentRow>("[dbo].[ReviewCommentList]", new { ViewerUniqueID = viewerId, ReviewID = reviewId });
}
