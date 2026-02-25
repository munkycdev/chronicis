using Chronicis.Shared.DTOs;

namespace Chronicis.Client.Services;

/// <summary>
/// Client API service for tutorial resolve endpoints.
/// </summary>
public class TutorialApiService : ITutorialApiService
{
    private readonly HttpClient _http;
    private readonly ILogger<TutorialApiService> _logger;

    public TutorialApiService(HttpClient http, ILogger<TutorialApiService> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<TutorialDto?> ResolveAsync(string pageType)
    {
        if (string.IsNullOrWhiteSpace(pageType))
        {
            return null;
        }

        var encodedPageType = Uri.EscapeDataString(pageType);
        return await _http.GetEntityAsync<TutorialDto>(
            $"tutorials/resolve?pageType={encodedPageType}",
            _logger,
            $"tutorial for page type '{pageType}'");
    }
}
