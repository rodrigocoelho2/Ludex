namespace Ludex.WebApi.Features;

/// <summary>
/// Module options, bound from the "Ludex" configuration section (in Docker: Ludex__ApiKey, Ludex__IgdbClientId…).
/// </summary>
public sealed class LudexOptions
{
    /// <summary>The configuration section name.</summary>
    public const string SectionName = "Ludex";

    /// <summary>Gets or sets the key the Website must send in the x-api-key header. Never commit a real value.</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Client ID of a Twitch application (https://dev.twitch.tv/console), which is how IGDB is used.
    /// Empty (with the secret): Ludex runs on the small list of games built into it, without images.
    /// </summary>
    public string IgdbClientId { get; set; } = string.Empty;

    /// <summary>Gets or sets the Client Secret of that Twitch application. Never commit a real value.</summary>
    public string IgdbClientSecret { get; set; } = string.Empty;

    /// <summary>Gets or sets the IGDB API address.</summary>
    public string IgdbUrl { get; set; } = "https://api.igdb.com/v4/";

    /// <summary>Gets or sets the Twitch address that hands out the IGDB access tokens.</summary>
    public string TwitchTokenUrl { get; set; } = "https://id.twitch.tv/oauth2/token";

    /// <summary>Gets or sets after how many days a game's data is fetched from IGDB again when someone opens it.</summary>
    public int RefreshDays { get; set; } = 30;

    /// <summary>
    /// Gets or sets the Steam Web API key (https://steamcommunity.com/dev/apikey), for importing a Steam library.
    /// Empty: the import is off. Never commit a real value.
    /// </summary>
    public string SteamApiKey { get; set; } = string.Empty;

    /// <summary>Gets or sets the Steam Web API address.</summary>
    public string SteamUrl { get; set; } = "https://api.steampowered.com/";

    /// <summary>
    /// Gets or sets the IsThereAnyDeal API key (https://isthereanydeal.com/apps/my/), for wishlist prices and alerts.
    /// Empty: no prices. Never commit a real value.
    /// </summary>
    public string ItadApiKey { get; set; } = string.Empty;

    /// <summary>Gets or sets the IsThereAnyDeal API address.</summary>
    public string ItadUrl { get; set; } = "https://api.isthereanydeal.com/";

    /// <summary>Gets or sets the country prices are for (two letters: PT, ES, GB, US…).</summary>
    public string PriceCountry { get; set; } = "PT";

    /// <summary>Gets a value indicating whether the IsThereAnyDeal key is set.</summary>
    public bool HasItad => !string.IsNullOrWhiteSpace(ItadApiKey);

    /// <summary>Gets or sets a value indicating whether the background jobs run (daily Steam sync, prices, catalog refresh).</summary>
    public bool JobsEnabled { get; set; } = true;

    /// <summary>Gets a value indicating whether the Steam Web API key is set.</summary>
    public bool HasSteam => !string.IsNullOrWhiteSpace(SteamApiKey);

    /// <summary>Gets a value indicating whether the IGDB credentials are set.</summary>
    public bool HasIgdb => !string.IsNullOrWhiteSpace(IgdbClientId) && !string.IsNullOrWhiteSpace(IgdbClientSecret);
}
