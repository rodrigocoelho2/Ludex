using Ludex.Library.Enums;
using Ludex.Library.Services;
using Ludex.WebApi.Extensions.Globalization;
using Ludex.WebApi.Features.Catalog;
using Ludex.WebApi.Features.Games.Models.Entities;
using Ludex.WebApi.Features.Games.Services;
using Ludex.WebApi.Features.Imports.Models.DTOs;
using Ludex.WebApi.Features.Imports.Services;
using Ludex.WebApi.Features.Library.Repositories;
using Ludex.WebApi.Features.Library.Services;
using Ludex.WebApi.Features.Notifications.Repositories;
using Ludex.WebApi.Features.Psn.Models.DTOs;
using Ludex.WebApi.Features.Psn.Repositories;
using Ludex.WebApi.Features.Steam.Services;
using Microsoft.Extensions.Caching.Memory;

namespace Ludex.WebApi.Features.Psn.Services;

/// <summary>
/// The PlayStation connection: connect with an NPSSO, import the games played (time played, trophies), and the
/// daily sync.
/// </summary>
internal interface IPsnService
{
    /// <summary>An account's connection.</summary>
    /// <param name="userId">The account.</param>
    /// <returns>The connection.</returns>
    Task<ServiceApiResponse<PsnStatusDTO>> StatusAsync(string userId);

    /// <summary>Connects PlayStation with an NPSSO (only the refresh token is kept, encrypted).</summary>
    /// <param name="item">The account and the NPSSO.</param>
    /// <returns>The connection, or what's wrong with the NPSSO.</returns>
    Task<ServiceApiResponse<PsnStatusDTO>> ConnectAsync(PsnConnectRequest item);

    /// <summary>Reads the games played and matches them to the catalog (the matched games are saved to dbo.Game).</summary>
    /// <param name="userId">The account.</param>
    /// <returns>The games with a suggested status, or what's in the way.</returns>
    Task<ServiceApiResponse<PsnPreviewDTO>> PreviewAsync(string userId);

    /// <summary>Adds the games ticked; every game PlayStation lists now counts as seen (the sync only adds newer ones).</summary>
    /// <param name="item">The account and the games.</param>
    /// <returns>How many were added and updated.</returns>
    Task<ServiceApiResponse<ImportResultDTO>> ImportAsync(PsnImportRequest item);

    /// <summary>Syncs the library with PlayStation now (what the daily job does).</summary>
    /// <param name="item">The account.</param>
    /// <returns>What changed, or what's in the way.</returns>
    Task<ServiceApiResponse<ImportResultDTO>> SyncNowAsync(PsnAccountRequest item);

    /// <summary>Disconnects PlayStation (the games imported stay in the library).</summary>
    /// <param name="item">The account.</param>
    /// <returns>The connection (none).</returns>
    Task<ServiceApiResponse<PsnStatusDTO>> DisconnectAsync(PsnAccountRequest item);

    /// <summary>The account's trophies in a game it played on PlayStation (the game page).</summary>
    /// <param name="userId">The account.</param>
    /// <param name="gameId">The game.</param>
    /// <returns>The trophies, or not found (no trophy list known, or not connected).</returns>
    Task<ServiceApiResponse<PsnTrophiesDTO>> TrophiesAsync(string userId, int gameId);

    /// <summary>The daily job for one connection: warn before the sign-in runs out, then sync when it's due.</summary>
    /// <param name="account">The connection.</param>
    /// <param name="due">Whether the library is due a sync.</param>
    /// <param name="cancellationToken">Stops the run.</param>
    /// <returns>A task.</returns>
    Task RunDailyAsync(PsnAccountRow account, bool due, CancellationToken cancellationToken = default);
}

/// <summary>
/// The PlayStation connection.
/// </summary>
/// <param name="psn">PlayStation Network.</param>
/// <param name="protector">Encrypts the refresh token.</param>
/// <param name="psnRepository">Connections and the games they showed.</param>
/// <param name="matcher">Names → games.</param>
/// <param name="catalog">The catalog (whether IGDB is on).</param>
/// <param name="libraryRepository">Libraries.</param>
/// <param name="notificationRepository">The bell.</param>
/// <param name="globalizationHelper">Messages.</param>
/// <param name="cache">Access tokens (an hour) and trophy lists.</param>
/// <param name="timeProvider">The clock.</param>
/// <param name="logger">Logger.</param>
internal sealed class PsnService(
    PsnClient psn,
    PsnTokenProtector protector,
    IPsnRepository psnRepository,
    GameMatcher matcher,
    IGameCatalog catalog,
    ILibraryRepository libraryRepository,
    INotificationRepository notificationRepository,
    IGlobalizationHelper globalizationHelper,
    IMemoryCache cache,
    TimeProvider timeProvider,
    ILogger<PsnService> logger) : IPsnService
{
    /// <summary>How long before the sign-in runs out the bell says so.</summary>
    private static readonly TimeSpan WarnBefore = TimeSpan.FromDays(7);

    /// <inheritdoc />
    public async Task<ServiceApiResponse<PsnStatusDTO>> StatusAsync(string userId)
    {
        if (!Guid.TryParse(userId, out var user))
        {
            return await NotFoundAsync<PsnStatusDTO>(userId);
        }

        return ServiceApiResponse<PsnStatusDTO>.Success(Status(await psnRepository.GetAsync(user)));
    }

    /// <inheritdoc />
    public async Task<ServiceApiResponse<PsnStatusDTO>> ConnectAsync(PsnConnectRequest item)
    {
        if (!Guid.TryParse(item.UserId, out var user))
        {
            return await NotFoundAsync<PsnStatusDTO>(item.UserId);
        }

        if (catalog.Source != CatalogSource.Igdb)
        {
            return Invalid<PsnStatusDTO>(NeedsIgdb);
        }

        if (PsnRules.Npsso(item.Npsso) is not { } npsso)
        {
            return Invalid<PsnStatusDTO>("That isn't an NPSSO: it has 64 letters and digits. Copy the value after \"npsso\": on the page ca.account.sony.com/api/v1/ssocookie (signed in to playstation.com).");
        }

        try
        {
            var tokens = await psn.SignInAsync(npsso);
            cache.Set($"psn:access:{user:N}", tokens.AccessToken, TimeSpan.FromMinutes(50));
            var onlineId = await psn.OnlineIdAsync(tokens.AccessToken);
            await psnRepository.SaveAsync(user, onlineId, protector.Protect(tokens.RefreshToken), tokens.RefreshExpiresAt.UtcDateTime);
            logger.LogInformation("PlayStation connected for one account");
            return ServiceApiResponse<PsnStatusDTO>.Success(Status(await psnRepository.GetAsync(user)));
        }
        catch (Exception exception) when (exception is PsnSignInException or PsnUnavailableException)
        {
            return Invalid<PsnStatusDTO>(exception.Message);
        }
    }

    /// <inheritdoc />
    public async Task<ServiceApiResponse<PsnPreviewDTO>> PreviewAsync(string userId)
    {
        if (!Guid.TryParse(userId, out var user))
        {
            return await NotFoundAsync<PsnPreviewDTO>(userId);
        }

        var account = await psnRepository.GetAsync(user);
        var page = new PsnPreviewDTO { Status = Status(account) };
        if (!page.Status.Available || !page.Status.Connected)
        {
            return ServiceApiResponse<PsnPreviewDTO>.Success(page);
        }

        try
        {
            var games = await GamesAsync(account!, CancellationToken.None);
            page.GameCount = games.Count;
            var known = (await psnRepository.TitlesAsync(user)).ToDictionary(static t => t.TitleKey, StringComparer.Ordinal);
            var library = await libraryRepository.ListAsync(user);
            var mine = library.ToLookup(static r => r.GameID);

            // A title matched before keeps its game; the rest are matched by name (the library first).
            var resolved = new Dictionary<string, GameRow>(StringComparer.Ordinal);
            foreach (var game in games)
            {
                if (known.TryGetValue(game.Key, out var title) && title.GameID is { } id && mine[id].FirstOrDefault() is { } row)
                {
                    resolved[game.Key] = row;
                }
            }

            var rest = games.Where(g => !resolved.ContainsKey(g.Key)).ToList();
            var byName = await matcher.MatchAsync([.. rest.Select(static g => (g.Name, (string?)g.Platform))], library);
            foreach (var game in rest)
            {
                if (byName.TryGetValue(game.Name, out var row))
                {
                    resolved[game.Key] = row;
                }
            }

            var now = timeProvider.GetUtcNow();
            page.Games = [.. games
                .Where(g => resolved.ContainsKey(g.Key))
                .GroupBy(g => resolved[g.Key].GameID)
                .Select(group =>
                {
                    var g = group.OrderByDescending(static x => x.Minutes).ThenByDescending(static x => x.TrophiesEarned ?? 0).First();
                    var game = resolved[g.Key];
                    var copy = CopyRules.Pick(mine[game.GameID], g.Platform);   // the PlayStation copy; one on Steam doesn't count
                    var dto = ImportRules.Row(g.Name, g.Platform, game, copy, g.Platform, null, null, g.Minutes, group.Max(static x => x.LastPlayed), PsnRules.Status(g, now), now);
                    dto.Key = g.Key;
                    dto.TrophiesEarned = g.TrophiesEarned;
                    dto.TrophiesTotal = g.TrophiesTotal;
                    if (copy is { } entry && g.TrophiesTotal is > 0 && entry.SteamAppID is null
                        && (entry.AchievementsUnlocked ?? -1) != g.TrophiesEarned)
                    {
                        dto.Selected = true;   // new trophies are worth an update too
                    }

                    return dto;
                })
                .OrderByDescending(static g => g.LastPlayedAt ?? DateTimeOffset.MinValue)
                .ThenByDescending(static g => g.Hours)];
            page.Unmatched = [.. games.Where(g => !resolved.ContainsKey(g.Key)).Select(static g => g.Name).Distinct(StringComparer.Ordinal).Order(StringComparer.OrdinalIgnoreCase)];
            return ServiceApiResponse<PsnPreviewDTO>.Success(page);
        }
        catch (PsnSignInException exception)
        {
            await BreakAsync(user, notify: false);
            page.Status = Status(await psnRepository.GetAsync(user));
            page.Status.Problem = exception.Message;
            return ServiceApiResponse<PsnPreviewDTO>.Success(page);
        }
        catch (PsnUnavailableException exception)
        {
            page.Status.Problem = exception.Message;
            return ServiceApiResponse<PsnPreviewDTO>.Success(page);
        }
        catch (CatalogUnavailableException exception)
        {
            logger.LogWarning("IGDB failed during a PlayStation import: {Problem}", exception.Message);
            page.Status.Problem = await globalizationHelper.TranslateAsync(nameof(TranslationKeys.CatalogUnreachable), exception.Message);
            return ServiceApiResponse<PsnPreviewDTO>.Success(page);
        }
    }

    /// <inheritdoc />
    public async Task<ServiceApiResponse<ImportResultDTO>> ImportAsync(PsnImportRequest item)
    {
        if (!Guid.TryParse(item.UserId, out var user))
        {
            return await NotFoundAsync<ImportResultDTO>(item.UserId);
        }

        var account = await psnRepository.GetAsync(user);
        if (account?.Token is null)
        {
            return Invalid<ImportResultDTO>("Connect PlayStation first.");
        }

        var counts = await libraryRepository.ImportAsync(user, ImportRules.Rows(item.Games, withTrophies: true));

        // Remember every game PlayStation lists now (the ones left out without a game), so the daily sync only adds
        // games played for the first time after this.
        var ticked = item.Games.Where(static g => g.GameId > 0 && !string.IsNullOrWhiteSpace(g.Key))
            .GroupBy(static g => g.Key!, StringComparer.Ordinal)
            .ToDictionary(static g => g.Key, static g => g.First().GameId, StringComparer.Ordinal);
        List<PsnTitleRemember> titles;
        try
        {
            titles = [.. (await GamesAsync(account, CancellationToken.None)).Select(g => new PsnTitleRemember(g.Key, g.Name, ticked.TryGetValue(g.Key, out var id) ? id : null, g.TrophyList))];
        }
        catch (Exception exception) when (exception is PsnSignInException or PsnUnavailableException)
        {
            logger.LogWarning("PlayStation import: the games listed weren't remembered ({Problem})", exception.Message);
            titles = [.. item.Games.Where(static g => g.GameId > 0 && !string.IsNullOrWhiteSpace(g.Key)).Select(static g => new PsnTitleRemember(g.Key!, g.Key!, g.GameId))];
        }

        await psnRepository.RememberAsync(user, titles);
        logger.LogInformation("PlayStation import: {Added} added, {Updated} updated", counts.Added, counts.Updated);
        return ServiceApiResponse<ImportResultDTO>.Success(new ImportResultDTO { Added = counts.Added, Updated = counts.Updated });
    }

    /// <inheritdoc />
    public async Task<ServiceApiResponse<ImportResultDTO>> SyncNowAsync(PsnAccountRequest item)
    {
        if (!Guid.TryParse(item.UserId, out var user))
        {
            return await NotFoundAsync<ImportResultDTO>(item.UserId);
        }

        var account = await psnRepository.GetAsync(user);
        if (account?.Token is null || account.SyncedAt is null || catalog.Source != CatalogSource.Igdb)
        {
            return Invalid<ImportResultDTO>("Connect PlayStation and import your games first.");
        }

        try
        {
            return ServiceApiResponse<ImportResultDTO>.Success(await SyncAsync(account, CancellationToken.None));
        }
        catch (PsnSignInException exception)
        {
            await BreakAsync(user, notify: false);
            return Invalid<ImportResultDTO>(exception.Message);
        }
        catch (Exception exception) when (exception is PsnUnavailableException or CatalogUnavailableException)
        {
            return Invalid<ImportResultDTO>(exception.Message);
        }
    }

    /// <inheritdoc />
    public async Task<ServiceApiResponse<PsnStatusDTO>> DisconnectAsync(PsnAccountRequest item)
    {
        if (!Guid.TryParse(item.UserId, out var user))
        {
            return await NotFoundAsync<PsnStatusDTO>(item.UserId);
        }

        cache.Remove($"psn:access:{user:N}");
        await psnRepository.DeleteAsync(user);
        logger.LogInformation("PlayStation disconnected for one account");
        return ServiceApiResponse<PsnStatusDTO>.Success(Status(null));
    }

    /// <inheritdoc />
    public async Task RunDailyAsync(PsnAccountRow account, bool due, CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow();
        if (account.TokenExpiresAt is { } expires && account.WarnedAt is null && GameMapper.Utc(expires) - now < WarnBefore)
        {
            await notificationRepository.AddAsync(
                account.UserUniqueID,
                null,
                NotificationKind.ConnectionProblem,
                $"PlayStation: the connection runs out on {GameMapper.Utc(expires):d MMM}. Paste a new NPSSO to keep the daily sync.",
                "/psn");
            await psnRepository.MarkWarnedAsync(account.UserUniqueID);
        }

        if (!due || account.SyncedAt is null || catalog.Source != CatalogSource.Igdb)
        {
            return;
        }

        try
        {
            await SyncAsync(account, cancellationToken);
        }
        catch (PsnSignInException)
        {
            await BreakAsync(account.UserUniqueID, notify: true);
        }
    }

    private const string NeedsIgdb = "PlayStation needs IGDB to match the games: set IGDB_CLIENT_ID and IGDB_CLIENT_SECRET in Ludex's .env.";

    private PsnStatusDTO Status(PsnAccountRow? account) => new()
    {
        Available = catalog.Source == CatalogSource.Igdb,
        Problem = catalog.Source == CatalogSource.Igdb ? null : NeedsIgdb,
        Connected = account?.Token is not null,
        Broken = account is not null && account.Token is null,
        OnlineId = account?.OnlineID,
        ConnectedAt = account is null ? null : GameMapper.Utc(account.ConnectedAt),
        SyncedAt = account?.SyncedAt is { } synced ? GameMapper.Utc(synced) : null,
        ExpiresAt = account?.Token is not null && account.TokenExpiresAt is { } expires ? GameMapper.Utc(expires) : null,
    };

    // One sync: new games (played for the first time since the last look) are added, the rest get their time and trophies.
    private async Task<ImportResultDTO> SyncAsync(PsnAccountRow account, CancellationToken cancellationToken)
    {
        var user = account.UserUniqueID;
        var games = await GamesAsync(account, cancellationToken);
        var known = (await psnRepository.TitlesAsync(user)).ToDictionary(static t => t.TitleKey, StringComparer.Ordinal);
        var library = await libraryRepository.ListAsync(user);
        var copies = library.ToLookup(static r => r.GameID);
        var fresh = games.Where(g => !known.ContainsKey(g.Key)).ToList();
        var matched = fresh.Count == 0
            ? new Dictionary<string, GameRow>(StringComparer.Ordinal)
            : await matcher.MatchAsync([.. fresh.Select(static g => (g.Name, (string?)g.Platform))], library, cancellationToken);

        var now = timeProvider.GetUtcNow();
        var requests = new List<ImportGameRequest>();
        var remember = new List<PsnTitleRemember>();
        foreach (var game in games)
        {
            int gameId;
            GameStatus status;
            if (known.TryGetValue(game.Key, out var title))
            {
                remember.Add(new PsnTitleRemember(game.Key, game.Name, null, game.TrophyList));   // keeps its game, takes its trophy list
                if (title.GameID is not { } id || CopyRules.Pick(copies[id], game.Platform) is null)
                {
                    continue;   // left out at the import, or its PlayStation copy taken out of the library since
                }

                gameId = id;
                status = GameStatus.Playing;   // ignored: the game is (or was) in the library
            }
            else
            {
                var row = matched.GetValueOrDefault(game.Name);
                remember.Add(new PsnTitleRemember(game.Key, game.Name, row?.GameID, game.TrophyList));
                if (row is null)
                {
                    continue;
                }

                gameId = row.GameID;
                status = PsnRules.Status(game, now) ?? SteamRules.Suggest(game.Minutes, game.LastPlayed, now).Status;
            }

            requests.Add(new ImportGameRequest
            {
                Key = game.Key,
                GameId = gameId,
                Status = status,
                Hours = game.Minutes > 0 ? SteamRules.Hours(game.Minutes) : null,
                LastPlayedAt = game.LastPlayed,
                Platform = game.Platform,
                TrophiesEarned = game.TrophiesEarned,
                TrophiesTotal = game.TrophiesTotal,
            });
        }

        var counts = await libraryRepository.ImportAsync(user, ImportRules.Rows(requests, withTrophies: true));
        await psnRepository.RememberAsync(user, remember);
        if (counts.Added > 0)
        {
            await notificationRepository.AddAsync(
                user,
                null,
                NotificationKind.NewGames,
                counts.Added == 1 ? "PlayStation: 1 new game in your library." : $"PlayStation: {counts.Added} new games in your library.",
                "/library");
        }

        logger.LogInformation("PlayStation sync: {Added} added, {Updated} updated", counts.Added, counts.Updated);
        return new ImportResultDTO { Added = counts.Added, Updated = counts.Updated };
    }

    /// <inheritdoc />
    public async Task<ServiceApiResponse<PsnTrophiesDTO>> TrophiesAsync(string userId, int gameId)
    {
        if (!Guid.TryParse(userId, out var user))
        {
            return await NotFoundAsync<PsnTrophiesDTO>(userId);
        }

        var account = await psnRepository.GetAsync(user);
        if (account?.Token is null || await psnRepository.TrophyListAsync(user, gameId) is not { } saved || saved.Split(':') is not [var list, var service])
        {
            return await NotFoundAsync<PsnTrophiesDTO>(userId);
        }

        try
        {
            var access = await AccessAsync(account, CancellationToken.None);
            var definitions = await cache.GetOrCreateAsync($"psn:list:{saved}", async e =>
            {
                e.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(12);
                return await psn.TrophyListAsync(access, list, service);
            }) ?? [];
            var earned = await cache.GetOrCreateAsync($"psn:earned:{user:N}:{saved}", async e =>
            {
                e.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
                return await psn.TrophiesEarnedAsync(access, list, service);
            }) ?? [];
            var items = PsnRules.Trophies(definitions, earned);
            return items.Count == 0
                ? await NotFoundAsync<PsnTrophiesDTO>(userId)
                : ServiceApiResponse<PsnTrophiesDTO>.Success(new PsnTrophiesDTO { Earned = items.Count(static t => t.Earned), Total = items.Count, Items = items });
        }
        catch (PsnSignInException exception)
        {
            await BreakAsync(user, notify: false);
            return Invalid<PsnTrophiesDTO>(exception.Message);
        }
        catch (PsnUnavailableException exception)
        {
            return Invalid<PsnTrophiesDTO>(exception.Message);
        }
    }

    // The account's games.
    private async Task<List<PsnGame>> GamesAsync(PsnAccountRow account, CancellationToken cancellationToken)
    {
        var access = await AccessAsync(account, cancellationToken);
        var played = await psn.PlayedAsync(access, cancellationToken);
        var trophies = await psn.TrophiesAsync(access, cancellationToken);
        return PsnRules.Games(played, trophies);
    }

    // An access token: kept for 50 minutes (Sony's last an hour); a new one comes from the refresh token, and a new
    // refresh token Sony sends is kept.
    private async Task<string> AccessAsync(PsnAccountRow account, CancellationToken cancellationToken)
    {
        var key = $"psn:access:{account.UserUniqueID:N}";
        if (cache.TryGetValue(key, out string? kept) && kept is not null)
        {
            return kept;
        }

        var refresh = protector.Unprotect(account.Token)
                      ?? throw new PsnSignInException("Ludex can't read the saved PlayStation sign-in (was LUDEX_API_KEY changed?). Connect PlayStation again.");
        var tokens = await psn.RefreshAsync(refresh, cancellationToken);
        if (tokens.RefreshToken != refresh)
        {
            await psnRepository.SaveAsync(account.UserUniqueID, null, protector.Protect(tokens.RefreshToken), tokens.RefreshExpiresAt.UtcDateTime);
        }

        cache.Set(key, tokens.AccessToken, TimeSpan.FromMinutes(50));
        return tokens.AccessToken;
    }

    // The sign-in no longer works: the token goes (the page asks to connect again), and the bell says so once.
    private async Task BreakAsync(Guid user, bool notify)
    {
        cache.Remove($"psn:access:{user:N}");
        await psnRepository.SaveAsync(user, null, null, null);
        if (notify)
        {
            await notificationRepository.AddAsync(user, null, NotificationKind.ConnectionProblem, "PlayStation: Ludex can't sign in any more. Connect again with a new NPSSO to keep the daily sync.", "/psn");
        }

        logger.LogInformation("PlayStation sign-in stopped working for one account");
    }

    private async Task<ServiceApiResponse<T>> NotFoundAsync<T>(string? userId) =>
        ServiceApiResponse<T>.NotFound(await globalizationHelper.TranslateAsync(nameof(TranslationKeys.NoRecordsFound), userId ?? string.Empty));

    private static ServiceApiResponse<T> Invalid<T>(string message) => ServiceApiResponse<T>.ValidationProblems(message).AddError("Psn", message);
}
