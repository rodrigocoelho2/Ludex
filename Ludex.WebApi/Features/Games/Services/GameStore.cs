using Ludex.Library.Enums;
using Ludex.WebApi.Features.Catalog;
using Ludex.WebApi.Features.Games.Models.Entities;
using Ludex.WebApi.Features.Games.Repositories;
using Microsoft.Extensions.Options;

namespace Ludex.WebApi.Features.Games.Services;

/// <summary>
/// A game's data, from the database when it's there and fresh, otherwise from the catalog (and then saved).
/// </summary>
/// <param name="gameRepository">The saved games.</param>
/// <param name="catalog">The catalog.</param>
/// <param name="options">How long saved data stays fresh.</param>
/// <param name="timeProvider">The clock.</param>
/// <param name="logger">Logger.</param>
internal sealed class GameStore(
    IGameRepository gameRepository,
    IGameCatalog catalog,
    IOptions<LudexOptions> options,
    TimeProvider timeProvider,
    ILogger<GameStore> logger)
{
    /// <summary>
    /// Gets a game, copying it from the catalog the first time and refreshing it when its data is old.
    /// When the catalog can't be reached, the saved copy is used.
    /// </summary>
    /// <param name="gameId">The game.</param>
    /// <returns>The game, or null when neither the database nor the catalog know it.</returns>
    /// <exception cref="CatalogUnavailableException">The game isn't saved yet and the catalog can't be reached.</exception>
    public async Task<GameRow?> GetAsync(int gameId)
    {
        var row = await gameRepository.GetAsync(gameId);
        if (row is not null && !IsStale(row))
        {
            return row;
        }

        CatalogGame? game;
        try
        {
            // A game of the built-in list keeps its data even after IGDB is set up.
            game = gameId >= OfflineCatalog.FirstId
                ? OfflineCatalog.All.FirstOrDefault(g => g.Id == gameId)
                : await catalog.GetAsync(gameId);
        }
        catch (CatalogUnavailableException exception) when (row is not null)
        {
            logger.LogWarning("Using the saved data of game {GameId}: {Problem}", gameId, exception.Message);
            return row;
        }

        return game is null ? row : await gameRepository.UpsertAsync(GameRow.From(game));
    }

    private bool IsStale(GameRow row)
    {
        if (row.GameID >= OfflineCatalog.FirstId || catalog.Source != CatalogSource.Igdb)
        {
            return false;
        }

        if (row.DataVersion < GameRow.CurrentDataVersion)
        {
            return true;
        }

        var fetchedAt = DateTime.SpecifyKind(row.FetchedAt, DateTimeKind.Utc);
        return timeProvider.GetUtcNow().UtcDateTime - fetchedAt > TimeSpan.FromDays(Math.Max(1, options.Value.RefreshDays));
    }
}
