using Chronicis.Client.Services;

namespace Chronicis.Client.Extensions;

/// <summary>
/// Extension methods for configuring HTTP clients.
/// </summary>
public static class HttpClientServiceExtensions
{
    /// <summary>
    /// Adds all Chronicis HTTP clients including authenticated API client, 
    /// public API client, and external service clients.
    /// </summary>
    public static IServiceCollection AddChronicisHttpClients(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var apiBaseUrl = configuration["ApiBaseUrl"] ?? "http://localhost:7071";

        // Register the auth handler
        services.AddScoped<ChronicisAuthHandler>();

        // Chronicis API client (with auth)
        services.AddHttpClient("ChronicisApi", client =>
        {
            client.BaseAddress = new Uri(apiBaseUrl);
        })
        .AddHttpMessageHandler<ChronicisAuthHandler>()
        .RemoveAllLoggers();

        // Public API client (no auth)
        services.AddHttpClient("ChronicisPublicApi", client =>
        {
            client.BaseAddress = new Uri(apiBaseUrl);
        }).RemoveAllLoggers();

        // Quote service (external API)
        services.AddHttpClient<IQuoteService, QuoteService>(client =>
        {
            client.BaseAddress = new Uri("https://api.quotable.io/");
            client.Timeout = TimeSpan.FromSeconds(10);
        }).RemoveAllLoggers();

        return services;
    }
}
