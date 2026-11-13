using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Ludex.Library.Validation;
using Ludex.Sdk.Models;

namespace Ludex.Sdk;

/// <summary>
/// Typed client of Ludex.WebApi. Method names follow the API operation ids ({Feature}{Operation}Async).
/// Register with <see cref="ServiceCollectionExtensions.AddLudexClient"/>, which sets the base address and the x-api-key header.
/// </summary>
/// <param name="httpClient">The configured HttpClient.</param>
public sealed class LudexClient(HttpClient httpClient)
{
    private static readonly JsonSerializerOptions SerializerOptions = CreateSerializerOptions();

    // ----- User -----

    /// <summary>How many accounts exist.</summary>
    /// <returns>The status.</returns>
    public Task<UserStatusDTO> UserStatusAsync() => GetAsync<UserStatusDTO>("user/status");

    /// <summary>Lists every account.</summary>
    /// <returns>The accounts.</returns>
    public Task<List<UserDTO>> UserListAsync() => GetAsync<List<UserDTO>>("user/list");

    /// <summary>Gets one account.</summary>
    /// <param name="identifier">The account id.</param>
    /// <returns>The account.</returns>
    public Task<UserDTO> UserGetAsync(string identifier) => GetAsync<UserDTO>($"user?identifier={Uri.EscapeDataString(identifier)}");

    /// <summary>Creates an account (the first one is an admin).</summary>
    /// <param name="item">User name and password.</param>
    /// <returns>The account.</returns>
    public Task<UserDTO> UserCreateAsync(UserCreateRequest item) => PostAsync<UserDTO>("user", item);

    /// <summary>Checks a user name and password.</summary>
    /// <param name="item">User name and password.</param>
    /// <returns>The account.</returns>
    public Task<UserDTO> UserSignInAsync(UserSignInRequest item) => PostAsync<UserDTO>("user/signin", item);

    /// <summary>Finds the account a Discord account is linked to (a <see cref="LudexApiException"/> with 404 when none).</summary>
    /// <param name="item">The Discord account's id.</param>
    /// <returns>The account.</returns>
    public Task<UserDTO> UserDiscordSignInAsync(UserDiscordSignInRequest item) => PostAsync<UserDTO>("user/discord/signin", item);

    /// <summary>Creates an account that signs in with Discord (no password until one is set).</summary>
    /// <param name="item">The user name and the Discord account.</param>
    /// <returns>The account.</returns>
    public Task<UserDTO> UserDiscordCreateAsync(UserDiscordCreateRequest item) => PostAsync<UserDTO>("user/discord/create", item);

    /// <summary>Links a Discord account to an account, or unlinks it (no DiscordId).</summary>
    /// <param name="item">The account and the Discord account.</param>
    /// <returns>The account.</returns>
    public Task<UserDTO> UserDiscordLinkAsync(UserDiscordLinkRequest item) => PostAsync<UserDTO>("user/discord", item);

    /// <summary>Changes a password.</summary>
    /// <param name="item">The account, current and new password.</param>
    /// <returns>The account.</returns>
    public Task<UserDTO> UserPasswordAsync(UserPasswordRequest item) => PostAsync<UserDTO>("user/password", item);

    /// <summary>Deletes an account and its data.</summary>
    /// <param name="identifier">The account id.</param>
    /// <returns>A task that completes on success.</returns>
    public Task UserDeleteAsync(string identifier) => DeleteAsync($"user?identifier={Uri.EscapeDataString(identifier)}");

    // ----- Games -----

    /// <summary>Searches the catalog.</summary>
    /// <param name="userId">The account.</param>
    /// <param name="query">The text typed.</param>
    /// <returns>The games.</returns>
    public Task<SearchResultDTO> GamesSearchAsync(string userId, string? query) =>
        GetAsync<SearchResultDTO>($"games/search?userId={Uri.EscapeDataString(userId)}&query={Uri.EscapeDataString(query ?? string.Empty)}");

    /// <summary>Everything the game page shows.</summary>
    /// <param name="userId">The account.</param>
    /// <param name="gameId">The game.</param>
    /// <param name="entryId">The account's copy to show (null: the first one).</param>
    /// <returns>The game.</returns>
    public Task<GameDetailDTO> GamesDetailAsync(string userId, int gameId, long? entryId = null) =>
        GetAsync<GameDetailDTO>($"games/detail?userId={Uri.EscapeDataString(userId)}&gameId={gameId.ToString(CultureInfo.InvariantCulture)}"
                                + (entryId is { } entry ? $"&entryId={entry.ToString(CultureInfo.InvariantCulture)}" : string.Empty));

    /// <summary>Covers for the sign-in page.</summary>
    /// <returns>The covers.</returns>
    public Task<CoverWallDTO> GamesWallAsync() => GetAsync<CoverWallDTO>("games/wall");

    // ----- Library -----

    /// <summary>The library.</summary>
    /// <param name="userId">The account.</param>
    /// <returns>Every game and the counts.</returns>
    public Task<LibraryDTO> LibraryListAsync(string userId) => GetAsync<LibraryDTO>($"library?userId={Uri.EscapeDataString(userId)}");

    /// <summary>Adds a game or changes it.</summary>
    /// <param name="item">The game and its values.</param>
    /// <returns>The entry.</returns>
    public Task<LibraryEntryDTO> LibrarySaveAsync(LibrarySaveRequest item) => PostAsync<LibraryEntryDTO>("library", item);

    /// <summary>Sets only the status (adds the game when it isn't there).</summary>
    /// <param name="item">The game and its status.</param>
    /// <returns>The entry.</returns>
    public Task<LibraryEntryDTO> LibraryStatusAsync(LibraryStatusRequest item) => PostAsync<LibraryEntryDTO>("library/status", item);

    /// <summary>Takes a game, or one of its copies, out of the library.</summary>
    /// <param name="userId">The account.</param>
    /// <param name="gameId">The game.</param>
    /// <param name="entryId">The copy (null: every copy of the game).</param>
    /// <returns>A task that completes on success.</returns>
    public Task LibraryDeleteAsync(string userId, int gameId, long? entryId = null) =>
        DeleteAsync($"library?userId={Uri.EscapeDataString(userId)}&gameId={gameId.ToString(CultureInfo.InvariantCulture)}"
                    + (entryId is { } entry ? $"&entryId={entry.ToString(CultureInfo.InvariantCulture)}" : string.Empty));

    /// <summary>Empties the library, to import everything again.</summary>
    /// <param name="userId">The account.</param>
    /// <param name="confirm">The user name typed to confirm.</param>
    /// <returns>How many entries went.</returns>
    public Task<LibraryClearDTO> LibraryClearAsync(string userId, string? confirm) =>
        PostAsync<LibraryClearDTO>("library/clear", new LibraryClearRequest { UserId = userId, Confirm = confirm });

    /// <summary>The home page.</summary>
    /// <param name="userId">The account.</param>
    /// <returns>The home page.</returns>
    public Task<HomeDTO> LibraryHomeAsync(string userId) => GetAsync<HomeDTO>($"library/home?userId={Uri.EscapeDataString(userId)}");

    // ----- Friends -----

    /// <summary>The friends page.</summary>
    /// <param name="userId">The account.</param>
    /// <returns>Friends, requests, activity and favourite games.</returns>
    public Task<FriendsDTO> FriendsListAsync(string userId) => GetAsync<FriendsDTO>($"friends?userId={Uri.EscapeDataString(userId)}");

    /// <summary>Asks an account to be friends, by user name.</summary>
    /// <param name="item">The account asking and the user name.</param>
    /// <returns>The friends page.</returns>
    public Task<FriendsDTO> FriendsRequestAsync(FriendRequestRequest item) => PostAsync<FriendsDTO>("friends/request", item);

    /// <summary>Accepts a request.</summary>
    /// <param name="item">The account and the one that asked.</param>
    /// <returns>The friends page.</returns>
    public Task<FriendsDTO> FriendsAcceptAsync(FriendActionRequest item) => PostAsync<FriendsDTO>("friends/accept", item);

    /// <summary>Declines or cancels a request, or ends a friendship.</summary>
    /// <param name="item">The account and the other one.</param>
    /// <returns>The friends page.</returns>
    public Task<FriendsDTO> FriendsRemoveAsync(FriendActionRequest item) => PostAsync<FriendsDTO>("friends/remove", item);

    // ----- Profile -----

    /// <summary>A profile as an account sees it.</summary>
    /// <param name="viewerId">The signed-in account.</param>
    /// <param name="userName">Whose profile (null: the viewer's own).</param>
    /// <returns>The profile.</returns>
    public Task<ProfileDTO> ProfileGetAsync(string viewerId, string? userName = null) =>
        GetAsync<ProfileDTO>($"profile?viewerId={Uri.EscapeDataString(viewerId)}&userName={Uri.EscapeDataString(userName ?? string.Empty)}");

    /// <summary>Saves the bio, location and favourite game.</summary>
    /// <param name="item">The account and the values.</param>
    /// <returns>The profile.</returns>
    public Task<ProfileDTO> ProfileSaveAsync(ProfileSaveRequest item) => PostAsync<ProfileDTO>("profile/save", item);

    /// <summary>Saves everything Customize profile sets.</summary>
    /// <param name="item">The account and the values.</param>
    /// <returns>The profile.</returns>
    public Task<ProfileDTO> ProfileCustomizeAsync(ProfileCustomizeRequest item) => PostAsync<ProfileDTO>("profile/customize", item);

    /// <summary>Makes a game the favourite, or clears it.</summary>
    /// <param name="item">The account and the game (null: clear).</param>
    /// <returns>The favourite now.</returns>
    public Task<ProfileFavoriteRequest> ProfileFavoriteAsync(ProfileFavoriteRequest item) => PostAsync<ProfileFavoriteRequest>("profile/favorite", item);

    /// <summary>Sets the colour theme.</summary>
    /// <param name="item">The account and the theme.</param>
    /// <returns>The theme now.</returns>
    public Task<ProfileAccentRequest> ProfileAccentAsync(ProfileAccentRequest item) => PostAsync<ProfileAccentRequest>("profile/accent", item);

    /// <summary>Sets the photo, or removes it (no data).</summary>
    /// <param name="item">The account and the image.</param>
    /// <returns>The new version.</returns>
    public Task<AvatarDTO> ProfileAvatarSaveAsync(ProfileAvatarRequest item) => PostAsync<AvatarDTO>("profile/avatar", item);

    /// <summary>A photo.</summary>
    /// <param name="userId">The account.</param>
    /// <returns>The image (a <see cref="LudexApiException"/> with 404 when there is none).</returns>
    public Task<AvatarDTO> ProfileAvatarGetAsync(string userId) => GetAsync<AvatarDTO>($"profile/avatar?userId={Uri.EscapeDataString(userId)}");

    /// <summary>Notes that the account is using the app now ("last seen").</summary>
    /// <param name="userId">The account.</param>
    /// <returns>A task.</returns>
    public async Task ProfileTouchAsync(string userId)
    {
        using var response = await httpClient.PostAsJsonAsync("profile/touch", new ProfileUserRequest { UserId = userId }, SerializerOptions);
        await EnsureSuccessAsync(response);
    }

    // ----- Steam -----

    /// <summary>Reads a Steam library and matches it to the catalog.</summary>
    /// <param name="userId">The account.</param>
    /// <param name="profile">The Steam profile typed (null: the one imported from last time).</param>
    /// <returns>The games with a suggested status (a <see cref="LudexApiException"/> with 400 when the profile can't be read).</returns>
    public Task<SteamPreviewDTO> SteamPreviewAsync(string userId, string? profile = null) =>
        GetAsync<SteamPreviewDTO>($"steam?userId={Uri.EscapeDataString(userId)}&profile={Uri.EscapeDataString(profile ?? string.Empty)}");

    /// <summary>Adds the games ticked.</summary>
    /// <param name="item">The account, the Steam account and the games.</param>
    /// <returns>How many were added and updated.</returns>
    public Task<SteamImportResultDTO> SteamImportAsync(SteamImportRequest item) => PostAsync<SteamImportResultDTO>("steam/import", item);

    /// <summary>Syncs the library with Steam now.</summary>
    /// <param name="userId">The account.</param>
    /// <returns>What changed.</returns>
    public Task<SteamSyncResultDTO> SteamSyncAsync(string userId) => PostAsync<SteamSyncResultDTO>("steam/sync", new SteamSyncRequest { UserId = userId });

    /// <summary>The account's Steam achievements in a game.</summary>
    /// <param name="userId">The account.</param>
    /// <param name="gameId">The game.</param>
    /// <returns>The achievements (a <see cref="LudexApiException"/> with 404 when it isn't a Steam game or has none).</returns>
    public Task<AchievementsDTO> SteamAchievementsAsync(string userId, int gameId) =>
        GetAsync<AchievementsDTO>($"steam/achievements?userId={Uri.EscapeDataString(userId)}&gameId={gameId}");

    // ----- Playnite -----

    /// <summary>Reads Playnite's export (Library Exporter Advanced) and matches it to the catalog.</summary>
    /// <param name="userId">The account.</param>
    /// <param name="csv">The CSV.</param>
    /// <returns>The games with a suggested status (Problem: why the file can't be read).</returns>
    public Task<PlaynitePreviewDTO> PlaynitePreviewAsync(string userId, string? csv) =>
        PostAsync<PlaynitePreviewDTO>("playnite/preview", new PlaynitePreviewRequest { UserId = userId, Csv = csv });

    /// <summary>Adds the Playnite games ticked.</summary>
    /// <param name="item">The account and the games.</param>
    /// <returns>How many were added and updated.</returns>
    public Task<ImportResultDTO> PlayniteImportAsync(PlayniteImportRequest item) => PostAsync<ImportResultDTO>("playnite/import", item);

    // ----- PlayStation -----

    /// <summary>The account's PlayStation connection.</summary>
    /// <param name="userId">The account.</param>
    /// <returns>The connection.</returns>
    public Task<PsnStatusDTO> PsnStatusAsync(string userId) => GetAsync<PsnStatusDTO>($"psn?userId={Uri.EscapeDataString(userId)}");

    /// <summary>Connects PlayStation with an NPSSO.</summary>
    /// <param name="userId">The account.</param>
    /// <param name="npsso">The NPSSO (or the whole page that shows it).</param>
    /// <returns>The connection (a <see cref="LudexApiException"/> with 400 when Sony refuses the NPSSO).</returns>
    public Task<PsnStatusDTO> PsnConnectAsync(string userId, string? npsso) =>
        PostAsync<PsnStatusDTO>("psn/connect", new PsnConnectRequest { UserId = userId, Npsso = npsso });

    /// <summary>Reads the PlayStation games played and matches them to the catalog.</summary>
    /// <param name="userId">The account.</param>
    /// <returns>The games with a suggested status (Status.Problem: what's in the way).</returns>
    public Task<PsnPreviewDTO> PsnPreviewAsync(string userId) => GetAsync<PsnPreviewDTO>($"psn/preview?userId={Uri.EscapeDataString(userId)}");

    /// <summary>Adds the PlayStation games ticked.</summary>
    /// <param name="item">The account and the games.</param>
    /// <returns>How many were added and updated.</returns>
    public Task<ImportResultDTO> PsnImportAsync(PsnImportRequest item) => PostAsync<ImportResultDTO>("psn/import", item);

    /// <summary>Syncs the library with PlayStation now.</summary>
    /// <param name="userId">The account.</param>
    /// <returns>What changed.</returns>
    public Task<ImportResultDTO> PsnSyncAsync(string userId) => PostAsync<ImportResultDTO>("psn/sync", new PsnAccountRequest { UserId = userId });

    /// <summary>The account's trophies in a game it played on PlayStation.</summary>
    /// <param name="userId">The account.</param>
    /// <param name="gameId">The game.</param>
    /// <returns>The trophies (a <see cref="LudexApiException"/> with 404 when no trophy list is known).</returns>
    public Task<PsnTrophiesDTO> PsnTrophiesAsync(string userId, int gameId) =>
        GetAsync<PsnTrophiesDTO>($"psn/trophies?userId={Uri.EscapeDataString(userId)}&gameId={gameId.ToString(CultureInfo.InvariantCulture)}");

    /// <summary>Disconnects PlayStation (the games imported stay).</summary>
    /// <param name="userId">The account.</param>
    /// <returns>The connection (none).</returns>
    public Task<PsnStatusDTO> PsnDisconnectAsync(string userId) => PostAsync<PsnStatusDTO>("psn/disconnect", new PsnAccountRequest { UserId = userId });

    // ----- Collections -----

    /// <summary>The account's collections.</summary>
    /// <param name="userId">The account.</param>
    /// <returns>The collections.</returns>
    public Task<List<CollectionDTO>> CollectionsListAsync(string userId) => GetAsync<List<CollectionDTO>>($"collections?userId={Uri.EscapeDataString(userId)}");

    /// <summary>Creates or renames a collection.</summary>
    /// <param name="item">The account, the collection and the name.</param>
    /// <returns>The collections.</returns>
    public Task<List<CollectionDTO>> CollectionsSaveAsync(CollectionSaveRequest item) => PostAsync<List<CollectionDTO>>("collections/save", item);

    /// <summary>Deletes a collection.</summary>
    /// <param name="item">The account and the collection.</param>
    /// <returns>The collections.</returns>
    public Task<List<CollectionDTO>> CollectionsDeleteAsync(CollectionDeleteRequest item) => PostAsync<List<CollectionDTO>>("collections/delete", item);

    /// <summary>Puts a game in a collection or takes it out.</summary>
    /// <param name="item">The account, the collection and the game.</param>
    /// <returns>The collections.</returns>
    public Task<List<CollectionDTO>> CollectionsGameAsync(CollectionGameRequest item) => PostAsync<List<CollectionDTO>>("collections/game", item);

    // ----- Prices -----

    /// <summary>Sets the price under which a wishlisted game raises an alert.</summary>
    /// <param name="item">The account, the game and the price.</param>
    /// <returns>The price now.</returns>
    public Task<PriceTargetRequest> LibraryPriceTargetAsync(PriceTargetRequest item) => PostAsync<PriceTargetRequest>("library/price-target", item);

    // ----- Notifications -----

    /// <summary>The latest notifications.</summary>
    /// <param name="userId">The account.</param>
    /// <returns>The bell.</returns>
    public Task<NotificationsDTO> NotificationsListAsync(string userId) => GetAsync<NotificationsDTO>($"notifications?userId={Uri.EscapeDataString(userId)}");

    /// <summary>How many notifications are new.</summary>
    /// <param name="userId">The account.</param>
    /// <returns>The count.</returns>
    public Task<int> NotificationsUnreadAsync(string userId) => GetAsync<int>($"notifications/unread?userId={Uri.EscapeDataString(userId)}");

    /// <summary>Marks every notification as seen.</summary>
    /// <param name="userId">The account.</param>
    /// <returns>The bell as it was (the new ones still marked new).</returns>
    public Task<NotificationsDTO> NotificationsReadAsync(string userId) => PostAsync<NotificationsDTO>("notifications/read", new NotificationReadRequest { UserId = userId });

    // ----- Health -----

    /// <summary>Checks the WebApi and its database.</summary>
    /// <returns>The health.</returns>
    public Task<HealthDTO> HealthGetAsync() => GetAsync<HealthDTO>("health");

    private async Task<T> GetAsync<T>(string path)
    {
        using var response = await httpClient.GetAsync(path);
        return await ReadAsync<T>(response);
    }

    private async Task<T> PostAsync<T>(string path, object? body)
    {
        using var response = await httpClient.PostAsJsonAsync(path, body, SerializerOptions);
        return await ReadAsync<T>(response);
    }

    private async Task DeleteAsync(string path)
    {
        using var response = await httpClient.DeleteAsync(path);
        await EnsureSuccessAsync(response);
    }

    private static async Task<T> ReadAsync<T>(HttpResponseMessage response)
    {
        await EnsureSuccessAsync(response);
        return (await response.Content.ReadFromJsonAsync<T>(SerializerOptions))!;
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        ApiValidationProblemDetails? problems = null;
        try
        {
            problems = await response.Content.ReadFromJsonAsync<ApiValidationProblemDetails>(SerializerOptions);
        }
        catch (Exception exception) when (exception is JsonException or NotSupportedException)
        {
            // Not a problem body (an empty 401, an HTML error page).
        }

        throw new LudexApiException(response.StatusCode, problems);
    }

    private static JsonSerializerOptions CreateSerializerOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}
