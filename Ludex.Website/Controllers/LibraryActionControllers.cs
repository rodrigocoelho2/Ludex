using Ludex.Library.Enums;
using Ludex.Sdk;
using Ludex.Sdk.Models;
using Ludex.Website.Model;
using Microsoft.AspNetCore.Mvc;

namespace Ludex.Website.Controllers;

/// <summary>
/// Changes to many games at once (the library's selection bar) and putting games back as they were (the toasts' Undo).
/// Changes made here, and undone here, stay out of the friends' feed.
/// </summary>
/// <param name="apiClient">The API client.</param>
public sealed class LibraryActionController(LudexClient apiClient) : WebsiteBaseController(apiClient)
{
    /// <summary>The most games one call changes.</summary>
    public const int MaxGames = 500;

    /// <summary>Changes the status of games, takes them out of the library, or puts them in or out of a collection.</summary>
    /// <param name="form">The games and what to do.</param>
    /// <returns>How it went, with what Undo needs, or the errors.</returns>
    [HttpPost("/library/bulk")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Bulk([FromBody] BulkForm form)
    {
        var ids = form.GameIds.Distinct().Take(MaxGames).ToList();
        if (ids.Count == 0)
        {
            return Json(Problem("Pick at least one game."));
        }

        var complete = form.Action switch
        {
            BulkAction.Status => form.Status is not null,
            BulkAction.Remove => true,
            BulkAction.AddTo or BulkAction.TakeOut => form.CollectionId is not null,
            _ => false,
        };
        if (!complete)
        {
            return Json(Problem("Pick what to do with these games."));
        }

        return Json(await CallAsync(async () =>
        {
            var library = await Api.LibraryListAsync(UserId);

            // The copies picked (a game on two platforms is two cards); without copies, every copy of the games.
            var entryIds = form.EntryIds.Where(static e => e > 0).ToHashSet();
            var entries = library.Entries
                .Where(e => entryIds.Count > 0 ? entryIds.Contains(e.EntryId) : ids.Contains(e.Game.Id))
                .Take(MaxGames)
                .ToList();
            var result = new BulkResult();
            switch (form.Action)
            {
                case BulkAction.Status when form.Status is { } status:
                    foreach (var before in entries.Where(e => e.Status != status))
                    {
                        result.Before.Add(Snapshot(before));
                        await Api.LibraryStatusAsync(new LibraryStatusRequest { UserId = UserId, GameId = before.Game.Id, EntryId = before.EntryId, Status = status, Quiet = true });
                        result.Done++;
                    }

                    foreach (var id in entryIds.Count > 0 ? Enumerable.Empty<int>() : ids.Where(id => entries.All(e => e.Game.Id != id)))
                    {
                        result.Added.Add(id);
                        await Api.LibraryStatusAsync(new LibraryStatusRequest { UserId = UserId, GameId = id, Status = status, Quiet = true });
                        result.Done++;
                    }

                    break;
                case BulkAction.Remove:
                    foreach (var entry in entries)
                    {
                        result.Before.Add(Snapshot(entry));
                        await Api.LibraryDeleteAsync(UserId, entry.Game.Id, entry.EntryId);
                        result.Done++;
                    }

                    break;
                case BulkAction.AddTo or BulkAction.TakeOut when form.CollectionId is { } collectionId:
                    var putIn = form.Action == BulkAction.AddTo;
                    foreach (var entry in entries.DistinctBy(static e => e.Game.Id).Where(e => e.CollectionIds.Contains(collectionId) != putIn))
                    {
                        await Api.CollectionsGameAsync(new CollectionGameRequest { UserId = UserId, CollectionId = collectionId, GameId = entry.Game.Id, In = putIn });
                        result.Changed.Add(entry.Game.Id);
                        result.Done++;
                    }

                    break;
            }

            return result;
        }));
    }

    /// <summary>Puts games back as they were: their status, rating, hours, copy, notes and collections.</summary>
    /// <param name="form">The games as they were (and the ones to take out again, because the change added them).</param>
    /// <returns>How many came back, or the errors.</returns>
    [HttpPost("/library/restore")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Restore([FromBody] RestoreForm form) => Json(await CallAsync(async () =>
    {
        var done = 0;
        foreach (var entry in form.Entries.Take(MaxGames))
        {
            await Api.LibrarySaveAsync(new LibrarySaveRequest
            {
                UserId = UserId,
                GameId = entry.GameId,
                EntryId = entry.EntryId,
                Status = entry.Status,
                Rating = entry.Rating,
                Hours = entry.Hours,
                Platform = entry.Platform,
                Store = entry.Store,
                Format = entry.Format,
                Notes = entry.Notes,
                Quiet = true,
            });
            foreach (var collectionId in entry.CollectionIds)
            {
                await Api.CollectionsGameAsync(new CollectionGameRequest { UserId = UserId, CollectionId = collectionId, GameId = entry.GameId, In = true });
            }

            done++;
        }

        foreach (var gameId in form.Remove.Distinct().Take(MaxGames))
        {
            await Api.LibraryDeleteAsync(UserId, gameId);
            done++;
        }

        return done;
    }));

    /// <summary>Empties the library (Settings), to import everything again.</summary>
    /// <param name="confirm">The user name typed to confirm.</param>
    /// <returns>Back to Settings, with how many games went.</returns>
    [HttpPost("/library/clear")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Clear(string? confirm)
    {
        if (!string.Equals(confirm?.Trim(), User.Identity?.Name, StringComparison.OrdinalIgnoreCase))
        {
            TempData["problem"] = "The library wasn't cleared: type your user name exactly to confirm.";
            return LocalRedirect("/settings#clear");
        }

        var removed = 0;
        var result = await FormAsync(async () => removed = (await Api.LibraryClearAsync(UserId, confirm)).Removed, null, "/settings");
        if (removed > 0 || TempData.Peek("problem") is null)
        {
            TempData["notice"] = $"Your library is empty ({removed} {(removed == 1 ? "game" : "games")} taken out). Import from Steam, PlayStation or Playnite again.";
        }

        return result;
    }

    private static EntrySnapshot Snapshot(LibraryEntryDTO entry) => new()
    {
        GameId = entry.Game.Id,
        EntryId = entry.EntryId,
        Status = entry.Status,
        Rating = entry.Rating,
        Hours = entry.Hours,
        Platform = entry.Platform,
        Store = entry.Store,
        Format = entry.Format,
        Notes = entry.Notes,
        CollectionIds = entry.CollectionIds,
    };

    private static AjaxDataResult<BulkResult> Problem(string message) =>
        new() { IsValid = false, ValidationErrors = [new Ludex.Library.Validation.ApiValidationError(string.Empty, message)] };
}

/// <summary>What the selection bar does.</summary>
public enum BulkAction
{
    /// <summary>Sets the status.</summary>
    Status,

    /// <summary>Takes the games out of the library.</summary>
    Remove,

    /// <summary>Puts them in a collection.</summary>
    AddTo,

    /// <summary>Takes them out of a collection.</summary>
    TakeOut,
}

/// <summary>Many games and what to do with them.</summary>
public sealed class BulkForm
{
    /// <summary>Gets or sets the games.</summary>
    public List<int> GameIds { get; set; } = [];

    /// <summary>Gets or sets the copies picked (a game on two platforms is two cards); empty: every copy of the games.</summary>
    public List<long> EntryIds { get; set; } = [];

    /// <summary>Gets or sets what to do.</summary>
    public BulkAction Action { get; set; }

    /// <summary>Gets or sets the status (for <see cref="BulkAction.Status"/>).</summary>
    public GameStatus? Status { get; set; }

    /// <summary>Gets or sets the collection (for <see cref="BulkAction.AddTo"/> and <see cref="BulkAction.TakeOut"/>).</summary>
    public int? CollectionId { get; set; }
}

/// <summary>How a change to many games went, with what Undo needs.</summary>
public sealed class BulkResult
{
    /// <summary>Gets or sets how many games changed.</summary>
    public int Done { get; set; }

    /// <summary>Gets the games as they were (status changes and removals).</summary>
    public List<EntrySnapshot> Before { get; } = [];

    /// <summary>Gets the games the change added to the library (Undo takes them out again).</summary>
    public List<int> Added { get; } = [];

    /// <summary>Gets the games put in or taken out of the collection.</summary>
    public List<int> Changed { get; } = [];
}

/// <summary>A game of the library as it was.</summary>
public sealed class EntrySnapshot
{
    /// <summary>Gets or sets the game.</summary>
    public int GameId { get; set; }

    /// <summary>Gets or sets the copy (taken out meanwhile: it comes back as a new one).</summary>
    public long? EntryId { get; set; }

    /// <summary>Gets or sets the status.</summary>
    public GameStatus Status { get; set; }

    /// <summary>Gets or sets the rating.</summary>
    public int? Rating { get; set; }

    /// <summary>Gets or sets the hours.</summary>
    public decimal? Hours { get; set; }

    /// <summary>Gets or sets the platform.</summary>
    public string? Platform { get; set; }

    /// <summary>Gets or sets the PC store.</summary>
    public string? Store { get; set; }

    /// <summary>Gets or sets physical or digital.</summary>
    public GameFormat? Format { get; set; }

    /// <summary>Gets or sets the notes.</summary>
    public string? Notes { get; set; }

    /// <summary>Gets or sets the collections it was in.</summary>
    public List<int> CollectionIds { get; set; } = [];
}

/// <summary>Games to put back as they were.</summary>
public sealed class RestoreForm
{
    /// <summary>Gets or sets the games as they were.</summary>
    public List<EntrySnapshot> Entries { get; set; } = [];

    /// <summary>Gets or sets games to take out again (the undone change added them).</summary>
    public List<int> Remove { get; set; } = [];
}
