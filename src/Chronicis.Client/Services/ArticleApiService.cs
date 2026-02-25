using Chronicis.Shared.DTOs;

namespace Chronicis.Client.Services;

/// <summary>
/// Service for communicating with the Article API.
/// Uses HttpClientExtensions for consistent error handling and logging.
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

    public async Task<List<ArticleTreeDto>> GetRootArticlesAsync(Guid? worldId = null)
    {
        var url = worldId.HasValue
            ? $"articles?worldId={worldId.Value}"
            : "articles";

        return await _http.GetListAsync<ArticleTreeDto>(url, _logger, "root articles");
    }

    public async Task<List<ArticleTreeDto>> GetAllArticlesAsync(Guid? worldId = null)
    {
        var url = worldId.HasValue
            ? $"articles/all?worldId={worldId.Value}"
            : "articles/all";

        return await _http.GetListAsync<ArticleTreeDto>(url, _logger, "all articles");
    }

    public async Task<List<ArticleTreeDto>> GetChildrenAsync(Guid parentId)
    {
        return await _http.GetListAsync<ArticleTreeDto>(
            $"articles/{parentId}/children",
            _logger,
            $"children for article {parentId}");
    }

    public async Task<ArticleDto?> GetArticleDetailAsync(Guid id)
    {
        return await _http.GetEntityAsync<ArticleDto>(
            $"articles/{id}",
            _logger,
            $"article {id}");
    }

    public async Task<ArticleDto?> GetArticleAsync(Guid id) => await GetArticleDetailAsync(id);

    public async Task<ArticleDto?> GetArticleByPathAsync(string path)
    {
        var encodedPath = string.Join("/", path.Split('/').Select(Uri.EscapeDataString));

        return await _http.GetEntityAsync<ArticleDto>(
            $"articles/by-path/{encodedPath}",
            _logger,
            $"article at path '{path}'");
    }

    public async Task<ArticleDto?> CreateArticleAsync(ArticleCreateDto dto)
    {
        return await _http.PostEntityAsync<ArticleDto>(
            "articles",
            dto,
            _logger,
            "article");
    }

    public async Task<ArticleDto?> UpdateArticleAsync(Guid id, ArticleUpdateDto dto)
    {
        return await _http.PutEntityAsync<ArticleDto>(
            $"articles/{id}",
            dto,
            _logger,
            $"article {id}");
    }

    public async Task<bool> DeleteArticleAsync(Guid id)
    {
        return await _http.DeleteEntityAsync(
            $"articles/{id}",
            _logger,
            $"article {id}");
    }

    public async Task<bool> MoveArticleAsync(Guid articleId, Guid? newParentId, Guid? newSessionId = null)
    {
        var moveDto = new ArticleMoveDto
        {
            NewParentId = newParentId,
            NewSessionId = newSessionId
        };

        return await _http.PutBoolAsync(
            $"articles/{articleId}/move",
            moveDto,
            _logger,
            $"move article {articleId}");
    }

    public async Task<List<ArticleSearchResultDto>> SearchArticlesAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return new List<ArticleSearchResultDto>();

        var results = await _http.GetEntityAsync<GlobalSearchResultsDto>(
            $"articles/search?query={Uri.EscapeDataString(query)}",
            _logger,
            $"search results for '{query}'");

        if (results == null)
            return new List<ArticleSearchResultDto>();

        // Combine all match types into a single list
        var allResults = new List<ArticleSearchResultDto>();
        allResults.AddRange(results.TitleMatches);
        allResults.AddRange(results.BodyMatches);
        allResults.AddRange(results.HashtagMatches);
        return allResults;
    }

    public async Task<List<ArticleSearchResultDto>> SearchArticlesByTitleAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return new List<ArticleSearchResultDto>();

        var results = await _http.GetEntityAsync<GlobalSearchResultsDto>(
            $"articles/search?query={Uri.EscapeDataString(query)}",
            _logger,
            $"title search results for '{query}'");

        return results?.TitleMatches ?? new List<ArticleSearchResultDto>();
    }

    public async Task<ArticleDto?> UpdateAliasesAsync(Guid articleId, string aliases)
    {
        var dto = new ArticleAliasesUpdateDto { Aliases = aliases ?? string.Empty };

        return await _http.PutEntityAsync<ArticleDto>(
            $"articles/{articleId}/aliases",
            dto,
            _logger,
            $"aliases for article {articleId}");
    }
}
