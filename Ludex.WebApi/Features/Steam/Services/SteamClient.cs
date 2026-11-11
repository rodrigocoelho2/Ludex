using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace Ludex.WebApi.Features.Steam.Services;

/// <summary>
/// Thrown when the Steam Web API can't be reached or refuses the key.
/// </summary>
/// <param name="message">What went wrong (shown to the user).</param>
/// <param name="inner">The cause.</param>
internal sealed class SteamUnavailableException(string message, Exception? inner = null) : Exception(message, inner);

/// <summary>
/// The Steam Web API (https://steamcommunity.com/dev): a profile's name and picture, a custom profile name's SteamID,
/// and the games an account owns with its playtime. The key goes in the query string, so addresses are never logged.
/// </summary>
/// <param name="httpClientFactory">Creates the HTTP client.</param>
/// <param name="options">The key and the address.</param>
/// <param name="logger">Logger.</param>
internal sealed class SteamClient(IHttpClientFactory httpClientFactory, IOptions<LudexOptions> options, ILogger<SteamClient> logger)
{
    /// <summary>The name of the HTTP client.</summary>
    public const string HttpClientName = "steam";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// The SteamID64 behind a custom profile name (steamcommunity.com/id/name).
    /// </summary>
    /// <param name="vanity">The name.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The SteamID64, or null when no profile has that name.</returns>
    public async Task<string?> ResolveAsync(string vanity, CancellationToken cancellationToken = default)
    {
        var answer = await GetAsync<SteamEnvelope<SteamVanity>>($"ISteamUser/ResolveVanityURL/v1/?vanityurl={Uri.EscapeDataString(vanity)}", cancellationToken);
        return answer?.Response is { Success: 1, SteamId: { Length: > 0 } id } ? id : null;
    }

    /// <summary>
    /// A profile's public summary.
    /// </summary>
    /// <param name="steamId">The SteamID64.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The profile, or null when there's no such account.</returns>
    public async Task<SteamPlayer?> PlayerAsync(string steamId, CancellationToken cancellationToken = default)
    {
        var answer = await GetAsync<SteamEnvelope<SteamPlayers>>($"ISteamUser/GetPlayerSummaries/v2/?steamids={Uri.EscapeDataString(steamId)}", cancellationToken);
        return answer?.Response?.Players?.FirstOrDefault();
    }

    /// <summary>
    /// The games an account owns (free games it played included), with playtime.
    /// </summary>
    /// <param name="steamId">The SteamID64.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The games, or null when Steam won't say (the profile's game details aren't public).</returns>
    public async Task<List<SteamOwnedGame>?> OwnedGamesAsync(string steamId, CancellationToken cancellationToken = default)
    {
        var answer = await GetAsync<SteamEnvelope<SteamOwned>>(
            $"IPlayerService/GetOwnedGames/v1/?steamid={Uri.EscapeDataString(steamId)}&include_appinfo=1&include_played_free_games=1&format=json",
            cancellationToken);
        return answer?.Response?.Games;
    }

    private async Task<T?> GetAsync<T>(string pathAndQuery, CancellationToken cancellationToken)
    {
        var settings = options.Value;
        var url = new Uri(new Uri(settings.SteamUrl), pathAndQuery + "&key=" + Uri.EscapeDataString(settings.SteamApiKey));
        HttpResponseMessage response;
        try
        {
            response = await httpClientFactory.CreateClient(HttpClientName).GetAsync(url, cancellationToken);
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            throw new SteamUnavailableException("Can't reach Steam right now. Try again in a minute.", exception);
        }

        using (response)
        {
            if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            {
                logger.LogWarning("Steam refused the Web API key ({Status})", (int)response.StatusCode);
                throw new SteamUnavailableException("Steam refused the Web API key. Check STEAM_API_KEY in Ludex's .env.");
            }

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Steam answered {Status} on {Method}", (int)response.StatusCode, pathAndQuery.Split('?')[0]);
                throw new SteamUnavailableException(string.Create(CultureInfo.InvariantCulture, $"Steam answered {(int)response.StatusCode}. Try again in a minute."));
            }

            try
            {
                return await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken);
            }
            catch (JsonException exception)
            {
                throw new SteamUnavailableException("Steam sent something Ludex can't read.", exception);
            }
        }
    }
}

/// <summary>Steam's answers come inside "response".</summary>
/// <typeparam name="T">The answer.</typeparam>
internal sealed class SteamEnvelope<T>
{
    [JsonPropertyName("response")]
    public T? Response { get; set; }
}

/// <summary>ResolveVanityURL's answer (success 1: found, 42: no match).</summary>
internal sealed class SteamVanity
{
    [JsonPropertyName("steamid")]
    public string? SteamId { get; set; }

    [JsonPropertyName("success")]
    public int Success { get; set; }
}

/// <summary>GetPlayerSummaries' answer.</summary>
internal sealed class SteamPlayers
{
    [JsonPropertyName("players")]
    public List<SteamPlayer>? Players { get; set; }
}

/// <summary>A Steam profile (communityvisibilitystate 3 is public).</summary>
internal sealed class SteamPlayer
{
    [JsonPropertyName("steamid")]
    public string? SteamId { get; set; }

    [JsonPropertyName("personaname")]
    public string? PersonaName { get; set; }

    [JsonPropertyName("avatarfull")]
    public string? AvatarFull { get; set; }

    [JsonPropertyName("profileurl")]
    public string? ProfileUrl { get; set; }

    [JsonPropertyName("communityvisibilitystate")]
    public int CommunityVisibilityState { get; set; }
}

/// <summary>GetOwnedGames' answer (empty when the game details are private).</summary>
internal sealed class SteamOwned
{
    [JsonPropertyName("game_count")]
    public int GameCount { get; set; }

    [JsonPropertyName("games")]
    public List<SteamOwnedGame>? Games { get; set; }
}

/// <summary>A game an account owns.</summary>
internal sealed class SteamOwnedGame
{
    /// <summary>Gets or sets the app id.</summary>
    [JsonPropertyName("appid")]
    public int AppId { get; set; }

    /// <summary>Gets or sets the name.</summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>Gets or sets the minutes played in all.</summary>
    [JsonPropertyName("playtime_forever")]
    public int PlaytimeForever { get; set; }

    /// <summary>Gets or sets the minutes played in the last two weeks.</summary>
    [JsonPropertyName("playtime_2weeks")]
    public int? Playtime2Weeks { get; set; }

    /// <summary>Gets or sets when it was last played (Unix seconds; 0: never).</summary>
    [JsonPropertyName("rtime_last_played")]
    public long? LastPlayed { get; set; }
}
