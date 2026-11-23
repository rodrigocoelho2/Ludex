using System.Net;
using System.Net.Mime;
using Ludex.Library.Enums;
using Ludex.Library.Validation;
using Ludex.WebApi.Extensions.Mvc;
using Ludex.WebApi.Extensions.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Ludex.WebApi.Features.Reviews.Models.DTOs;
using Ludex.WebApi.Features.Reviews.Services;

namespace Ludex.WebApi.Features.Reviews.Controllers;

/// <summary>
/// Reviews: read, like, comment.
/// </summary>
/// <param name="reviewService">Reviews.</param>
[Authorize]
[ApiController]
[Produces(MediaTypeNames.Application.Json)]
[ProducesErrorResponseType(typeof(ApiValidationProblemDetails))]
[ProducesResponseType((int)HttpStatusCode.BadRequest)]
[ProducesResponseType((int)HttpStatusCode.Unauthorized)]
[Route("reviews")]
internal sealed class ReviewController(IReviewService reviewService) : InternalControllerBase
{
    /// <summary>A page of reviews: of a game, by an account, or of everyone.</summary>
    /// <param name="viewerId">The reader.</param>
    /// <param name="gameId">Only this game.</param>
    /// <param name="author">Only this account's (user name).</param>
    /// <param name="sort">The order.</param>
    /// <param name="skip">How many to skip.</param>
    /// <param name="take">How many.</param>
    /// <returns>The reviews.</returns>
    [HttpGet]
    [ProducesResponseType<ReviewListDTO>((int)HttpStatusCode.OK)]
    [SwaggerOperation(Summary = "ReviewsList", OperationId = "ReviewsList")]
    public async Task<ActionResult<ReviewListDTO>> ReviewsListAsync(string viewerId, int? gameId = null, string? author = null, ReviewSort sort = ReviewSort.Popular, int skip = 0, int take = 20) =>
        (await reviewService.ListAsync(viewerId, gameId, author, sort, skip, take)).ToActionResult();

    /// <summary>A review's own page.</summary>
    /// <param name="viewerId">The reader.</param>
    /// <param name="id">The review.</param>
    /// <returns>The page.</returns>
    [HttpGet("{id:long}")]
    [ProducesResponseType<ReviewPageDTO>((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    [SwaggerOperation(Summary = "ReviewsPage", OperationId = "ReviewsPage")]
    public async Task<ActionResult<ReviewPageDTO>> ReviewsPageAsync(string viewerId, long id) => (await reviewService.PageAsync(viewerId, id)).ToActionResult();

    /// <summary>Likes a review or takes the like back.</summary>
    /// <param name="item">The account and the review.</param>
    /// <returns>The likes now.</returns>
    [HttpPost("like")]
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType<ReviewLikeDTO>((int)HttpStatusCode.OK)]
    [SwaggerOperation(Summary = "ReviewsLike", OperationId = "ReviewsLike")]
    public async Task<ActionResult<ReviewLikeDTO>> ReviewsLikeAsync(ReviewLikeRequest item) => (await reviewService.LikeAsync(item)).ToActionResult();

    /// <summary>Writes a comment under a review.</summary>
    /// <param name="item">The account, the review and the text.</param>
    /// <returns>The comments now.</returns>
    [HttpPost("comment")]
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType<List<ReviewCommentDTO>>((int)HttpStatusCode.OK)]
    [SwaggerOperation(Summary = "ReviewsComment", OperationId = "ReviewsComment")]
    public async Task<ActionResult<List<ReviewCommentDTO>>> ReviewsCommentAsync(ReviewCommentRequest item) => (await reviewService.CommentAsync(item)).ToActionResult();

    /// <summary>Deletes a comment.</summary>
    /// <param name="item">The account, the review and the comment.</param>
    /// <returns>The comments now.</returns>
    [HttpPost("comment/delete")]
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType<List<ReviewCommentDTO>>((int)HttpStatusCode.OK)]
    [SwaggerOperation(Summary = "ReviewsCommentDelete", OperationId = "ReviewsCommentDelete")]
    public async Task<ActionResult<List<ReviewCommentDTO>>> ReviewsCommentDeleteAsync(ReviewCommentDeleteRequest item) => (await reviewService.DeleteCommentAsync(item)).ToActionResult();
}
