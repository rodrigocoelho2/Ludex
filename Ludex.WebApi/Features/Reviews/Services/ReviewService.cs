using Ludex.Library.Enums;
using Ludex.Library.Services;
using Ludex.WebApi.Extensions.Globalization;
using Ludex.WebApi.Features.Community.Repositories;
using Ludex.WebApi.Features.Community.Services;
using Ludex.WebApi.Features.Community.Models.DTOs;
using Ludex.WebApi.Features.Reviews.Models.DTOs;
using Ludex.WebApi.Features.Reviews.Repositories;
using Ludex.WebApi.Features.Users.Repositories;

namespace Ludex.WebApi.Features.Reviews.Services;

/// <summary>
/// Reviews: read them, like them, comment on them.
/// </summary>
internal interface IReviewService
{
    /// <summary>A page of reviews: of a game, by an account, or of everyone.</summary>
    /// <param name="viewerId">The reader.</param>
    /// <param name="gameId">Only this game.</param>
    /// <param name="author">Only this account's (user name).</param>
    /// <param name="sort">The order.</param>
    /// <param name="skip">How many to skip.</param>
    /// <param name="take">How many (at most 50).</param>
    /// <returns>The reviews.</returns>
    Task<ServiceApiResponse<ReviewListDTO>> ListAsync(string viewerId, int? gameId, string? author, ReviewSort sort, int skip, int take);

    /// <summary>A review's own page.</summary>
    /// <param name="viewerId">The reader.</param>
    /// <param name="reviewId">The review.</param>
    /// <returns>The page, or not found.</returns>
    Task<ServiceApiResponse<ReviewPageDTO>> PageAsync(string viewerId, long reviewId);

    /// <summary>Likes a review or takes the like back.</summary>
    /// <param name="item">The account and the review.</param>
    /// <returns>The likes now.</returns>
    Task<ServiceApiResponse<ReviewLikeDTO>> LikeAsync(ReviewLikeRequest item);

    /// <summary>Writes a comment under a review.</summary>
    /// <param name="item">The account, the review and the text.</param>
    /// <returns>The comments now.</returns>
    Task<ServiceApiResponse<List<ReviewCommentDTO>>> CommentAsync(ReviewCommentRequest item);

    /// <summary>Deletes a comment.</summary>
    /// <param name="item">The account, the review and the comment.</param>
    /// <returns>The comments now.</returns>
    Task<ServiceApiResponse<List<ReviewCommentDTO>>> DeleteCommentAsync(ReviewCommentDeleteRequest item);
}

/// <summary>
/// Reviews.
/// </summary>
/// <param name="reviewRepository">Reviews.</param>
/// <param name="communityRepository">The game's ratings here.</param>
/// <param name="userRepository">Accounts (by name).</param>
/// <param name="globalizationHelper">Messages.</param>
internal sealed class ReviewService(
    IReviewRepository reviewRepository,
    ICommunityRepository communityRepository,
    IUserRepository userRepository,
    IGlobalizationHelper globalizationHelper) : IReviewService
{
    /// <summary>How many days "popular now" looks back.</summary>
    internal const int PopularDays = 7;

    /// <inheritdoc />
    public async Task<ServiceApiResponse<ReviewListDTO>> ListAsync(string viewerId, int? gameId, string? author, ReviewSort sort, int skip, int take)
    {
        if (!Guid.TryParse(viewerId, out var viewer))
        {
            return await NotFoundAsync<ReviewListDTO>(viewerId);
        }

        Guid? authorId = null;
        var name = (author ?? string.Empty).Trim().TrimStart('@');
        if (name.Length > 0)
        {
            var account = name.Length <= 32 ? await userRepository.GetByNameAsync(name) : null;
            if (account is null)
            {
                return await NotFoundAsync<ReviewListDTO>(name);
            }

            authorId = account.UserUniqueID;
        }

        var order = Enum.IsDefined(sort) ? sort : ReviewSort.Popular;
        var size = Math.Clamp(take, 1, 50);
        var rows = await reviewRepository.ListAsync(viewer, gameId, authorId, (byte)order, PopularDays, Math.Max(0, skip), size + 1);
        return ServiceApiResponse<ReviewListDTO>.Success(new ReviewListDTO
        {
            Reviews = [.. rows.Take(size).Select(ReviewRules.Review)],
            More = rows.Count > size,
            Sort = order,
        });
    }

    /// <inheritdoc />
    public async Task<ServiceApiResponse<ReviewPageDTO>> PageAsync(string viewerId, long reviewId)
    {
        if (!Guid.TryParse(viewerId, out var viewer))
        {
            return await NotFoundAsync<ReviewPageDTO>(viewerId);
        }

        var row = await reviewRepository.GetAsync(viewer, reviewId);
        if (row is null)
        {
            return ServiceApiResponse<ReviewPageDTO>.NotFound(await globalizationHelper.TranslateAsync(nameof(TranslationKeys.ReviewNotFound)));
        }

        var game = new GameCommunityDTO();
        CommunityRules.Fill(await communityRepository.RatingsAsync(row.GameID), game);
        var more = await reviewRepository.ListAsync(viewer, null, row.AuthorUniqueID, (byte)ReviewSort.AllTime, PopularDays, 0, 4);
        var page = new ReviewPageDTO
        {
            Review = ReviewRules.Review(row),
            Comments = [.. (await reviewRepository.CommentsAsync(viewer, reviewId)).Select(ReviewRules.Comment)],
            LikedBy = await reviewRepository.LikersAsync(reviewId, 3),
            MoreByAuthor = [.. more.Where(r => r.ReviewID != reviewId).Take(3).Select(ReviewRules.Review)],
            GameAverageRating = game.AverageRating,
            GameRatingCount = game.RatingCount,
        };
        page.Review.Game.Developer ??= row.Developer;
        return ServiceApiResponse<ReviewPageDTO>.Success(page);
    }

    /// <inheritdoc />
    public async Task<ServiceApiResponse<ReviewLikeDTO>> LikeAsync(ReviewLikeRequest item)
    {
        if (!Guid.TryParse(item.UserId, out var user))
        {
            return await NotFoundAsync<ReviewLikeDTO>(item.UserId);
        }

        var likes = await reviewRepository.LikeAsync(user, item.ReviewId, item.Like);
        var row = await reviewRepository.GetAsync(user, item.ReviewId);
        if (row is null)
        {
            return ServiceApiResponse<ReviewLikeDTO>.NotFound(await globalizationHelper.TranslateAsync(nameof(TranslationKeys.ReviewNotFound)));
        }

        return ServiceApiResponse<ReviewLikeDTO>.Success(new ReviewLikeDTO { ReviewId = item.ReviewId, Likes = likes, Liked = row.LikedByMe });
    }

    /// <inheritdoc />
    public async Task<ServiceApiResponse<List<ReviewCommentDTO>>> CommentAsync(ReviewCommentRequest item)
    {
        if (!Guid.TryParse(item.UserId, out var user))
        {
            return await NotFoundAsync<List<ReviewCommentDTO>>(item.UserId);
        }

        var text = ReviewRules.Clean(item.Text);
        if (text is null || text.Length > ReviewRules.MaxCommentLength)
        {
            var error = text is null
                ? await globalizationHelper.ValidationErrorAsync("Text", nameof(TranslationKeys.RequiredField))
                : await globalizationHelper.ValidationErrorAsync("Text", nameof(TranslationKeys.TooLong), ReviewRules.MaxCommentLength);
            return ServiceApiResponse<List<ReviewCommentDTO>>.ValidationProblems(error.Message).AddError("Text", error.Message);
        }

        if (await reviewRepository.CommentAsync(user, item.ReviewId, text) == 0)
        {
            return ServiceApiResponse<List<ReviewCommentDTO>>.NotFound(await globalizationHelper.TranslateAsync(nameof(TranslationKeys.ReviewNotFound)));
        }

        return ServiceApiResponse<List<ReviewCommentDTO>>.Success([.. (await reviewRepository.CommentsAsync(user, item.ReviewId)).Select(ReviewRules.Comment)]);
    }

    /// <inheritdoc />
    public async Task<ServiceApiResponse<List<ReviewCommentDTO>>> DeleteCommentAsync(ReviewCommentDeleteRequest item)
    {
        if (!Guid.TryParse(item.UserId, out var user))
        {
            return await NotFoundAsync<List<ReviewCommentDTO>>(item.UserId);
        }

        await reviewRepository.DeleteCommentAsync(user, item.CommentId);
        return ServiceApiResponse<List<ReviewCommentDTO>>.Success([.. (await reviewRepository.CommentsAsync(user, item.ReviewId)).Select(ReviewRules.Comment)]);
    }

    private async Task<ServiceApiResponse<T>> NotFoundAsync<T>(string? identifier) =>
        ServiceApiResponse<T>.NotFound(await globalizationHelper.TranslateAsync(nameof(TranslationKeys.NoRecordsFound), identifier ?? string.Empty));
}
