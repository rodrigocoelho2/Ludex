using Ludex.Library.Enums;
using Ludex.Library.Services;
using Ludex.Library.Validation;
using Ludex.WebApi.Extensions.Globalization;
using Ludex.WebApi.Features.Catalog;
using Ludex.WebApi.Features.Games.Services;
using Ludex.WebApi.Features.Lists.Models.DTOs;
using Ludex.WebApi.Features.Lists.Models.Entities;
using Ludex.WebApi.Features.Lists.Repositories;
using Ludex.WebApi.Features.Users.Repositories;

namespace Ludex.WebApi.Features.Lists.Services;

/// <summary>
/// Lists of games: browse, read, make, change, like.
/// </summary>
internal interface IListService
{
    /// <summary>The lists page: one account's lists (the reader's when no name), the popular ones and the newest.</summary>
    /// <param name="viewerId">The reader.</param>
    /// <param name="owner">An account's user name (null: the reader's own).</param>
    /// <returns>The lists.</returns>
    Task<ServiceApiResponse<ListBrowseDTO>> BrowseAsync(string viewerId, string? owner);

    /// <summary>Lists holding a game, or the most liked, for a shelf.</summary>
    /// <param name="viewerId">The reader.</param>
    /// <param name="gameId">Only lists with this game (null: any).</param>
    /// <param name="take">How many.</param>
    /// <returns>The lists, most liked first.</returns>
    Task<List<ListSummaryDTO>> PopularAsync(Guid viewerId, int? gameId, int take);

    /// <summary>A list's own page.</summary>
    /// <param name="viewerId">The reader.</param>
    /// <param name="listId">The list.</param>
    /// <returns>The list, or not found (also when the reader may not see it).</returns>
    Task<ServiceApiResponse<ListDetailDTO>> GetAsync(string viewerId, int listId);

    /// <summary>Makes a list or changes one.</summary>
    /// <param name="item">The list.</param>
    /// <returns>The list now.</returns>
    Task<ServiceApiResponse<ListDetailDTO>> SaveAsync(ListSaveRequest item);

    /// <summary>Deletes a list.</summary>
    /// <param name="item">The account and the list.</param>
    /// <returns>The list id.</returns>
    Task<ServiceApiResponse<int>> DeleteAsync(ListDeleteRequest item);

    /// <summary>Puts a game in a list or takes it out.</summary>
    /// <param name="item">The account, the list and the game.</param>
    /// <returns>The account's lists and whether the game is in each.</returns>
    Task<ServiceApiResponse<List<MyListDTO>>> SetItemAsync(ListItemRequest item);

    /// <summary>Moves a game of a list.</summary>
    /// <param name="item">The account, the list, the game and its new place.</param>
    /// <returns>The list now.</returns>
    Task<ServiceApiResponse<ListDetailDTO>> MoveAsync(ListMoveRequest item);

    /// <summary>Likes a list or takes the like back.</summary>
    /// <param name="item">The account and the list.</param>
    /// <returns>The likes now.</returns>
    Task<ServiceApiResponse<ListLikeDTO>> LikeAsync(ListLikeRequest item);

    /// <summary>The account's lists and whether a game is in each.</summary>
    /// <param name="userId">The account.</param>
    /// <param name="gameId">The game.</param>
    /// <returns>The lists.</returns>
    Task<ServiceApiResponse<List<MyListDTO>>> ForGameAsync(string userId, int gameId);
}

/// <summary>
/// Lists of games.
/// </summary>
/// <param name="listRepository">Lists.</param>
/// <param name="userRepository">Accounts (by name).</param>
/// <param name="gameStore">Games, from the database or the catalog.</param>
/// <param name="globalizationHelper">Messages.</param>
internal sealed class ListService(
    IListRepository listRepository,
    IUserRepository userRepository,
    GameStore gameStore,
    IGlobalizationHelper globalizationHelper) : IListService
{
    /// <summary>How many days "popular" looks back.</summary>
    internal const int PopularDays = 7;

    /// <inheritdoc />
    public async Task<ServiceApiResponse<ListBrowseDTO>> BrowseAsync(string viewerId, string? owner)
    {
        if (!Guid.TryParse(viewerId, out var viewer))
        {
            return await NotFoundAsync<ListBrowseDTO>(viewerId);
        }

        var ownerId = viewer;
        var name = (owner ?? string.Empty).Trim().TrimStart('@');
        if (name.Length > 0)
        {
            var account = name.Length <= 32 ? await userRepository.GetByNameAsync(name) : null;
            if (account is null)
            {
                return await NotFoundAsync<ListBrowseDTO>(name);
            }

            ownerId = account.UserUniqueID;
        }

        var mine = await listRepository.BrowseAsync(viewer, ownerId, null, 2, PopularDays, 100);
        var popular = await listRepository.BrowseAsync(viewer, null, null, 1, PopularDays, 12);
        var recent = await listRepository.BrowseAsync(viewer, null, null, 2, PopularDays, 12);
        var covers = await listRepository.CoversAsync([.. mine.Concat(popular).Concat(recent).Select(static l => l.ListID).Distinct()], ListRules.CoverCount);
        return ServiceApiResponse<ListBrowseDTO>.Success(new ListBrowseDTO
        {
            Mine = ListRules.Summaries(mine, covers),
            Popular = ListRules.Summaries(popular, covers),
            Recent = ListRules.Summaries(recent, covers),
        });
    }

    /// <inheritdoc />
    public async Task<List<ListSummaryDTO>> PopularAsync(Guid viewerId, int? gameId, int take)
    {
        var rows = await listRepository.BrowseAsync(viewerId, null, gameId, 1, PopularDays, take);
        return ListRules.Summaries(rows, await listRepository.CoversAsync([.. rows.Select(static l => l.ListID)], ListRules.CoverCount));
    }

    /// <inheritdoc />
    public async Task<ServiceApiResponse<ListDetailDTO>> GetAsync(string viewerId, int listId)
    {
        if (!Guid.TryParse(viewerId, out var viewer))
        {
            return await NotFoundAsync<ListDetailDTO>(viewerId);
        }

        var detail = await DetailAsync(viewer, listId);
        return detail is null ? await ListNotFoundAsync<ListDetailDTO>() : ServiceApiResponse<ListDetailDTO>.Success(detail);
    }

    /// <inheritdoc />
    public async Task<ServiceApiResponse<ListDetailDTO>> SaveAsync(ListSaveRequest item)
    {
        if (!Guid.TryParse(item.UserId, out var user))
        {
            return await NotFoundAsync<ListDetailDTO>(item.UserId);
        }

        var name = (item.Name ?? string.Empty).Replace('\n', ' ').Trim();
        var description = Reviews.Services.ReviewRules.Clean(item.Description);
        var errors = new List<ApiValidationError>();
        if (name.Length == 0)
        {
            errors.Add(await globalizationHelper.ValidationErrorAsync("Name", nameof(TranslationKeys.RequiredField)));
        }
        else if (name.Length > ListRules.MaxNameLength)
        {
            errors.Add(await globalizationHelper.ValidationErrorAsync("Name", nameof(TranslationKeys.TooLong), ListRules.MaxNameLength));
        }

        if (description is { Length: > ListRules.MaxDescriptionLength })
        {
            errors.Add(await globalizationHelper.ValidationErrorAsync("Description", nameof(TranslationKeys.TooLong), ListRules.MaxDescriptionLength));
        }

        if (errors.Count > 0)
        {
            return ServiceApiResponse<ListDetailDTO>.ValidationProblems(await globalizationHelper.TranslateAsync(nameof(TranslationKeys.ValidationProblems)))
                .AddErrorRange(errors);
        }

        var visibility = Enum.IsDefined(item.Visibility) ? item.Visibility : ListVisibility.Public;
        var id = await listRepository.SaveAsync(user, item.Id, name, description, item.Ranked, (byte)visibility);
        if (id == 0)
        {
            return await ListNotFoundAsync<ListDetailDTO>();
        }

        if (item.FirstGameId is { } first)
        {
            try
            {
                if (await EnsureGameAsync(first))
                {
                    await listRepository.SetItemAsync(user, id, first, true, null);
                }
            }
            catch (CatalogUnavailableException)
            {
                // The list is made; the game can be added from its page once the catalog answers.
            }
        }

        var detail = await DetailAsync(user, id);
        return detail is null ? await ListNotFoundAsync<ListDetailDTO>() : ServiceApiResponse<ListDetailDTO>.Success(detail);
    }

    /// <inheritdoc />
    public async Task<ServiceApiResponse<int>> DeleteAsync(ListDeleteRequest item)
    {
        if (!Guid.TryParse(item.UserId, out var user))
        {
            return await NotFoundAsync<int>(item.UserId);
        }

        await listRepository.DeleteAsync(user, item.Id);
        return ServiceApiResponse<int>.Success(item.Id);
    }

    /// <inheritdoc />
    public async Task<ServiceApiResponse<List<MyListDTO>>> SetItemAsync(ListItemRequest item)
    {
        if (!Guid.TryParse(item.UserId, out var user))
        {
            return await NotFoundAsync<List<MyListDTO>>(item.UserId);
        }

        var note = string.IsNullOrWhiteSpace(item.Note) ? null : item.Note.Replace('\n', ' ').Trim();
        if (note is { Length: > ListRules.MaxNoteLength })
        {
            var error = await globalizationHelper.ValidationErrorAsync("Note", nameof(TranslationKeys.TooLong), ListRules.MaxNoteLength);
            return ServiceApiResponse<List<MyListDTO>>.ValidationProblems(error.Message).AddError("Note", error.Message);
        }

        if (item.In)
        {
            if (await listRepository.GetAsync(user, item.ListId) is not { IsMine: true } list)
            {
                return await ListNotFoundAsync<List<MyListDTO>>();
            }

            if (list.GameCount >= ListRules.MaxGames)
            {
                var error = await globalizationHelper.ValidationErrorAsync("GameId", nameof(TranslationKeys.OutOfRange), 1, ListRules.MaxGames);
                return ServiceApiResponse<List<MyListDTO>>.ValidationProblems(error.Message).AddError("GameId", error.Message);
            }

            try
            {
                if (!await EnsureGameAsync(item.GameId))
                {
                    return ServiceApiResponse<List<MyListDTO>>.NotFound(await globalizationHelper.TranslateAsync(nameof(TranslationKeys.GameNotFound)));
                }
            }
            catch (CatalogUnavailableException exception)
            {
                var message = await globalizationHelper.TranslateAsync(nameof(TranslationKeys.CatalogUnreachable), exception.Message);
                return ServiceApiResponse<List<MyListDTO>>.ValidationProblems(message).AddError("GameId", message);
            }
        }

        if (!await listRepository.SetItemAsync(user, item.ListId, item.GameId, item.In, note))
        {
            return await ListNotFoundAsync<List<MyListDTO>>();
        }

        return ServiceApiResponse<List<MyListDTO>>.Success(Mine(await listRepository.ForGameAsync(user, item.GameId)));
    }

    /// <inheritdoc />
    public async Task<ServiceApiResponse<ListDetailDTO>> MoveAsync(ListMoveRequest item)
    {
        if (!Guid.TryParse(item.UserId, out var user))
        {
            return await NotFoundAsync<ListDetailDTO>(item.UserId);
        }

        await listRepository.MoveAsync(user, item.ListId, item.GameId, item.Position);
        var detail = await DetailAsync(user, item.ListId);
        return detail is null ? await ListNotFoundAsync<ListDetailDTO>() : ServiceApiResponse<ListDetailDTO>.Success(detail);
    }

    /// <inheritdoc />
    public async Task<ServiceApiResponse<ListLikeDTO>> LikeAsync(ListLikeRequest item)
    {
        if (!Guid.TryParse(item.UserId, out var user))
        {
            return await NotFoundAsync<ListLikeDTO>(item.UserId);
        }

        var likes = await listRepository.LikeAsync(user, item.ListId, item.Like);
        var list = await listRepository.GetAsync(user, item.ListId);
        return list is null
            ? await ListNotFoundAsync<ListLikeDTO>()
            : ServiceApiResponse<ListLikeDTO>.Success(new ListLikeDTO { ListId = item.ListId, Likes = likes, Liked = list.LikedByMe });
    }

    /// <inheritdoc />
    public async Task<ServiceApiResponse<List<MyListDTO>>> ForGameAsync(string userId, int gameId) =>
        Guid.TryParse(userId, out var user)
            ? ServiceApiResponse<List<MyListDTO>>.Success(Mine(await listRepository.ForGameAsync(user, gameId)))
            : await NotFoundAsync<List<MyListDTO>>(userId);

    /// <summary>
    /// The account's lists as the Website shows them.
    /// </summary>
    /// <param name="rows">The rows.</param>
    /// <returns>The lists.</returns>
    internal static List<MyListDTO> Mine(IEnumerable<MyListRow> rows) =>
        [.. rows.Select(static r => new MyListDTO { Id = r.ListID, Name = r.Name, Ranked = r.Ranked, Visibility = ListRules.Visibility(r.Visibility), HasGame = r.HasGame })];

    private async Task<ListDetailDTO?> DetailAsync(Guid viewer, int listId)
    {
        var row = await listRepository.GetAsync(viewer, listId);
        if (row is null)
        {
            return null;
        }

        var items = await listRepository.ItemsAsync(viewer, listId);
        var covers = items.Take(ListRules.CoverCount).Select(i => new ListCoverRow
        {
            ListID = listId,
            Position = i.Position,
            GameID = i.GameID,
            Name = i.Name,
            CoverImageID = i.CoverImageID,
            FirstReleaseDate = i.FirstReleaseDate,
            Developer = i.Developer,
            Platforms = i.Platforms,
        });
        return new ListDetailDTO { List = ListRules.Summary(row, covers), Items = [.. items.Select(ListRules.Item)] };
    }

    private async Task<bool> EnsureGameAsync(int gameId) => await gameStore.GetAsync(gameId) is not null;

    private async Task<ServiceApiResponse<T>> ListNotFoundAsync<T>() =>
        ServiceApiResponse<T>.NotFound(await globalizationHelper.TranslateAsync(nameof(TranslationKeys.ListNotFound)));

    private async Task<ServiceApiResponse<T>> NotFoundAsync<T>(string? identifier) =>
        ServiceApiResponse<T>.NotFound(await globalizationHelper.TranslateAsync(nameof(TranslationKeys.NoRecordsFound), identifier ?? string.Empty));
}
