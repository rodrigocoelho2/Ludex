using Ludex.WebApi.Extensions.Data;

namespace Ludex.WebApi.Features.Prices.Repositories;

/// <summary>A wishlisted game whose price to check (dbo.PriceTargets).</summary>
public sealed class PriceTargetRow
{
    /// <summary>Gets or sets the game.</summary>
    public int GameID { get; set; }

    /// <summary>Gets or sets its name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets IsThereAnyDeal's id (null: not looked up yet).</summary>
    public string? ItadID { get; set; }

    /// <summary>Gets or sets a Steam app of it.</summary>
    public int? SteamAppID { get; set; }
}

/// <summary>A game's best current price, for dbo.PriceSave.</summary>
/// <param name="GameID">The game.</param>
/// <param name="ItadID">IsThereAnyDeal's id.</param>
/// <param name="Amount">The price (null: not sold).</param>
/// <param name="Regular">The regular price.</param>
/// <param name="Cut">The % off.</param>
/// <param name="Currency">The currency.</param>
/// <param name="Shop">The store.</param>
/// <param name="Url">The store's page.</param>
/// <param name="Low">The lowest price ever.</param>
internal sealed record PriceSaveRow(int GameID, string? ItadID, decimal? Amount, decimal? Regular, byte? Cut, string? Currency, string? Shop, string? Url, decimal? Low);

/// <summary>
/// Wishlist prices (dbo.GamePrice) and their alerts.
/// </summary>
internal interface IPriceRepository
{
    /// <summary>The wishlisted games not checked in the last hours.</summary>
    /// <param name="hours">How long a check lasts.</param>
    /// <returns>The games.</returns>
    Task<List<PriceTargetRow>> TargetsAsync(int hours);

    /// <summary>Keeps a game's price and raises the alerts it reaches.</summary>
    /// <param name="price">The price.</param>
    /// <returns>A task.</returns>
    Task SaveAsync(PriceSaveRow price);
}

/// <summary>
/// Wishlist prices in SQL Server (stored procedures in Ludex.Database/StoredProcedures/Prices).
/// </summary>
/// <param name="connectionFactory">Opens connections.</param>
internal sealed class PriceRepository(ISqlConnectionFactory connectionFactory) : RepositoryBase(connectionFactory), IPriceRepository
{
    /// <inheritdoc />
    public Task<List<PriceTargetRow>> TargetsAsync(int hours) => QueryAsync<PriceTargetRow>("[dbo].[PriceTargets]", new { Hours = hours });

    /// <inheritdoc />
    public Task SaveAsync(PriceSaveRow price) => ExecuteAsync("[dbo].[PriceSave]", new
    {
        price.GameID,
        price.ItadID,
        price.Amount,
        price.Regular,
        price.Cut,
        Currency = price.Currency is { Length: 3 } c ? c : null,
        Shop = price.Shop is { Length: > 60 } s ? s[..60] : price.Shop,
        Url = price.Url is { Length: > 500 } ? null : price.Url,
        price.Low,
    });
}
