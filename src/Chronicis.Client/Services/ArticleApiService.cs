using System.Net.Http.Json;
using Chronicis.Shared.DTOs;
using Chronicis.Shared.Models;

namespace Chronicis.Client.Services;

/// <summary>
/// Service for communicating with the Article API.
/// </summary>
public class ArticleApiService : IArticleApiService
{
    private readonly HttpClient _http;
    private readonly ILogger<ArticleApiService> _logger;

    public ArticleApiService(HttpClient http, ILogger<ArticleApiService> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<List<ArticleTreeDto>> GetRootArticlesAsync()
    {
        try
        {
            var articles = await _http.GetFromJsonAsync<List<ArticleTreeDto>>("api/articles");
            return articles ?? new List<ArticleTreeDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching root articles");
            throw;
        }
    }

    public async Task<List<ArticleTreeDto>> GetAllArticlesAsync()
    {
        try
        {
            var articles = await _http.GetFromJsonAsync<List<ArticleTreeDto>>("api/articles/all");
            return articles ?? new List<ArticleTreeDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching all articles");
            throw;
        }
    }

    public async Task<List<ArticleTreeDto>> GetChildrenAsync(int parentId)
    {
        try
        {
            var children = await _http.GetFromJsonAsync<List<ArticleTreeDto>>($"api/articles/{parentId}/children");
            return children ?? new List<ArticleTreeDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching children for article {ParentId}", parentId);
            throw;
        }
    }

    public async Task<ArticleDto?> GetArticleDetailAsync(int id)
    {
        try
        {
            return await _http.GetFromJsonAsync<ArticleDto>($"api/articles/{id}");
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogWarning("Article {ArticleId} not found", id);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching article detail for {ArticleId}", id);
            throw;
        }
    }

    public async Task<ArticleDto?> GetArticleAsync(int id) => await GetArticleDetailAsync(id);

    public async Task<ArticleDto?> GetArticleByPathAsync(string path)
    {
        try
        {
            var encodedPath = string.Join("/", path.Split('/').Select(Uri.EscapeDataString));
            return await _http.GetFromJsonAsync<ArticleDto>($"api/articles/by-path/{encodedPath}");
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogWarning("Article not found at path: {Path}", path);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching article by path: {Path}", path);
            throw;
        }
    }

    public async Task<ArticleDto> CreateArticleAsync(ArticleCreateDto dto)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("api/articles", dto);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<ArticleDto>()
                ?? throw new Exception("Failed to deserialize created article");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating article");
            throw;
        }
    }

    public async Task<ArticleDto> UpdateArticleAsync(int id, ArticleUpdateDto dto)
    {
        try
        {
            var response = await _http.PutAsJsonAsync($"api/articles/{id}", dto);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<ArticleDto>()
                ?? throw new Exception("Failed to deserialize updated article");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating article {ArticleId}", id);
            throw;
        }
    }

    public async Task DeleteArticleAsync(int id)
    {
        try
        {
            var response = await _http.DeleteAsync($"api/articles/{id}");
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting article {ArticleId}", id);
            throw;
        }
    }

    public async Task<bool> MoveArticleAsync(int articleId, int? newParentId)
    {
        try
        {
            var moveDto = new ArticleMoveDto { NewParentId = newParentId };
            var response = await _http.PatchAsJsonAsync($"api/articles/{articleId}/parent", moveDto);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Failed to move article {ArticleId}: {Error}", articleId, errorContent);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error moving article {ArticleId}", articleId);
            throw;
        }
    }

    public async Task<List<ArticleSearchResultDto>> SearchArticlesAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return new List<ArticleSearchResultDto>();

        try
        {
            var results = await _http.GetFromJsonAsync<GlobalSearchResultsDto>(
                $"api/articles/search?query={Uri.EscapeDataString(query)}");

            if (results == null)
                return new List<ArticleSearchResultDto>();

            // Combine all match types into a single list
            var allResults = new List<ArticleSearchResultDto>();
            allResults.AddRange(results.TitleMatches);
            allResults.AddRange(results.BodyMatches);
            allResults.AddRange(results.HashtagMatches);
            return allResults;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching articles");
            throw;
        }
    }

    public async Task<List<ArticleSearchResultDto>> SearchArticlesByTitleAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return new List<ArticleSearchResultDto>();

        try
        {
            // Use the global search endpoint and extract just title matches
            var results = await _http.GetFromJsonAsync<GlobalSearchResultsDto>(
                $"api/articles/search?query={Uri.EscapeDataString(query)}");
            return results?.TitleMatches ?? new List<ArticleSearchResultDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching articles by title");
            throw;
        }
    }

    public async Task<List<BacklinkDto>> GetArticleBacklinksAsync(int articleId)
    {
        try
        {
            var backlinks = await _http.GetFromJsonAsync<List<BacklinkDto>>($"api/articles/{articleId}/backlinks");
            return backlinks ?? new List<BacklinkDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching backlinks for article {ArticleId}", articleId);
            return new List<BacklinkDto>();
        }
    }

    public async Task<List<HashtagDto>> GetArticleHashtagsAsync(int articleId)
    {
        try
        {
            var hashtags = await _http.GetFromJsonAsync<List<HashtagDto>>($"api/articles/{articleId}/hashtags") ?? new List<HashtagDto>();

            return hashtags ?? new List<HashtagDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching hashtags for article {ArticleId}", articleId);
            return new List<HashtagDto>();
        }
    }
}
