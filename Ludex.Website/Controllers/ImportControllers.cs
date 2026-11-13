using System.Net;
using System.Text;
using Ludex.Library.Enums;
using Ludex.Sdk;
using Ludex.Sdk.Models;
using Ludex.Website.Model;
using Microsoft.AspNetCore.Mvc;

namespace Ludex.Website.Controllers;

/// <summary>
/// Importing a Playnite library: upload the CSV of Library Exporter Advanced, review it, add the games ticked.
/// </summary>
/// <param name="apiClient">The API client.</param>
public sealed class PlayniteController(LudexClient apiClient) : WebsiteBaseController(apiClient)
{
    /// <summary>Largest CSV taken.</summary>
    private const int MaxFile = 24 * 1024 * 1024;

    /// <summary>The import page.</summary>
    /// <returns>The view.</returns>
    [HttpGet("/playnite")]
    public Task<IActionResult> Index() => PageAsync(null, null);

    /// <summary>Reads the CSV chosen and shows its games.</summary>
    /// <param name="file">The CSV.</param>
    /// <returns>The view.</returns>
    [HttpPost("/playnite")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(MaxFile + (1024 * 1024))]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxFile + (1024 * 1024))]
    public async Task<IActionResult> Upload(IFormFile? file)
    {
        if (file is null || file.Length == 0)
        {
            return await PageAsync(null, null, "Choose the CSV that Library Exporter Advanced wrote.");
        }

        if (file.Length > MaxFile)
        {
            return await PageAsync(null, file.FileName, "That file is too big for a Playnite export (24 MB at most). Export only the columns the steps list.");
        }

        string csv;
        using (var reader = new StreamReader(file.OpenReadStream(), Encoding.UTF8, detectEncodingFromByteOrderMarks: true))
        {
            csv = await reader.ReadToEndAsync();
        }

        return await PageAsync(csv, file.FileName);
    }

    /// <summary>Adds the games ticked, for the page script.</summary>
    /// <param name="form">The games.</param>
    /// <returns>How many were added and updated, or the errors.</returns>
    [HttpPost("/playnite/import")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(8 * 1024 * 1024)]
    public async Task<IActionResult> Import([FromBody] ImportForm form) =>
        Json(await CallAsync(() => Api.PlayniteImportAsync(new PlayniteImportRequest { UserId = UserId, Games = form.Requests() })));

    private async Task<IActionResult> PageAsync(string? csv, string? fileName, string? problem = null)
    {
        var page = Shell(new PlaynitePageModel { FileName = fileName, ReadProblem = problem }, AppSection.Settings);
        try
        {
            page.Preview = await Api.PlaynitePreviewAsync(UserId, problem is null ? csv : null);
            page.ReadProblem ??= csv is null ? null : page.Preview.Problem;
        }
        catch (LudexApiException exception) when (exception.StatusCode == HttpStatusCode.BadRequest)
        {
            page.ReadProblem = string.Join(" ", exception.ToValidationErrors().Select(static e => e.Message).Distinct());
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or LudexApiException)
        {
            page.Unavailable = exception is TaskCanceledException
                ? "Matching the games took too long. Try again (games already matched go faster), or export fewer games."
                : ApiUnavailableMessage;
            return View("~/Views/Shared/Unavailable.cshtml", page);
        }

        return View("~/Views/Playnite/Index.cshtml", page);
    }
}

/// <summary>
/// The PlayStation connection: connect with an NPSSO, review the games played, add the games ticked, sync.
/// </summary>
/// <param name="apiClient">The API client.</param>
public sealed class PsnController(LudexClient apiClient) : WebsiteBaseController(apiClient)
{
    /// <summary>The PlayStation page; with load, the games to review.</summary>
    /// <param name="load">Whether to read the games played.</param>
    /// <returns>The view.</returns>
    [HttpGet("/psn")]
    public async Task<IActionResult> Index(bool load = false)
    {
        var page = Shell(new PsnPageModel(), AppSection.Settings);
        try
        {
            if (load)
            {
                page.Preview = await Api.PsnPreviewAsync(UserId);
                page.Status = page.Preview.Status;
            }
            else
            {
                page.Status = await Api.PsnStatusAsync(UserId);
            }
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or LudexApiException)
        {
            page.Unavailable = exception is TaskCanceledException
                ? "Reading your PlayStation games took too long. Try again (games already matched go faster)."
                : ApiUnavailableMessage;
            return View("~/Views/Shared/Unavailable.cshtml", page);
        }

        return View("~/Views/Psn/Index.cshtml", page);
    }

    /// <summary>Connects PlayStation, then reads the games.</summary>
    /// <param name="npsso">The NPSSO pasted.</param>
    /// <returns>The page, with the games.</returns>
    [HttpPost("/psn/connect")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Connect(string? npsso)
    {
        var connected = false;
        var result = await FormAsync(async () => { await Api.PsnConnectAsync(UserId, npsso); connected = true; }, "PlayStation is connected.", "/psn");
        return connected ? LocalRedirect("/psn?load=true") : result;
    }

    /// <summary>Disconnects PlayStation (the games imported stay).</summary>
    /// <returns>The page.</returns>
    [HttpPost("/psn/disconnect")]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Disconnect() => FormAsync(() => Api.PsnDisconnectAsync(UserId), "PlayStation is disconnected. The games you imported stay in your library.", "/psn");

    /// <summary>Adds the games ticked, for the page script.</summary>
    /// <param name="form">The games.</param>
    /// <returns>How many were added and updated, or the errors.</returns>
    [HttpPost("/psn/import")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(8 * 1024 * 1024)]
    public async Task<IActionResult> Import([FromBody] ImportForm form) =>
        Json(await CallAsync(() => Api.PsnImportAsync(new PsnImportRequest { UserId = UserId, Games = form.Requests() })));

    /// <summary>Syncs the library with PlayStation now.</summary>
    /// <returns>What changed, or the errors.</returns>
    [HttpPost("/psn/sync")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Sync() => Json(await CallAsync(() => Api.PsnSyncAsync(UserId)));
}

/// <summary>What the Playnite and PlayStation pages send.</summary>
public sealed class ImportForm
{
    /// <summary>Gets or sets the games ticked.</summary>
    public List<ImportFormGame> Games { get; set; } = [];

    /// <summary>The games as the API takes them.</summary>
    /// <returns>The requests.</returns>
    public List<ImportGameRequest> Requests() =>
    [
        .. Games.Select(static g => new ImportGameRequest
        {
            Key = g.Key,
            GameId = g.GameId,
            Status = g.Status,
            Hours = g.Hours,
            LastPlayedAt = g.LastPlayedAt,
            Platform = g.Platform,
            Store = g.Store,
            Format = g.Format,
            TrophiesEarned = g.TrophiesEarned,
            TrophiesTotal = g.TrophiesTotal,
        }),
    ];
}

/// <summary>One game ticked.</summary>
public sealed class ImportFormGame
{
    /// <summary>Gets or sets the game's id in the other library.</summary>
    public string? Key { get; set; }

    /// <summary>Gets or sets the game.</summary>
    public int GameId { get; set; }

    /// <summary>Gets or sets the status.</summary>
    public GameStatus Status { get; set; } = GameStatus.Played;

    /// <summary>Gets or sets the hours played.</summary>
    public decimal? Hours { get; set; }

    /// <summary>Gets or sets when it was last played.</summary>
    public DateTimeOffset? LastPlayedAt { get; set; }

    /// <summary>Gets or sets the platform.</summary>
    public string? Platform { get; set; }

    /// <summary>Gets or sets the PC store.</summary>
    public string? Store { get; set; }

    /// <summary>Gets or sets physical or digital.</summary>
    public GameFormat? Format { get; set; }

    /// <summary>Gets or sets the trophies earned.</summary>
    public int? TrophiesEarned { get; set; }

    /// <summary>Gets or sets the trophies the game has.</summary>
    public int? TrophiesTotal { get; set; }
}
