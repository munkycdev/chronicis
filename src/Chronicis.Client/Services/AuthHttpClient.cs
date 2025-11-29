using System.Net.Http.Headers;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;

namespace Chronicis.Client.Services;

public class AuthHttpClient
{
    private readonly HttpClient _httpClient;
    private readonly IAccessTokenProvider _tokenProvider;

    public AuthHttpClient(HttpClient httpClient, IAccessTokenProvider tokenProvider)
    {
        _httpClient = httpClient;
        _tokenProvider = tokenProvider;
    }

    public async Task<HttpClient> GetAuthenticatedClientAsync()
    {
        var tokenResult = await _tokenProvider.RequestAccessToken();

        if (tokenResult.TryGetToken(out var token))
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token.Value);
        }

        return _httpClient;
    }
}