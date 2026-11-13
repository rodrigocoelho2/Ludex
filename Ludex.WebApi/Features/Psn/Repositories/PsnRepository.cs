using Ludex.WebApi.Extensions.Data;

namespace Ludex.WebApi.Features.Psn.Repositories;

/// <summary>An account's PlayStation connection (dbo.PsnAccountGet / dbo.PsnAccountList).</summary>
public sealed class PsnAccountRow
{
    /// <summary>Gets or sets the account.</summary>
    public Guid UserUniqueID { get; set; }

    /// <summary>Gets or sets the PSN name.</summary>
    public string? OnlineID { get; set; }

    /// <summary>Gets or sets the encrypted refresh token (null: the connection broke).</summary>
    public string? Token { get; set; }

    /// <summary>Gets or sets when the refresh token runs out (UTC).</summary>
    public DateTime? TokenExpiresAt { get; set; }

    /// <summary>Gets or sets when it was connected (UTC).</summary>
    public DateTime ConnectedAt { get; set; }

    /// <summary>Gets or sets when the library was last synced (UTC; null: never imported).</summary>
    public DateTime? SyncedAt { get; set; }

    /// <summary>Gets or sets when the bell last warned about the connection (UTC).</summary>
    public DateTime? WarnedAt { get; set; }
}

/// <summary>A PlayStation game seen (dbo.PsnTitleList).</summary>
public sealed class PsnTitleRow
{
    /// <summary>Gets or sets the title's key.</summary>
    public string TitleKey { get; set; } = string.Empty;

    /// <summary>Gets or sets its name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets its game in the library (null: left out, or not matched).</summary>
    public int? GameID { get; set; }
}

/// <summary>A PlayStation game to remember (sent as JSON to dbo.PsnTitleRemember).</summary>
/// <param name="TitleKey">The title's key.</param>
/// <param name="Name">Its name.</param>
/// <param name="GameID">Its game (null: none).</param>
internal sealed record PsnTitleRemember(string TitleKey, string Name, int? GameID);

/// <summary>
/// PlayStation connections and the games they have shown.
/// </summary>
internal interface IPsnRepository
{
    /// <summary>An account's connection.</summary>
    /// <param name="userId">The account.</param>
    /// <returns>The connection, or null when there's none.</returns>
    Task<PsnAccountRow?> GetAsync(Guid userId);

    /// <summary>Every connection that still has a token, the one synced longest ago first.</summary>
    /// <returns>The connections.</returns>
    Task<List<PsnAccountRow>> ListAsync();

    /// <summary>Saves a connection (a null token marks it broken).</summary>
    /// <param name="userId">The account.</param>
    /// <param name="onlineId">The PSN name (null: keep).</param>
    /// <param name="token">The encrypted refresh token.</param>
    /// <param name="expiresAt">When it runs out (UTC).</param>
    /// <returns>A task.</returns>
    Task SaveAsync(Guid userId, string? onlineId, string? token, DateTime? expiresAt);

    /// <summary>Records a warning on the bell.</summary>
    /// <param name="userId">The account.</param>
    /// <returns>A task.</returns>
    Task MarkWarnedAsync(Guid userId);

    /// <summary>Removes the connection and the games it showed (the games in the library stay).</summary>
    /// <param name="userId">The account.</param>
    /// <returns>A task.</returns>
    Task DeleteAsync(Guid userId);

    /// <summary>The games the connection has shown.</summary>
    /// <param name="userId">The account.</param>
    /// <returns>The titles.</returns>
    Task<List<PsnTitleRow>> TitlesAsync(Guid userId);

    /// <summary>Remembers games shown (a game id given replaces none), and marks the library synced.</summary>
    /// <param name="userId">The account.</param>
    /// <param name="titles">The titles.</param>
    /// <returns>A task.</returns>
    Task RememberAsync(Guid userId, IReadOnlyCollection<PsnTitleRemember> titles);
}

/// <summary>
/// PlayStation connections in SQL Server (stored procedures in Ludex.Database/StoredProcedures/Psn).
/// </summary>
/// <param name="connectionFactory">Opens connections.</param>
internal sealed class PsnRepository(ISqlConnectionFactory connectionFactory) : RepositoryBase(connectionFactory), IPsnRepository
{
    /// <inheritdoc />
    public Task<PsnAccountRow?> GetAsync(Guid userId) => QuerySingleOrDefaultAsync<PsnAccountRow>("[dbo].[PsnAccountGet]", new { UserUniqueID = userId });

    /// <inheritdoc />
    public Task<List<PsnAccountRow>> ListAsync() => QueryAsync<PsnAccountRow>("[dbo].[PsnAccountList]");

    /// <inheritdoc />
    public Task SaveAsync(Guid userId, string? onlineId, string? token, DateTime? expiresAt) =>
        ExecuteAsync("[dbo].[PsnAccountSave]", new { UserUniqueID = userId, OnlineID = onlineId, Token = token, TokenExpiresAt = expiresAt });

    /// <inheritdoc />
    public Task MarkWarnedAsync(Guid userId) => ExecuteAsync("[dbo].[PsnAccountMark]", new { UserUniqueID = userId, Synced = false, Warned = true });

    /// <inheritdoc />
    public Task DeleteAsync(Guid userId) => ExecuteAsync("[dbo].[PsnAccountDelete]", new { UserUniqueID = userId });

    /// <inheritdoc />
    public Task<List<PsnTitleRow>> TitlesAsync(Guid userId) => QueryAsync<PsnTitleRow>("[dbo].[PsnTitleList]", new { UserUniqueID = userId });

    /// <inheritdoc />
    public Task RememberAsync(Guid userId, IReadOnlyCollection<PsnTitleRemember> titles) =>
        ExecuteAsync("[dbo].[PsnTitleRemember]", new { UserUniqueID = userId, Titles = Json(titles) });
}
