using Ludex.WebApi.Extensions.Data;
using Ludex.WebApi.Features.Journal.Models.Entities;

namespace Ludex.WebApi.Features.Journal.Repositories;

/// <summary>
/// The journal (dbo.PlaySession, and the syncs' dbo.PlaytimeDay).
/// </summary>
internal interface IJournalRepository
{
    /// <summary>Writes down a session.</summary>
    /// <param name="userId">The account.</param>
    /// <param name="gameId">The game (in dbo.Game).</param>
    /// <param name="day">The day.</param>
    /// <param name="minutes">How long.</param>
    /// <param name="platform">The platform.</param>
    /// <param name="note">A note.</param>
    /// <returns>The session id.</returns>
    Task<long> AddAsync(Guid userId, int gameId, DateOnly day, int minutes, string? platform, string? note);

    /// <summary>Deletes one of the account's sessions.</summary>
    /// <param name="userId">The account.</param>
    /// <param name="sessionId">The session.</param>
    /// <returns>A task.</returns>
    Task DeleteAsync(Guid userId, long sessionId);

    /// <summary>The journal between two days.</summary>
    /// <param name="userId">The account.</param>
    /// <param name="from">The first day.</param>
    /// <param name="to">The last day.</param>
    /// <param name="gameId">Only this game.</param>
    /// <returns>The lines, newest first.</returns>
    Task<List<JournalRow>> ListAsync(Guid userId, DateOnly from, DateOnly to, int? gameId = null);
}

/// <summary>
/// The journal in SQL Server (stored procedures in Ludex.Database/StoredProcedures/Journal).
/// </summary>
/// <param name="connectionFactory">Opens connections.</param>
internal sealed class JournalRepository(ISqlConnectionFactory connectionFactory) : RepositoryBase(connectionFactory), IJournalRepository
{
    /// <inheritdoc />
    public Task<long> AddAsync(Guid userId, int gameId, DateOnly day, int minutes, string? platform, string? note) =>
        ExecuteScalarAsync<long>("[dbo].[SessionAdd]", new
        {
            UserUniqueID = userId,
            GameID = gameId,
            Day = Date(day),
            Minutes = minutes,
            Platform = platform,
            Note = note,
        });

    /// <inheritdoc />
    public Task DeleteAsync(Guid userId, long sessionId) => ExecuteAsync("[dbo].[SessionDelete]", new { UserUniqueID = userId, SessionID = sessionId });

    /// <inheritdoc />
    public Task<List<JournalRow>> ListAsync(Guid userId, DateOnly from, DateOnly to, int? gameId = null) =>
        QueryAsync<JournalRow>("[dbo].[JournalList]", new { UserUniqueID = userId, From = Date(from), To = Date(to), GameID = gameId });
}
