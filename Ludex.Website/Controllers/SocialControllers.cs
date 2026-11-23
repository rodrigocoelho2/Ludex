using Ludex.Library.Enums;
using Ludex.Sdk;
using Ludex.Sdk.Models;
using Ludex.Website.Helpers;
using Ludex.Website.Model;
using Microsoft.AspNetCore.Mvc;

namespace Ludex.Website.Controllers;

/// <summary>
/// Reviews: a review's own page, a page of reviews, likes and comments.
/// </summary>
/// <param name="apiClient">The API client.</param>
public sealed class ReviewsController(LudexClient apiClient) : WebsiteBaseController(apiClient)
{
    /// <summary>How many reviews a page shows.</summary>
    public const int PageSize = 20;

    /// <summary>A review's own page.</summary>
    /// <param name="id">The review.</param>
    /// <returns>The view.</returns>
    [HttpGet("/reviews/{id:long}")]
    public async Task<IActionResult> Show(long id)
    {
        var page = Shell(new ReviewPageModel(), AppSection.Home);
        try
        {
            page.Page = await Api.ReviewsPageAsync(UserId, id);
            return View("Show", page);
        }
        catch (LudexApiException exception) when (exception.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            Response.StatusCode = StatusCodes.Status404NotFound;
            page.Unavailable = "That review isn't there any more.";
            return View("~/Views/Shared/Unavailable.cshtml", page);
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or LudexApiException)
        {
            page.Unavailable = ApiUnavailableMessage;
            return View("~/Views/Shared/Unavailable.cshtml", page);
        }
    }

    /// <summary>A page of reviews: of a game, by an account, or of everyone.</summary>
    /// <param name="game">Only this game.</param>
    /// <param name="user">Only this account's.</param>
    /// <param name="sort">popular, recent, friends, highest or alltime.</param>
    /// <param name="page">The page (0 = first).</param>
    /// <returns>The view.</returns>
    [HttpGet("/reviews")]
    public async Task<IActionResult> Index(int? game = null, string? user = null, string? sort = null, int page = 0)
    {
        var order = Enum.TryParse<ReviewSort>(sort, true, out var parsed) && Enum.IsDefined(parsed) ? parsed : ReviewSort.Popular;
        var model = Shell(new ReviewsPageModel { GameId = game, Author = string.IsNullOrWhiteSpace(user) ? null : user.Trim(), PageNumber = Math.Max(0, page) }, AppSection.Home);
        return await ScreenAsync("Index", model, async p =>
        {
            p.Reviews = await Api.ReviewsListAsync(UserId, p.GameId, p.Author, order, p.PageNumber * PageSize, PageSize);
            p.GameName = p.GameId is not null ? p.Reviews.Reviews.FirstOrDefault()?.Game.Name : null;
            if (p.GameId is { } only && p.GameName is null)
            {
                p.GameName = (await Api.GamesDetailAsync(UserId, only)).Game.Name;
            }
        });
    }

    /// <summary>Likes a review or takes the like back (the page script).</summary>
    /// <param name="id">The review.</param>
    /// <param name="form">Like or not.</param>
    /// <returns>The likes now.</returns>
    [HttpPost("/reviews/{id:long}/like")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Like(long id, [FromBody] LikeForm form) =>
        Json(await CallAsync(() => Api.ReviewsLikeAsync(new ReviewLikeRequest { UserId = UserId, ReviewId = id, Like = form.Like })));

    /// <summary>Writes a comment under a review.</summary>
    /// <param name="id">The review.</param>
    /// <param name="text">The comment.</param>
    /// <returns>Back to the review.</returns>
    [HttpPost("/reviews/{id:long}/comment")]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Comment(long id, [FromForm] string? text) =>
        FormAsync(() => Api.ReviewsCommentAsync(new ReviewCommentRequest { UserId = UserId, ReviewId = id, Text = text }), null, $"/reviews/{id}#comments");

    /// <summary>Deletes a comment.</summary>
    /// <param name="id">The review.</param>
    /// <param name="comment">The comment.</param>
    /// <returns>Back to the review.</returns>
    [HttpPost("/reviews/{id:long}/comment/delete")]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> DeleteComment(long id, [FromForm] long comment) =>
        FormAsync(() => Api.ReviewsCommentDeleteAsync(new ReviewCommentDeleteRequest { UserId = UserId, ReviewId = id, CommentId = comment }), "Comment deleted.", $"/reviews/{id}#comments");
}

/// <summary>A like, from a page script.</summary>
public sealed class LikeForm
{
    /// <summary>Gets or sets a value indicating whether to like (false: take the like back).</summary>
    public bool Like { get; set; } = true;
}

/// <summary>
/// Lists of games: browse, read, make, change, like.
/// </summary>
/// <param name="apiClient">The API client.</param>
public sealed class ListsController(LudexClient apiClient) : WebsiteBaseController(apiClient)
{
    /// <summary>The lists page.</summary>
    /// <param name="user">Whose lists (empty: yours).</param>
    /// <returns>The view.</returns>
    [HttpGet("/lists")]
    public Task<IActionResult> Index(string? user = null) =>
        ScreenAsync("Index", Shell(new ListsPageModel { Owner = string.IsNullOrWhiteSpace(user) ? null : user.Trim() }, AppSection.Lists), async p =>
            p.Lists = await Api.ListsBrowseAsync(UserId, p.Owner));

    /// <summary>A list's own page.</summary>
    /// <param name="id">The list.</param>
    /// <param name="hide">"played" hides the games you've played.</param>
    /// <returns>The view.</returns>
    [HttpGet("/lists/{id:int}")]
    public async Task<IActionResult> Show(int id, string? hide = null)
    {
        var page = Shell(new ListPageModel { HidePlayed = hide == "played" }, AppSection.Lists);
        try
        {
            page.Detail = await Api.ListsGetAsync(UserId, id);
            return View("Show", page);
        }
        catch (LudexApiException exception) when (exception.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            Response.StatusCode = StatusCodes.Status404NotFound;
            page.Unavailable = "That list isn't there, or it isn't shared with you.";
            return View("~/Views/Shared/Unavailable.cshtml", page);
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or LudexApiException)
        {
            page.Unavailable = ApiUnavailableMessage;
            return View("~/Views/Shared/Unavailable.cshtml", page);
        }
    }

    /// <summary>Making a list (with a game in it already, from a game page).</summary>
    /// <param name="game">A game to put in it.</param>
    /// <returns>The view.</returns>
    [HttpGet("/lists/new")]
    public Task<IActionResult> New(int? game = null) =>
        ScreenAsync("Edit", Shell(new ListEditPageModel(), AppSection.Lists), async p =>
        {
            if (game is { } first)
            {
                p.FirstGame = (await Api.GamesDetailAsync(UserId, first)).Game;
            }
        });

    /// <summary>Changing one of your lists.</summary>
    /// <param name="id">The list.</param>
    /// <returns>The view.</returns>
    [HttpGet("/lists/{id:int}/edit")]
    public Task<IActionResult> Edit(int id) =>
        ScreenAsync("Edit", Shell(new ListEditPageModel(), AppSection.Lists), async p =>
        {
            var detail = await Api.ListsGetAsync(UserId, id);
            if (!detail.List.IsMine)
            {
                throw new LudexApiException(System.Net.HttpStatusCode.Forbidden, new Ludex.Library.Validation.ApiValidationProblemDetails { Title = "That list isn't yours to change.", Status = 403 });
            }

            p.Detail = detail;
        });

    /// <summary>Saves a list.</summary>
    /// <param name="form">The list.</param>
    /// <returns>The list's page.</returns>
    [HttpPost("/lists/save")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save([FromForm] ListForm form)
    {
        int? saved = null;
        var result = await FormAsync(
            async () => saved = (await Api.ListsSaveAsync(new ListSaveRequest
            {
                UserId = UserId,
                Id = form.Id,
                Name = form.Name,
                Description = form.Description,
                Ranked = form.Ranked,
                Visibility = Enum.IsDefined(form.Visibility) ? form.Visibility : ListVisibility.Public,
                FirstGameId = form.Game,
            })).List.Id,
            form.Id is null ? "List made." : "List saved.",
            form.Id is { } id ? $"/lists/{id}/edit" : "/lists/new");
        return saved is { } listId ? LocalRedirect($"/lists/{listId}") : result;
    }

    /// <summary>Deletes one of your lists.</summary>
    /// <param name="id">The list.</param>
    /// <returns>Back to the lists.</returns>
    [HttpPost("/lists/{id:int}/delete")]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Delete(int id) => FormAsync(() => Api.ListsDeleteAsync(new ListDeleteRequest { UserId = UserId, Id = id }), "List deleted.", "/lists");

    /// <summary>Puts a game in a list or takes it out (the game page's script).</summary>
    /// <param name="form">The list, the game, in or out.</param>
    /// <returns>Your lists and whether the game is in each.</returns>
    [HttpPost("/lists/item")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Item([FromBody] ListItemForm form) =>
        Json(await CallAsync(() => Api.ListsItemAsync(new ListItemRequest { UserId = UserId, ListId = form.ListId, GameId = form.GameId, In = form.In })));

    /// <summary>Takes a game out of a list (the list's edit page).</summary>
    /// <param name="id">The list.</param>
    /// <param name="game">The game.</param>
    /// <returns>Back to the list's edit page.</returns>
    [HttpPost("/lists/{id:int}/remove")]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Remove(int id, [FromForm] int game) =>
        FormAsync(() => Api.ListsItemAsync(new ListItemRequest { UserId = UserId, ListId = id, GameId = game, In = false }), null, $"/lists/{id}/edit#games");

    /// <summary>Moves a game of a list (the list's edit page).</summary>
    /// <param name="id">The list.</param>
    /// <param name="game">The game.</param>
    /// <param name="position">Its new place.</param>
    /// <returns>Back to the list's edit page.</returns>
    [HttpPost("/lists/{id:int}/move")]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Move(int id, [FromForm] int game, [FromForm] int position) =>
        FormAsync(() => Api.ListsMoveAsync(new ListMoveRequest { UserId = UserId, ListId = id, GameId = game, Position = position }), null, $"/lists/{id}/edit#game-{game}");

    /// <summary>Likes a list or takes the like back (the page script).</summary>
    /// <param name="id">The list.</param>
    /// <param name="form">Like or not.</param>
    /// <returns>The likes now.</returns>
    [HttpPost("/lists/{id:int}/like")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Like(int id, [FromBody] LikeForm form) =>
        Json(await CallAsync(() => Api.ListsLikeAsync(new ListLikeRequest { UserId = UserId, ListId = id, Like = form.Like })));
}

/// <summary>A list, from its form.</summary>
public sealed class ListForm
{
    /// <summary>Gets or sets the list (null: a new one).</summary>
    public int? Id { get; set; }

    /// <summary>Gets or sets the name.</summary>
    public string? Name { get; set; }

    /// <summary>Gets or sets the description.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets a value indicating whether it is ranked.</summary>
    public bool Ranked { get; set; }

    /// <summary>Gets or sets who can see it.</summary>
    public ListVisibility Visibility { get; set; } = ListVisibility.Public;

    /// <summary>Gets or sets a game to put in a new list.</summary>
    public int? Game { get; set; }
}

/// <summary>A game in or out of a list, from the game page's script.</summary>
public sealed class ListItemForm
{
    /// <summary>Gets or sets the list.</summary>
    public int ListId { get; set; }

    /// <summary>Gets or sets the game.</summary>
    public int GameId { get; set; }

    /// <summary>Gets or sets a value indicating whether to put it in.</summary>
    public bool In { get; set; } = true;
}

/// <summary>
/// The journal: a month of sessions, and writing one down.
/// </summary>
/// <param name="apiClient">The API client.</param>
public sealed class JournalController(LudexClient apiClient) : WebsiteBaseController(apiClient)
{
    /// <summary>A month of the journal.</summary>
    /// <param name="month">yyyy-MM (empty: this month).</param>
    /// <param name="day">The day picked (yyyy-MM-dd; empty: today, or the last day with a session).</param>
    /// <returns>The view.</returns>
    [HttpGet("/journal")]
    public Task<IActionResult> Index(string? month = null, string? day = null)
    {
        var today = Clock.Today;
        var first = DateOnly.TryParseExact((month ?? string.Empty) + "-01", "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var m)
            ? m
            : new DateOnly(today.Year, today.Month, 1);
        return ScreenAsync("Index", Shell(new JournalPageModel { Today = today }, AppSection.Journal), async p =>
        {
            p.Journal = await Api.JournalMonthAsync(UserId, first.Year, first.Month, today);
            var picked = DateOnly.TryParseExact(day ?? string.Empty, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var d) && d.Year == first.Year && d.Month == first.Month
                ? d
                : today.Year == first.Year && today.Month == first.Month ? today : p.Journal.Entries.Select(static e => e.Day).DefaultIfEmpty(first).Max();
            p.Day = picked;
            p.Games = [.. (await Api.LibraryListAsync(UserId)).Entries
                .Where(static e => e.Status is not GameStatus.Wishlist)
                .GroupBy(static e => e.Game.Id).Select(static g => g.First())
                .OrderByDescending(static e => e.Status == GameStatus.Playing)
                .ThenByDescending(static e => e.UpdatedAt)];
        });
    }

    /// <summary>Writes down a session.</summary>
    /// <param name="form">The session.</param>
    /// <returns>Back to the journal (or the game).</returns>
    [HttpPost("/journal/session")]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Add([FromForm] SessionForm form)
    {
        var day = form.Day ?? Clock.Today;
        var back = form.Back is { Length: > 0 } b && b.StartsWith('/') && !b.StartsWith("//", StringComparison.Ordinal)
            ? b
            : $"/journal?month={day:yyyy-MM}&day={day:yyyy-MM-dd}";
        return FormAsync(
            () => Api.JournalAddAsync(new SessionAddRequest
            {
                UserId = UserId,
                GameId = form.Game,
                Day = day,
                Minutes = (form.Hours * 60) + form.Minutes,
                Platform = form.Platform,
                Note = form.Note,
            }),
            "Session written down.",
            back);
    }

    /// <summary>Takes a session out of the journal.</summary>
    /// <param name="id">The session.</param>
    /// <param name="back">Where to go afterwards.</param>
    /// <returns>Back to the journal.</returns>
    [HttpPost("/journal/session/{id:long}/delete")]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Delete(long id, [FromForm] string? back = null) =>
        FormAsync(
            () => Api.JournalDeleteAsync(new SessionDeleteRequest { UserId = UserId, Id = id }),
            "Session taken out.",
            back is { Length: > 0 } b && b.StartsWith('/') && !b.StartsWith("//", StringComparison.Ordinal) ? b : "/journal");
}

/// <summary>A session, from its form.</summary>
public sealed class SessionForm
{
    /// <summary>Gets or sets the game.</summary>
    public int Game { get; set; }

    /// <summary>Gets or sets the day.</summary>
    public DateOnly? Day { get; set; }

    /// <summary>Gets or sets the hours.</summary>
    public int Hours { get; set; }

    /// <summary>Gets or sets the minutes.</summary>
    public int Minutes { get; set; }

    /// <summary>Gets or sets the platform.</summary>
    public string? Platform { get; set; }

    /// <summary>Gets or sets a note.</summary>
    public string? Note { get; set; }

    /// <summary>Gets or sets where to go afterwards.</summary>
    public string? Back { get; set; }
}

/// <summary>
/// "Log a game": the dialog's search, its form, and saving it.
/// </summary>
/// <param name="apiClient">The API client.</param>
public sealed class LogController(LudexClient apiClient) : WebsiteBaseController(apiClient)
{
    /// <summary>The platforms offered when the catalog doesn't say which a game is on.</summary>
    private static readonly string[] FallbackPlatforms = ["PC", "PS5", "Switch 2", "Switch", "Xbox Series", "PS4", "Mac", "iOS", "Android"];

    /// <summary>Games for the dialog's search (the library first, then the catalog).</summary>
    /// <param name="q">The text typed (empty: what you're playing and the last games you touched).</param>
    /// <returns>The results partial.</returns>
    [HttpGet("/log/search")]
    public async Task<IActionResult> Search(string? q = null)
    {
        var query = (q ?? string.Empty).Trim();
        var results = new List<GameSummaryDTO>();
        string? problem = null;
        try
        {
            if (query.Length < 2)
            {
                results = [.. (await Api.LibraryListAsync(UserId)).Entries
                    .OrderByDescending(static e => e.Status == GameStatus.Playing)
                    .ThenByDescending(static e => e.UpdatedAt)
                    .Select(static e => e.Game)
                    .DistinctBy(static g => g.Id)
                    .Take(8)];
            }
            else
            {
                results = [.. (await Api.GamesSearchAsync(UserId, query)).Results.Take(12)];
            }
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or LudexApiException)
        {
            problem = exception is LudexApiException api ? api.Message : ApiUnavailableMessage;
        }

        ViewData["Query"] = query;
        ViewData["Problem"] = problem;
        return PartialView("~/Views/Log/_Search.cshtml", results);
    }

    /// <summary>The dialog's form for a game.</summary>
    /// <param name="id">The game.</param>
    /// <param name="entry">The copy.</param>
    /// <returns>The form partial.</returns>
    [HttpGet("/log/{id:int}")]
    public async Task<IActionResult> Form(int id, long? entry = null)
    {
        try
        {
            var form = await Api.LogFormAsync(UserId, id, entry);
            var platforms = form.Game.Platforms.ToList();
            if (form.Platform is { Length: > 0 } saved && !platforms.Contains(saved, StringComparer.OrdinalIgnoreCase))
            {
                platforms.Insert(0, saved);
            }

            if (platforms.Count == 0)
            {
                platforms.AddRange(FallbackPlatforms);
            }

            return PartialView("~/Views/Log/_Form.cshtml", new LogFormView(form, platforms, Clock.Today));
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or LudexApiException)
        {
            Response.StatusCode = exception is LudexApiException { StatusCode: System.Net.HttpStatusCode.NotFound } ? 404 : 503;
            return Content(exception is LudexApiException api ? api.Message : ApiUnavailableMessage);
        }
    }

    /// <summary>Saves the dialog.</summary>
    /// <param name="id">The game.</param>
    /// <param name="form">Everything on it.</param>
    /// <returns>What was saved, or the errors.</returns>
    [HttpPost("/log/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(int id, [FromBody] LogForm form) => Json(await CallAsync(() => Api.LogSaveAsync(new LogSaveRequest
    {
        UserId = UserId,
        GameId = id,
        EntryId = form.EntryId,
        Status = form.Status,
        Rating = form.Rating is >= 1 and <= 10 ? form.Rating : null,
        Platform = form.Platform,
        Hours = form.Hours,
        Ending = form.Ending,
        StartedOn = form.StartedOn,
        FinishedOn = form.FinishedOn,
        Review = form.Review,
        Spoilers = form.Spoilers,
        SessionMinutes = form.SessionMinutes,
        SessionDay = form.SessionMinutes is > 0 ? Clock.Today : null,
        SessionNote = form.SessionNote,
        Quiet = !form.Share,
    })));
}

/// <summary>The Log a game dialog, from its script.</summary>
public sealed class LogForm
{
    /// <summary>Gets or sets the copy.</summary>
    public long? EntryId { get; set; }

    /// <summary>Gets or sets the status.</summary>
    public GameStatus Status { get; set; } = GameStatus.Playing;

    /// <summary>Gets or sets the rating (1–10; 0 or null: none).</summary>
    public int? Rating { get; set; }

    /// <summary>Gets or sets the platform.</summary>
    public string? Platform { get; set; }

    /// <summary>Gets or sets the hours.</summary>
    public decimal? Hours { get; set; }

    /// <summary>Gets or sets how it ended.</summary>
    public GameEnding? Ending { get; set; }

    /// <summary>Gets or sets the day it was started.</summary>
    public DateOnly? StartedOn { get; set; }

    /// <summary>Gets or sets the day it was finished.</summary>
    public DateOnly? FinishedOn { get; set; }

    /// <summary>Gets or sets the review.</summary>
    public string? Review { get; set; }

    /// <summary>Gets or sets a value indicating whether the review has spoilers.</summary>
    public bool Spoilers { get; set; }

    /// <summary>Gets or sets the minutes of a session for today (0: none).</summary>
    public int? SessionMinutes { get; set; }

    /// <summary>Gets or sets the session's note.</summary>
    public string? SessionNote { get; set; }

    /// <summary>Gets or sets a value indicating whether friends see it in their feed.</summary>
    public bool Share { get; set; } = true;
}
