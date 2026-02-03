using Chronicis.Shared.DTOs;
using Microsoft.Extensions.Logging;

namespace Chronicis.Client.Services;

/// <summary>
/// Cached article information for navigation and tooltips.
/// </summary>
public class CachedArticleInfo
{
    public Guid ArticleId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? DisplayPath { get; set; }
    public List<BreadcrumbDto>? Breadcrumbs { get; set; }
    public DateTime CachedAt { get; set; }
}

/// <summary>
/// Service interface for caching article metadata to reduce API calls.
/// </summary>
public interface IArticleCacheService
{
    /// <summary>
    /// Gets cached article info, fetching from API if not cached.
    /// </summary>
    Task<CachedArticleInfo?> GetArticleInfoAsync(Guid articleId);

    /// <summary>
    /// Gets the display path for an article (from cache if available).
    /// </summary>
    Task<string?> GetArticlePathAsync(Guid articleId);

    /// <summary>
    /// Gets the navigation URL path for an article (from cache if available).
    /// </summary>
    Task<string?> GetNavigationPathAsync(Guid articleId);

    /// <summary>
    /// Adds or updates an article in the cache (called when article is loaded).
    /// </summary>
    void CacheArticle(ArticleDto article);

    /// <summary>
    /// Invalidates all cached data (called on save/delete).
    /// </summary>
    void InvalidateCache();

    /// <summary>
    /// Invalidates a specific article from the cache.
    /// </summary>
    void InvalidateArticle(Guid articleId);
}

/// <summary>
/// In-memory cache for article metadata to reduce API calls for tooltips and navigation.
/// </summary>
public class ArticleCacheService : IArticleCacheService
{
    private readonly Dictionary<Guid, CachedArticleInfo> _cache = new();
    private readonly IArticleApiService _articleApi;
    private readonly ILogger<ArticleCacheService> _logger;
    private readonly object _lock = new();

    public ArticleCacheService(IArticleApiService articleApi, ILogger<ArticleCacheService> logger)
    {
        _articleApi = articleApi;
        _logger = logger;
    }

    /// <summary>
    /// Gets cached article info, fetching from API if not cached.
    /// </summary>
    public async Task<CachedArticleInfo?> GetArticleInfoAsync(Guid articleId)
    {
        // Check cache first
        lock (_lock)
        {
            if (_cache.TryGetValue(articleId, out var cached))
            {
                _logger.LogDebug("Cache hit for article {ArticleId}", articleId);
                return cached;
            }
        }

        // Fetch from API
        try
        {
            _logger.LogDebug("Cache miss for article {ArticleId}, fetching from API", articleId);
            var article = await _articleApi.GetArticleAsync(articleId);
            
            if (article == null)
            {
                return null;
            }

            // Cache the result
            CacheArticle(article);
            
            lock (_lock)
            {
                return _cache.GetValueOrDefault(articleId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching article {ArticleId} for cache", articleId);
            return null;
        }
    }

    /// <summary>
    /// Gets the display path for an article (from cache if available).
    /// </summary>
    public async Task<string?> GetArticlePathAsync(Guid articleId)
    {
        var info = await GetArticleInfoAsync(articleId);
        return info?.DisplayPath;
    }

    /// <summary>
    /// Gets the navigation URL path for an article (from cache if available).
    /// </summary>
    public async Task<string?> GetNavigationPathAsync(Guid articleId)
    {
        var info = await GetArticleInfoAsync(articleId);
        
        if (info?.Breadcrumbs != null && info.Breadcrumbs.Any())
        {
            return string.Join("/", info.Breadcrumbs.Select(b => b.Slug));
        }
        
        return info?.Slug;
    }

    /// <summary>
    /// Adds or updates an article in the cache (called when article is loaded).
    /// </summary>
    public void CacheArticle(ArticleDto article)
    {
        if (article == null) return;

        var cachedInfo = new CachedArticleInfo
        {
            ArticleId = article.Id,
            Title = article.Title,
            Slug = article.Slug,
            Breadcrumbs = article.Breadcrumbs,
            CachedAt = DateTime.UtcNow
        };

        // Build display path from breadcrumbs, skipping the World name (first element)
        if (article.Breadcrumbs != null && article.Breadcrumbs.Any())
        {
            var pathSegments = article.Breadcrumbs.Skip(1).Select(b => b.Title);
            cachedInfo.DisplayPath = string.Join(" / ", pathSegments);
        }

        lock (_lock)
        {
            _cache[article.Id] = cachedInfo;
        }

        _logger.LogDebug("Cached article {ArticleId}: {Title}", article.Id, article.Title);
    }

    /// <summary>
    /// Invalidates all cached data (called on save/delete).
    /// </summary>
    public void InvalidateCache()
    {
        lock (_lock)
        {
            var count = _cache.Count;
            _cache.Clear();
            _logger.LogDebug("Invalidated entire article cache ({Count} entries)", count);
        }
    }

    /// <summary>
    /// Invalidates a specific article from the cache.
    /// </summary>
    public void InvalidateArticle(Guid articleId)
    {
        lock (_lock)
        {
            if (_cache.Remove(articleId))
            {
                _logger.LogDebug("Invalidated cached article {ArticleId}", articleId);
            }
        }
    }
}
