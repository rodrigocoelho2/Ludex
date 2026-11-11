using Ludex.Library.Enums;
using Ludex.Library.Services;
using Ludex.WebApi.Extensions.Globalization;
using Ludex.WebApi.Features.Friends.Models.DTOs;
using Ludex.WebApi.Features.Friends.Models.Entities;
using Ludex.WebApi.Features.Friends.Repositories;
using Ludex.WebApi.Features.Games.Models.DTOs;
using Ludex.WebApi.Features.Games.Services;
using Ludex.WebApi.Features.Users.Repositories;

namespace Ludex.WebApi.Features.Friends.Services;

/// <summary>
/// Friends: the list, requests, and what they play.
/// </summary>
internal interface IFriendService
{
    /// <summary>The friends page.</summary>
    /// <param name="userId">The account.</param>
    /// <returns>Friends, requests, their activity and their favourite games.</returns>
    Task<ServiceApiResponse<FriendsDTO>> ListAsync(string userId);

    /// <summary>Asks an account to be friends, by user name (accepts at once when it had asked first).</summary>
    /// <param name="item">The account asking and the user name.</param>
    /// <returns>The friends page, or a problem (unknown name, oneself).</returns>
    Task<ServiceApiResponse<FriendsDTO>> RequestAsync(FriendRequestRequest item);

    /// <summary>Accepts a request.</summary>
    /// <param name="item">The account and the one that asked.</param>
    /// <returns>The friends page.</returns>
    Task<ServiceApiResponse<FriendsDTO>> AcceptAsync(FriendActionRequest item);

    /// <summary>Declines or cancels a request, or ends a friendship.</summary>
    /// <param name="item">The account and the other one.</param>
    /// <returns>The friends page.</returns>
    Task<ServiceApiResponse<FriendsDTO>> RemoveAsync(FriendActionRequest item);
}

/// <summary>
/// Friends: the list, requests, and what they play.
/// </summary>
/// <param name="friendRepository">Friendships.</param>
/// <param name="userRepository">Accounts (to find one by name).</param>
/// <param name="globalizationHelper">Messages.</param>
internal sealed class FriendService(
    IFriendRepository friendRepository,
    IUserRepository userRepository,
    IGlobalizationHelper globalizationHelper) : IFriendService
{
    /// <inheritdoc />
    public async Task<ServiceApiResponse<FriendsDTO>> ListAsync(string userId) =>
        Guid.TryParse(userId, out var user)
            ? ServiceApiResponse<FriendsDTO>.Success(await PageAsync(user))
            : await NotFoundAsync(userId);

    /// <inheritdoc />
    public async Task<ServiceApiResponse<FriendsDTO>> RequestAsync(FriendRequestRequest item)
    {
        if (!Guid.TryParse(item.UserId, out var user))
        {
            return await NotFoundAsync(item.UserId);
        }

        var name = (item.UserName ?? string.Empty).Trim().TrimStart('@');
        var other = name.Length is > 0 and <= 32 ? await userRepository.GetByNameAsync(name) : null;
        if (other is null)
        {
            return await ProblemAsync(nameof(TranslationKeys.FriendUnknown), name);
        }

        if (other.UserUniqueID == user)
        {
            return await ProblemAsync(nameof(TranslationKeys.FriendSelf));
        }

        await friendRepository.RequestAsync(user, other.UserUniqueID);
        return ServiceApiResponse<FriendsDTO>.Success(await PageAsync(user));
    }

    /// <inheritdoc />
    public async Task<ServiceApiResponse<FriendsDTO>> AcceptAsync(FriendActionRequest item)
    {
        if (!Guid.TryParse(item.UserId, out var user) || !Guid.TryParse(item.FriendId, out var friend))
        {
            return await NotFoundAsync(item.FriendId);
        }

        await friendRepository.AcceptAsync(user, friend);
        return ServiceApiResponse<FriendsDTO>.Success(await PageAsync(user));
    }

    /// <inheritdoc />
    public async Task<ServiceApiResponse<FriendsDTO>> RemoveAsync(FriendActionRequest item)
    {
        if (!Guid.TryParse(item.UserId, out var user) || !Guid.TryParse(item.FriendId, out var friend))
        {
            return await NotFoundAsync(item.FriendId);
        }

        await friendRepository.RemoveAsync(user, friend);
        return ServiceApiResponse<FriendsDTO>.Success(await PageAsync(user));
    }

    /// <summary>
    /// The games most friends have that the account doesn't, with who has them.
    /// </summary>
    /// <param name="friendRepository">Friendships.</param>
    /// <param name="user">The account.</param>
    /// <param name="take">At most this many.</param>
    /// <returns>The games.</returns>
    internal static async Task<List<PopularGameDTO>> PopularAsync(IFriendRepository friendRepository, Guid user, int take)
    {
        var rows = await friendRepository.PopularAsync(user, take);
        var names = GameMapper.NamesByGame((await friendRepository.OwnershipAsync(user, rows.Select(static r => r.GameID).ToList()))
            .Select(static r => (r.GameID, r.Username)));
        return [.. rows.Select(row => new PopularGameDTO
        {
            Game = GameMapper.Summary(row, null, names.GetValueOrDefault(row.GameID)),
            AverageRating = row.AverageRating is { } average ? Math.Round(average, 1) : null,
        })];
    }

    /// <summary>
    /// A feed line as the Website shows it.
    /// </summary>
    /// <param name="row">The line.</param>
    /// <returns>The line.</returns>
    internal static ActivityDTO Activity(ActivityRow row) => new()
    {
        Id = row.ActivityID,
        UserId = row.UserUniqueID.ToString("N"),
        UserName = row.Username,
        Kind = Enum.IsDefined((ActivityKind)row.Kind) ? (ActivityKind)row.Kind : ActivityKind.Added,
        Rating = row.Rating,
        At = GameMapper.Utc(row.At),
        AvatarVersion = row.AvatarVersion,
        Game = new GameSummaryDTO
        {
            Id = row.GameID,
            Name = row.Name,
            CoverImageId = row.CoverImageID,
            Tint = GameTint.For(row.GameID),
            MyStatus = GameMapper.Status(row.MyStatus),
        },
    };

    /// <summary>
    /// A link as the Website shows it.
    /// </summary>
    /// <param name="row">The link.</param>
    /// <returns>The friend.</returns>
    internal static FriendDTO Friend(FriendRow row) => new()
    {
        Id = row.UserUniqueID.ToString("N"),
        UserName = row.Username,
        State = Enum.IsDefined((FriendState)row.State) ? (FriendState)row.State : FriendState.None,
        Since = GameMapper.Utc(row.Since),
        GameCount = row.GameCount,
        AvatarVersion = row.AvatarVersion,
    };

    private async Task<FriendsDTO> PageAsync(Guid user)
    {
        var links = (await friendRepository.ListAsync(user)).Select(Friend).ToList();
        return new FriendsDTO
        {
            Friends = [.. links.Where(static f => f.State == FriendState.Friends)],
            Incoming = [.. links.Where(static f => f.State == FriendState.RequestReceived)],
            Outgoing = [.. links.Where(static f => f.State == FriendState.RequestSent)],
            Feed = [.. (await friendRepository.FeedAsync(user, 30)).Select(Activity)],
            Popular = await PopularAsync(friendRepository, user, 8),
        };
    }

    private async Task<ServiceApiResponse<FriendsDTO>> ProblemAsync(string key, params object[] arguments)
    {
        var message = await globalizationHelper.TranslateAsync(key, arguments);
        return ServiceApiResponse<FriendsDTO>.ValidationProblems(message).AddError("Friend", message);
    }

    private async Task<ServiceApiResponse<FriendsDTO>> NotFoundAsync(string? identifier) =>
        ServiceApiResponse<FriendsDTO>.NotFound(await globalizationHelper.TranslateAsync(nameof(TranslationKeys.NoRecordsFound), identifier ?? string.Empty));
}
