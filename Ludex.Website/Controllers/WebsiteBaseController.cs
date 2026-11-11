using Ludex.Library.Validation;
using Ludex.Sdk;
using Ludex.Website.Model;
using Ludex.Website.Security;
using Microsoft.AspNetCore.Mvc;

namespace Ludex.Website.Controllers;

/// <summary>
/// Base of every signed-in Website controller: fills in the shell of a page, reads its data from the WebApi, and turns
/// "the engine isn't answering" into a card instead of an error page.
/// </summary>
/// <param name="apiClient">The Ludex.WebApi client.</param>
[Microsoft.AspNetCore.Authorization.Authorize]
public abstract class WebsiteBaseController(LudexClient apiClient) : Controller
{
    /// <summary>What the pages say when the WebApi can't be reached.</summary>
    public const string ApiUnavailableMessage = "Can't reach Ludex's engine (WebApi) right now. Check that the ludex-webapi container is running.";

    private const string NoticeKey = "notice";
    private const string ProblemKey = "problem";

    /// <summary>Gets the API client.</summary>
    protected LudexClient Api { get; } = apiClient;

    /// <summary>Gets the signed-in account's id.</summary>
    protected string UserId => LudexAuthentication.UserId(User);

    /// <summary>
    /// Fills in the shell of a page: who's signed in, the tab, and the message a form left.
    /// </summary>
    /// <typeparam name="T">The page model.</typeparam>
    /// <param name="page">The page.</param>
    /// <param name="section">The tab.</param>
    /// <returns>The page.</returns>
    protected T Shell<T>(T page, AppSection section)
        where T : AppPageModel
    {
        page.UserName = User.Identity?.Name ?? string.Empty;
        page.MyId = UserId;
        page.MyAvatarVersion = LudexAuthentication.AvatarVersion(User);
        page.IsAdmin = User.IsInRole("Admin");
        page.Section = section;
        page.Notice = TempData[NoticeKey] as string;
        page.Problem = TempData[ProblemKey] as string;
        return page;
    }

    /// <summary>
    /// Reads a page's data and shows it, or the "engine isn't answering" card.
    /// </summary>
    /// <typeparam name="T">The page model.</typeparam>
    /// <param name="viewName">The screen's view.</param>
    /// <param name="page">The page (shell filled in).</param>
    /// <param name="load">Reads the data into the page.</param>
    /// <returns>The view.</returns>
    protected async Task<IActionResult> ScreenAsync<T>(string viewName, T page, Func<T, Task> load)
        where T : AppPageModel
    {
        try
        {
            await load(page);
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or LudexApiException)
        {
            page.Unavailable = exception is LudexApiException api && api.StatusCode != System.Net.HttpStatusCode.Unauthorized
                ? api.Message
                : ApiUnavailableMessage;
        }

        return View(page.Unavailable is null ? viewName : "~/Views/Shared/Unavailable.cshtml", page);
    }

    /// <summary>
    /// Calls the API for a page script and wraps the answer.
    /// </summary>
    /// <typeparam name="T">The data type.</typeparam>
    /// <param name="call">The call.</param>
    /// <returns>The data, or the errors.</returns>
    protected static async Task<AjaxDataResult<T>> CallAsync<T>(Func<Task<T>> call)
    {
        try
        {
            return new AjaxDataResult<T> { Data = await call() };
        }
        catch (LudexApiException exception)
        {
            return new AjaxDataResult<T> { IsValid = false, ValidationErrors = [.. exception.ToValidationErrors()] };
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            return new AjaxDataResult<T> { IsValid = false, ValidationErrors = [new ApiValidationError(string.Empty, ApiUnavailableMessage)] };
        }
    }

    /// <summary>
    /// Runs what a form asked for and goes back to a page, carrying the message to show there.
    /// </summary>
    /// <param name="call">The call.</param>
    /// <param name="notice">What to say when it worked.</param>
    /// <param name="redirectTo">Where to go afterwards.</param>
    /// <returns>The redirect.</returns>
    protected async Task<IActionResult> FormAsync(Func<Task> call, string? notice, string redirectTo)
    {
        try
        {
            await call();
            if (notice is not null)
            {
                TempData[NoticeKey] = notice;
            }
        }
        catch (LudexApiException exception)
        {
            TempData[ProblemKey] = string.Join(" ", exception.ToValidationErrors().Select(static error => error.Message));
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            TempData[ProblemKey] = ApiUnavailableMessage;
        }

        return LocalRedirect(redirectTo);
    }
}
