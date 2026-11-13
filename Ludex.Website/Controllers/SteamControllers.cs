using System.Net;
using Ludex.Library.Enums;
using Ludex.Sdk;
using Ludex.Sdk.Models;
using Ludex.Website.Model;
using Microsoft.AspNetCore.Mvc;

namespace Ludex.Website.Controllers;

/// <summary>
/// Importing a Steam library: read it, review it, add the games ticked.
/// </summary>
/// <param name="apiClient">The API client.</param>
public sealed class SteamController(LudexClient apiClient) : WebsiteBaseController(apiClient)
{
    /// <summary>The import page: with a profile (or one imported from before), the games to review.</summary>
    /// <param name="profile">The Steam profile typed.</param>
    /// <returns>The view.</returns>
    [HttpGet("/steam")]
    public async Task<IActionResult> Index(string? profile = null)
    {
        var page = Shell(new SteamPageModel { ProfileInput = (profile ?? string.Empty).Trim() }, AppSection.Settings);
        try
        {
            page.Preview = await Api.SteamPreviewAsync(UserId, page.ProfileInput);
            if (page.ProfileInput.Length == 0 && page.Preview.ProfileUrl is { } url)
            {
                page.ProfileInput = url;
            }
        }
        catch (LudexApiException exception) when (exception.StatusCode == HttpStatusCode.BadRequest)
        {
            page.ReadProblem = string.Join(" ", exception.ToValidationErrors().Select(static e => e.Message).Distinct());
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or LudexApiException)
        {
            page.Unavailable = ApiUnavailableMessage;
            return View("~/Views/Shared/Unavailable.cshtml", page);
        }

        return View("Index", page);
    }

    /// <summary>Adds the games ticked, for the page script.</summary>
    /// <param name="form">The Steam account and the games.</param>
    /// <returns>How many were added and updated, or the errors.</returns>
    [HttpPost("/steam/import")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(4 * 1024 * 1024)]
    public async Task<IActionResult> Import([FromBody] SteamImportForm form) => Json(await CallAsync(() => Api.SteamImportAsync(new SteamImportRequest
    {
        UserId = UserId,
        SteamId = form.SteamId,
        Games = [.. form.Games.Select(static g => new SteamImportGameRequest { GameId = g.GameId, Status = g.Status, Hours = g.Hours, LastPlayedAt = g.LastPlayedAt, AppId = g.AppId, RecentHours = g.RecentHours })],
    })));
}

/// <summary>What the import page sends.</summary>
public sealed class SteamImportForm
{
    /// <summary>Gets or sets the SteamID64.</summary>
    public string? SteamId { get; set; }

    /// <summary>Gets or sets the games ticked.</summary>
    public List<SteamImportFormGame> Games { get; set; } = [];
}

/// <summary>One game ticked.</summary>
public sealed class SteamImportFormGame
{
    /// <summary>Gets or sets the game.</summary>
    public int GameId { get; set; }

    /// <summary>Gets or sets the status.</summary>
    public GameStatus Status { get; set; } = GameStatus.Finished;

    /// <summary>Gets or sets the hours played on Steam.</summary>
    public decimal? Hours { get; set; }

    /// <summary>Gets or sets when it was last played on Steam.</summary>
    public DateTimeOffset? LastPlayedAt { get; set; }

    /// <summary>Gets or sets its Steam app (its achievements are read by it).</summary>
    public int? AppId { get; set; }

    /// <summary>Gets or sets the hours played on Steam in the last two weeks.</summary>
    public decimal? RecentHours { get; set; }
}
