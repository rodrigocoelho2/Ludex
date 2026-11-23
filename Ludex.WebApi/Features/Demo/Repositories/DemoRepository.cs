using Ludex.WebApi.Extensions.Data;
using Ludex.WebApi.Features.Demo.Models.Entities;

namespace Ludex.WebApi.Features.Demo.Repositories;

/// <summary>
/// The made-up demo accounts (dbo.User.IsDemo) and what they did.
/// </summary>
internal interface IDemoRepository
{
    /// <summary>How many demo accounts and reviews there are.</summary>
    /// <returns>The counts.</returns>
    Task<DemoStatusRow> StatusAsync();

    /// <summary>Creates a demo account (or finds it) with its profile.</summary>
    /// <param name="userName">The user name.</param>
    /// <param name="bio">The profile line.</param>
    /// <param name="location">Where.</param>
    /// <param name="accent">The colour.</param>
    /// <param name="createdAt">When it "joined".</param>
    /// <returns>Its id, or null when a real account has the name.</returns>
    Task<Guid?> CreateUserAsync(string userName, string bio, string location, string accent, DateTime createdAt);

    /// <summary>Deletes every demo account and what it wrote.</summary>
    /// <returns>How many accounts went.</returns>
    Task<int> RemoveAsync();

    /// <summary>A line of a demo account's activity.</summary>
    /// <param name="userId">The account.</param>
    /// <param name="gameId">The game.</param>
    /// <param name="kind">What it did.</param>
    /// <param name="rating">The rating.</param>
    /// <param name="at">When.</param>
    /// <returns>A task.</returns>
    Task ActivityAsync(Guid userId, int gameId, byte kind, byte? rating, DateTime at);

    /// <summary>A demo account likes a review or a list.</summary>
    /// <param name="userId">The account.</param>
    /// <param name="reviewId">The review.</param>
    /// <param name="listId">The list.</param>
    /// <param name="at">When.</param>
    /// <returns>A task.</returns>
    Task LikeAsync(Guid userId, long? reviewId, int? listId, DateTime at);

    /// <summary>A session in a demo account's journal.</summary>
    /// <param name="userId">The account.</param>
    /// <param name="gameId">The game.</param>
    /// <param name="day">The day.</param>
    /// <param name="minutes">How long.</param>
    /// <param name="platform">The platform.</param>
    /// <param name="note">A note.</param>
    /// <returns>A task.</returns>
    Task SessionAsync(Guid userId, int gameId, DateOnly day, int minutes, string? platform, string? note);
}

/// <summary>
/// The demo accounts in SQL Server (stored procedures in Ludex.Database/StoredProcedures/Demo).
/// </summary>
/// <param name="connectionFactory">Opens connections.</param>
internal sealed class DemoRepository(ISqlConnectionFactory connectionFactory) : RepositoryBase(connectionFactory), IDemoRepository
{
    /// <inheritdoc />
    public async Task<DemoStatusRow> StatusAsync() => await QuerySingleOrDefaultAsync<DemoStatusRow>("[dbo].[DemoStatus]") ?? new DemoStatusRow();

    /// <inheritdoc />
    public Task<Guid?> CreateUserAsync(string userName, string bio, string location, string accent, DateTime createdAt) =>
        QuerySingleOrDefaultAsync<Guid?>("[dbo].[DemoUserCreate]", new
        {
            Username = userName,
            SecurityStamp = Guid.NewGuid().ToString("N"),
            Bio = bio,
            Location = location,
            Accent = accent,
            CreatedAt = createdAt,
        });

    /// <inheritdoc />
    public Task<int> RemoveAsync() => ExecuteScalarAsync<int>("[dbo].[DemoRemove]");

    /// <inheritdoc />
    public Task ActivityAsync(Guid userId, int gameId, byte kind, byte? rating, DateTime at) =>
        ExecuteAsync("[dbo].[DemoActivityAdd]", new { UserUniqueID = userId, GameID = gameId, Kind = kind, Rating = rating, At = at });

    /// <inheritdoc />
    public Task LikeAsync(Guid userId, long? reviewId, int? listId, DateTime at) =>
        ExecuteAsync("[dbo].[DemoLikeAdd]", new { UserUniqueID = userId, ReviewID = reviewId, ListID = listId, At = at });

    /// <inheritdoc />
    public Task SessionAsync(Guid userId, int gameId, DateOnly day, int minutes, string? platform, string? note) =>
        ExecuteAsync("[dbo].[DemoSessionAdd]", new { UserUniqueID = userId, GameID = gameId, Day = Date(day), Minutes = minutes, Platform = platform, Note = note });
}
