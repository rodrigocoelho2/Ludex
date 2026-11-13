using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Ludex.WebApi.Features.Psn.Services;

/// <summary>
/// Thrown when PlayStation Network can't be reached or answers something unexpected (try again later).
/// </summary>
/// <param name="message">What went wrong (shown to the user).</param>
/// <param name="inner">The cause.</param>
internal sealed class PsnUnavailableException(string message, Exception? inner = null) : Exception(message, inner);

/// <summary>
/// Thrown when Sony refuses the sign-in (an NPSSO that ran out or was mistyped, a refresh token that expired): the
/// account has to be connected again.
/// </summary>
/// <param name="message">What went wrong (shown to the user).</param>
internal sealed class PsnSignInException(string message) : Exception(message);

/// <summary>Sony's tokens.</summary>
/// <param name="AccessToken">For the calls (about an hour).</param>
/// <param name="RefreshToken">For new access tokens (about two months).</param>
/// <param name="RefreshExpiresAt">When the refresh token runs out.</param>
internal sealed record PsnTokens(string AccessToken, string RefreshToken, DateTimeOffset RefreshExpiresAt);

/// <summary>
/// PlayStation Network, as Sony's own PlayStation App uses it (the calls the psn-api project documents): an NPSSO
/// cookie (from ca.account.sony.com/api/v1/ssocookie, signed in to playstation.com) buys an authorization code, the
/// code buys an access token and a refresh token, and the access token reads the games played (with time played) and
/// the trophies. Nothing is ever written to the account. Tokens never reach the logs.
/// </summary>
/// <param name="httpClientFactory">Creates the HTTP client (no redirects followed, no cookies kept).</param>
/// <param name="timeProvider">The clock.</param>
/// <param name="logger">Logger.</param>
internal sealed class PsnClient(IHttpClientFactory httpClientFactory, TimeProvider timeProvider, ILogger<PsnClient> logger)
{
    /// <summary>The name of the HTTP client.</summary>
    public const string HttpClientName = "psn";

    private const string AuthUrl = "https://ca.account.sony.com/api/authz/v3/oauth/";
    private const string ApiUrl = "https://m.np.playstation.com/api/";
    private const string ClientId = "09515159-7237-4370-9b40-3806e67c0891";
    private const string RedirectUri = "com.scee.psxandroid.scecompcall://redirect";
    private const string Scope = "psn:mobile.v2.core psn:clientapp";

    // The PlayStation App's own client (public: it is in every copy of the app).
    private const string BasicAuth = "MDk1MTUxNTktNzIzNy00MzcwLTliNDAtMzgwNmU2N2MwODkxOnVjUGprYTV0bnRCMktxc1A=";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { NumberHandling = JsonNumberHandling.AllowReadingFromString };

    /// <summary>
    /// Signs in with an NPSSO.
    /// </summary>
    /// <param name="npsso">The NPSSO (64 characters).</param>
    /// <param name="cancellationToken">Cancels the calls.</param>
    /// <returns>The tokens.</returns>
    /// <exception cref="PsnSignInException">Sony refused the NPSSO.</exception>
    /// <exception cref="PsnUnavailableException">Sony can't be reached.</exception>
    public async Task<PsnTokens> SignInAsync(string npsso, CancellationToken cancellationToken = default)
    {
        var url = $"{AuthUrl}authorize?access_type=offline&client_id={ClientId}&redirect_uri={Uri.EscapeDataString(RedirectUri)}"
                  + $"&response_type=code&scope={Uri.EscapeDataString(Scope)}";
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("Cookie", $"npsso={npsso}");
        using var response = await SendAsync(request, cancellationToken);

        // Sony answers with a redirect to the app's address, the code in its query string; no code means no sign-in.
        var location = response.Headers.Location?.OriginalString ?? string.Empty;
        var code = Query(location, "code");
        if (code is null)
        {
            logger.LogInformation("PlayStation sign-in refused ({Status}, {Error})", (int)response.StatusCode, Query(location, "error") ?? "no code");
            throw new PsnSignInException("Sony didn't accept that NPSSO. Sign in at playstation.com, open ca.account.sony.com/api/v1/ssocookie in the same browser and copy the npsso value again.");
        }

        return await TokenAsync(
            new Dictionary<string, string>
            {
                ["code"] = code,
                ["redirect_uri"] = RedirectUri,
                ["grant_type"] = "authorization_code",
                ["token_format"] = "jwt",
            },
            cancellationToken);
    }

    /// <summary>
    /// A new access token from the refresh token.
    /// </summary>
    /// <param name="refreshToken">The refresh token.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The tokens (Sony may send a new refresh token).</returns>
    /// <exception cref="PsnSignInException">The refresh token ran out or was revoked.</exception>
    /// <exception cref="PsnUnavailableException">Sony can't be reached.</exception>
    public Task<PsnTokens> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default) =>
        TokenAsync(
            new Dictionary<string, string>
            {
                ["refresh_token"] = refreshToken,
                ["grant_type"] = "refresh_token",
                ["token_format"] = "jwt",
                ["scope"] = Scope,
            },
            cancellationToken);

    /// <summary>
    /// The PS4 and PS5 games the account played, with time played.
    /// </summary>
    /// <param name="accessToken">The access token.</param>
    /// <param name="cancellationToken">Cancels the calls.</param>
    /// <returns>The games.</returns>
    public async Task<List<PsnPlayedTitle>> PlayedAsync(string accessToken, CancellationToken cancellationToken = default)
    {
        var titles = new List<PsnPlayedTitle>();
        for (var offset = 0; offset < 10000;)
        {
            var page = await GetAsync<PsnPlayedPage>($"gamelist/v2/users/me/titles?categories=ps4_game,ps5_native_game&limit=200&offset={offset}", accessToken, cancellationToken);
            titles.AddRange(page?.Titles ?? []);
            if (page?.NextOffset is not { } next || next <= offset || (page.Titles?.Count ?? 0) == 0)
            {
                break;
            }

            offset = next;
        }

        return titles;
    }

    /// <summary>
    /// The games the account has trophies in (PS3, PS Vita, PS4, PS5).
    /// </summary>
    /// <param name="accessToken">The access token.</param>
    /// <param name="cancellationToken">Cancels the calls.</param>
    /// <returns>The games with their trophy counts.</returns>
    public async Task<List<PsnTrophyTitle>> TrophiesAsync(string accessToken, CancellationToken cancellationToken = default)
    {
        var titles = new List<PsnTrophyTitle>();
        for (var offset = 0; offset < 10000;)
        {
            var page = await GetAsync<PsnTrophyPage>($"trophy/v1/users/me/trophyTitles?limit=800&offset={offset}", accessToken, cancellationToken);
            var count = page?.TrophyTitles?.Count ?? 0;
            titles.AddRange(page?.TrophyTitles ?? []);
            if (count == 0 || page?.NextOffset is not { } next || next <= offset)
            {
                break;
            }

            offset = next;
        }

        return titles;
    }

    /// <summary>
    /// The account's PSN name (best effort: null when Sony won't say).
    /// </summary>
    /// <param name="accessToken">The access token.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The online id, or null.</returns>
    public async Task<string?> OnlineIdAsync(string accessToken, CancellationToken cancellationToken = default)
    {
        try
        {
            return (await GetAsync<PsnProfile>("userProfile/v1/internal/users/me/profiles", accessToken, cancellationToken))?.OnlineId;
        }
        catch (PsnUnavailableException)
        {
            return null;
        }
    }

    private static string? Query(string url, string name)
    {
        var at = url.IndexOf('?', StringComparison.Ordinal);
        if (at < 0)
        {
            return null;
        }

        foreach (var pair in url[(at + 1)..].Split('&'))
        {
            var eq = pair.IndexOf('=', StringComparison.Ordinal);
            if (eq > 0 && pair[..eq] == name && pair.Length > eq + 1)
            {
                return Uri.UnescapeDataString(pair[(eq + 1)..]);
            }
        }

        return null;
    }

    private async Task<PsnTokens> TokenAsync(Dictionary<string, string> form, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{AuthUrl}token") { Content = new FormUrlEncodedContent(form) };
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", BasicAuth);
        using var response = await SendAsync(request, cancellationToken);
        if (response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            logger.LogInformation("PlayStation refused a token ({Status})", (int)response.StatusCode);
            throw new PsnSignInException("Sony's sign-in ran out. Connect PlayStation again with a new NPSSO.");
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new PsnUnavailableException($"PlayStation Network answered {(int)response.StatusCode}. Try again in a while.");
        }

        var token = await Read<PsnTokenAnswer>(response, cancellationToken);
        if (string.IsNullOrEmpty(token?.AccessToken) || string.IsNullOrEmpty(token.RefreshToken))
        {
            throw new PsnUnavailableException("PlayStation Network didn't send a token. Try again in a while.");
        }

        var lasts = token.RefreshTokenExpiresIn is > 0 ? token.RefreshTokenExpiresIn.Value : 60L * 24 * 3600;
        return new PsnTokens(token.AccessToken, token.RefreshToken, timeProvider.GetUtcNow().AddSeconds(lasts));
    }

    private async Task<T?> GetAsync<T>(string path, string accessToken, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, ApiUrl + path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        using var response = await SendAsync(request, cancellationToken);
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            throw new PsnSignInException("Sony's sign-in ran out. Connect PlayStation again with a new NPSSO.");
        }

        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("PlayStation Network answered {Status} for {Path}", (int)response.StatusCode, path.Split('?')[0]);
            throw new PsnUnavailableException($"PlayStation Network answered {(int)response.StatusCode}. Try again in a while.");
        }

        return await Read<T>(response, cancellationToken);
    }

    private static async Task<T?> Read<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            return await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken);
        }
        catch (JsonException exception)
        {
            throw new PsnUnavailableException("PlayStation Network sent something Ludex can't read.", exception);
        }
    }

    private async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        try
        {
            return await httpClientFactory.CreateClient(HttpClientName).SendAsync(request, cancellationToken);
        }
        catch (Exception exception) when ((exception is HttpRequestException or TaskCanceledException) && !cancellationToken.IsCancellationRequested)
        {
            throw new PsnUnavailableException("PlayStation Network can't be reached right now. Try again in a while.", exception);
        }
    }
}

/// <summary>Sony's token answer.</summary>
internal sealed class PsnTokenAnswer
{
    [JsonPropertyName("access_token")]
    public string? AccessToken { get; set; }

    [JsonPropertyName("refresh_token")]
    public string? RefreshToken { get; set; }

    [JsonPropertyName("refresh_token_expires_in")]
    public long? RefreshTokenExpiresIn { get; set; }
}

/// <summary>A page of games played.</summary>
internal sealed class PsnPlayedPage
{
    public List<PsnPlayedTitle>? Titles { get; set; }

    public int? NextOffset { get; set; }

    public int? TotalItemCount { get; set; }
}

/// <summary>A game played (one version: PS4 or PS5).</summary>
internal sealed class PsnPlayedTitle
{
    public string? TitleId { get; set; }

    public string? Name { get; set; }

    public string? LocalizedName { get; set; }

    public string? ImageUrl { get; set; }

    public string? Category { get; set; }

    public int? PlayCount { get; set; }

    public PsnConcept? Concept { get; set; }

    public DateTimeOffset? FirstPlayedDateTime { get; set; }

    public DateTimeOffset? LastPlayedDateTime { get; set; }

    public string? PlayDuration { get; set; }
}

/// <summary>The game behind its versions (a PS4 and a PS5 copy share it).</summary>
internal sealed class PsnConcept
{
    public long? Id { get; set; }

    public string? Name { get; set; }
}

/// <summary>A page of trophy titles.</summary>
internal sealed class PsnTrophyPage
{
    public List<PsnTrophyTitle>? TrophyTitles { get; set; }

    public int? NextOffset { get; set; }

    public int? TotalItemCount { get; set; }
}

/// <summary>A game with trophies.</summary>
internal sealed class PsnTrophyTitle
{
    public string? NpCommunicationId { get; set; }

    public string? TrophyTitleName { get; set; }

    public string? TrophyTitlePlatform { get; set; }

    public PsnTrophyCounts? DefinedTrophies { get; set; }

    public PsnTrophyCounts? EarnedTrophies { get; set; }

    public int? Progress { get; set; }

    public DateTimeOffset? LastUpdatedDateTime { get; set; }
}

/// <summary>Trophies by grade.</summary>
internal sealed class PsnTrophyCounts
{
    public int Bronze { get; set; }

    public int Silver { get; set; }

    public int Gold { get; set; }

    public int Platinum { get; set; }

    /// <summary>Gets all of them.</summary>
    [JsonIgnore]
    public int Total => Bronze + Silver + Gold + Platinum;
}

/// <summary>The account's profile.</summary>
internal sealed class PsnProfile
{
    public string? OnlineId { get; set; }
}
