using Ludex.WebApi.Extensions.Data;
using Ludex.WebApi.Features.Profile.Models.Entities;

namespace Ludex.WebApi.Features.Profile.Repositories;

/// <summary>
/// Profiles (dbo.Profile): bio, location, favourite game, photo and when the account was last seen.
/// </summary>
internal interface IProfileRepository
{
    /// <summary>An account and its profile.</summary>
    /// <param name="userId">The account.</param>
    /// <returns>The profile, or null when the account doesn't exist.</returns>
    Task<ProfileRow?> GetAsync(Guid userId);

    /// <summary>Saves the bio, location and favourite game.</summary>
    /// <param name="userId">The account.</param>
    /// <param name="bio">The bio.</param>
    /// <param name="location">The location.</param>
    /// <param name="favoriteGameId">The favourite game.</param>
    /// <returns>A task.</returns>
    Task SaveAsync(Guid userId, string? bio, string? location, int? favoriteGameId);

    /// <summary>Sets or clears the favourite game.</summary>
    /// <param name="userId">The account.</param>
    /// <param name="gameId">The game (null: clear).</param>
    /// <returns>A task.</returns>
    Task SetFavoriteAsync(Guid userId, int? gameId);

    /// <summary>Sets or clears the colour theme.</summary>
    /// <param name="userId">The account.</param>
    /// <param name="accent">The theme's key (null: the default).</param>
    /// <returns>A task.</returns>
    Task SetAccentAsync(Guid userId, string? accent);

    /// <summary>Saves the photo, or removes it.</summary>
    /// <param name="userId">The account.</param>
    /// <param name="data">The image (null: remove).</param>
    /// <param name="contentType">Its media type.</param>
    /// <returns>The new version (null: no photo).</returns>
    Task<int?> SaveAvatarAsync(Guid userId, byte[]? data, string? contentType);

    /// <summary>The photo.</summary>
    /// <param name="userId">The account.</param>
    /// <returns>The photo, or null when there is none.</returns>
    Task<AvatarRow?> GetAvatarAsync(Guid userId);

    /// <summary>Notes that the account is using the app now.</summary>
    /// <param name="userId">The account.</param>
    /// <returns>A task.</returns>
    Task TouchAsync(Guid userId);

    /// <summary>Remembers the Steam account a library was imported from.</summary>
    /// <param name="userId">The account.</param>
    /// <param name="steamId">The SteamID64.</param>
    /// <returns>A task.</returns>
    Task SetSteamAsync(Guid userId, string steamId);

    /// <summary>The Steam apps the account had at its last import or sync.</summary>
    /// <param name="userId">The account.</param>
    /// <returns>The app ids.</returns>
    Task<List<int>> SteamAppsAsync(Guid userId);

    /// <summary>Remembers Steam apps as seen, and that the account was synced now.</summary>
    /// <param name="userId">The account.</param>
    /// <param name="appIds">The apps owned.</param>
    /// <returns>A task.</returns>
    Task RememberSteamAppsAsync(Guid userId, IReadOnlyCollection<int> appIds);

    /// <summary>The accounts with a Steam library to sync, the least recently synced first.</summary>
    /// <returns>The accounts.</returns>
    Task<List<SteamAccountRow>> SteamAccountsAsync();

    /// <summary>The account's friends, with what they are playing.</summary>
    /// <param name="userId">The account.</param>
    /// <returns>The friends, playing first.</returns>
    Task<List<ProfileFriendRow>> FriendsAsync(Guid userId);

    /// <summary>Saves everything Customize profile sets.</summary>
    /// <param name="userId">The account.</param>
    /// <param name="profile">The values (already checked).</param>
    /// <returns>A task.</returns>
    Task CustomizeAsync(Guid userId, ProfileCustomization profile);

    /// <summary>How long the account played each day since a day.</summary>
    /// <param name="userId">The account.</param>
    /// <param name="from">The first day.</param>
    /// <returns>The days played.</returns>
    Task<List<PlayDayRow>> PlayDaysAsync(Guid userId, DateOnly from);
}

/// <summary>What Customize profile saves (checked by the service).</summary>
internal sealed record ProfileCustomization(
    string? Bio,
    string? Location,
    int? FavoriteGameID,
    string? FavoriteNote,
    string? Accent,
    string? Banner,
    int? BannerGameID,
    string? Frame,
    string? Title,
    string? Tags,
    string? Showcases,
    int? PinnedCollectionID,
    byte? LibraryVisibility,
    bool? ShowPlaying);

/// <summary>
/// Profiles in SQL Server (stored procedures in Ludex.Database/StoredProcedures/Profile).
/// </summary>
/// <param name="connectionFactory">Opens connections.</param>
internal sealed class ProfileRepository(ISqlConnectionFactory connectionFactory) : RepositoryBase(connectionFactory), IProfileRepository
{
    /// <inheritdoc />
    public Task<ProfileRow?> GetAsync(Guid userId) => QuerySingleOrDefaultAsync<ProfileRow>("[dbo].[ProfileGet]", new { UserUniqueID = userId });

    /// <inheritdoc />
    public Task SaveAsync(Guid userId, string? bio, string? location, int? favoriteGameId) =>
        ExecuteAsync("[dbo].[ProfileSave]", new { UserUniqueID = userId, Bio = bio, Location = location, FavoriteGameID = favoriteGameId });

    /// <inheritdoc />
    public Task SetFavoriteAsync(Guid userId, int? gameId) =>
        ExecuteAsync("[dbo].[ProfileSetFavorite]", new { UserUniqueID = userId, FavoriteGameID = gameId });

    /// <inheritdoc />
    public Task SetAccentAsync(Guid userId, string? accent) =>
        ExecuteAsync("[dbo].[ProfileSetAccent]", new { UserUniqueID = userId, Accent = accent });

    /// <inheritdoc />
    public Task<int?> SaveAvatarAsync(Guid userId, byte[]? data, string? contentType) =>
        ExecuteScalarAsync<int?>("[dbo].[ProfileAvatarSave]", new { UserUniqueID = userId, Avatar = data, ContentType = contentType });

    /// <inheritdoc />
    public Task<AvatarRow?> GetAvatarAsync(Guid userId) => QuerySingleOrDefaultAsync<AvatarRow>("[dbo].[ProfileAvatarGet]", new { UserUniqueID = userId });

    /// <inheritdoc />
    public Task TouchAsync(Guid userId) => ExecuteAsync("[dbo].[ProfileTouch]", new { UserUniqueID = userId });

    /// <inheritdoc />
    public Task SetSteamAsync(Guid userId, string steamId) => ExecuteAsync("[dbo].[ProfileSetSteam]", new { UserUniqueID = userId, SteamID = steamId });

    /// <inheritdoc />
    public Task<List<int>> SteamAppsAsync(Guid userId) => QueryAsync<int>("[dbo].[SteamOwnedList]", new { UserUniqueID = userId });

    /// <inheritdoc />
    public Task RememberSteamAppsAsync(Guid userId, IReadOnlyCollection<int> appIds) =>
        ExecuteAsync("[dbo].[SteamOwnedRemember]", new { UserUniqueID = userId, AppIDs = Json(appIds.Distinct()) });

    /// <inheritdoc />
    public Task<List<SteamAccountRow>> SteamAccountsAsync() => QueryAsync<SteamAccountRow>("[dbo].[SteamAccountList]");

    /// <inheritdoc />
    public Task<List<ProfileFriendRow>> FriendsAsync(Guid userId) => QueryAsync<ProfileFriendRow>("[dbo].[ProfileFriends]", new { UserUniqueID = userId });

    /// <inheritdoc />
    public Task CustomizeAsync(Guid userId, ProfileCustomization profile) =>
        ExecuteAsync("[dbo].[ProfileCustomize]", new
        {
            UserUniqueID = userId,
            profile.Bio,
            profile.Location,
            profile.FavoriteGameID,
            profile.FavoriteNote,
            profile.Accent,
            profile.Banner,
            profile.BannerGameID,
            profile.Frame,
            profile.Title,
            profile.Tags,
            profile.Showcases,
            profile.PinnedCollectionID,
            profile.LibraryVisibility,
            profile.ShowPlaying,
        });

    /// <inheritdoc />
    public Task<List<PlayDayRow>> PlayDaysAsync(Guid userId, DateOnly from) =>
        QueryAsync<PlayDayRow>("[dbo].[ProfilePlayDays]", new { UserUniqueID = userId, From = from.ToDateTime(TimeOnly.MinValue) });
}
