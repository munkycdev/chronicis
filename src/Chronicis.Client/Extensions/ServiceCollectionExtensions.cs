using Microsoft.JSInterop;
using MudBlazor;

namespace Chronicis.Client.Extensions;

/// <summary>
/// Extension methods for IServiceCollection to simplify API service registrations.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers a Chronicis API service with the standard dependencies (HttpClient via factory, ILogger).
    /// </summary>
    /// <typeparam name="TInterface">The service interface type.</typeparam>
    /// <typeparam name="TImplementation">The service implementation type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="httpClientName">The name of the configured HTTP client (default: "ChronicisApi").</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddChronicisApiService<TInterface, TImplementation>(
        this IServiceCollection services,
        string httpClientName = "ChronicisApi")
        where TInterface : class
        where TImplementation : class, TInterface
    {
        services.AddScoped<TInterface>(sp =>
        {
            var factory = sp.GetRequiredService<IHttpClientFactory>();
            var logger = sp.GetRequiredService<ILogger<TImplementation>>();
            var httpClient = factory.CreateClient(httpClientName);

            return (TInterface)Activator.CreateInstance(typeof(TImplementation), httpClient, logger)!;
        });

        return services;
    }

    /// <summary>
    /// Registers a Chronicis API service with additional ISnackbar dependency.
    /// </summary>
    /// <typeparam name="TInterface">The service interface type.</typeparam>
    /// <typeparam name="TImplementation">The service implementation type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="httpClientName">The name of the configured HTTP client (default: "ChronicisApi").</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddChronicisApiServiceWithSnackbar<TInterface, TImplementation>(
        this IServiceCollection services,
        string httpClientName = "ChronicisApi")
        where TInterface : class
        where TImplementation : class, TInterface
    {
        services.AddScoped<TInterface>(sp =>
        {
            var factory = sp.GetRequiredService<IHttpClientFactory>();
            var logger = sp.GetRequiredService<ILogger<TImplementation>>();
            var snackbar = sp.GetRequiredService<ISnackbar>();
            var httpClient = factory.CreateClient(httpClientName);

            return (TInterface)Activator.CreateInstance(typeof(TImplementation), httpClient, logger, snackbar)!;
        });

        return services;
    }

    /// <summary>
    /// Registers a Chronicis API service with additional IJSRuntime dependency.
    /// </summary>
    /// <typeparam name="TInterface">The service interface type.</typeparam>
    /// <typeparam name="TImplementation">The service implementation type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="httpClientName">The name of the configured HTTP client (default: "ChronicisApi").</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddChronicisApiServiceWithJSRuntime<TInterface, TImplementation>(
        this IServiceCollection services,
        string httpClientName = "ChronicisApi")
        where TInterface : class
        where TImplementation : class, TInterface
    {
        services.AddScoped<TInterface>(sp =>
        {
            var factory = sp.GetRequiredService<IHttpClientFactory>();
            var jsRuntime = sp.GetRequiredService<IJSRuntime>();
            var logger = sp.GetRequiredService<ILogger<TImplementation>>();
            var httpClient = factory.CreateClient(httpClientName);

            return (TInterface)Activator.CreateInstance(typeof(TImplementation), httpClient, jsRuntime, logger)!;
        });

        return services;
    }

    /// <summary>
    /// Registers a concrete Chronicis API service (no interface).
    /// </summary>
    /// <typeparam name="TImplementation">The service implementation type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="httpClientName">The name of the configured HTTP client (default: "ChronicisApi").</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddChronicisApiServiceConcrete<TImplementation>(
        this IServiceCollection services,
        string httpClientName = "ChronicisApi")
        where TImplementation : class
    {
        services.AddScoped<TImplementation>(sp =>
        {
            var factory = sp.GetRequiredService<IHttpClientFactory>();
            var logger = sp.GetRequiredService<ILogger<TImplementation>>();
            var httpClient = factory.CreateClient(httpClientName);

            return (TImplementation)Activator.CreateInstance(typeof(TImplementation), httpClient, logger)!;
        });

        return services;
    }
}
