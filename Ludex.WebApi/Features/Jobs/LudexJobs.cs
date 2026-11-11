using Ludex.Library.Enums;
using Ludex.WebApi.Features.Catalog;
using Ludex.WebApi.Features.Games.Models.Entities;
using Ludex.WebApi.Features.Games.Repositories;
using Ludex.WebApi.Features.Prices.Services;
using Ludex.WebApi.Features.Profile.Repositories;
using Ludex.WebApi.Features.Steam.Services;
using Microsoft.Extensions.Options;

namespace Ludex.WebApi.Features.Jobs;

/// <summary>
/// What Ludex does on its own, checked every half hour (the first time two minutes after it starts):
/// 1. games whose data is older than the current version are fetched again (critics' rating, times to beat);
/// 2. every Steam library not synced in the last 20 hours is synced (hours, the last two weeks, games bought since,
///    achievement counts);
/// 3. wishlist prices not checked in the last 20 hours are checked, and the alerts they reach raised.
/// Each step that has nothing to do costs one query; a failure is logged and the next run tries again.
/// </summary>
/// <param name="catalog">The catalog.</param>
/// <param name="gameRepository">Games.</param>
/// <param name="profileRepository">Profiles (the Steam accounts).</param>
/// <param name="steamService">The Steam sync.</param>
/// <param name="priceService">Wishlist prices.</param>
/// <param name="options">What is configured.</param>
/// <param name="timeProvider">The clock.</param>
/// <param name="logger">Logger.</param>
internal sealed class LudexJobs(
    IGameCatalog catalog,
    IGameRepository gameRepository,
    IProfileRepository profileRepository,
    ISteamService steamService,
    PriceService priceService,
    IOptions<LudexOptions> options,
    TimeProvider timeProvider,
    ILogger<LudexJobs> logger) : BackgroundService
{
    private static readonly TimeSpan Every = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan SyncAfter = TimeSpan.FromHours(20);

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.JobsEnabled)
        {
            logger.LogInformation("Background jobs are off (Ludex:JobsEnabled = false)");
            return;
        }

        try
        {
            await Task.Delay(TimeSpan.FromMinutes(2), timeProvider, stoppingToken);
            using var timer = new PeriodicTimer(Every, timeProvider);
            do
            {
                await RunAsync(stoppingToken);
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // shutting down
        }
    }

    /// <summary>
    /// One run of every job.
    /// </summary>
    /// <param name="cancellationToken">Stops the run.</param>
    /// <returns>A task.</returns>
    internal async Task RunAsync(CancellationToken cancellationToken)
    {
        await StepAsync("catalog refresh", RefreshCatalogAsync, cancellationToken);
        await StepAsync("Steam sync", SyncSteamAsync, cancellationToken);
        await StepAsync("wishlist prices", CheckPricesAsync, cancellationToken);
    }

    private async Task RefreshCatalogAsync(CancellationToken cancellationToken)
    {
        if (catalog.Source != CatalogSource.Igdb)
        {
            return;
        }

        var stale = await gameRepository.StaleAsync(400);
        if (stale.Count == 0)
        {
            return;
        }

        var games = await catalog.GetManyAsync(stale, cancellationToken);
        foreach (var game in games)
        {
            await gameRepository.UpsertAsync(GameRow.From(game));
        }

        logger.LogInformation("Catalog refresh: {Count} games fetched again", games.Count);
    }

    private async Task SyncSteamAsync(CancellationToken cancellationToken)
    {
        if (!options.Value.HasSteam || catalog.Source != CatalogSource.Igdb)
        {
            return;
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        foreach (var account in await profileRepository.SteamAccountsAsync())
        {
            if (account.SteamSyncedAt is { } synced && now - DateTime.SpecifyKind(synced, DateTimeKind.Utc) < SyncAfter)
            {
                continue;
            }

            try
            {
                var result = await steamService.SyncAsync(account.UserUniqueID, account.SteamID, cancellationToken);
                if (result is null)
                {
                    logger.LogWarning("Steam sync: no games listed for one account (private game details?)");
                }
            }
            catch (Exception exception) when (exception is SteamUnavailableException or CatalogUnavailableException)
            {
                logger.LogWarning("Steam sync stopped for one account: {Problem}", exception.Message);
            }
        }
    }

    private async Task CheckPricesAsync(CancellationToken cancellationToken)
    {
        if (options.Value.HasItad)
        {
            await priceService.CheckAsync(cancellationToken);
        }
    }

    private async Task StepAsync(string name, Func<CancellationToken, Task> step, CancellationToken cancellationToken)
    {
        try
        {
            await step(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Background job '{Job}' failed; it runs again in half an hour", name);
        }
    }
}
