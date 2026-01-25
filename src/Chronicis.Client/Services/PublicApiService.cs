using System.Net.Http.Json;
using Chronicis.Shared.DTOs;
using Microsoft.Extensions.Logging;

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
}
