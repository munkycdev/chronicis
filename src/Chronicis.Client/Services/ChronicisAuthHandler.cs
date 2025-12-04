using System.Net.Http.Headers;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;

namespace Chronicis.Client.Services;

/// <summary>
/// DelegatingHandler that automatically attaches the Auth0 bearer token to all outgoing requests.
/// Used by IHttpClientFactory to create pre-configured HttpClient instances.
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
        // Request access token with the API audience
        var tokenResult = await _tokenProvider.RequestAccessToken(
            new AccessTokenRequestOptions
            {
                Scopes = new[] { "openid", "profile", "email" }
            });

        if (tokenResult.TryGetToken(out var token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Value);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
