using System.Net.Http.Json;
using System.Web;
using Chronicis.Shared.DTOs;

namespace Chronicis.Client.Services;

public interface ISearchApiService
{
    Task<GlobalSearchResultsDto?> SearchContentAsync(string query);
}

public class SearchApiService : ISearchApiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<SearchApiService> _logger;

    public SearchApiService(HttpClient httpClient, ILogger<SearchApiService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<GlobalSearchResultsDto?> SearchContentAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return null;

        try
        {
            var encodedQuery = HttpUtility.UrlEncode(query);
            var response = await _httpClient.GetAsync($"/api/articles/search?query={encodedQuery}");

            if (!response.IsSuccessStatusCode)
            {
                
                _logger.LogError($"Search failed with status: {response.StatusCode}");
                return null;
            }

            return await response.Content.ReadFromJsonAsync<GlobalSearchResultsDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error searching content: {ex.Message}");
            return null;
        }
    }
}
