using System.Net.Http.Json;
using Chronicis.Shared.DTOs;
using Chronicis.Shared.DTOs.Maps;

namespace Chronicis.Client.Services;

/// <summary>
/// Service for anonymous public API operations.
/// Uses a separate HttpClient without authentication headers.
/// </summary>
public class PublicApiService : IPublicApiService
{
    private readonly HttpClient _http;
    private readonly ILogger<PublicApiService> _logger;

    public PublicApiService(HttpClient http, ILogger<PublicApiService> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<WorldDetailDto?> GetPublicWorldAsync(string publicSlug)
    {
        try
        {
            var response = await _http.GetAsync($"public/worlds/{publicSlug}");

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<WorldDetailDto>();
            }

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogDebug("Public world not found: {PublicSlug}", publicSlug);
                return null;
            }

            _logger.LogWarning("Failed to get public world {PublicSlug}: {StatusCode}",
                publicSlug, response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting public world {PublicSlug}", publicSlug);
            return null;
        }
    }

    public async Task<List<ArticleTreeDto>> GetPublicArticleTreeAsync(string publicSlug)
    {
        try
        {
            var response = await _http.GetAsync($"public/worlds/{publicSlug}/articles");

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<List<ArticleTreeDto>>()
                    ?? new List<ArticleTreeDto>();
            }

            _logger.LogWarning("Failed to get public article tree for {PublicSlug}: {StatusCode}",
                publicSlug, response.StatusCode);
            return new List<ArticleTreeDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting public article tree for {PublicSlug}", publicSlug);
            return new List<ArticleTreeDto>();
        }
    }

    public async Task<ArticleDto?> GetPublicArticleAsync(string publicSlug, string articlePath)
    {
        try
        {
            var response = await _http.GetAsync($"public/worlds/{publicSlug}/articles/{articlePath}");

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<ArticleDto>();
            }

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogDebug("Public article not found: {PublicSlug}/{ArticlePath}", publicSlug, articlePath);
                return null;
            }

            _logger.LogWarning("Failed to get public article {PublicSlug}/{ArticlePath}: {StatusCode}",
                publicSlug, articlePath, response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting public article {PublicSlug}/{ArticlePath}", publicSlug, articlePath);
            return null;
        }
    }

    public async Task<string?> ResolvePublicArticlePathAsync(string publicSlug, Guid articleId)
    {
        try
        {
            var response = await _http.GetAsync($"public/worlds/{publicSlug}/articles/resolve/{articleId}");

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync();
            }

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogDebug("Public article path not found: {PublicSlug}/{ArticleId}", publicSlug, articleId);
                return null;
            }

            _logger.LogWarning("Failed to resolve public article path {PublicSlug}/{ArticleId}: {StatusCode}",
                publicSlug, articleId, response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resolving public article path {PublicSlug}/{ArticleId}", publicSlug, articleId);
            return null;
        }
    }

    public async Task<(GetBasemapReadUrlResponseDto? Basemap, int? StatusCode, string? Error)> GetPublicMapBasemapReadUrlAsync(
        string publicSlug,
        Guid mapId)
    {
        return await GetEntityWithStatusAsync<GetBasemapReadUrlResponseDto>(
            $"public/worlds/{publicSlug}/maps/{mapId}/basemap",
            $"public basemap for map {mapId}");
    }

    public async Task<List<MapLayerDto>> GetPublicMapLayersAsync(string publicSlug, Guid mapId)
    {
        return await GetListAsync<MapLayerDto>(
            $"public/worlds/{publicSlug}/maps/{mapId}/layers",
            $"public layers for map {mapId}");
    }

    public async Task<List<MapPinResponseDto>> GetPublicMapPinsAsync(string publicSlug, Guid mapId)
    {
        return await GetListAsync<MapPinResponseDto>(
            $"public/worlds/{publicSlug}/maps/{mapId}/pins",
            $"public pins for map {mapId}");
    }

    public async Task<List<MapFeatureDto>> GetPublicMapFeaturesAsync(string publicSlug, Guid mapId)
    {
        return await GetListAsync<MapFeatureDto>(
            $"public/worlds/{publicSlug}/maps/{mapId}/features",
            $"public features for map {mapId}");
    }

    private async Task<List<T>> GetListAsync<T>(string url, string description)
    {
        try
        {
            var response = await _http.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<List<T>>()
                    ?? new List<T>();
            }

            _logger.LogWarning("Failed to get {Description}: {StatusCode}", description, response.StatusCode);
            return new List<T>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting {Description}", description);
            return new List<T>();
        }
    }

    private async Task<(T? Entity, int? StatusCode, string? Error)> GetEntityWithStatusAsync<T>(
        string url,
        string description) where T : class
    {
        try
        {
            var response = await _http.GetAsync(url);
            var statusCode = (int)response.StatusCode;

            if (!response.IsSuccessStatusCode)
            {
                var error = await TryReadErrorMessageAsync(response);
                _logger.LogWarning("Failed to fetch {Description}: {StatusCode}", description, response.StatusCode);
                return (null, statusCode, error);
            }

            var entity = await response.Content.ReadFromJsonAsync<T>();
            return (entity, statusCode, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching {Description} from {Url}", description, url);
            return (null, null, ex.Message);
        }
    }

    private static async Task<string?> TryReadErrorMessageAsync(HttpResponseMessage response)
    {
        try
        {
            var payload = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
            if (payload != null && payload.TryGetValue("error", out var error))
            {
                return error;
            }

            return null;
        }
        catch
        {
            return null;
        }
    }
}
