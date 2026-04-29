using System.Net.Http.Json;
using Chronicis.Shared.Routing;

namespace Chronicis.Client.Services;

/// <summary>
/// Client service for the unified path-resolution endpoint.
/// </summary>
public class PathApiService : IPathApiService
{
    private readonly HttpClient _http;
    private readonly ILogger<PathApiService> _logger;

    public PathApiService(HttpClient http, ILogger<PathApiService> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<SlugPathResolution?> ResolveAsync(string path, CancellationToken cancellationToken = default)
    {
        var encodedPath = string.Join("/", path.Trim('/').Split('/').Select(Uri.EscapeDataString));

        try
        {
            var response = await _http.GetAsync($"paths/resolve/{encodedPath}", cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return null;

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Path resolution failed for '{Path}': {StatusCode}", path, response.StatusCode);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<SlugPathResolution>(cancellationToken: cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error resolving path '{Path}'", path);
            return null;
        }
    }
}
