using Ludex.Library.Enums;
using Ludex.Library.Services;
using Ludex.WebApi.Extensions.Globalization;
using Ludex.WebApi.Features.Catalog;
using Ludex.WebApi.Features.Games.Models.Entities;
using Ludex.WebApi.Features.Games.Repositories;
using Ludex.WebApi.Features.Games.Services;
using Ludex.WebApi.Features.Library.Repositories;
using Ludex.WebApi.Features.Library.Services;
using Ludex.WebApi.Features.Profile.Repositories;
using Ludex.WebApi.Features.Library.Models.Entities;
using Ludex.WebApi.Features.Notifications.Repositories;
using Ludex.WebApi.Features.Steam.Models.DTOs;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Ludex.WebApi.Features.Steam.Services;

/// <summary>
/// Importing a Steam library: read it, match it to the catalog, and add the games ticked.
/// </summary>
internal interface ISteamService
{
    /// <summary>Reads a Steam library and matches it to the catalog (the matched games are saved to dbo.Game).</summary>
    /// <param name="userId">The account.</param>
    /// <param name="profile">The Steam profile typed (empty: the one imported from last time).</param>
    /// <returns>The games with a suggested status, or what's in the way.</returns>
    Task<ServiceApiResponse<SteamPreviewDTO>> PreviewAsync(string userId, string? profile);

    /// <summary>Adds the games ticked (and more hours to the ones already there).</summary>
    /// <param name="item">The account, the Steam account and the games.</param>
    /// <returns>How many were added and updated.</returns>
    Task<ServiceApiResponse<SteamImportResultDTO>> ImportAsync(SteamImportRequest item);

    /// <summary>Syncs an account's library with Steam now (what the daily job does).</summary>
    /// <param name="item">The account.</param>
    /// <returns>What changed, or what's in the way.</returns>
    Task<ServiceApiResponse<SteamSyncResultDTO>> SyncNowAsync(SteamSyncRequest item);

    /// <summary>Syncs one account: more hours, the last two weeks, games bought since, achievement counts.</summary>
    /// <param name="user">The account.</param>
    /// <param name="steamId">Its SteamID64.</param>
    /// <param name="cancellationToken">Stops the run.</param>
    /// <returns>What changed (null: Steam wouldn't list the games).</returns>
    Task<SteamSyncResultDTO?> SyncAsync(Guid user, string steamId, CancellationToken cancellationToken = default);

    /// <summary>An account's Steam achievements in a game of its library.</summary>
    /// <param name="userId">The account.</param>
    /// <param name="gameId">The game.</param>
    /// <returns>The achievements, or not found (not a Steam game, no achievements).</returns>
    Task<ServiceApiResponse<AchievementsDTO>> AchievementsAsync(string userId, int gameId);
}

/// <summary>
/// Importing a Steam library: read it, match it to the catalog, and add the games ticked.
/// </summary>
/// <param name="steam">The Steam Web API.</param>
/// <param name="catalog">The catalog (Steam app → game).</param>
/// <param name="gameRepository">Games (the matched ones are saved).</param>
/// <param name="libraryRepository">Libraries.</param>
/// <param name="profileRepository">Profiles (the Steam account imported from).</param>
/// <param name="notificationRepository">The bell (games bought since).</param>
/// <param name="cache">Achievement names and icons, and how rare they are.</param>
/// <param name="options">Whether a Steam key is set.</param>
/// <param name="globalizationHelper">Messages.</param>
/// <param name="timeProvider">The clock.</param>
/// <param name="logger">Logger.</param>
internal sealed class SteamService(
    SteamClient steam,
    IGameCatalog catalog,
    IGameRepository gameRepository,
    ILibraryRepository libraryRepository,
    IProfileRepository profileRepository,
    INotificationRepository notificationRepository,
    IMemoryCache cache,
    IOptions<LudexOptions> options,
    IGlobalizationHelper globalizationHelper,
    TimeProvider timeProvider,
    ILogger<SteamService> logger) : ISteamService
{
    /// <summary>Most hours a game may have (as in the game page).</summary>
    private const decimal MaxHours = 99999m;

    /// <inheritdoc />
    public async Task<ServiceApiResponse<SteamPreviewDTO>> PreviewAsync(string userId, string? profile)
    {
        if (!Guid.TryParse(userId, out var user))
        {
            return ServiceApiResponse<SteamPreviewDTO>.NotFound(await globalizationHelper.TranslateAsync(nameof(TranslationKeys.NoRecordsFound), userId ?? string.Empty));
        }

        if (!options.Value.HasSteam)
        {
            return Answer(new SteamPreviewDTO { Problem = "The Steam import is off: set STEAM_API_KEY in Ludex's .env (a key from steamcommunity.com/dev/apikey) and update Ludex." });
        }

        if (catalog.Source != CatalogSource.Igdb)
        {
            return Answer(new SteamPreviewDTO { Problem = "The Steam import needs IGDB to match the games: set IGDB_CLIENT_ID and IGDB_CLIENT_SECRET in Ludex's .env." });
        }

        var typed = (profile ?? string.Empty).Trim();
        if (typed.Length == 0)
        {
            typed = (await profileRepository.GetAsync(user))?.SteamID ?? string.Empty;
            if (typed.Length == 0)
            {
                return Answer(new SteamPreviewDTO { Available = true });
            }
        }

        try
        {
            var (steamId, vanity) = SteamRules.ParseProfile(typed);
            if (steamId is null && vanity is not null)
            {
                steamId = await steam.ResolveAsync(vanity);
                if (steamId is null)
                {
                    return Problem($"There's no Steam profile called '{vanity}'. Paste the address of your Steam profile instead.");
                }
            }

            if (steamId is null)
            {
                return Problem("That isn't a Steam profile. Paste its address (steamcommunity.com/id/… or /profiles/…) or the custom name in it.");
            }

            var player = await steam.PlayerAsync(steamId);
            if (player is null)
            {
                return Problem("Steam has no account with that id.");
            }

            var synced = (await profileRepository.GetAsync(user))?.SteamSyncedAt;
            var page = new SteamPreviewDTO
            {
                SyncedAt = synced is { } syncedAt ? GameMapper.Utc(syncedAt) : null,
                Available = true,
                SteamId = steamId,
                PersonaName = player.PersonaName,
                AvatarUrl = player.AvatarFull,
                ProfileUrl = player.ProfileUrl,
            };

            var owned = await steam.OwnedGamesAsync(steamId);
            if (owned is null || owned.Count == 0)
            {
                page.Problem = player.CommunityVisibilityState == 3
                    ? "Steam lists no games for this account. If it has games, set Game details to Public (Steam → Edit profile → Privacy settings) and try again."
                    : "This Steam profile is private. Set the profile and Game details to Public (Steam → Edit profile → Privacy settings) and try again.";
                return Answer(page);
            }

            page.OwnedCount = owned.Count;
            var matched = await catalog.SteamAsync([.. owned.Select(static g => g.AppId)]);
            foreach (var game in matched.Values.DistinctBy(static g => g.Id))
            {
                await gameRepository.UpsertAsync(GameRow.From(game));
            }

            var library = (await libraryRepository.ListAsync(user)).ToLookup(static r => r.GameID);
            var now = timeProvider.GetUtcNow();

            // Two apps can be the same game (an old version and a remaster): one row, with the most hours.
            page.Games = [.. owned
                .Where(g => matched.ContainsKey(g.AppId))
                .GroupBy(g => matched[g.AppId].Id)
                .Select(group =>
                {
                    var game = matched[group.First().AppId];
                    var appId = group.OrderByDescending(static g => g.PlaytimeForever).First().AppId;
                    var minutes = group.Max(static g => g.PlaytimeForever);
                    var last = group.Select(static g => SteamRules.LastPlayed(g.LastPlayed)).Max();
                    var hours = SteamRules.Hours(minutes);
                    var (status, selected) = SteamRules.Suggest(minutes, last, now);
                    var mine = CopyRules.Pick(library[game.Id], "PC", appId);   // the PC copy; one on a console doesn't count
                    return new SteamGameDTO
                    {
                        AppId = appId,
                        Game = GameMapper.Summary(game, mine is null ? null : GameMapper.Status(mine.Status)),
                        Hours = hours,
                        RecentHours = SteamRules.Hours(group.Max(static g => g.Playtime2Weeks ?? 0)),
                        LastPlayedAt = last,
                        LibraryHours = mine?.Hours,
                        SuggestedStatus = status,
                        Selected = mine is null ? selected : hours > (mine.Hours ?? 0m),
                    };
                })
                .OrderByDescending(static g => g.LastPlayedAt ?? DateTimeOffset.MinValue)
                .ThenByDescending(static g => g.Hours)];
            page.Unmatched = [.. owned
                .Where(g => !matched.ContainsKey(g.AppId))
                .Select(static g => string.IsNullOrWhiteSpace(g.Name) ? $"App {g.AppId}" : g.Name.Trim())
                .Order(StringComparer.OrdinalIgnoreCase)];
            return Answer(page);
        }
        catch (SteamUnavailableException exception)
        {
            return Problem(exception.Message);
        }
        catch (CatalogUnavailableException exception)
        {
            logger.LogWarning("IGDB failed during a Steam import: {Problem}", exception.Message);
            return Problem(await globalizationHelper.TranslateAsync(nameof(TranslationKeys.CatalogUnreachable), exception.Message));
        }
    }

    /// <inheritdoc />
    public async Task<ServiceApiResponse<SteamImportResultDTO>> ImportAsync(SteamImportRequest item)
    {
        if (!Guid.TryParse(item.UserId, out var user))
        {
            return ServiceApiResponse<SteamImportResultDTO>.NotFound(await globalizationHelper.TranslateAsync(nameof(TranslationKeys.NoRecordsFound), item.UserId ?? string.Empty));
        }

        if (!SteamRules.IsSteamId(item.SteamId))
        {
            const string message = "That isn't a Steam account: load your Steam games again.";
            return ServiceApiResponse<SteamImportResultDTO>.ValidationProblems(message).AddError("SteamId", message);
        }

        var rows = item.Games
            .Where(static g => g.GameId > 0 && Enum.IsDefined(g.Status) && g.Hours is null or (>= 0 and <= MaxHours))
            .GroupBy(static g => g.GameId)
            .Select(static group =>
            {
                var game = group.OrderByDescending(static g => g.Hours ?? 0m).First();
                return new LibraryImportRow(
                    game.GameId,
                    (byte)game.Status,
                    game.Hours is { } hours ? Math.Round(hours, 1, MidpointRounding.AwayFromZero) : null,
                    game.LastPlayedAt?.UtcDateTime is { } at ? DateTime.SpecifyKind(at, DateTimeKind.Unspecified) : null,
                    game.AppId,
                    game.RecentHours is { } recent ? (int)Math.Round(recent * 60m) : null);
            })
            .ToList();

        var counts = await libraryRepository.ImportAsync(user, rows);
        await profileRepository.SetSteamAsync(user, item.SteamId!);

        // Every game owned now counts as seen: the daily sync only adds games bought after this.
        try
        {
            var owned = await steam.OwnedGamesAsync(item.SteamId!);
            await profileRepository.RememberSteamAppsAsync(user, [.. (owned ?? []).Select(static g => g.AppId)]);
        }
        catch (SteamUnavailableException exception)
        {
            logger.LogWarning("Steam import: the owned games weren't remembered ({Problem})", exception.Message);
        }
        logger.LogInformation("Steam import: {Added} added, {Updated} with more hours", counts.Added, counts.Updated);
        return ServiceApiResponse<SteamImportResultDTO>.Success(new SteamImportResultDTO { Added = counts.Added, Updated = counts.Updated });
    }

    /// <inheritdoc />
    public async Task<ServiceApiResponse<SteamSyncResultDTO>> SyncNowAsync(SteamSyncRequest item)
    {
        if (!Guid.TryParse(item.UserId, out var user))
        {
            return ServiceApiResponse<SteamSyncResultDTO>.NotFound(await globalizationHelper.TranslateAsync(nameof(TranslationKeys.NoRecordsFound), item.UserId ?? string.Empty));
        }

        var steamId = (await profileRepository.GetAsync(user))?.SteamID;
        if (!options.Value.HasSteam || catalog.Source != CatalogSource.Igdb || steamId is null)
        {
            const string message = "Import your Steam library first (it needs STEAM_API_KEY and IGDB).";
            return ServiceApiResponse<SteamSyncResultDTO>.ValidationProblems(message).AddError("Steam", message);
        }

        try
        {
            var result = await SyncAsync(user, steamId);
            if (result is null)
            {
                const string message = "Steam didn't list your games. Is Game details still Public in Steam's privacy settings?";
                return ServiceApiResponse<SteamSyncResultDTO>.ValidationProblems(message).AddError("Steam", message);
            }

            return ServiceApiResponse<SteamSyncResultDTO>.Success(result);
        }
        catch (Exception exception) when (exception is SteamUnavailableException or CatalogUnavailableException)
        {
            return ServiceApiResponse<SteamSyncResultDTO>.ValidationProblems(exception.Message).AddError("Steam", exception.Message);
        }
    }

    /// <inheritdoc />
    public async Task<SteamSyncResultDTO?> SyncAsync(Guid user, string steamId, CancellationToken cancellationToken = default)
    {
        var owned = await steam.OwnedGamesAsync(steamId, cancellationToken);
        if (owned is null || owned.Count == 0)
        {
            return null;
        }

        var known = (await profileRepository.SteamAppsAsync(user)).ToHashSet();

        // Before Ludex remembered what was owned (an import from an older version), nothing counts as bought since:
        // this first sync only learns, so games left out on purpose aren't added.
        var addNew = known.Count > 0;
        var library = await libraryRepository.CopiesAsync(user);
        var gameByApp = library.Where(static r => r.SteamAppID is not null)
            .GroupBy(static r => r.SteamAppID!.Value)
            .ToDictionary(static g => g.Key, static g => g.First().GameID);
        var copies = library.ToLookup(static r => r.GameID);

        // Match to the catalog only what isn't matched yet and could matter: everything the first time, then new apps.
        var toMatch = owned.Select(static g => g.AppId).Where(app => !gameByApp.ContainsKey(app) && (!addNew || !known.Contains(app))).ToList();
        var matched = toMatch.Count == 0 ? new Dictionary<int, CatalogGame>() : await catalog.SteamAsync(toMatch, cancellationToken);
        foreach (var game in matched.Values.DistinctBy(static g => g.Id))
        {
            await gameRepository.UpsertAsync(GameRow.From(game));
        }

        var now = timeProvider.GetUtcNow();
        var rows = new List<LibraryImportRow>();
        foreach (var app in owned)
        {
            int gameId;
            if (gameByApp.TryGetValue(app.AppId, out var mapped))
            {
                gameId = mapped;
            }
            else if (matched.TryGetValue(app.AppId, out var game))
            {
                gameId = game.Id;
            }
            else
            {
                continue;
            }

            var isNew = CopyRules.Pick(copies[gameId], "PC", app.AppId) is null;   // no PC copy yet (one on a console doesn't count)
            if (isNew && (!addNew || known.Contains(app.AppId)))
            {
                continue;   // left out at the import, or the first sync: not added
            }

            var last = SteamRules.LastPlayed(app.LastPlayed);
            var status = isNew ? SteamRules.Suggest(app.PlaytimeForever, last, now).Status : GameStatus.Playing;   // ignored for games already there
            rows.Add(new LibraryImportRow(
                gameId,
                (byte)status,
                SteamRules.Hours(app.PlaytimeForever),
                last?.UtcDateTime is { } at ? DateTime.SpecifyKind(at, DateTimeKind.Unspecified) : null,
                app.AppId,
                app.Playtime2Weeks ?? 0));
        }

        var counts = await libraryRepository.ImportAsync(user, rows);
        await profileRepository.RememberSteamAppsAsync(user, [.. owned.Select(static g => g.AppId)]);
        if (counts.Added > 0)
        {
            await notificationRepository.AddAsync(
                user,
                null,
                NotificationKind.SteamNewGames,
                counts.Added == 1 ? "Steam: 1 new game in your library." : $"Steam: {counts.Added} new games in your library.",
                "/library?status=backlog");
        }

        var achievements = await RefreshAchievementsAsync(user, steamId, cancellationToken);
        logger.LogInformation("Steam sync: {Added} added, {Updated} with more hours, {Achievements} achievement counts", counts.Added, counts.Updated, achievements);
        return new SteamSyncResultDTO { Added = counts.Added, Updated = counts.Updated, Achievements = achievements };
    }

    /// <inheritdoc />
    public async Task<ServiceApiResponse<AchievementsDTO>> AchievementsAsync(string userId, int gameId)
    {
        if (!Guid.TryParse(userId, out var user) || !options.Value.HasSteam)
        {
            return await AchievementsNotFoundAsync(userId);
        }

        var entry = (await libraryRepository.CopiesAsync(user, gameId)).FirstOrDefault(static c => c.SteamAppID is not null);
        var steamId = (await profileRepository.GetAsync(user))?.SteamID;
        if (entry?.SteamAppID is not { } appId || steamId is null)
        {
            return await AchievementsNotFoundAsync(userId);
        }

        try
        {
            var schema = await cache.GetOrCreateAsync($"steam:schema:{appId}", async e =>
            {
                e.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(12);
                return await steam.SchemaAsync(appId);
            }) ?? [];
            if (schema.Count == 0)
            {
                await libraryRepository.SaveAchievementsAsync(user, gameId, 0, 0);
                return await AchievementsNotFoundAsync(userId);
            }

            var mine = await cache.GetOrCreateAsync($"steam:mine:{steamId}:{appId}", async e =>
            {
                e.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
                return await steam.PlayerAchievementsAsync(steamId, appId);
            });
            var global = await cache.GetOrCreateAsync($"steam:global:{appId}", async e =>
            {
                e.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(12);
                return await steam.GlobalPercentagesAsync(appId);
            }) ?? new Dictionary<string, double>(StringComparer.Ordinal);

            var page = SteamRules.Achievements(appId, schema, mine, global);
            if (mine is not null)
            {
                await libraryRepository.SaveAchievementsAsync(user, gameId, page.Unlocked, page.Total);
            }

            return ServiceApiResponse<AchievementsDTO>.Success(page);
        }
        catch (SteamUnavailableException exception)
        {
            return ServiceApiResponse<AchievementsDTO>.ValidationProblems(exception.Message).AddError("Steam", exception.Message);
        }
    }

    /// <summary>
    /// Reads the achievement counts of the games that need it (never read, played lately, or a week old), at most 300
    /// a run and about seven a second.
    /// </summary>
    private async Task<int> RefreshAchievementsAsync(Guid user, string steamId, CancellationToken cancellationToken)
    {
        var done = 0;
        foreach (var target in (await libraryRepository.AchievementTargetsAsync(user)).Take(300))
        {
            cancellationToken.ThrowIfCancellationRequested();
            List<SteamPlayerAchievement>? achievements;
            try
            {
                achievements = await steam.PlayerAchievementsAsync(steamId, target.SteamAppID, cancellationToken);
            }
            catch (SteamUnavailableException exception)
            {
                logger.LogWarning("Steam achievements stopped: {Problem}", exception.Message);
                break;
            }

            if (achievements is null)
            {
                break;   // private game details: none of them can be read
            }

            await libraryRepository.SaveAchievementsAsync(user, target.GameID, achievements.Count(static a => a.Achieved == 1), achievements.Count);
            done++;
            await Task.Delay(TimeSpan.FromMilliseconds(150), timeProvider, cancellationToken);
        }

        return done;
    }

    private async Task<ServiceApiResponse<AchievementsDTO>> AchievementsNotFoundAsync(string? userId) =>
        ServiceApiResponse<AchievementsDTO>.NotFound(await globalizationHelper.TranslateAsync(nameof(TranslationKeys.NoRecordsFound), userId ?? string.Empty));

    private static ServiceApiResponse<SteamPreviewDTO> Answer(SteamPreviewDTO page) => ServiceApiResponse<SteamPreviewDTO>.Success(page);

    private static ServiceApiResponse<SteamPreviewDTO> Problem(string message) =>
        ServiceApiResponse<SteamPreviewDTO>.ValidationProblems(message).AddError("Profile", message);
}
