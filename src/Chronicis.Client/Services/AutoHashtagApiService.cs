using System.Net.Http.Json;
using Chronicis.Shared.DTOs;

namespace Chronicis.Client.Services;

public class AutoHashtagApiService : IAutoHashtagApiService
{
    private readonly HttpClient _http;
    private readonly ILogger<AutoHashtagApiService> _logger;

    public AutoHashtagApiService(HttpClient http, ILogger<AutoHashtagApiService> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<AutoHashtagResponse> PreviewAutoHashtagAsync(List<int>? articleIds = null)
    {
        try
        {
            var request = new AutoHashtagRequest
            {
                DryRun = true,
                ArticleIds = articleIds
            };

            var response = await _http.PostAsJsonAsync("api/articles/auto-hashtag", request);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<AutoHashtagResponse>()
                ?? throw new Exception("Failed to deserialize response");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error previewing auto-hashtag");
            throw;
        }
    }

    public async Task<AutoHashtagResponse> ApplyAutoHashtagAsync(List<int>? articleIds = null)
    {
        try
        {
            var request = new AutoHashtagRequest
            {
                DryRun = false,
                ArticleIds = articleIds
            };

            var response = await _http.PostAsJsonAsync("api/articles/auto-hashtag", request);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<AutoHashtagResponse>()
                ?? throw new Exception("Failed to deserialize response");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying auto-hashtag");
            throw;
        }
    }
}
