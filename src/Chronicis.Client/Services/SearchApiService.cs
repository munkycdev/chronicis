using System.Web;
using Chronicis.Shared.DTOs;

namespace Chronicis.Client.Services;

/// <summary>
/// Service for search API operations.
/// Uses HttpClientExtensions for consistent error handling and logging.
/// </summary>
public class SearchApiService : ISearchApiService
{
    private readonly HttpClient _http;
    private readonly ILogger<SearchApiService> _logger;

    public SearchApiService(HttpClient http, ILogger<SearchApiService> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<GlobalSearchResultsDto?> SearchContentAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return null;

        var encodedQuery = HttpUtility.UrlEncode(query);

        return await _http.GetEntityAsync<GlobalSearchResultsDto>(
            $"search?query={encodedQuery}",
            _logger,
            $"search results for '{query}'");
    }
}
