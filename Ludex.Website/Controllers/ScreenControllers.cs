using Ludex.Library.Enums;
using Ludex.Sdk;
using Ludex.Sdk.Models;
using Ludex.Website.Model;
using Ludex.Website.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Ludex.Website.Controllers;

/// <summary>
/// Home: the game being played, the numbers, the last games and what the friends are up to.
/// </summary>
/// <param name="apiClient">The API client.</param>
public sealed class HomeController(LudexClient apiClient) : WebsiteBaseController(apiClient)
{
    /// <summary>Signed in: home. Signed out: the sign-in page (with its wall of covers).</summary>
    /// <returns>The view or a redirect.</returns>
    [HttpGet("/")]
    [AllowAnonymous]
    public async Task<IActionResult> Index()
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return LocalRedirect("/sign-in");
        }

        return await ScreenAsync("Index", Shell(new HomePageModel(), AppSection.Home), async page =>
        {
            var home = Api.LibraryHomeAsync(UserId);
            var library = Api.LibraryListAsync(UserId);
            page.Home = await home;
            page.Library = await library;
        });
    }
}

/// <summary>
/// The library: every game, filtered by status (server) and by text, platform and friends (in the page).
/// </summary>
/// <param name="apiClient">The API client.</param>
public sealed class LibraryController(LudexClient apiClient) : WebsiteBaseController(apiClient)
{
    /// <summary>The library.</summary>
    /// <param name="status">playing, finished, wishlist, dropped or backlog (empty: all).</param>
    /// <param name="collection">A collection (empty: all).</param>
    /// <returns>The view.</returns>
    [HttpGet("/library")]
    public async Task<IActionResult> Index(string? status = null, int? collection = null)
    {
        var page = Shell(new LibraryPageModel
        {
            Status = Enum.TryParse<GameStatus>(status, true, out var only) && Enum.IsDefined(only) ? only : null,
            Collection = collection,
        }, AppSection.Library);
        return await ScreenAsync("Index", page, async p => p.Library = await Api.LibraryListAsync(UserId));
    }
}

/// <summary>
/// Discover: search the catalog and add games in one click.
/// </summary>
/// <param name="apiClient">The API client.</param>
public sealed class DiscoverController(LudexClient apiClient) : WebsiteBaseController(apiClient)
{
    /// <summary>The page (results drawn on the server when a query comes in the address).</summary>
    /// <param name="q">The text searched.</param>
    /// <returns>The view.</returns>
    [HttpGet("/discover")]
    public async Task<IActionResult> Index(string? q = null)
    {
        var page = Shell(new DiscoverPageModel { Query = (q ?? string.Empty).Trim() }, AppSection.Discover);
        return await ScreenAsync("Index", page, async p =>
        {
            var friends = await Api.FriendsListAsync(UserId);
            p.Popular = friends.Popular;
            var health = await Api.HealthGetAsync();
            p.Source = health.Source;
            if (p.Query.Length >= 2)
            {
                await SearchIntoAsync(p);
            }
        });
    }

    /// <summary>Only the results, for the page script (it shows skeletons meanwhile).</summary>
    /// <param name="q">The text searched.</param>
    /// <returns>The results partial.</returns>
    [HttpGet("/discover/results")]
    public async Task<IActionResult> Results(string? q = null)
    {
        var page = Shell(new DiscoverPageModel { Query = (q ?? string.Empty).Trim() }, AppSection.Discover);
        if (page.Query.Length >= 2)
        {
            try
            {
                await SearchIntoAsync(page);
            }
            catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or LudexApiException)
            {
                page.SearchError = exception is LudexApiException api ? api.Message : ApiUnavailableMessage;
            }
        }

        return PartialView("_Results", page);
    }

    private async Task SearchIntoAsync(DiscoverPageModel page)
    {
        try
        {
            page.Result = await Api.GamesSearchAsync(UserId, page.Query);
            page.Source = page.Result.Source;
        }
        catch (LudexApiException exception) when (exception.StatusCode == System.Net.HttpStatusCode.BadRequest)
        {
            page.SearchError = exception.Message;
        }
    }
}

/// <summary>
/// A game's page, and the page scripts that save its status, rating, hours, platform and notes.
/// </summary>
/// <param name="apiClient">The API client.</param>
public sealed class GameController(LudexClient apiClient) : WebsiteBaseController(apiClient)
{
    /// <summary>The game page.</summary>
    /// <param name="id">The game.</param>
    /// <returns>The view.</returns>
    [HttpGet("/games/{id:int}")]
    public async Task<IActionResult> Index(int id)
    {
        try
        {
            var detail = await Api.GamesDetailAsync(UserId, id);
            var page = Shell(new GamePageModel { Detail = detail }, AppSection.Library);
            try
            {
                // The library down the side, as on Steam (the page still works without it).
                var library = await Api.LibraryListAsync(UserId);
                page.Sidebar = library.Entries;
                page.SidebarCollections = library.Collections;
            }
            catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or LudexApiException)
            {
                page.Sidebar = [];
            }

            if (detail.Mine?.SteamAppId is not null)
            {
                try
                {
                    page.Achievements = await Api.SteamAchievementsAsync(UserId, id);
                }
                catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or LudexApiException)
                {
                    page.Achievements = null;   // no achievements, or Steam can't be reached: the page goes without
                }
            }

            return View("Index", page);
        }
        catch (LudexApiException exception) when (exception.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            Response.StatusCode = StatusCodes.Status404NotFound;
            return View("~/Views/Shared/Unavailable.cshtml", Shell(new AppPageModel { Unavailable = "That game isn't in the catalog." }, AppSection.Discover));
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or LudexApiException)
        {
            var message = exception is LudexApiException { StatusCode: System.Net.HttpStatusCode.BadRequest } api ? api.Message : ApiUnavailableMessage;
            return View("~/Views/Shared/Unavailable.cshtml", Shell(new AppPageModel { Unavailable = message }, AppSection.Library));
        }
    }

    /// <summary>Saves the game page's values (adding the game when it isn't in the library).</summary>
    /// <param name="id">The game.</param>
    /// <param name="form">Status, rating, hours, platform and notes.</param>
    /// <returns>The entry, or the errors.</returns>
    [HttpPost("/games/{id:int}/save")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(int id, [FromBody] GameSaveForm form) => Json(await CallAsync(() => Api.LibrarySaveAsync(new LibrarySaveRequest
    {
        UserId = UserId,
        GameId = id,
        Status = form.Status,
        Rating = form.Rating,
        Hours = form.Hours,
        Platform = form.Platform,
        Store = form.Store,
        Format = form.Format,
        Notes = form.Notes,
        Quiet = form.Quiet,
    })));

    /// <summary>Sets only the status (the search page's and the cards' buttons).</summary>
    /// <param name="id">The game.</param>
    /// <param name="form">The status.</param>
    /// <returns>The entry, or the errors.</returns>
    [HttpPost("/games/{id:int}/status")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetStatus(int id, [FromBody] GameStatusForm form) =>
        Json(await CallAsync(() => Api.LibraryStatusAsync(new LibraryStatusRequest { UserId = UserId, GameId = id, Status = form.Status })));

    /// <summary>Sets the price under which a wishlisted game raises an alert.</summary>
    /// <param name="id">The game.</param>
    /// <param name="form">The price (null: no alert).</param>
    /// <returns>The price now, or the errors.</returns>
    [HttpPost("/games/{id:int}/price-target")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PriceTarget(int id, [FromBody] PriceTargetForm form) =>
        Json(await CallAsync(() => Api.LibraryPriceTargetAsync(new PriceTargetRequest { UserId = UserId, GameId = id, Target = form.Target })));

    /// <summary>Takes the game out of the library.</summary>
    /// <param name="id">The game.</param>
    /// <returns>Back to the library.</returns>
    [HttpPost("/games/{id:int}/remove")]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Remove(int id) => FormAsync(() => Api.LibraryDeleteAsync(UserId, id), "Taken out of your library.", "/library");
}

/// <summary>The game page's values.</summary>
public sealed class GameSaveForm
{
    /// <summary>Gets or sets the status.</summary>
    public GameStatus Status { get; set; } = GameStatus.Playing;

    /// <summary>Gets or sets the rating (1–10).</summary>
    public int? Rating { get; set; }

    /// <summary>Gets or sets the hours.</summary>
    public decimal? Hours { get; set; }

    /// <summary>Gets or sets the platform.</summary>
    public string? Platform { get; set; }

    /// <summary>Gets or sets the PC store (Steam, Epic Games…).</summary>
    public string? Store { get; set; }

    /// <summary>Gets or sets physical or digital.</summary>
    public GameFormat? Format { get; set; }

    /// <summary>Gets or sets the notes.</summary>
    public string? Notes { get; set; }

    /// <summary>Gets or sets a value indicating whether to leave the change out of the friends' feed (Undo).</summary>
    public bool Quiet { get; set; }
}

/// <summary>A price target from the game page.</summary>
public sealed class PriceTargetForm
{
    /// <summary>Gets or sets the price (null: no alert).</summary>
    public decimal? Target { get; set; }
}

/// <summary>A status from a button.</summary>
public sealed class GameStatusForm
{
    /// <summary>Gets or sets the status.</summary>
    public GameStatus Status { get; set; } = GameStatus.Playing;
}

/// <summary>
/// Friends: requests, the list, and what they're playing.
/// </summary>
/// <param name="apiClient">The API client.</param>
public sealed class FriendsController(LudexClient apiClient) : WebsiteBaseController(apiClient)
{
    /// <summary>The friends page.</summary>
    /// <returns>The view.</returns>
    [HttpGet("/friends")]
    public async Task<IActionResult> Index() =>
        await ScreenAsync("Index", Shell(new FriendsPageModel(), AppSection.Friends), async page => page.Friends = await Api.FriendsListAsync(UserId));

    /// <summary>Asks someone to be friends.</summary>
    /// <param name="userName">Their user name.</param>
    /// <param name="back">Where to go afterwards (a profile page; default the friends page).</param>
    /// <returns>Back to the friends page or the profile.</returns>
    [HttpPost("/friends/request")]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> SendRequest([FromForm] string? userName, [FromForm] string? back = null) =>
        FormAsync(
            () => Api.FriendsRequestAsync(new FriendRequestRequest { UserId = UserId, UserName = userName }),
            $"Request sent to {userName?.Trim()}.",
            Back(back));

    /// <summary>Accepts a request.</summary>
    /// <param name="friendId">The account that asked.</param>
    /// <param name="name">Its user name (for the message).</param>
    /// <param name="back">Where to go afterwards (a profile page; default the friends page).</param>
    /// <returns>Back to the friends page or the profile.</returns>
    [HttpPost("/friends/accept")]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Accept([FromForm] string? friendId, [FromForm] string? name, [FromForm] string? back = null) =>
        FormAsync(() => Api.FriendsAcceptAsync(new FriendActionRequest { UserId = UserId, FriendId = friendId }), $"You and {name} are friends now.", Back(back));

    /// <summary>Declines or cancels a request, or ends a friendship.</summary>
    /// <param name="friendId">The other account.</param>
    /// <param name="back">Where to go afterwards (a profile page; default the friends page).</param>
    /// <returns>Back to the friends page or the profile.</returns>
    [HttpPost("/friends/remove")]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Remove([FromForm] string? friendId, [FromForm] string? back = null) =>
        FormAsync(() => Api.FriendsRemoveAsync(new FriendActionRequest { UserId = UserId, FriendId = friendId }), null, Back(back));

    // Only a profile page is a place to go back to; anything else goes to the friends page.
    private static string Back(string? back) =>
        back is { Length: > 3 } path && path.StartsWith("/u/", StringComparison.Ordinal) && !path.Contains("//", StringComparison.Ordinal) && !path.Contains('\\')
            ? path
            : "/friends";
}

/// <summary>
/// Settings: the account, the password, and (admins) the other accounts.
/// </summary>
/// <param name="apiClient">The API client.</param>
public sealed class SettingsController(LudexClient apiClient, IOptions<WebsiteOptions> options) : WebsiteBaseController(apiClient)
{
    /// <summary>The settings page.</summary>
    /// <param name="discord">What just happened with Discord (linked, unlinked, taken, failed), for a note.</param>
    /// <returns>The view.</returns>
    [HttpGet("/settings")]
    public async Task<IActionResult> Index(string? discord = null)
    {
        var page = Shell(new SettingsPageModel { UserId = UserId, DiscordEnabled = options.Value.DiscordEnabled, DiscordNote = discord }, AppSection.Settings);
        return await ScreenAsync("Index", page, async p =>
        {
            var me = await Api.UserGetAsync(UserId);
            p.HasPassword = me.HasPassword;
            p.DiscordLinked = me.DiscordLinked;
            p.DiscordName = me.DiscordName;

            p.Health = await Api.HealthGetAsync();
            if (p.IsAdmin)
            {
                p.Users = await Api.UserListAsync();
            }
        });
    }

    /// <summary>Sends the browser to Discord to link a Discord account to this account.</summary>
    /// <returns>A redirect to Discord.</returns>
    [HttpPost("/settings/discord")]
    [ValidateAntiForgeryToken]
    public IActionResult DiscordLink()
    {
        if (!options.Value.DiscordEnabled)
        {
            return NotFound();
        }

        var properties = new AuthenticationProperties { RedirectUri = "/sign-in/discord/done" };
        properties.Items[DiscordAuthentication.ModeKey] = "link";
        return Challenge(properties, DiscordAuthentication.Scheme);
    }

    /// <summary>Unlinks the Discord account (the password still signs in).</summary>
    /// <returns>Back to Settings with a note.</returns>
    [HttpPost("/settings/discord/unlink")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DiscordUnlink()
    {
        try
        {
            await Api.UserDiscordLinkAsync(new UserDiscordLinkRequest { Id = UserId });
        }
        catch (LudexApiException exception) when (exception.StatusCode == System.Net.HttpStatusCode.BadRequest)
        {
            return Redirect("/settings?discord=needs-password#discord");
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or LudexApiException)
        {
            return Redirect("/settings?discord=failed#discord");
        }

        return Redirect("/settings?discord=unlinked#discord");
    }

    /// <summary>Changes the colour theme (the page reloads with it).</summary>
    /// <param name="form">The theme.</param>
    /// <returns>The theme now, or the errors.</returns>
    [HttpPost("/settings/accent")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Accent([FromBody] AccentForm form)
    {
        var result = await CallAsync(() => Api.ProfileAccentAsync(new ProfileAccentRequest { UserId = UserId, Accent = form.Accent }));
        try
        {
            await Security.LudexAuthentication.RefreshAsync(HttpContext, await Api.UserGetAsync(UserId));
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or LudexApiException)
        {
            // Saved; the page catches up at the next check of the session.
        }

        return Json(result);
    }

    /// <summary>Changes the password (the other devices are signed out).</summary>
    /// <param name="currentPassword">The current password.</param>
    /// <param name="newPassword">The new one.</param>
    /// <param name="confirmPassword">The new one again.</param>
    /// <returns>Back to the settings, signed in again.</returns>
    [HttpPost("/settings/password")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Password([FromForm] string? currentPassword, [FromForm] string? newPassword, [FromForm] string? confirmPassword)
    {
        if (!string.Equals(newPassword, confirmPassword, StringComparison.Ordinal))
        {
            TempData["problem"] = "The two new passwords are different.";
            return LocalRedirect("/settings");
        }

        return await FormAsync(
            async () =>
            {
                var user = await Api.UserPasswordAsync(new UserPasswordRequest { Id = UserId, CurrentPassword = currentPassword, NewPassword = newPassword });
                await Security.LudexAuthentication.SignInAsync(HttpContext, user, remember: true, rememberDays: 30);
            },
            "Password changed. Your other devices are signed out.",
            "/settings");
    }

    /// <summary>Deletes another account (admins only).</summary>
    /// <param name="id">The account.</param>
    /// <returns>Back to the settings.</returns>
    [HttpPost("/settings/delete-user")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public Task<IActionResult> DeleteUser([FromForm] string? id) =>
        id == UserId
            ? Task.FromResult<IActionResult>(LocalRedirect("/settings"))
            : FormAsync(() => Api.UserDeleteAsync(id ?? string.Empty), "Account deleted.", "/settings");
}

/// <summary>The colour theme from the settings' swatches.</summary>
public sealed class AccentForm
{
    /// <summary>Gets or sets the theme's key.</summary>
    public string? Accent { get; set; }
}
