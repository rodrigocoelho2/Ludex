using System.Text.Json.Serialization;
using Ludex.WebApi.Extensions.Authentication;
using Ludex.WebApi.Extensions.Data;
using Ludex.WebApi.Extensions.Globalization;
using Ludex.WebApi.Extensions.Mvc;
using Ludex.WebApi.Features;
using Ludex.WebApi.Features.Catalog;
using Ludex.WebApi.Features.Friends.Repositories;
using Ludex.WebApi.Features.Friends.Services;
using Ludex.WebApi.Features.Games.Repositories;
using Ludex.WebApi.Features.Games.Services;
using Ludex.WebApi.Features.Library.Repositories;
using Ludex.WebApi.Features.Library.Services;
using Ludex.WebApi.Features.Profile.Repositories;
using Ludex.WebApi.Features.Profile.Services;
using Ludex.WebApi.Features.Steam.Services;
using Ludex.WebApi.Features.Users.Repositories;
using Ludex.WebApi.Features.Users.Services;
using Microsoft.AspNetCore.Authentication;

namespace Ludex.WebApi;

/// <summary>
/// Service registration and request pipeline of the WebApi.
/// </summary>
internal static class LudexStartup
{
    /// <summary>
    /// Registers every service. All services and repositories are singletons; each repository is registered before its
    /// service. With IGDB credentials the catalog is IGDB, without them the list built into Ludex.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <param name="environment">The hosting environment.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddLudex(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment)
    {
        var section = configuration.GetSection(LudexOptions.SectionName);
        var options = section.Get<LudexOptions>() ?? new LudexOptions();
        services.AddOptions<LudexOptions>()
            .Bind(section)
            .Validate(static o => !string.IsNullOrWhiteSpace(o.ApiKey), "Ludex:ApiKey is required (env Ludex__ApiKey, LUDEX_API_KEY in .env).")
            .Validate(
                static o => string.IsNullOrWhiteSpace(o.IgdbClientId) == string.IsNullOrWhiteSpace(o.IgdbClientSecret),
                "Set both IGDB_CLIENT_ID and IGDB_CLIENT_SECRET (a Twitch application), or neither.")
            .ValidateOnStart();

        services.AddControllers()
            .ConfigureApplicationPartManager(manager => manager.FeatureProviders.Add(new InternalControllerFeatureProvider()))
            .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

        services.AddAuthentication(ApiKeyAuthenticationHandler.SchemeName)
            .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(ApiKeyAuthenticationHandler.SchemeName, null);
        services.AddAuthorization();

        if (environment.IsDevelopment())
        {
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(o => o.EnableAnnotations());
        }

        // Outgoing calls: Twitch (the token) and IGDB.
        services.AddHttpClient(IgdbCatalog.HttpClientName, static client =>
        {
            client.Timeout = TimeSpan.FromSeconds(20);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Ludex/1.0 (+https://github.com/rodrigocoelho2/Ludex)");
        });

        // The Steam Web API (the Steam import).
        services.AddHttpClient(SteamClient.HttpClientName, static client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Ludex/1.0 (+https://github.com/rodrigocoelho2/Ludex)");
        });

        // Infrastructure
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IGlobalizationHelper, GlobalizationHelper>();
        services.AddSingleton<ISqlConnectionFactory, SqlConnectionFactory>();

        // The catalog
        if (options.HasIgdb)
        {
            services.AddSingleton<IGameCatalog, IgdbCatalog>();
        }
        else
        {
            services.AddSingleton<IGameCatalog, OfflineCatalog>();
        }

        // Repositories
        services.AddSingleton<IUserRepository, UserRepository>();
        services.AddSingleton<IGameRepository, GameRepository>();
        services.AddSingleton<ILibraryRepository, LibraryRepository>();
        services.AddSingleton<IFriendRepository, FriendRepository>();
        services.AddSingleton<IProfileRepository, ProfileRepository>();

        // Services
        services.AddSingleton<GameStore>();
        services.AddSingleton<IUserService, UserService>();
        services.AddSingleton<IGameService, GameService>();
        services.AddSingleton<ILibraryService, LibraryService>();
        services.AddSingleton<IFriendService, FriendService>();
        services.AddSingleton<IProfileService, ProfileService>();
        services.AddSingleton<SteamClient>();
        services.AddSingleton<ISteamService, SteamService>();

        return services;
    }

    /// <summary>
    /// Configures the request pipeline.
    /// </summary>
    /// <param name="app">The application.</param>
    /// <returns>The application.</returns>
    public static WebApplication UseLudex(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        var catalog = app.Services.GetRequiredService<IGameCatalog>();
        app.Logger.LogInformation(
            "Game catalog: {Catalog}",
            catalog.Source == Ludex.Library.Enums.CatalogSource.Igdb ? "IGDB" : "the built-in list (set IGDB_CLIENT_ID and IGDB_CLIENT_SECRET for IGDB)");
        app.Logger.LogInformation(
            "Steam import: {Steam}",
            app.Services.GetRequiredService<Microsoft.Extensions.Options.IOptions<LudexOptions>>().Value.HasSteam ? "on" : "off (set STEAM_API_KEY)");

        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();

        return app;
    }
}
