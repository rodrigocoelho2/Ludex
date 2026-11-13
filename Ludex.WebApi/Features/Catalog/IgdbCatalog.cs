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
        SearchFields + ",summary,artworks.image_id,screenshots.image_id,game_modes.name,aggregated_rating";

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
        var games = await QueryAsync<IgdbGame>("games", body, cancellationToken);
        return [.. games.Select(Map)];
    }

    /// <inheritdoc />
    public async Task<CatalogGame?> GetAsync(int id, CancellationToken cancellationToken = default)
    {
        var body = $"fields {DetailFields}; where id = {id.ToString(CultureInfo.InvariantCulture)}; limit 1;";
        var games = await QueryAsync<IgdbGame>("games", body, cancellationToken);
        if (games.Count == 0)
        {
            return null;
        }

        var game = Map(games[0]);
        await AddTimesAsync([game], cancellationToken);
        return game;
    }

    /// <inheritdoc />
    public async Task<List<CatalogGame>> GetManyAsync(IReadOnlyCollection<int> ids, CancellationToken cancellationToken = default)
    {
        var games = (await GamesAsync(ids, cancellationToken)).Values.Select(Map).ToList();
        await AddTimesAsync(games, cancellationToken);
        return games;
    }

    /// <inheritdoc />
    public async Task<List<CatalogGame>> PopularAsync(int take, CancellationToken cancellationToken = default)
    {
        // The most rated games with a cover: the ones anybody recognises on the wall.
        var body = "fields name,slug,cover.image_id,first_release_date; "
                   + "where cover != null & version_parent = null & total_rating_count > 400; "
                   + $"sort total_rating_count desc; limit {Math.Clamp(take, 1, 100)};";
        var games = await QueryAsync<IgdbGame>("games", body, cancellationToken);
        return [.. games.Select(Map)];
    }

    /// <inheritdoc />
    public async Task<Dictionary<int, CatalogGame>> SteamAsync(IReadOnlyCollection<int> appIds, CancellationToken cancellationToken = default)
    {
        // Steam app → IGDB game, through IGDB's external games (source 1 is Steam; the uid is the app id, as text).
        var gameByApp = new Dictionary<int, int>();
        foreach (var chunk in appIds.Distinct().Chunk(200))
        {
            var uids = string.Join(",", chunk.Select(static id => $"\"{id.ToString(CultureInfo.InvariantCulture)}\""));
            var rows = await QueryAsync<IgdbExternalGame>("external_games", $"fields uid,game; where external_game_source = 1 & uid = ({uids}); limit 500;", cancellationToken);
            foreach (var row in rows)
            {
                if (row.Game is { } game && int.TryParse(row.Uid, NumberStyles.None, CultureInfo.InvariantCulture, out var app))
                {
                    gameByApp.TryAdd(app, game);
                }
            }

            await PaceAsync(cancellationToken);
        }

        var games = await GamesAsync([.. gameByApp.Values.Distinct()], cancellationToken);

        // An edition or a version ("Deluxe Edition", "Game of the Year") counts as its main game.
        var parents = games.Values
            .Select(static g => g.VersionParent)
            .OfType<int>()
            .Where(id => !games.ContainsKey(id))
            .Distinct()
            .ToList();
        foreach (var (id, parent) in await GamesAsync(parents, cancellationToken))
        {
            games[id] = parent;
        }

        var result = new Dictionary<int, CatalogGame>();
        foreach (var (app, id) in gameByApp)
        {
            if (!games.TryGetValue(id, out var game))
            {
                continue;
            }

            if (game.VersionParent is { } parentId && games.TryGetValue(parentId, out var main))
            {
                game = main;
            }

            result[app] = Map(game);
        }

        await AddTimesAsync([.. result.Values.DistinctBy(static g => g.Id)], cancellationToken);
        foreach (var (app, game) in result.ToList())
        {
            // The same game for two apps: the times were added to the first copy only.
            var withTimes = result.Values.FirstOrDefault(g => g.Id == game.Id && (g.TimeToBeatMain is not null || g.TimeToBeatFull is not null));
            if (withTimes is not null && !ReferenceEquals(withTimes, game))
            {
                game.TimeToBeatMain = withTimes.TimeToBeatMain;
                game.TimeToBeatFull = withTimes.TimeToBeatFull;
            }
        }

        return result;
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
            CriticScore = game.AggregatedRating is > 0 and <= 100 ? (int)Math.Round(game.AggregatedRating.Value) : null,
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

    /// <inheritdoc />
    public async Task<Dictionary<string, List<CatalogGame>>> SearchManyAsync(IReadOnlyCollection<string> names, int take, CancellationToken cancellationToken = default)
    {
        // 1. The exact name (ignoring case), ten names a call with IGDB's multiquery: most names are found here.
        // 2. A full-text search, one name a call, for the rest (a store's spelling: "Grand Theft Auto V (PlayStation5)").
        //    IGDB's multiquery doesn't search, so this can't be batched; at most 100 names a run keep it under a minute.
        // Calls start every 300 ms with up to three in flight (IGDB allows four a second).
        var distinct = names.Where(static n => !string.IsNullOrWhiteSpace(n)).Distinct(StringComparer.Ordinal).ToList();
        var limit = Math.Clamp(take, 1, 20).ToString(CultureInfo.InvariantCulture);
        var found = new Dictionary<string, List<CatalogGame>>(StringComparer.Ordinal);

        var exact = await PacedAsync(distinct.Chunk(10), chunk => ExactChunkAsync(chunk, limit, cancellationToken), cancellationToken);
        foreach (var (name, games) in exact.SelectMany(static a => a))
        {
            found[name] = games;
        }

        var rest = distinct.Where(n => !found.ContainsKey(n)).Take(100).ToList();
        var searched = await PacedAsync(rest, async name =>
        {
            // No version filter here: a game IGDB files as a version of another still matches by its own name.
            var body = $"search \"{Escape(Plain(name))}\"; fields {SearchFields}; limit 15;";
            return (Name: name, Games: (await RetryAsync(() => QueryAsync<IgdbGame>("games", body, cancellationToken), cancellationToken)).Select(Map).ToList());
        }, cancellationToken);
        foreach (var (name, games) in searched.Where(static s => s.Games.Count > 0))
        {
            found[name] = games;
        }

        return found;
    }

    // A store's name as IGDB spells it: ’ as ', no ™ ® ©.
    private static string Plain(string name) =>
        string.Join(' ', name.Replace('’', '\'').Replace("™", string.Empty, StringComparison.Ordinal).Replace("®", string.Empty, StringComparison.Ordinal)
            .Replace("©", string.Empty, StringComparison.Ordinal).Split(' ', StringSplitOptions.RemoveEmptyEntries));

    private async Task<List<(string Name, List<CatalogGame> Games)>> ExactChunkAsync(string[] chunk, string limit, CancellationToken cancellationToken)
    {
        var body = new StringBuilder();
        for (var i = 0; i < chunk.Length; i++)
        {
            body.Append(CultureInfo.InvariantCulture, $"query games \"q{i}\" {{ fields {SearchFields}; where name ~ \"{Escape(Plain(chunk[i]))}\" & version_parent = null; limit {limit}; }};\n");
        }

        var answers = await RetryAsync(() => QueryAsync<IgdbMultiAnswer>("multiquery", body.ToString(), cancellationToken), cancellationToken);
        var found = new List<(string Name, List<CatalogGame> Games)>();
        foreach (var answer in answers)
        {
            if (answer.Name is { Length: > 1 } label
                && int.TryParse(label.AsSpan(1), NumberStyles.None, CultureInfo.InvariantCulture, out var index)
                && index < chunk.Length
                && answer.Result is { Count: > 0 } games)
            {
                found.Add((chunk[index], games.Select(Map).ToList()));
            }
        }

        return found;
    }

    // Runs calls one every 300 ms, at most three at once, and returns their answers in order.
    private async Task<List<T>> PacedAsync<TIn, T>(IEnumerable<TIn> items, Func<TIn, Task<T>> call, CancellationToken cancellationToken)
    {
        var gate = new SemaphoreSlim(3);   // not disposed: calls still running after a cancel release it
        var calls = new List<Task<T>>();
        foreach (var item in items)
        {
            if (calls.Count > 0)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(300), timeProvider, cancellationToken);
            }

            await gate.WaitAsync(cancellationToken);
            calls.Add(Run(item));
        }

        return [.. await Task.WhenAll(calls)];

        async Task<T> Run(TIn item)
        {
            try
            {
                return await call(item);
            }
            finally
            {
                gate.Release();
            }
        }
    }

    // Too many calls at once (429): wait a second and try once more.
    private async Task<List<T>> RetryAsync<T>(Func<Task<List<T>>> call, CancellationToken cancellationToken)
    {
        try
        {
            return await call();
        }
        catch (CatalogUnavailableException exception) when (exception.Message.Contains("429", StringComparison.Ordinal))
        {
            await Task.Delay(TimeSpan.FromSeconds(1), timeProvider, cancellationToken);
            return await call();
        }
    }

    /// <summary>Several games by id, with everything the game page shows, in as few calls as IGDB allows.</summary>
    private async Task<Dictionary<int, IgdbGame>> GamesAsync(IReadOnlyCollection<int> ids, CancellationToken cancellationToken)
    {
        var games = new Dictionary<int, IgdbGame>();
        foreach (var chunk in ids.Chunk(400))
        {
            var list = string.Join(",", chunk.Select(static id => id.ToString(CultureInfo.InvariantCulture)));
            foreach (var game in await QueryAsync<IgdbGame>("games", $"fields {DetailFields},version_parent; where id = ({list}); limit 500;", cancellationToken))
            {
                games[game.Id] = game;
            }

            await PaceAsync(cancellationToken);
        }

        return games;
    }

    /// <summary>
    /// Adds how long the games take to finish (IGDB's game_time_to_beats, in seconds: hastily is the main story,
    /// completely is 100%).
    /// </summary>
    private async Task AddTimesAsync(IReadOnlyCollection<CatalogGame> games, CancellationToken cancellationToken)
    {
        if (games.Count == 0)
        {
            return;
        }

        var byId = games.GroupBy(static g => g.Id).ToDictionary(static g => g.Key, static g => g.First());
        foreach (var chunk in byId.Keys.Chunk(400))
        {
            var list = string.Join(",", chunk.Select(static id => id.ToString(CultureInfo.InvariantCulture)));
            List<IgdbTimeToBeat> times;
            try
            {
                times = await QueryAsync<IgdbTimeToBeat>("game_time_to_beats", $"fields game_id,hastily,normally,completely; where game_id = ({list}); limit 500;", cancellationToken);
            }
            catch (CatalogUnavailableException exception)
            {
                logger.LogWarning("IGDB times to beat skipped: {Problem}", exception.Message);
                return;   // nice to have: the games are fine without
            }

            foreach (var time in times)
            {
                if (time.GameId is { } id && byId.TryGetValue(id, out var game))
                {
                    game.TimeToBeatMain = Hours(time.Hastily ?? time.Normally);
                    game.TimeToBeatFull = Hours(time.Completely);
                }
            }

            if (byId.Count > 400)
            {
                await PaceAsync(cancellationToken);
            }
        }
    }

    private static decimal? Hours(long? seconds) => seconds is > 0 ? Math.Round(seconds.Value / 3600m, 1, MidpointRounding.AwayFromZero) : null;

    /// <summary>IGDB allows four calls a second: a pause between the calls of a long run (the Steam import).</summary>
    private Task PaceAsync(CancellationToken cancellationToken) => Task.Delay(TimeSpan.FromMilliseconds(260), timeProvider, cancellationToken);

    private async Task<List<T>> QueryAsync<T>(string endpoint, string body, CancellationToken cancellationToken)
    {
        for (var attempt = 1; attempt <= 2; attempt++)
        {
            var token = await TokenAsync(forceNew: attempt > 1, cancellationToken);
            using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(new Uri(options.Value.IgdbUrl), endpoint))
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

                return await response.Content.ReadFromJsonAsync<List<T>>(JsonOptions, cancellationToken) ?? [];
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

    [JsonPropertyName("version_parent")]
    public int? VersionParent { get; set; }

    [JsonPropertyName("aggregated_rating")]
    public double? AggregatedRating { get; set; }
}

/// <summary>How long a game takes (IGDB's game_time_to_beats, in seconds).</summary>
internal sealed class IgdbTimeToBeat
{
    [JsonPropertyName("game_id")]
    public int? GameId { get; set; }

    [JsonPropertyName("hastily")]
    public long? Hastily { get; set; }

    [JsonPropertyName("normally")]
    public long? Normally { get; set; }

    [JsonPropertyName("completely")]
    public long? Completely { get; set; }
}

/// <summary>One labelled answer of IGDB's multiquery.</summary>
internal sealed class IgdbMultiAnswer
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("result")]
    public List<IgdbGame>? Result { get; set; }
}

/// <summary>A game in another store, as IGDB links it (here: a Steam app).</summary>
internal sealed class IgdbExternalGame
{
    [JsonPropertyName("uid")]
    public string? Uid { get; set; }

    [JsonPropertyName("game")]
    public int? Game { get; set; }
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
