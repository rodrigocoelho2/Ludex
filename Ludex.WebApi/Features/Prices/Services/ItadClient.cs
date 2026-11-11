using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace Ludex.WebApi.Features.Prices.Services;

/// <summary>
/// IsThereAnyDeal (https://docs.isthereanydeal.com): finds a game's id by its Steam app or its title, then the current
/// prices in every PC store it follows, and the lowest price ever. The key goes in the query string, so addresses are
/// never logged.
/// </summary>
/// <param name="httpClientFactory">Creates the HTTP client.</param>
/// <param name="options">The key, the address and the country.</param>
/// <param name="logger">Logger.</param>
internal sealed class ItadClient(IHttpClientFactory httpClientFactory, IOptions<LudexOptions> options, ILogger<ItadClient> logger)
{
    /// <summary>The name of the HTTP client.</summary>
    public const string HttpClientName = "itad";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { NumberHandling = JsonNumberHandling.AllowReadingFromString };

    /// <summary>
    /// IsThereAnyDeal's id of a game.
    /// </summary>
    /// <param name="title">The game's name.</param>
    /// <param name="steamAppId">Its Steam app, when known (a surer match).</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The id, or null when IsThereAnyDeal doesn't know the game (or can't be reached).</returns>
    public async Task<string?> LookupAsync(string title, int? steamAppId, CancellationToken cancellationToken = default)
    {
        var query = steamAppId is { } app
            ? $"appid={app.ToString(CultureInfo.InvariantCulture)}"
            : $"title={Uri.EscapeDataString(title)}";
        try
        {
            using var response = await Client().GetAsync(Url($"games/lookup/v1?{query}"), cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("IsThereAnyDeal lookup answered {Status}", (int)response.StatusCode);
                return null;
            }

            var answer = await response.Content.ReadFromJsonAsync<ItadLookup>(JsonOptions, cancellationToken);
            return answer is { Found: true, Game.Id: { Length: > 0 } id } ? id : null;
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or JsonException)
        {
            logger.LogWarning("IsThereAnyDeal lookup failed: {Problem}", exception.Message);
            return null;
        }
    }

    /// <summary>
    /// The current prices of games.
    /// </summary>
    /// <param name="ids">IsThereAnyDeal ids (at most 200).</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The prices of each game IsThereAnyDeal answered for.</returns>
    public async Task<List<ItadGamePrices>> PricesAsync(IReadOnlyCollection<string> ids, CancellationToken cancellationToken = default)
    {
        if (ids.Count == 0)
        {
            return [];
        }

        var country = Uri.EscapeDataString(options.Value.PriceCountry.Trim().ToUpperInvariant());
        try
        {
            using var response = await Client().PostAsJsonAsync(Url($"games/prices/v3?country={country}"), ids, JsonOptions, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("IsThereAnyDeal prices answered {Status}", (int)response.StatusCode);
                return [];
            }

            return await response.Content.ReadFromJsonAsync<List<ItadGamePrices>>(JsonOptions, cancellationToken) ?? [];
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or JsonException)
        {
            logger.LogWarning("IsThereAnyDeal prices failed: {Problem}", exception.Message);
            return [];
        }
    }

    private HttpClient Client() => httpClientFactory.CreateClient(HttpClientName);

    private Uri Url(string pathAndQuery) =>
        new(new Uri(options.Value.ItadUrl), pathAndQuery + "&key=" + Uri.EscapeDataString(options.Value.ItadApiKey));
}

/// <summary>games/lookup's answer.</summary>
internal sealed class ItadLookup
{
    [JsonPropertyName("found")]
    public bool Found { get; set; }

    [JsonPropertyName("game")]
    public ItadGame? Game { get; set; }
}

/// <summary>A game IsThereAnyDeal knows.</summary>
internal sealed class ItadGame
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }
}

/// <summary>A game's prices (games/prices).</summary>
internal sealed class ItadGamePrices
{
    /// <summary>Gets or sets IsThereAnyDeal's id.</summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>Gets or sets the lowest prices (all: ever).</summary>
    [JsonPropertyName("historyLow")]
    public ItadHistoryLow? HistoryLow { get; set; }

    /// <summary>Gets or sets the current price in each store.</summary>
    [JsonPropertyName("deals")]
    public List<ItadDeal>? Deals { get; set; }
}

/// <summary>The lowest prices.</summary>
internal sealed class ItadHistoryLow
{
    [JsonPropertyName("all")]
    public ItadPrice? All { get; set; }
}

/// <summary>A price in one store.</summary>
internal sealed class ItadDeal
{
    [JsonPropertyName("shop")]
    public ItadShop? Shop { get; set; }

    [JsonPropertyName("price")]
    public ItadPrice? Price { get; set; }

    [JsonPropertyName("regular")]
    public ItadPrice? Regular { get; set; }

    [JsonPropertyName("cut")]
    public int Cut { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }
}

/// <summary>A store.</summary>
internal sealed class ItadShop
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }
}

/// <summary>An amount of money.</summary>
internal sealed class ItadPrice
{
    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }

    [JsonPropertyName("currency")]
    public string? Currency { get; set; }
}
