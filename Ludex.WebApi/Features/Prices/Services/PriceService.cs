using Ludex.WebApi.Features.Prices.Repositories;

namespace Ludex.WebApi.Features.Prices.Services;

/// <summary>
/// Checks the wishlist prices (the daily job): the best current PC price of every wishlisted game, and alerts.
/// </summary>
/// <param name="itad">IsThereAnyDeal.</param>
/// <param name="priceRepository">Prices.</param>
/// <param name="timeProvider">The clock (pauses).</param>
/// <param name="logger">Logger.</param>
internal sealed class PriceService(ItadClient itad, IPriceRepository priceRepository, TimeProvider timeProvider, ILogger<PriceService> logger)
{
    /// <summary>A price is checked again after this many hours.</summary>
    public const int CheckEveryHours = 20;

    /// <summary>
    /// Checks the prices that are due.
    /// </summary>
    /// <param name="cancellationToken">Stops the run.</param>
    /// <returns>How many games were checked.</returns>
    public async Task<int> CheckAsync(CancellationToken cancellationToken = default)
    {
        var targets = await priceRepository.TargetsAsync(CheckEveryHours);
        if (targets.Count == 0)
        {
            return 0;
        }

        // IsThereAnyDeal's id, once per game.
        foreach (var target in targets.Where(static t => t.ItadID is null))
        {
            cancellationToken.ThrowIfCancellationRequested();
            target.ItadID = await itad.LookupAsync(target.Name, target.SteamAppID, cancellationToken)
                            ?? (target.SteamAppID is not null ? await itad.LookupAsync(target.Name, null, cancellationToken) : null);
            await Task.Delay(TimeSpan.FromMilliseconds(250), timeProvider, cancellationToken);
        }

        var checkedCount = 0;
        foreach (var chunk in targets.Where(static t => t.ItadID is not null).Chunk(100))
        {
            var prices = (await itad.PricesAsync([.. chunk.Select(static t => t.ItadID!)], cancellationToken))
                .Where(static p => p.Id is not null)
                .ToDictionary(static p => p.Id!, StringComparer.Ordinal);
            foreach (var target in chunk)
            {
                var row = PriceRules.Best(target.GameID, target.ItadID!, prices.GetValueOrDefault(target.ItadID!));
                await priceRepository.SaveAsync(row);
                checkedCount++;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(500), timeProvider, cancellationToken);
        }

        // Games IsThereAnyDeal doesn't know: remembered as checked, so they aren't looked up every run.
        foreach (var target in targets.Where(static t => t.ItadID is null))
        {
            await priceRepository.SaveAsync(new PriceSaveRow(target.GameID, null, null, null, null, null, null, null, null));
        }

        logger.LogInformation("Wishlist prices: {Count} checked", checkedCount);
        return checkedCount;
    }
}

/// <summary>
/// The rules of prices, kept apart so the tests can check them.
/// </summary>
internal static class PriceRules
{
    /// <summary>
    /// The best current price of a game: the cheapest store, its regular price and cut, and the lowest ever.
    /// </summary>
    /// <param name="gameId">The game.</param>
    /// <param name="itadId">IsThereAnyDeal's id.</param>
    /// <param name="prices">What IsThereAnyDeal answered (null: nothing).</param>
    /// <returns>The row to save (Amount null: not sold anywhere it follows).</returns>
    public static PriceSaveRow Best(int gameId, string itadId, ItadGamePrices? prices)
    {
        var best = (prices?.Deals ?? [])
            .Where(static d => d.Price is not null)
            .OrderBy(static d => d.Price!.Amount)
            .ThenByDescending(static d => d.Cut)
            .FirstOrDefault();
        var low = prices?.HistoryLow?.All?.Amount;
        return best is null
            ? new PriceSaveRow(gameId, itadId, null, null, null, null, null, null, low)
            : new PriceSaveRow(
                gameId,
                itadId,
                best.Price!.Amount,
                best.Regular?.Amount,
                (byte)Math.Clamp(best.Cut, 0, 100),
                best.Price.Currency,
                best.Shop?.Name,
                best.Url,
                low);
    }
}
