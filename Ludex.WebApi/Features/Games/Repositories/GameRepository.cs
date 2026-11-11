using Ludex.WebApi.Extensions.Data;
using Ludex.WebApi.Features.Games.Models.Entities;

namespace Ludex.WebApi.Features.Games.Repositories;

/// <summary>
/// The games copied from the catalog (dbo.Game).
/// </summary>
internal interface IGameRepository
{
    /// <summary>Gets a game.</summary>
    /// <param name="gameId">The id.</param>
    /// <returns>The game, or null when it was never copied.</returns>
    Task<GameRow?> GetAsync(int gameId);

    /// <summary>Saves a game (inserts or refreshes it).</summary>
    /// <param name="game">The game.</param>
    /// <returns>The saved game.</returns>
    Task<GameRow> UpsertAsync(GameRow game);

    /// <summary>Games with a cover, the most owned first (for the sign-in page).</summary>
    /// <param name="take">At most this many.</param>
    /// <returns>The games.</returns>
    Task<List<GameRow>> CoverWallAsync(int take);

    /// <summary>Games in someone's library whose data is older than <see cref="GameRow.CurrentDataVersion"/>.</summary>
    /// <param name="take">At most this many.</param>
    /// <returns>Their ids.</returns>
    Task<List<int>> StaleAsync(int take);
}

/// <summary>
/// The games in SQL Server (stored procedures in Ludex.Database/StoredProcedures/Games).
/// </summary>
/// <param name="connectionFactory">Opens connections.</param>
internal sealed class GameRepository(ISqlConnectionFactory connectionFactory) : RepositoryBase(connectionFactory), IGameRepository
{
    /// <inheritdoc />
    public Task<GameRow?> GetAsync(int gameId) => QuerySingleOrDefaultAsync<GameRow>("[dbo].[GameGet]", new { GameID = gameId });

    /// <inheritdoc />
    public async Task<GameRow> UpsertAsync(GameRow game) =>
        (await QuerySingleOrDefaultAsync<GameRow>("[dbo].[GameUpsert]", new
        {
            game.GameID,
            game.Name,
            game.Slug,
            game.Summary,
            game.CoverImageID,
            game.HeroImageID,
            game.Screenshots,
            game.FirstReleaseDate,
            game.Developer,
            game.Publisher,
            game.Genres,
            game.Platforms,
            game.Modes,
            game.CriticScore,
            game.TimeToBeatMain,
            game.TimeToBeatFull,
            game.DataVersion,
        }))!;

    /// <inheritdoc />
    public Task<List<GameRow>> CoverWallAsync(int take) => QueryAsync<GameRow>("[dbo].[GameCoverWall]", new { Take = take });

    /// <inheritdoc />
    public Task<List<int>> StaleAsync(int take) => QueryAsync<int>("[dbo].[GameStaleList]", new { DataVersion = GameRow.CurrentDataVersion, Take = take });
}
