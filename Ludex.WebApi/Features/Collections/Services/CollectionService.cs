using Ludex.Library.Enums;
using Ludex.Library.Services;
using Ludex.WebApi.Extensions.Globalization;
using Ludex.WebApi.Features.Collections.Repositories;
using Ludex.WebApi.Features.Library.Models.DTOs;

namespace Ludex.WebApi.Features.Collections.Services;

/// <summary>
/// An account's collections: list, create, rename, delete, and which games are in them.
/// </summary>
internal interface ICollectionService
{
    /// <summary>The collections.</summary>
    /// <param name="userId">The account.</param>
    /// <returns>The collections, by name.</returns>
    Task<ServiceApiResponse<List<CollectionDTO>>> ListAsync(string userId);

    /// <summary>Creates or renames a collection.</summary>
    /// <param name="item">The account, the collection and the name.</param>
    /// <returns>The collections, or a problem (name taken, empty).</returns>
    Task<ServiceApiResponse<List<CollectionDTO>>> SaveAsync(CollectionSaveRequest item);

    /// <summary>Deletes a collection.</summary>
    /// <param name="item">The account and the collection.</param>
    /// <returns>The collections.</returns>
    Task<ServiceApiResponse<List<CollectionDTO>>> DeleteAsync(CollectionDeleteRequest item);

    /// <summary>Puts a game in a collection or takes it out.</summary>
    /// <param name="item">The account, the collection and the game.</param>
    /// <returns>The collections.</returns>
    Task<ServiceApiResponse<List<CollectionDTO>>> SetGameAsync(CollectionGameRequest item);
}

/// <summary>
/// An account's collections.
/// </summary>
/// <param name="collectionRepository">Collections.</param>
/// <param name="globalizationHelper">Messages.</param>
internal sealed class CollectionService(ICollectionRepository collectionRepository, IGlobalizationHelper globalizationHelper) : ICollectionService
{
    /// <summary>Longest collection name.</summary>
    internal const int MaxNameLength = 60;

    /// <inheritdoc />
    public async Task<ServiceApiResponse<List<CollectionDTO>>> ListAsync(string userId) =>
        Guid.TryParse(userId, out var user)
            ? ServiceApiResponse<List<CollectionDTO>>.Success(ToDtos(await collectionRepository.ListAsync(user)))
            : await NotFoundAsync(userId);

    /// <inheritdoc />
    public async Task<ServiceApiResponse<List<CollectionDTO>>> SaveAsync(CollectionSaveRequest item)
    {
        if (!Guid.TryParse(item.UserId, out var user))
        {
            return await NotFoundAsync(item.UserId);
        }

        var name = (item.Name ?? string.Empty).Trim();
        if (name.Length == 0 || name.Length > MaxNameLength)
        {
            var error = name.Length == 0
                ? await globalizationHelper.ValidationErrorAsync("Name", nameof(TranslationKeys.RequiredField))
                : await globalizationHelper.ValidationErrorAsync("Name", nameof(TranslationKeys.TooLong), MaxNameLength);
            return ServiceApiResponse<List<CollectionDTO>>.ValidationProblems(error.Message).AddError("Name", error.Message);
        }

        var id = await collectionRepository.SaveAsync(user, item.Id, name);
        if (id == -1)
        {
            var message = await globalizationHelper.TranslateAsync(nameof(TranslationKeys.CollectionNameTaken), name);
            return ServiceApiResponse<List<CollectionDTO>>.ValidationProblems(message).AddError("Name", message);
        }

        return id == 0 ? await NotFoundAsync(item.Id?.ToString(System.Globalization.CultureInfo.InvariantCulture)) : await ListAsync(item.UserId!);
    }

    /// <inheritdoc />
    public async Task<ServiceApiResponse<List<CollectionDTO>>> DeleteAsync(CollectionDeleteRequest item)
    {
        if (!Guid.TryParse(item.UserId, out var user))
        {
            return await NotFoundAsync(item.UserId);
        }

        await collectionRepository.DeleteAsync(user, item.Id);
        return await ListAsync(item.UserId!);
    }

    /// <inheritdoc />
    public async Task<ServiceApiResponse<List<CollectionDTO>>> SetGameAsync(CollectionGameRequest item)
    {
        if (!Guid.TryParse(item.UserId, out var user))
        {
            return await NotFoundAsync(item.UserId);
        }

        await collectionRepository.SetGameAsync(user, item.CollectionId, item.GameId, item.In);
        return await ListAsync(item.UserId!);
    }

    /// <summary>
    /// The collections as the Website shows them.
    /// </summary>
    /// <param name="rows">One row per collection and game.</param>
    /// <returns>The collections, by name, each with its games.</returns>
    internal static List<CollectionDTO> ToDtos(IEnumerable<CollectionRow> rows) =>
        [.. rows.GroupBy(static r => (r.CollectionID, r.Name))
            .Select(static g => new CollectionDTO
            {
                Id = g.Key.CollectionID,
                Name = g.Key.Name,
                GameIds = [.. g.Select(static r => r.GameID).OfType<int>().Distinct()],
            })
            .OrderBy(static c => c.Name, StringComparer.CurrentCultureIgnoreCase)];

    /// <summary>
    /// The collections each game is in.
    /// </summary>
    /// <param name="collections">The collections.</param>
    /// <returns>Collection ids by game.</returns>
    internal static Dictionary<int, List<int>> ByGame(IEnumerable<CollectionDTO> collections) =>
        collections.SelectMany(static c => c.GameIds.Select(g => (Game: g, c.Id)))
            .GroupBy(static x => x.Game)
            .ToDictionary(static g => g.Key, static g => g.Select(static x => x.Id).ToList());

    private async Task<ServiceApiResponse<List<CollectionDTO>>> NotFoundAsync(string? identifier) =>
        ServiceApiResponse<List<CollectionDTO>>.NotFound(await globalizationHelper.TranslateAsync(nameof(TranslationKeys.NoRecordsFound), identifier ?? string.Empty));
}
