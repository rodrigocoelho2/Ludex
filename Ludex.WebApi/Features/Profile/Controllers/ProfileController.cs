using System.Net;
using System.Net.Mime;
using Ludex.Library.Validation;
using Ludex.WebApi.Extensions.Mvc;
using Ludex.WebApi.Extensions.Services;
using Ludex.WebApi.Features.Profile.Models.DTOs;
using Ludex.WebApi.Features.Profile.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Ludex.WebApi.Features.Profile.Controllers;

/// <summary>
/// Profiles: the page, editing it, the photo and "last seen".
/// </summary>
/// <param name="profileService">Profiles.</param>
[Authorize]
[Route("profile")]
[ApiController]
[Produces(MediaTypeNames.Application.Json)]
[ProducesErrorResponseType(typeof(ApiValidationProblemDetails))]
[ProducesResponseType((int)HttpStatusCode.BadRequest)]
[ProducesResponseType((int)HttpStatusCode.Unauthorized)]
[ProducesResponseType((int)HttpStatusCode.NotFound)]
internal sealed class ProfileController(IProfileService profileService) : InternalControllerBase
{
    /// <summary>A profile as an account sees it.</summary>
    /// <param name="viewerId">The signed-in account.</param>
    /// <param name="userName">Whose profile (empty: the viewer's own).</param>
    /// <returns>The profile.</returns>
    [HttpGet]
    [ProducesResponseType<ProfileDTO>((int)HttpStatusCode.OK)]
    [SwaggerOperation(Summary = "ProfileGet", OperationId = "ProfileGet")]
    public async Task<ActionResult<ProfileDTO>> ProfileGetAsync(string viewerId, string? userName) =>
        (await profileService.GetAsync(viewerId, userName)).ToActionResult();

    /// <summary>Saves the bio, location and favourite game.</summary>
    /// <param name="item">The account and the values.</param>
    /// <returns>The profile.</returns>
    [HttpPost("save")]
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType<ProfileDTO>((int)HttpStatusCode.OK)]
    [SwaggerOperation(Summary = "ProfileSave", OperationId = "ProfileSave")]
    public async Task<ActionResult<ProfileDTO>> ProfileSaveAsync(ProfileSaveRequest item) => (await profileService.SaveAsync(item)).ToActionResult();

    /// <summary>Makes a game the favourite, or clears it.</summary>
    /// <param name="item">The account and the game.</param>
    /// <returns>The favourite now.</returns>
    [HttpPost("favorite")]
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType<ProfileFavoriteRequest>((int)HttpStatusCode.OK)]
    [SwaggerOperation(Summary = "ProfileFavorite", OperationId = "ProfileFavorite")]
    public async Task<ActionResult<ProfileFavoriteRequest>> ProfileFavoriteAsync(ProfileFavoriteRequest item) =>
        (await profileService.SetFavoriteAsync(item)).ToActionResult();

    /// <summary>Sets the photo, or removes it (no data).</summary>
    /// <param name="item">The account and the image.</param>
    /// <returns>The new version.</returns>
    [HttpPost("avatar")]
    [Consumes(MediaTypeNames.Application.Json)]
    [RequestSizeLimit(1024 * 1024)]
    [ProducesResponseType<AvatarDTO>((int)HttpStatusCode.OK)]
    [SwaggerOperation(Summary = "ProfileAvatarSave", OperationId = "ProfileAvatarSave")]
    public async Task<ActionResult<AvatarDTO>> ProfileAvatarSaveAsync(ProfileAvatarRequest item) => (await profileService.SaveAvatarAsync(item)).ToActionResult();

    /// <summary>A photo.</summary>
    /// <param name="userId">The account.</param>
    /// <returns>The image, its type and version.</returns>
    [HttpGet("avatar")]
    [ProducesResponseType<AvatarDTO>((int)HttpStatusCode.OK)]
    [SwaggerOperation(Summary = "ProfileAvatarGet", OperationId = "ProfileAvatarGet")]
    public async Task<ActionResult<AvatarDTO>> ProfileAvatarGetAsync(string userId) => (await profileService.GetAvatarAsync(userId)).ToActionResult();

    /// <summary>Notes that the account is using the app now ("last seen").</summary>
    /// <param name="item">The account.</param>
    /// <returns>True.</returns>
    [HttpPost("touch")]
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType<bool>((int)HttpStatusCode.OK)]
    [SwaggerOperation(Summary = "ProfileTouch", OperationId = "ProfileTouch")]
    public async Task<ActionResult<bool>> ProfileTouchAsync(ProfileUserRequest item) => (await profileService.TouchAsync(item)).ToActionResult();
}
