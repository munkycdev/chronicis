using System.Net.Http.Headers;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;

namespace Chronicis.Client.Services;

/// <summary>
/// DelegatingHandler that automatically attaches the Auth0 bearer token to all outgoing requests.
/// Uses X-Auth0-Token header to bypass Azure Static Web Apps' auth interception.
/// </summary>
public class ChronicisAuthHandler : DelegatingHandler
{
    private readonly IAccessTokenProvider _tokenProvider;

    public ChronicisAuthHandler(IAccessTokenProvider tokenProvider)
    {
        _tokenProvider = tokenProvider;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var tokenResult = await _tokenProvider.RequestAccessToken(
            new AccessTokenRequestOptions
            {
                Scopes = new[] { "openid", "profile", "email" }
            });

        if (tokenResult.TryGetToken(out var token))
        {
            // Use custom header to bypass Azure SWA's auth interception
            // SWA intercepts and replaces the standard Authorization header
            request.Headers.Add("X-Auth0-Token", token.Value);
            
            // Also set standard header for local development
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Value);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
