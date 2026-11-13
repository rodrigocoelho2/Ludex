using Ludex.Library.Enums;
using Ludex.Library.Services;
using Ludex.Library.Themes;
using Ludex.Library.Validation;
using Ludex.WebApi.Extensions.Globalization;
using Ludex.WebApi.Features.Friends.Repositories;
using Ludex.WebApi.Features.Games.Services;
using Ludex.WebApi.Features.Library.Models.Entities;
using Ludex.WebApi.Features.Library.Repositories;
using Ludex.WebApi.Features.Library.Services;
using Ludex.WebApi.Features.Collections.Repositories;
using Ludex.WebApi.Features.Psn.Services;
using Ludex.WebApi.Features.Steam.Services;
using Microsoft.Extensions.Caching.Memory;
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

    /// <summary>Saves everything Customize profile sets (bio, favourite, colour, banner, frame, title, tags, showcases, who sees what).</summary>
    /// <param name="item">The account and the values.</param>
    /// <returns>The profile, or validation problems.</returns>
    Task<ServiceApiResponse<ProfileDTO>> CustomizeAsync(ProfileCustomizeRequest item);

    /// <summary>Makes a game of the library the favourite, or clears it.</summary>
    /// <param name="item">The account and the game.</param>
    /// <returns>The favourite now (null: none chosen).</returns>
    Task<ServiceApiResponse<ProfileFavoriteRequest>> SetFavoriteAsync(ProfileFavoriteRequest item);

    /// <summary>Sets the colour theme (null or the default: back to the default).</summary>
    /// <param name="item">The account and the theme.</param>
    /// <returns>The theme now.</returns>
    Task<ServiceApiResponse<ProfileAccentRequest>> SetAccentAsync(ProfileAccentRequest item);

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
/// <param name="collectionRepository">Collections (the pinned one).</param>
/// <param name="steamService">Steam achievements (the rarest).</param>
/// <param name="psnService">PlayStation trophies (the rarest).</param>
/// <param name="cache">The rarest achievements, kept a few hours.</param>
/// <param name="timeProvider">The clock (this year's numbers).</param>
/// <param name="logger">Logger.</param>
internal sealed class ProfileService(
    IProfileRepository profileRepository,
    IUserRepository userRepository,
    ILibraryRepository libraryRepository,
    IFriendRepository friendRepository,
    IGlobalizationHelper globalizationHelper,
    ICollectionRepository collectionRepository,
    ISteamService steamService,
    IPsnService psnService,
    IMemoryCache cache,
    TimeProvider timeProvider,
    ILogger<ProfileService> logger) : IProfileService
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
    public async Task<ServiceApiResponse<ProfileDTO>> CustomizeAsync(ProfileCustomizeRequest item)
    {
        if (!Guid.TryParse(item.UserId, out var user))
        {
            return await NotFoundAsync(item.UserId);
        }

        var bio = ProfileRules.Clean(item.Bio);
        var location = ProfileRules.Clean(item.Location)?.Replace('\n', ' ');
        var note = ProfileRules.Clean(item.FavoriteNote)?.Replace('\n', ' ');
        var errors = new List<ApiValidationError>();
        if (bio is { Length: > ProfileRules.MaxBioLength })
        {
            errors.Add(await globalizationHelper.ValidationErrorAsync("Bio", nameof(TranslationKeys.TooLong), ProfileRules.MaxBioLength));
        }

        if (location is { Length: > ProfileRules.MaxLocationLength })
        {
            errors.Add(await globalizationHelper.ValidationErrorAsync("Location", nameof(TranslationKeys.TooLong), ProfileRules.MaxLocationLength));
        }

        if (note is { Length: > ProfileRules.MaxFavoriteNote })
        {
            errors.Add(await globalizationHelper.ValidationErrorAsync("FavoriteNote", nameof(TranslationKeys.TooLong), ProfileRules.MaxFavoriteNote));
        }

        var rows = await libraryRepository.ListAsync(user);
        if (item.FavoriteGameId is { } favorite && rows.All(r => r.GameID != favorite))
        {
            errors.Add(await globalizationHelper.ValidationErrorAsync("FavoriteGameId", nameof(TranslationKeys.FavoriteNotInLibrary)));
        }

        if (errors.Count > 0)
        {
            return ServiceApiResponse<ProfileDTO>.ValidationProblems(await globalizationHelper.TranslateAsync(nameof(TranslationKeys.ValidationProblems)))
                .AddErrorRange(errors);
        }

        // A title is an earned badge; a banner game is one of the library's; anything else unknown goes back to the default.
        var friends = await profileRepository.FriendsAsync(user);
        var earned = ProfileRules.Badges(rows, friends.Count).Where(static b => b.Earned).Select(static b => b.Id).ToHashSet(StringComparer.Ordinal);
        var showcases = ProfileRules.Showcases(item.Showcases);
        var defaults = showcases.SequenceEqual(ProfileRules.DefaultShowcases);
        var tags = ProfileRules.Tags(item.Tags);
        var accent = Accents.Normalize(item.Accent);
        var banner = ProfileRules.Banners.Contains(item.Banner) ? item.Banner : null;
        var frame = ProfileRules.Frames.Contains(item.Frame) ? item.Frame : null;
        await profileRepository.CustomizeAsync(user, new ProfileCustomization(
            bio,
            location,
            item.FavoriteGameId,
            note,
            accent == Accents.Default ? null : accent,
            banner is "art" ? null : banner,
            item.BannerGameId is { } bannerGame && rows.Any(r => r.GameID == bannerGame) ? bannerGame : null,
            frame is "ring" ? null : frame,
            item.Title is { } title && earned.Contains(title) ? title : null,
            tags.Count == 0 ? null : string.Join('|', tags),
            defaults ? null : string.Join(',', showcases),
            item.PinnedCollectionId,
            Enum.IsDefined(item.LibraryVisibility) && item.LibraryVisibility != LibraryVisibility.Friends ? (byte)item.LibraryVisibility : null,
            item.ShowPlaying ? null : false));
        cache.Remove($"profile:rare:{user:N}");
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
    public async Task<ServiceApiResponse<ProfileAccentRequest>> SetAccentAsync(ProfileAccentRequest item)
    {
        if (!Guid.TryParse(item.UserId, out var user))
        {
            return ServiceApiResponse<ProfileAccentRequest>.NotFound(
                await globalizationHelper.TranslateAsync(nameof(TranslationKeys.NoRecordsFound), item.UserId ?? string.Empty));
        }

        var accent = string.IsNullOrWhiteSpace(item.Accent) ? Accents.Default : item.Accent.Trim();
        if (!Accents.IsKnown(accent))
        {
            var error = await globalizationHelper.ValidationErrorAsync("Accent", nameof(TranslationKeys.AccentUnknown));
            return ServiceApiResponse<ProfileAccentRequest>.ValidationProblems(error.Message).AddError("Accent", error.Message);
        }

        await profileRepository.SetAccentAsync(user, accent == Accents.Default ? null : accent);
        return ServiceApiResponse<ProfileAccentRequest>.Success(new ProfileAccentRequest { UserId = item.UserId, Accent = accent });
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
        var visibility = profile.LibraryVisibility is { } saved && Enum.IsDefined((LibraryVisibility)saved) ? (LibraryVisibility)saved : LibraryVisibility.Friends;
        var canSee = isMe || visibility switch
        {
            LibraryVisibility.Everyone => true,
            LibraryVisibility.OnlyMe => false,
            _ => state == FriendState.Friends,
        };

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
            Accent = Accents.Normalize(profile.Accent),
            Banner = profile.Banner is { } bannerKind && ProfileRules.Banners.Contains(bannerKind) ? bannerKind : "art",
            Frame = profile.Frame is { } frameKind && ProfileRules.Frames.Contains(frameKind) ? frameKind : "ring",
            Tags = ProfileRules.Tags(profile.Tags?.Split('|')),
            Showcases = ProfileRules.Showcases(profile.Showcases?.Split(',')),
            LibraryVisibility = visibility,
            ShowPlaying = profile.ShowPlaying != false,
        };
        page.Title = profile.Title is { } titleId ? page.Badges.FirstOrDefault(b => b.Earned && b.Id == titleId) : null;

        if (!canSee)
        {
            return page;
        }

        var (favorite, chosen) = Favorite(rows, profile.FavoriteGameID);
        var playing = rows.Where(static r => r.Status == (byte)GameStatus.Playing).OrderByDescending(static r => r.UpdatedAt).FirstOrDefault();

        page.Stats = LibraryRules.Stats(rows, timeProvider.GetUtcNow().Year);
        page.Playing = playing is null || (!isMe && !page.ShowPlaying) ? null : LibraryRules.Entry(playing);
        page.FavoriteNote = profile.FavoriteNote;

        // The banner: the game chosen, else the favourite.
        var bannerRow = (profile.BannerGameID is { } bannerId ? rows.FirstOrDefault(r => r.GameID == bannerId) : null) ?? favorite;
        if (bannerRow is not null)
        {
            page.BannerGame = GameMapper.Summary(bannerRow, null);
            page.BannerImageId = bannerRow.HeroImageID;
        }

        page.PlatformHours = ProfileRules.PlatformHours(rows);
        var complete = rows.Where(static r => r.AchievementsTotal is > 0 && r.AchievementsUnlocked >= r.AchievementsTotal).ToList();
        page.Completed = complete.Count;
        page.Platinums = [.. complete.OrderByDescending(static r => r.UpdatedAt).Take(12).Select(static r => LibraryRules.Entry(r))];
        page.Closest = [.. rows
            .Where(static r => r.AchievementsTotal is > 0 && r.AchievementsUnlocked is > 0 && r.AchievementsUnlocked < r.AchievementsTotal)
            .OrderByDescending(static r => (double)r.AchievementsUnlocked!.Value / r.AchievementsTotal!.Value)
            .Take(6)
            .Select(static r => LibraryRules.Entry(r))];
        page.Finished = [.. rows
            .Where(static r => r.Status == (byte)GameStatus.Finished)
            .OrderByDescending(static r => r.FinishedAt ?? r.UpdatedAt)
            .Take(6)
            .Select(static r => LibraryRules.Entry(r))];

        // My year in games: the days played in the last 52 weeks (from the syncs' daily snapshots).
        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        if (page.Showcases.Contains("year"))
        {
            page.PlayDays = [.. (await profileRepository.PlayDaysAsync(target, today.AddDays(-363)))
                .Select(static d => new PlayDayDTO { Day = new DateTimeOffset(DateTime.SpecifyKind(d.Day.Date, DateTimeKind.Utc)), Hours = d.Hours })];
        }

        if (page.Showcases.Contains("rare"))
        {
            page.Rare = await RareAsync(target, rows);
        }

        if (profile.PinnedCollectionID is { } pinnedId || isMe)
        {
            var collections = (await collectionRepository.ListAsync(target))
                .GroupBy(static c => (c.CollectionID, c.Name))
                .Select(g => new ProfileCollectionDTO
                {
                    Id = g.Key.CollectionID,
                    Name = g.Key.Name,
                    Count = g.Count(static c => c.GameID is not null),
                    Games = [.. g.Where(static c => c.GameID is not null)
                        .Select(c => rows.FirstOrDefault(r => r.GameID == c.GameID))
                        .Where(static r => r is not null)
                        .Take(4)
                        .Select(static r => GameMapper.Summary(r!, null))],
                })
                .ToList();
            page.Pinned = collections.FirstOrDefault(c => c.Id == profile.PinnedCollectionID);
            if (isMe)
            {
                page.CollectionChoices = [.. collections.Select(static c => new ProfileCollectionDTO { Id = c.Id, Name = c.Name, Count = c.Count })];
            }
        }
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

    // The rarest achievements and trophies earned, from the games with the most of theirs earned (at most eight
    // games read, kept six hours: Steam and PlayStation are asked only when the cache is empty).
    private async Task<List<RareAchievementDTO>> RareAsync(Guid target, IReadOnlyCollection<LibraryRow> rows)
    {
        return await cache.GetOrCreateAsync($"profile:rare:{target:N}", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(6);
            var candidates = rows
                .Where(static r => r.AchievementsUnlocked is > 0 && r.AchievementsTotal is > 0)
                .OrderByDescending(static r => (double)r.AchievementsUnlocked!.Value / r.AchievementsTotal!.Value)
                .ThenByDescending(static r => r.Hours ?? 0m)
                .DistinctBy(static r => r.GameID)
                .Take(8)
                .ToList();
            var user = target.ToString("N");
            var found = new List<RareAchievementDTO>();
            foreach (var row in candidates)
            {
                try
                {
                    if (row.SteamAppID is not null)
                    {
                        if ((await steamService.AchievementsAsync(user, row.GameID)).Result is { } steam)
                        {
                            found.AddRange(steam.Items.Where(static a => a.Unlocked && a.GlobalPercent is not null).Select(a => new RareAchievementDTO
                            {
                                Name = a.Name,
                                GameId = row.GameID,
                                GameName = row.Name,
                                Source = "Steam",
                                IconUrl = a.IconUrl,
                                Percent = a.GlobalPercent!.Value,
                            }));
                        }
                    }
                    else if ((row.Platform ?? string.Empty).StartsWith("PS", StringComparison.OrdinalIgnoreCase)
                             && (await psnService.TrophiesAsync(user, row.GameID)).Result is { } trophies)
                    {
                        found.AddRange(trophies.Items.Where(static t => t.Earned && t.Rarity is not null).Select(t => new RareAchievementDTO
                        {
                            Name = t.Name,
                            GameId = row.GameID,
                            GameName = row.Name,
                            Source = row.Platform ?? "PlayStation",
                            IconUrl = t.IconUrl,
                            Percent = t.Rarity!.Value,
                            Grade = t.Grade,
                        }));
                    }
                }
                catch (Exception exception) when (exception is not OperationCanceledException)
                {
                    logger.LogInformation("Profile: the achievements of one game couldn't be read ({Problem})", exception.Message);
                }
            }

            return found.OrderBy(static a => a.Percent).Take(6).ToList();
        }) ?? [];
    }

    private async Task<ServiceApiResponse<ProfileDTO>> NotFoundAsync(string? identifier) =>
        ServiceApiResponse<ProfileDTO>.NotFound(await globalizationHelper.TranslateAsync(nameof(TranslationKeys.NoRecordsFound), identifier ?? string.Empty));
}
