using Ludex.Library.Enums;
using Ludex.Library.Services;
using Ludex.Library.Validation;
using Ludex.WebApi.Extensions.Globalization;
using Ludex.WebApi.Features.Friends.Repositories;
using Ludex.WebApi.Features.Games.Services;
using Ludex.WebApi.Features.Library.Models.Entities;
using Ludex.WebApi.Features.Library.Repositories;
using Ludex.WebApi.Features.Library.Services;
using Ludex.WebApi.Features.Profile.Models.DTOs;
using Ludex.WebApi.Features.Profile.Models.Entities;
using Ludex.WebApi.Features.Profile.Repositories;
using Ludex.WebApi.Features.Users.Repositories;

namespace Ludex.WebApi.Features.Profile.Services;

/// <summary>
/// Profiles: the page (level, badges, showcases), editing it, the photo and "last seen".
/// </summary>
internal interface IProfileService
{
    /// <summary>A profile as an account sees it.</summary>
    /// <param name="viewerId">The signed-in account.</param>
    /// <param name="userName">Whose profile (empty: the viewer's own).</param>
    /// <returns>The profile, or not found.</returns>
    Task<ServiceApiResponse<ProfileDTO>> GetAsync(string viewerId, string? userName);

    /// <summary>Saves the bio, location and favourite game.</summary>
    /// <param name="item">The account and the values.</param>
    /// <returns>The profile, or validation problems.</returns>
    Task<ServiceApiResponse<ProfileDTO>> SaveAsync(ProfileSaveRequest item);

    /// <summary>Makes a game of the library the favourite, or clears it.</summary>
    /// <param name="item">The account and the game.</param>
    /// <returns>The favourite now (null: none chosen).</returns>
    Task<ServiceApiResponse<ProfileFavoriteRequest>> SetFavoriteAsync(ProfileFavoriteRequest item);

    /// <summary>Sets the photo, or removes it.</summary>
    /// <param name="item">The account and the image.</param>
    /// <returns>The new version (no data), or validation problems.</returns>
    Task<ServiceApiResponse<AvatarDTO>> SaveAvatarAsync(ProfileAvatarRequest item);

    /// <summary>A photo.</summary>
    /// <param name="userId">The account.</param>
    /// <returns>The photo, or not found.</returns>
    Task<ServiceApiResponse<AvatarDTO>> GetAvatarAsync(string userId);

    /// <summary>Notes that the account is using the app now.</summary>
    /// <param name="item">The account.</param>
    /// <returns>True.</returns>
    Task<ServiceApiResponse<bool>> TouchAsync(ProfileUserRequest item);
}

/// <summary>
/// Profiles: the page (level, badges, showcases), editing it, the photo and "last seen".
/// </summary>
/// <param name="profileRepository">Profiles.</param>
/// <param name="userRepository">Accounts (to find one by name).</param>
/// <param name="libraryRepository">Libraries.</param>
/// <param name="friendRepository">Friendships.</param>
/// <param name="globalizationHelper">Messages.</param>
/// <param name="timeProvider">The clock (this year's numbers).</param>
internal sealed class ProfileService(
    IProfileRepository profileRepository,
    IUserRepository userRepository,
    ILibraryRepository libraryRepository,
    IFriendRepository friendRepository,
    IGlobalizationHelper globalizationHelper,
    TimeProvider timeProvider) : IProfileService
{
    /// <inheritdoc />
    public async Task<ServiceApiResponse<ProfileDTO>> GetAsync(string viewerId, string? userName)
    {
        if (!Guid.TryParse(viewerId, out var viewer))
        {
            return await NotFoundAsync(viewerId);
        }

        var target = viewer;
        var name = (userName ?? string.Empty).Trim().TrimStart('@');
        if (name.Length > 0)
        {
            var account = name.Length <= 32 ? await userRepository.GetByNameAsync(name) : null;
            if (account is null)
            {
                return await NotFoundAsync(name);
            }

            target = account.UserUniqueID;
        }

        var page = await PageAsync(viewer, target);
        return page is null ? await NotFoundAsync(name) : ServiceApiResponse<ProfileDTO>.Success(page);
    }

    /// <inheritdoc />
    public async Task<ServiceApiResponse<ProfileDTO>> SaveAsync(ProfileSaveRequest item)
    {
        if (!Guid.TryParse(item.UserId, out var user))
        {
            return await NotFoundAsync(item.UserId);
        }

        var bio = ProfileRules.Clean(item.Bio);
        var location = ProfileRules.Clean(item.Location)?.Replace('\n', ' ');
        var errors = new List<ApiValidationError>();
        if (bio is { Length: > ProfileRules.MaxBioLength })
        {
            errors.Add(await globalizationHelper.ValidationErrorAsync("Bio", nameof(TranslationKeys.TooLong), ProfileRules.MaxBioLength));
        }

        if (location is { Length: > ProfileRules.MaxLocationLength })
        {
            errors.Add(await globalizationHelper.ValidationErrorAsync("Location", nameof(TranslationKeys.TooLong), ProfileRules.MaxLocationLength));
        }

        if (item.FavoriteGameId is { } favorite && await libraryRepository.GetAsync(user, favorite) is null)
        {
            errors.Add(await globalizationHelper.ValidationErrorAsync("FavoriteGameId", nameof(TranslationKeys.FavoriteNotInLibrary)));
        }

        if (errors.Count > 0)
        {
            return ServiceApiResponse<ProfileDTO>.ValidationProblems(await globalizationHelper.TranslateAsync(nameof(TranslationKeys.ValidationProblems)))
                .AddErrorRange(errors);
        }

        await profileRepository.SaveAsync(user, bio, location, item.FavoriteGameId);
        var page = await PageAsync(user, user);
        return page is null ? await NotFoundAsync(item.UserId) : ServiceApiResponse<ProfileDTO>.Success(page);
    }

    /// <inheritdoc />
    public async Task<ServiceApiResponse<ProfileFavoriteRequest>> SetFavoriteAsync(ProfileFavoriteRequest item)
    {
        if (!Guid.TryParse(item.UserId, out var user))
        {
            return ServiceApiResponse<ProfileFavoriteRequest>.NotFound(
                await globalizationHelper.TranslateAsync(nameof(TranslationKeys.NoRecordsFound), item.UserId ?? string.Empty));
        }

        if (item.GameId is { } gameId && await libraryRepository.GetAsync(user, gameId) is null)
        {
            var error = await globalizationHelper.ValidationErrorAsync("FavoriteGameId", nameof(TranslationKeys.FavoriteNotInLibrary));
            return ServiceApiResponse<ProfileFavoriteRequest>.ValidationProblems(error.Message).AddError("FavoriteGameId", error.Message);
        }

        await profileRepository.SetFavoriteAsync(user, item.GameId);
        return ServiceApiResponse<ProfileFavoriteRequest>.Success(new ProfileFavoriteRequest { UserId = item.UserId, GameId = item.GameId });
    }

    /// <inheritdoc />
    public async Task<ServiceApiResponse<AvatarDTO>> SaveAvatarAsync(ProfileAvatarRequest item)
    {
        if (!Guid.TryParse(item.UserId, out var user))
        {
            return ServiceApiResponse<AvatarDTO>.NotFound(
                await globalizationHelper.TranslateAsync(nameof(TranslationKeys.NoRecordsFound), item.UserId ?? string.Empty));
        }

        if (item.Data is not { Length: > 0 } data)
        {
            await profileRepository.SaveAvatarAsync(user, null, null);
            return ServiceApiResponse<AvatarDTO>.Success(new AvatarDTO());
        }

        var type = ProfileRules.AvatarType(data);
        if (type is null)
        {
            var error = await globalizationHelper.ValidationErrorAsync("Avatar", nameof(TranslationKeys.AvatarInvalid), ProfileRules.MaxAvatarBytes / 1024);
            return ServiceApiResponse<AvatarDTO>.ValidationProblems(error.Message).AddError("Avatar", error.Message);
        }

        var version = await profileRepository.SaveAvatarAsync(user, data, type);
        return ServiceApiResponse<AvatarDTO>.Success(new AvatarDTO { ContentType = type, Version = version });
    }

    /// <inheritdoc />
    public async Task<ServiceApiResponse<AvatarDTO>> GetAvatarAsync(string userId)
    {
        var avatar = Guid.TryParse(userId, out var user) ? await profileRepository.GetAvatarAsync(user) : null;
        return avatar is null
            ? ServiceApiResponse<AvatarDTO>.NotFound(await globalizationHelper.TranslateAsync(nameof(TranslationKeys.NoRecordsFound), userId ?? string.Empty))
            : ServiceApiResponse<AvatarDTO>.Success(new AvatarDTO { Data = avatar.Avatar, ContentType = avatar.AvatarContentType, Version = avatar.AvatarVersion });
    }

    /// <inheritdoc />
    public async Task<ServiceApiResponse<bool>> TouchAsync(ProfileUserRequest item)
    {
        if (Guid.TryParse(item.UserId, out var user))
        {
            await profileRepository.TouchAsync(user);
        }

        return ServiceApiResponse<bool>.Success(true);
    }

    /// <summary>
    /// The favourite game: the one chosen when it is still in the library, else the best rated (then the most played).
    /// </summary>
    /// <param name="rows">The library.</param>
    /// <param name="chosen">The game chosen.</param>
    /// <returns>The game and whether it was chosen.</returns>
    internal static (LibraryRow? Row, bool Chosen) Favorite(IReadOnlyCollection<LibraryRow> rows, int? chosen)
    {
        if (chosen is { } id && rows.FirstOrDefault(r => r.GameID == id) is { } row)
        {
            return (row, true);
        }

        var best = rows
            .Where(static r => r.Status != (byte)GameStatus.Wishlist && (r.Rating is not null || (r.Hours ?? 0m) > 0m))
            .OrderByDescending(static r => r.Rating ?? 0)
            .ThenByDescending(static r => r.Hours ?? 0m)
            .FirstOrDefault();
        return (best, false);
    }

    private async Task<ProfileDTO?> PageAsync(Guid viewer, Guid target)
    {
        var profile = await profileRepository.GetAsync(target);
        if (profile is null)
        {
            return null;
        }

        var isMe = viewer == target;
        var state = isMe
            ? FriendState.None
            : (await friendRepository.ListAsync(viewer)).FirstOrDefault(f => f.UserUniqueID == target) is { } link && Enum.IsDefined((FriendState)link.State)
                ? (FriendState)link.State
                : FriendState.None;
        var canSee = isMe || state == FriendState.Friends;

        var rows = await libraryRepository.ListAsync(target);
        var friends = await profileRepository.FriendsAsync(target);
        var seen = profile.LastSeenAt is { } lastSeen && (profile.LastLoginAt is null || lastSeen > profile.LastLoginAt) ? lastSeen : profile.LastLoginAt;

        var page = new ProfileDTO
        {
            Id = profile.UserUniqueID.ToString("N"),
            UserName = profile.Username,
            AvatarVersion = profile.AvatarVersion,
            Bio = profile.Bio,
            Location = profile.Location,
            MemberSince = GameMapper.Utc(profile.CreatedAt),
            LastSeenAt = canSee && seen is { } seenAt ? GameMapper.Utc(seenAt) : null,
            IsMe = isMe,
            FriendState = state,
            CanSeeLibrary = canSee,
            Level = ProfileRules.Level(ProfileRules.Xp(rows)),
            Badges = ProfileRules.Badges(rows, friends.Count),
            Counts = LibraryRules.Counts(rows),
        };

        if (!canSee)
        {
            return page;
        }

        var (favorite, chosen) = Favorite(rows, profile.FavoriteGameID);
        var playing = rows.Where(static r => r.Status == (byte)GameStatus.Playing).OrderByDescending(static r => r.UpdatedAt).FirstOrDefault();

        page.Stats = LibraryRules.Stats(rows, timeProvider.GetUtcNow().Year);
        page.Playing = playing is null ? null : LibraryRules.Entry(playing);
        page.Favorite = favorite is null ? null : LibraryRules.Entry(favorite);
        page.FavoriteChosen = chosen;
        page.TopRated = [.. rows
            .Where(static r => r.Rating is not null && r.Status != (byte)GameStatus.Wishlist)
            .OrderByDescending(static r => r.Rating)
            .ThenByDescending(static r => r.Hours ?? 0m)
            .Take(6)
            .Select(static r => LibraryRules.Entry(r))];
        page.Recent = [.. rows
            .OrderByDescending(static r => r.RecentMinutes ?? 0)
            .ThenByDescending(static r => r.UpdatedAt)
            .Take(5)
            .Select(static r => LibraryRules.Entry(r))];
        page.RecentHours = Math.Round(rows.Sum(static r => (decimal)(r.RecentMinutes ?? 0)) / 60m, 1, MidpointRounding.AwayFromZero);
        page.Achievements = rows.Sum(static r => (int)(r.AchievementsUnlocked ?? 0));
        page.Friends = [.. friends.Select(static f => new ProfileFriendDTO
        {
            Id = f.UserUniqueID.ToString("N"),
            UserName = f.Username,
            AvatarVersion = f.AvatarVersion,
            LastSeenAt = f.LastSeenAt is { } friendSeen ? GameMapper.Utc(friendSeen) : null,
            PlayingGameId = f.PlayingGameID,
            PlayingName = f.PlayingName,
            GameCount = f.GameCount,
        })];

        if (isMe)
        {
            page.FavoriteChoices = [.. rows
                .Where(static r => r.Status != (byte)GameStatus.Wishlist)
                .OrderBy(static r => r.Name, StringComparer.OrdinalIgnoreCase)
                .Select(static r => LibraryRules.Entry(r))];
        }

        return page;
    }

    private async Task<ServiceApiResponse<ProfileDTO>> NotFoundAsync(string? identifier) =>
        ServiceApiResponse<ProfileDTO>.NotFound(await globalizationHelper.TranslateAsync(nameof(TranslationKeys.NoRecordsFound), identifier ?? string.Empty));
}
