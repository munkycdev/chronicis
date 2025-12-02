using Chronicis.Shared.DTOs;
using System.Net.Http.Json;

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
            _logger.LogInformation("Fetching root articles from API");
            var articles = await _http.GetFromJsonAsync<List<ArticleTreeDto>>("api/articles");
            return articles ?? new List<ArticleTreeDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching root articles");
            throw;
        }
    }

    public async Task<List<ArticleTreeDto>> GetChildrenAsync(int parentId)
    {
        try
        {
            _logger.LogInformation("Fetching children for article {ParentId}", parentId);
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
            _logger.LogInformation("Fetching article detail for {ArticleId}", id);
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

    public async Task<ArticleDto> CreateArticleAsync(ArticleCreateDto dto)
    {
        try
        {
            _logger.LogInformation("Creating new article: {Title}", dto.Title);
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
            _logger.LogInformation("Updating article {ArticleId}", id);
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
            _logger.LogInformation("Deleting article {ArticleId}", id);
            var response = await _http.DeleteAsync($"api/articles/{id}");
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting article {ArticleId}", id);
            throw;
        }
    }

    public async Task<List<ArticleSearchResultDto>> SearchArticlesAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return new List<ArticleSearchResultDto>();

        try
        {
            _logger.LogInformation("Searching articles with query: {Query}", query);
            var results = await _http.GetFromJsonAsync<List<ArticleSearchResultDto>>(
                $"api/articles/search?query={Uri.EscapeDataString(query)}");
            return results ?? new List<ArticleSearchResultDto>();
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
            _logger.LogInformation("Searching articles by title: {Query}", query);
            var results = await _http.GetFromJsonAsync<List<ArticleSearchResultDto>>(
                $"api/articles/search/title?query={Uri.EscapeDataString(query)}");
            return results ?? new List<ArticleSearchResultDto>();
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
}
