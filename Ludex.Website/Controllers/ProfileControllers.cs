using System.Net;
using Ludex.Library.Validation;
using Ludex.Sdk;
using Ludex.Sdk.Models;
using Ludex.Website.Model;
using Ludex.Website.Security;
using Microsoft.AspNetCore.Mvc;

namespace Ludex.Website.Controllers;

/// <summary>
/// Profiles: one's own and the others', editing it, the photo, and the favourite game.
/// </summary>
/// <param name="apiClient">The API client.</param>
public sealed class ProfileController(LudexClient apiClient) : WebsiteBaseController(apiClient)
{
    /// <summary>Largest photo the page may send (it shrinks photos to 256 px first; the WebApi takes up to 400 KB).</summary>
    private const int MaxPhotoBytes = 400 * 1024;

    /// <summary>The signed-in account's profile.</summary>
    /// <returns>The view.</returns>
    [HttpGet("/profile")]
    public async Task<IActionResult> Index() =>
        await ScreenAsync("Index", Shell(new ProfilePageModel(), AppSection.Profile), async page => page.Profile = await Api.ProfileGetAsync(UserId));

    /// <summary>Someone's profile, by user name (one's own goes to /profile).</summary>
    /// <param name="userName">The user name.</param>
    /// <returns>The view.</returns>
    [HttpGet("/u/{userName}")]
    public async Task<IActionResult> Someone(string userName)
    {
        if (string.Equals(userName, User.Identity?.Name, StringComparison.OrdinalIgnoreCase))
        {
            return LocalRedirect("/profile");
        }

        try
        {
            var page = Shell(new ProfilePageModel { Profile = await Api.ProfileGetAsync(UserId, userName) }, AppSection.Profile);
            return View("Index", page);
        }
        catch (LudexApiException exception) when (exception.StatusCode == HttpStatusCode.NotFound)
        {
            Response.StatusCode = StatusCodes.Status404NotFound;
            return View("~/Views/Shared/Unavailable.cshtml", Shell(new AppPageModel { Unavailable = $"There's no account called '{userName}' here." }, AppSection.Profile));
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or LudexApiException)
        {
            return View("~/Views/Shared/Unavailable.cshtml", Shell(new AppPageModel { Unavailable = ApiUnavailableMessage }, AppSection.Profile));
        }
    }

    /// <summary>The edit form: photo, bio, location and favourite game.</summary>
    /// <returns>The view.</returns>
    [HttpGet("/profile/edit")]
    public async Task<IActionResult> Edit() =>
        await ScreenAsync("Edit", Shell(new ProfileEditPageModel(), AppSection.Profile), async page => page.Profile = await Api.ProfileGetAsync(UserId));

    /// <summary>Saves the bio, location and favourite game.</summary>
    /// <param name="bio">The bio.</param>
    /// <param name="location">The location.</param>
    /// <param name="favoriteGameId">The favourite game (empty: the best rated is shown).</param>
    /// <returns>Back to the profile.</returns>
    [HttpPost("/profile/edit")]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Save([FromForm] string? bio, [FromForm] string? location, [FromForm] int? favoriteGameId) =>
        FormAsync(
            () => Api.ProfileSaveAsync(new ProfileSaveRequest { UserId = UserId, Bio = bio, Location = location, FavoriteGameId = favoriteGameId }),
            "Profile saved.",
            "/profile");

    /// <summary>Sets the photo (the page sends it already shrunk), for the page script.</summary>
    /// <param name="photo">The image.</param>
    /// <returns>The new version, or the errors.</returns>
    [HttpPost("/profile/photo")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(2 * 1024 * 1024)]
    public async Task<IActionResult> Photo(IFormFile? photo)
    {
        if (photo is null || photo.Length == 0 || photo.Length > MaxPhotoBytes)
        {
            return Json(new AjaxDataResult<AvatarDTO>
            {
                IsValid = false,
                ValidationErrors = [new ApiValidationError("Avatar", "Choose a JPEG, PNG or WebP image (it is shrunk to 256 px before it's sent).")],
            });
        }

        byte[] data;
        await using (var stream = photo.OpenReadStream())
        using (var buffer = new MemoryStream())
        {
            await stream.CopyToAsync(buffer);
            data = buffer.ToArray();
        }

        var result = await CallAsync(() => Api.ProfileAvatarSaveAsync(new ProfileAvatarRequest { UserId = UserId, Data = data }));
        if (result.IsValid)
        {
            await RefreshSignInAsync();
        }

        return Json(result);
    }

    /// <summary>Removes the photo (the initials come back).</summary>
    /// <returns>Back to the edit form.</returns>
    [HttpPost("/profile/photo/remove")]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> RemovePhoto() =>
        FormAsync(
            async () =>
            {
                await Api.ProfileAvatarSaveAsync(new ProfileAvatarRequest { UserId = UserId, Data = null });
                await RefreshSignInAsync();
            },
            "Photo removed.",
            "/profile/edit");

    /// <summary>Makes a game the favourite on the profile, or clears it (the game page's button).</summary>
    /// <param name="form">The game, or none.</param>
    /// <returns>The favourite now, or the errors.</returns>
    [HttpPost("/profile/favorite")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Favorite([FromBody] FavoriteForm form) =>
        Json(await CallAsync(() => Api.ProfileFavoriteAsync(new ProfileFavoriteRequest { UserId = UserId, GameId = form.GameId })));

    /// <summary>
    /// A profile photo. The version in the address changes with every new photo, so each one can be cached for good.
    /// </summary>
    /// <param name="id">The account.</param>
    /// <param name="v">The version asked for.</param>
    /// <returns>The image, or 404.</returns>
    [HttpGet("/avatars/{id}")]
    public async Task<IActionResult> Avatar(string id, int? v = null)
    {
        try
        {
            var avatar = await Api.ProfileAvatarGetAsync(id);
            if (v is null || v != avatar.Version)
            {
                // An old or missing version: answer, but don't let it be kept.
                Response.Headers.CacheControl = "no-cache";
            }
            else
            {
                Response.Headers.CacheControl = "private, max-age=31536000, immutable";
            }

            return File(avatar.Data, avatar.ContentType);
        }
        catch (LudexApiException exception) when (exception.StatusCode == HttpStatusCode.NotFound)
        {
            return NotFound();
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or LudexApiException)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        }
    }

    private async Task RefreshSignInAsync()
    {
        try
        {
            await LudexAuthentication.RefreshAsync(HttpContext, await Api.UserGetAsync(UserId));
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or LudexApiException)
        {
            // The photo is saved; the top bar catches up at the next check of the session.
        }
    }
}

/// <summary>The favourite game from the game page's button.</summary>
public sealed class FavoriteForm
{
    /// <summary>Gets or sets the game (null: clear the favourite).</summary>
    public int? GameId { get; set; }
}
