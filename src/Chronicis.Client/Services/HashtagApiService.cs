// Services/HashtagApiService.cs
using Chronicis.Shared.DTOs;
using System.Net.Http.Json;

namespace Chronicis.Client.Services;

/// <summary>
/// Service for calling hashtag-related API endpoints
/// </summary>
public class HashtagApiService : IHashtagApiService
{
    private readonly HttpClient _httpClient;

    public HashtagApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
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
            Console.WriteLine($"Error fetching hashtags: {ex.Message}");
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
            Console.WriteLine($"Error fetching hashtag '{name}': {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Link a hashtag to an article (Phase 7+ feature)
    /// </summary>
    public async Task<bool> LinkHashtagAsync(string hashtagName, int articleId)
    {
        try
        {
            var linkDto = new LinkHashtagDto { ArticleId = articleId };
            var response = await _httpClient.PostAsJsonAsync($"api/hashtags/{hashtagName}/link", linkDto);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error linking hashtag '{hashtagName}': {ex.Message}");
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
            Console.Error.WriteLine($"Error fetching hashtag preview: {ex.Message}");
            return null;
        }
    }
}
