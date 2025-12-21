// Services/HashtagApiService.cs
using System.Net.Http.Json;
using Chronicis.Shared.DTOs;

namespace Chronicis.Client.Services;

/// <summary>
/// Service for calling hashtag-related API endpoints
/// </summary>
public class HashtagApiService : IHashtagApiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<HashtagApiService> _logger;

    public HashtagApiService(HttpClient httpClient, ILogger<HashtagApiService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <summary>
    /// Get all hashtags with usage counts
    /// </summary>
    public async Task<List<HashtagDto>> GetAllHashtagsAsync()
    {
        try
        {
            var hashtags = await _httpClient.GetFromJsonAsync<List<HashtagDto>>("api/hashtags");
            return hashtags ?? new List<HashtagDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error fetching hashtags: {ex.Message}");
            return new List<HashtagDto>();
        }
    }

    /// <summary>
    /// Get a specific hashtag by name
    /// </summary>
    public async Task<HashtagDto?> GetHashtagByNameAsync(string name)
    {
        try
        {
            var hashtag = await _httpClient.GetFromJsonAsync<HashtagDto>($"api/hashtags/{name}");
            return hashtag;
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error fetching hashtag '{name}': {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Link a hashtag to an article
    /// </summary>
    public async Task<bool> LinkHashtagAsync(string hashtagName, Guid articleId)
    {
        try
        {
            var linkDto = new LinkHashtagDto { ArticleId = articleId };
            var response = await _httpClient.PostAsJsonAsync($"api/hashtags/{hashtagName}/link", linkDto);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error linking hashtag '{hashtagName}': {ex.Message}");
            return false;
        }
    }

    public async Task<HashtagPreviewDto?> GetHashtagPreviewAsync(string name)
    {
        try
        {
            var encodedName = Uri.EscapeDataString(name);
            return await _httpClient.GetFromJsonAsync<HashtagPreviewDto>($"api/hashtags/{encodedName}/preview");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error fetching hashtag preview: {ex.Message}");
            return null;
        }
    }
}
