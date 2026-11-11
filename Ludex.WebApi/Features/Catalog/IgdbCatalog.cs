using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Ludex.Library.Enums;
using Microsoft.Extensions.Options;

namespace Ludex.WebApi.Features.Catalog;

/// <summary>
/// The IGDB catalog (https://api-docs.igdb.com). IGDB is used through a Twitch application: its Client ID and Secret
/// buy an app access token (client credentials), kept here and renewed before it runs out. IGDB allows four calls a
/// second, far more than a household's searches.
/// Images are not stored: the database keeps IGDB's image ids and the browser loads them from images.igdb.com.
/// </summary>
/// <param name="httpClientFactory">Creates the HTTP client.</param>
/// <param name="options">The Twitch credentials and addresses.</param>
/// <param name="timeProvider">The clock (token expiry).</param>
/// <param name="logger">Logger.</param>
internal sealed class IgdbCatalog(
    IHttpClientFactory httpClientFactory,
    IOptions<LudexOptions> options,
    TimeProvider timeProvider,
    ILogger<IgdbCatalog> logger) : IGameCatalog
{
    /// <summary>The name of the HTTP client.</summary>
    public const string HttpClientName = "igdb";

    private const string SearchFields =
        "name,slug,cover.image_id,first_release_date,platforms.name,platforms.abbreviation,"
        + "involved_companies.company.name,involved_companies.developer,involved_companies.publisher,genres.name";

    private const string DetailFields =
        SearchFields + ",summary,artworks.image_id,screenshots.image_id,game_modes.name";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly SemaphoreSlim _tokenGate = new(1, 1);
    private string? _token;
    private DateTimeOffset _tokenExpiresAt;

    /// <inheritdoc />
    public CatalogSource Source => CatalogSource.Igdb;

    /// <inheritdoc />
    public async Task<List<CatalogGame>> SearchAsync(string query, int take, CancellationToken cancellationToken = default)
    {
        var body = $"search \"{Escape(query)}\"; fields {SearchFields}; where version_parent = null; limit {Math.Clamp(take, 1, 50)};";
        var games = await QueryAsync(body, cancellationToken);
        return [.. games.Select(Map)];
    }

    /// <inheritdoc />
    public async Task<CatalogGame?> GetAsync(int id, CancellationToken cancellationToken = default)
    {
        var body = $"fields {DetailFields}; where id = {id.ToString(CultureInfo.InvariantCulture)}; limit 1;";
        var games = await QueryAsync(body, cancellationToken);
        return games.Count == 0 ? null : Map(games[0]);
    }

    /// <inheritdoc />
    public async Task<List<CatalogGame>> PopularAsync(int take, CancellationToken cancellationToken = default)
    {
        // The most rated games with a cover: the ones anybody recognises on the wall.
        var body = "fields name,slug,cover.image_id,first_release_date; "
                   + "where cover != null & version_parent = null & total_rating_count > 400; "
                   + $"sort total_rating_count desc; limit {Math.Clamp(take, 1, 100)};";
        var games = await QueryAsync(body, cancellationToken);
        return [.. games.Select(Map)];
    }

    /// <summary>
    /// Turns IGDB's game into the catalog's.
    /// </summary>
    /// <param name="game">IGDB's game.</param>
    /// <returns>The catalog game.</returns>
    internal static CatalogGame Map(IgdbGame game)
    {
        var companies = game.InvolvedCompanies ?? [];
        var summary = game.Summary?.Trim();
        return new CatalogGame
        {
            Id = game.Id,
            Name = (game.Name ?? string.Empty).Trim(),
            Slug = game.Slug,
            Summary = summary is { Length: > 4000 } ? summary[..3997] + "…" : summary,
            CoverImageId = game.Cover?.ImageId,
            HeroImageId = game.Artworks?.FirstOrDefault(static a => a.ImageId is not null)?.ImageId
                          ?? game.Screenshots?.FirstOrDefault(static s => s.ImageId is not null)?.ImageId,
            Screenshots = [.. (game.Screenshots ?? []).Select(static s => s.ImageId).OfType<string>().Take(8)],
            ReleaseDate = game.FirstReleaseDate is > 0
                ? DateOnly.FromDateTime(DateTimeOffset.FromUnixTimeSeconds(game.FirstReleaseDate.Value).UtcDateTime)
                : null,
            Developer = companies.FirstOrDefault(static c => c.Developer)?.Company?.Name,
            Publisher = companies.FirstOrDefault(static c => c.Publisher)?.Company?.Name,
            Genres = [.. (game.Genres ?? []).Select(static g => g.Name).OfType<string>().Select(static n => n.Replace(';', ',')).Distinct().Take(6)],
            Platforms = [.. (game.Platforms ?? []).Select(static p => PlatformNames.Friendly(p.Name, p.Abbreviation)).Where(static p => p.Length > 0).Distinct().Take(12)],
            Modes = [.. (game.GameModes ?? []).Select(static m => m.Name).OfType<string>().Select(static n => n.Replace(';', ',')).Distinct()],
        };
    }

    /// <summary>
    /// Escapes text for an IGDB query string.
    /// </summary>
    /// <param name="text">The text.</param>
    /// <returns>The text with backslashes and quotes escaped, at most 100 characters.</returns>
    internal static string Escape(string text)
    {
        var trimmed = text.Trim();
        if (trimmed.Length > 100)
        {
            trimmed = trimmed[..100];
        }

        return trimmed.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal);
    }

    private async Task<List<IgdbGame>> QueryAsync(string body, CancellationToken cancellationToken)
    {
        for (var attempt = 1; attempt <= 2; attempt++)
        {
            var token = await TokenAsync(forceNew: attempt > 1, cancellationToken);
            using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(new Uri(options.Value.IgdbUrl), "games"))
            {
                Content = new StringContent(body, Encoding.UTF8, "text/plain"),
            };
            request.Headers.Add("Client-ID", options.Value.IgdbClientId);
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            HttpResponseMessage response;
            try
            {
                response = await httpClientFactory.CreateClient(HttpClientName).SendAsync(request, cancellationToken);
            }
            catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
            {
                throw new CatalogUnavailableException(exception.Message, exception);
            }

            using (response)
            {
                if (response.StatusCode == HttpStatusCode.Unauthorized && attempt == 1)
                {
                    continue;   // the token was revoked or expired early: get a new one and try once more
                }

                if (!response.IsSuccessStatusCode)
                {
                    var text = await response.Content.ReadAsStringAsync(cancellationToken);
                    logger.LogWarning("IGDB answered {Status}: {Body}", (int)response.StatusCode, text.Length > 300 ? text[..300] : text);
                    throw new CatalogUnavailableException($"IGDB answered {(int)response.StatusCode}");
                }

                return await response.Content.ReadFromJsonAsync<List<IgdbGame>>(JsonOptions, cancellationToken) ?? [];
            }
        }

        throw new CatalogUnavailableException("IGDB refused the Twitch token");
    }

    private async Task<string> TokenAsync(bool forceNew, CancellationToken cancellationToken)
    {
        await _tokenGate.WaitAsync(cancellationToken);
        try
        {
            var now = timeProvider.GetUtcNow();
            if (!forceNew && _token is not null && now < _tokenExpiresAt - TimeSpan.FromMinutes(5))
            {
                return _token;
            }

            var settings = options.Value;
            var url = $"{settings.TwitchTokenUrl}?client_id={Uri.EscapeDataString(settings.IgdbClientId)}"
                      + $"&client_secret={Uri.EscapeDataString(settings.IgdbClientSecret)}&grant_type=client_credentials";
            HttpResponseMessage response;
            try
            {
                response = await httpClientFactory.CreateClient(HttpClientName).PostAsync(url, null, cancellationToken);
            }
            catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
            {
                throw new CatalogUnavailableException(exception.Message, exception);
            }

            using (response)
            {
                if (!response.IsSuccessStatusCode)
                {
                    logger.LogWarning("Twitch refused the IGDB credentials ({Status})", (int)response.StatusCode);
                    throw new CatalogUnavailableException("Twitch refused the Client ID / Secret");
                }

                var answer = await response.Content.ReadFromJsonAsync<TwitchToken>(JsonOptions, cancellationToken);
                if (string.IsNullOrEmpty(answer?.AccessToken))
                {
                    throw new CatalogUnavailableException("Twitch sent no token");
                }

                _token = answer.AccessToken;
                _tokenExpiresAt = now + TimeSpan.FromSeconds(Math.Max(60, answer.ExpiresIn));
                return _token;
            }
        }
        finally
        {
            _tokenGate.Release();
        }
    }

    private sealed class TwitchToken
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }

        [JsonPropertyName("expires_in")]
        public long ExpiresIn { get; set; }
    }
}

/// <summary>A game as IGDB sends it (only the fields Ludex asks for).</summary>
internal sealed class IgdbGame
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("slug")]
    public string? Slug { get; set; }

    [JsonPropertyName("summary")]
    public string? Summary { get; set; }

    [JsonPropertyName("first_release_date")]
    public long? FirstReleaseDate { get; set; }

    [JsonPropertyName("cover")]
    public IgdbImage? Cover { get; set; }

    [JsonPropertyName("artworks")]
    public List<IgdbImage>? Artworks { get; set; }

    [JsonPropertyName("screenshots")]
    public List<IgdbImage>? Screenshots { get; set; }

    [JsonPropertyName("platforms")]
    public List<IgdbPlatform>? Platforms { get; set; }

    [JsonPropertyName("genres")]
    public List<IgdbNamed>? Genres { get; set; }

    [JsonPropertyName("game_modes")]
    public List<IgdbNamed>? GameModes { get; set; }

    [JsonPropertyName("involved_companies")]
    public List<IgdbInvolvedCompany>? InvolvedCompanies { get; set; }
}

/// <summary>An IGDB image (cover, artwork, screenshot).</summary>
internal sealed class IgdbImage
{
    [JsonPropertyName("image_id")]
    public string? ImageId { get; set; }
}

/// <summary>An IGDB platform.</summary>
internal sealed class IgdbPlatform
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("abbreviation")]
    public string? Abbreviation { get; set; }
}

/// <summary>Anything IGDB names (genre, game mode, company).</summary>
internal sealed class IgdbNamed
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }
}

/// <summary>A company's part in a game.</summary>
internal sealed class IgdbInvolvedCompany
{
    [JsonPropertyName("company")]
    public IgdbNamed? Company { get; set; }

    [JsonPropertyName("developer")]
    public bool Developer { get; set; }

    [JsonPropertyName("publisher")]
    public bool Publisher { get; set; }
}
