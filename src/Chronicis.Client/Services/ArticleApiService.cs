using Chronicis.Shared.DTOs;
using System.Net.Http.Json;

namespace Chronicis.Client.Services
{
    /// <summary>
    /// Service for communicating with the Article API.
    /// Handles all HTTP requests to the backend Azure Functions.
    /// </summary>
    public class ArticleApiService : IArticleApiService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ArticleApiService> _logger;

        public ArticleApiService(HttpClient httpClient, ILogger<ArticleApiService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        /// <summary>
        /// Fetch all root-level articles with their children.
        /// </summary>
        public async Task<List<ArticleTreeDto>> GetRootArticlesAsync()
        {
            try
            {
                _logger.LogInformation("Fetching root articles from API");
                var articles = await _httpClient.GetFromJsonAsync<List<ArticleTreeDto>>("api/articles");
                return articles ?? new List<ArticleTreeDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching root articles");
                throw;
            }
        }

        /// <summary>
        /// Fetch child articles for a specific parent.
        /// </summary>
        public async Task<List<ArticleTreeDto>> GetChildrenAsync(int parentId)
        {
            try
            {
                _logger.LogInformation("Fetching children for article {ParentId}", parentId);
                var children = await _httpClient.GetFromJsonAsync<List<ArticleTreeDto>>($"api/articles/{parentId}/children");
                return children ?? new List<ArticleTreeDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching children for article {ParentId}", parentId);
                throw;
            }
        }

        /// <summary>
        /// Fetch detailed information for a specific article.
        /// </summary>
        public async Task<ArticleDto?> GetArticleDetailAsync(int id)
        {
            try
            {
                _logger.LogInformation("Fetching article detail for {ArticleId}", id);
                var article = await _httpClient.GetFromJsonAsync<ArticleDto>($"api/articles/{id}");
                return article;
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

        /// <summary>
        /// Alias for GetArticleDetailAsync - used by Phase 5 components.
        /// </summary>
        public async Task<ArticleDto?> GetArticleAsync(int id)
        {
            return await GetArticleDetailAsync(id);
        }

        /// <summary>
        /// Create a new article.
        /// </summary>
        public async Task<ArticleDto> CreateArticleAsync(ArticleCreateDto dto)
        {
            try
            {
                _logger.LogInformation("Creating new article: {Title}", dto.Title);
                var response = await _httpClient.PostAsJsonAsync("api/articles", dto);
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

        /// <summary>
        /// Update an existing article.
        /// </summary>
        public async Task<ArticleDto> UpdateArticleAsync(int id, ArticleUpdateDto dto)
        {
            try
            {
                _logger.LogInformation("Updating article {ArticleId}", id);
                var response = await _httpClient.PutAsJsonAsync($"api/articles/{id}", dto);
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

        /// <summary>
        /// Delete an article.
        /// </summary>
        public async Task DeleteArticleAsync(int id)
        {
            try
            {
                _logger.LogInformation("Deleting article {ArticleId}", id);
                var response = await _httpClient.DeleteAsync($"api/articles/{id}");
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting article {ArticleId}", id);
                throw;
            }
        }

        /// <summary>
        /// Search articles by query string (Phase 3).
        /// </summary>
        public async Task<List<ArticleSearchResultDto>> SearchArticlesAsync(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return new List<ArticleSearchResultDto>();
            }

            try
            {
                _logger.LogInformation("Searching articles with query: {Query}", query);
                var results = await _httpClient.GetFromJsonAsync<List<ArticleSearchResultDto>>(
                    $"api/articles/search?query={Uri.EscapeDataString(query)}");
                return results ?? new List<ArticleSearchResultDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching articles");
                throw;
            }
        }
    }
}