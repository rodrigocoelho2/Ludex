using Ludex.Library.Constants;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Ludex.Sdk;

/// <summary>
/// Where the WebApi is and the key to call it, bound from the "LudexApi" section.
/// </summary>
public sealed class LudexClientOptions
{
    /// <summary>The configuration section name.</summary>
    public const string SectionName = "LudexApi";

    /// <summary>Gets or sets the WebApi base URL, e.g. http://webapi:8080/.</summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>Gets or sets the key sent in x-api-key.</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Gets or sets the request timeout.</summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(100);
}

/// <summary>
/// Registers <see cref="LudexClient"/>.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the typed client with its base address and x-api-key header.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration holding the "LudexApi" section.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddLudexClient(this IServiceCollection services, IConfiguration configuration)
    {
        var options = configuration.GetSection(LudexClientOptions.SectionName).Get<LudexClientOptions>() ?? new LudexClientOptions();
        if (!Uri.TryCreate(options.Url, UriKind.Absolute, out var baseAddress))
        {
            throw new InvalidOperationException("LudexApi:Url must be an absolute URL (env LudexApi__Url).");
        }

        if (string.IsNullOrWhiteSpace(options.ApiKey))
        {
            throw new InvalidOperationException("LudexApi:ApiKey is required (env LudexApi__ApiKey).");
        }

        // A trailing slash makes relative paths ("library/home") append to the base path instead of replacing it.
        var normalizedBase = baseAddress.AbsoluteUri.EndsWith('/') ? baseAddress : new Uri(baseAddress.AbsoluteUri + "/");

        services.AddHttpClient<LudexClient>(client =>
        {
            client.BaseAddress = normalizedBase;
            client.Timeout = options.Timeout;
            client.DefaultRequestHeaders.Add(ApiHeaders.ApiKey, options.ApiKey);
        });

        return services;
    }
}
