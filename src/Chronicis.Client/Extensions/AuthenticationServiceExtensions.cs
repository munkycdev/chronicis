using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.Extensions.DependencyInjection;

namespace Chronicis.Client.Extensions;

/// <summary>
/// Extension methods for configuring authentication services.
/// </summary>
public static class AuthenticationServiceExtensions
{
    /// <summary>
    /// Adds Auth0 OIDC authentication configuration for Chronicis.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="baseUrl">The base URL of the application for redirect URIs.</param>
    public static IServiceCollection AddChronicisAuthentication(
        this IServiceCollection services, 
        string baseUrl)
    {
        services.AddOidcAuthentication(options =>
        {
            options.ProviderOptions.Authority = "https://auth.chronicis.app";
            options.ProviderOptions.ClientId = "Itq22vH9FBHKlYHL1j0A9EgVjA9f6NZQ";
            options.ProviderOptions.ResponseType = "code";
            options.ProviderOptions.RedirectUri = $"{baseUrl}/authentication/login-callback";
            options.ProviderOptions.PostLogoutRedirectUri = baseUrl;
            
            // Auth0 requires the audience parameter to issue a proper JWT access token
            options.ProviderOptions.AdditionalProviderParameters.Add("audience", "https://api.chronicis.app");

            options.ProviderOptions.DefaultScopes.Clear();
            options.ProviderOptions.DefaultScopes.Add("openid");
            options.ProviderOptions.DefaultScopes.Add("profile");
            options.ProviderOptions.DefaultScopes.Add("email");
        });

        return services;
    }
}
