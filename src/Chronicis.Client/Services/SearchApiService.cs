using Chronicis.Shared.DTOs;
using System.Net.Http.Json;
using System.Web;

namespace Chronicis.Client.Services;

public interface ISearchApiService
{
    Task<GlobalSearchResultsDto?> SearchContentAsync(string query);
}

public class SearchApiService : ISearchApiService
{
    private readonly HttpClient _httpClient;

    public SearchApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
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
                Console.WriteLine($"Search failed with status: {response.StatusCode}");
                return null;
            }

            return await response.Content.ReadFromJsonAsync<GlobalSearchResultsDto>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error searching content: {ex.Message}");
            return null;
        }
    }
}
