using Ludex.Sdk;
using Ludex.Sdk.Models;
using Ludex.Website.Model;
using Microsoft.AspNetCore.Mvc;

namespace Ludex.Website.Controllers;

/// <summary>
/// Collections (for the page scripts: the sidebar, the library bar and the game page), and the bell.
/// </summary>
/// <param name="apiClient">The API client.</param>
public sealed class CollectionController(LudexClient apiClient) : WebsiteBaseController(apiClient)
{
    /// <summary>Creates a collection.</summary>
    /// <param name="form">The name.</param>
    /// <returns>The collections, or the errors.</returns>
    [HttpPost("/collections/create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([FromBody] CollectionForm form) =>
        Json(await CallAsync(() => Api.CollectionsSaveAsync(new CollectionSaveRequest { UserId = UserId, Name = form.Name })));

    /// <summary>Renames a collection.</summary>
    /// <param name="form">The collection and the name.</param>
    /// <returns>The collections, or the errors.</returns>
    [HttpPost("/collections/rename")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Rename([FromBody] CollectionForm form) =>
        Json(await CallAsync(() => Api.CollectionsSaveAsync(new CollectionSaveRequest { UserId = UserId, Id = form.Id, Name = form.Name })));

    /// <summary>Deletes a collection (its games stay in the library).</summary>
    /// <param name="form">The collection.</param>
    /// <returns>The collections, or the errors.</returns>
    [HttpPost("/collections/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete([FromBody] CollectionForm form) =>
        Json(await CallAsync(() => Api.CollectionsDeleteAsync(new CollectionDeleteRequest { UserId = UserId, Id = form.Id ?? 0 })));

    /// <summary>Puts a game in a collection or takes it out.</summary>
    /// <param name="form">The collection, the game and which way.</param>
    /// <returns>The collections, or the errors.</returns>
    [HttpPost("/collections/game")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Game([FromBody] CollectionGameForm form) =>
        Json(await CallAsync(() => Api.CollectionsGameAsync(new CollectionGameRequest { UserId = UserId, CollectionId = form.CollectionId, GameId = form.GameId, In = form.In })));

    /// <summary>The bell's page (opening it marks everything as seen).</summary>
    /// <returns>The view.</returns>
    [HttpGet("/notifications")]
    public async Task<IActionResult> Notifications() =>
        await ScreenAsync("~/Views/Notifications/Index.cshtml", Shell(new NotificationsPageModel(), AppSection.Home), async page => page.Notifications = await Api.NotificationsReadAsync(UserId));

    /// <summary>How many notifications are new, for the bell in the top bar.</summary>
    /// <returns>The count (0 when the engine can't be reached).</returns>
    [HttpGet("/notifications/count")]
    public async Task<IActionResult> NotificationCount()
    {
        try
        {
            return Json(new { unread = await Api.NotificationsUnreadAsync(UserId) });
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or LudexApiException)
        {
            return Json(new { unread = 0 });
        }
    }

    /// <summary>Syncs the library with Steam now.</summary>
    /// <returns>What changed, or the errors.</returns>
    [HttpPost("/steam/sync")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SteamSync() => Json(await CallAsync(() => Api.SteamSyncAsync(UserId)));
}

/// <summary>A collection from a page script.</summary>
public sealed class CollectionForm
{
    /// <summary>Gets or sets the collection (null: a new one).</summary>
    public int? Id { get; set; }

    /// <summary>Gets or sets the name.</summary>
    public string? Name { get; set; }
}

/// <summary>A game going in or out of a collection.</summary>
public sealed class CollectionGameForm
{
    /// <summary>Gets or sets the collection.</summary>
    public int CollectionId { get; set; }

    /// <summary>Gets or sets the game.</summary>
    public int GameId { get; set; }

    /// <summary>Gets or sets a value indicating whether it goes in (true) or out.</summary>
    public bool In { get; set; } = true;
}
