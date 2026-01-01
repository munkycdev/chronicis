using Chronicis.Api.Data;
using Chronicis.Shared.DTOs;
using Chronicis.Shared.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Chronicis.Api.Services;

/// <summary>
/// Service for anonymous public access to worlds.
/// All methods return only publicly visible content - no authentication required.
/// </summary>
public class PublicWorldService : IPublicWorldService
{
    private readonly ChronicisDbContext _context;
    private readonly ILogger<PublicWorldService> _logger;

    public PublicWorldService(ChronicisDbContext context, ILogger<PublicWorldService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Get a public world by its public slug.
    /// Returns null if world doesn't exist or is not public.
    /// </summary>
    public async Task<WorldDetailDto?> GetPublicWorldAsync(string publicSlug)
    {
        var normalizedSlug = publicSlug.Trim().ToLowerInvariant();

        var world = await _context.Worlds
            .AsNoTracking()
            .Include(w => w.Owner)
            .Include(w => w.Campaigns)
            .Where(w => w.PublicSlug == normalizedSlug && w.IsPublic)
            .FirstOrDefaultAsync();

        if (world == null)
        {
            _logger.LogDebug("Public world not found for slug '{PublicSlug}'", normalizedSlug);
            return null;
        }

        _logger.LogInformation("Public world '{WorldName}' accessed via slug '{PublicSlug}'", 
            world.Name, normalizedSlug);

        return new WorldDetailDto
        {
            Id = world.Id,
            Name = world.Name,
            Slug = world.Slug,
            Description = world.Description,
            OwnerId = world.OwnerId,
            OwnerName = world.Owner?.DisplayName ?? "Unknown",
            CreatedAt = world.CreatedAt,
            CampaignCount = world.Campaigns?.Count ?? 0,
            IsPublic = world.IsPublic,
            PublicSlug = world.PublicSlug,
            // Don't expose campaign details to anonymous users
            Campaigns = new List<CampaignDto>()
        };
    }

    /// <summary>
    /// Get the article tree for a public world.
    /// Only returns articles with Public visibility.
    /// Returns a hierarchical tree structure.
    /// </summary>
    public async Task<List<ArticleTreeDto>> GetPublicArticleTreeAsync(string publicSlug)
    {
        var normalizedSlug = publicSlug.Trim().ToLowerInvariant();

        // First, verify the world exists and is public
        var world = await _context.Worlds
            .AsNoTracking()
            .Where(w => w.PublicSlug == normalizedSlug && w.IsPublic)
            .Select(w => new { w.Id })
            .FirstOrDefaultAsync();

        if (world == null)
        {
            _logger.LogDebug("Public world not found for slug '{PublicSlug}'", normalizedSlug);
            return new List<ArticleTreeDto>();
        }

        // Get all public articles for this world
        var allPublicArticles = await _context.Articles
            .AsNoTracking()
            .Where(a => a.WorldId == world.Id && a.Visibility == ArticleVisibility.Public)
            .Select(a => new ArticleTreeDto
            {
                Id = a.Id,
                Title = a.Title,
                Slug = a.Slug,
                ParentId = a.ParentId,
                WorldId = a.WorldId,
                CampaignId = a.CampaignId,
                ArcId = a.ArcId,
                Type = a.Type,
                Visibility = a.Visibility,
                HasChildren = false, // Will calculate below
                ChildCount = 0,      // Will calculate below
                Children = new List<ArticleTreeDto>(),
                CreatedAt = a.CreatedAt,
                EffectiveDate = a.EffectiveDate,
                IconEmoji = a.IconEmoji,
                CreatedBy = a.CreatedBy
            })
            .OrderBy(a => a.Title)
            .ToListAsync();

        // Build a lookup for quick parent finding
        var articleLookup = allPublicArticles.ToDictionary(a => a.Id);
        var publicArticleIds = new HashSet<Guid>(allPublicArticles.Select(a => a.Id));

        // Build the tree structure
        var rootArticles = new List<ArticleTreeDto>();

        foreach (var article in allPublicArticles)
        {
            // Check if parent is also public (or null for root)
            if (article.ParentId == null)
            {
                // Root article
                rootArticles.Add(article);
            }
            else if (publicArticleIds.Contains(article.ParentId.Value))
            {
                // Parent is public, add as child
                if (articleLookup.TryGetValue(article.ParentId.Value, out var parent))
                {
                    parent.Children ??= new List<ArticleTreeDto>();
                    parent.Children.Add(article);
                    parent.HasChildren = true;
                    parent.ChildCount++;
                }
            }
            else
            {
                // Parent is not public - treat this as a root for public view
                // This handles cases where a public article is nested under a non-public one
                rootArticles.Add(article);
            }
        }

        _logger.LogInformation("Retrieved {Count} public articles for world '{PublicSlug}'", 
            allPublicArticles.Count, normalizedSlug);

        return rootArticles;
    }

    /// <summary>
    /// Get a specific article by path in a public world.
    /// Returns null if article doesn't exist, world is not public, or article is not Public visibility.
    /// Path format: "article-slug/child-slug" (does not include world slug)
    /// </summary>
    public async Task<ArticleDto?> GetPublicArticleAsync(string publicSlug, string articlePath)
    {
        var normalizedSlug = publicSlug.Trim().ToLowerInvariant();

        // First, verify the world exists and is public
        var world = await _context.Worlds
            .AsNoTracking()
            .Where(w => w.PublicSlug == normalizedSlug && w.IsPublic)
            .Select(w => new { w.Id, w.Name, w.Slug })
            .FirstOrDefaultAsync();

        if (world == null)
        {
            _logger.LogDebug("Public world not found for slug '{PublicSlug}'", normalizedSlug);
            return null;
        }

        if (string.IsNullOrWhiteSpace(articlePath))
        {
            _logger.LogDebug("Empty article path for public world '{PublicSlug}'", normalizedSlug);
            return null;
        }

        var slugs = articlePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (slugs.Length == 0)
            return null;

        // Walk down the tree using slugs
        Guid? currentParentId = null;
        Guid? articleId = null;

        for (int i = 0; i < slugs.Length; i++)
        {
            var slug = slugs[i];
            var isRootLevel = (i == 0);

            Guid? foundArticleId;

            if (isRootLevel)
            {
                // Root-level article: filter by WorldId and ParentId = null
                foundArticleId = await _context.Articles
                    .AsNoTracking()
                    .Where(a => a.Slug == slug &&
                                a.ParentId == null &&
                                a.WorldId == world.Id &&
                                a.Visibility == ArticleVisibility.Public)
                    .Select(a => (Guid?)a.Id)
                    .FirstOrDefaultAsync();
            }
            else
            {
                // Child article: filter by ParentId
                foundArticleId = await _context.Articles
                    .AsNoTracking()
                    .Where(a => a.Slug == slug &&
                                a.ParentId == currentParentId &&
                                a.Visibility == ArticleVisibility.Public)
                    .Select(a => (Guid?)a.Id)
                    .FirstOrDefaultAsync();
            }

            if (!foundArticleId.HasValue)
            {
                _logger.LogDebug("Public article not found for slug '{Slug}' in path '{Path}' for world '{PublicSlug}'",
                    slug, articlePath, normalizedSlug);
                return null;
            }

            articleId = foundArticleId.Value;
            currentParentId = foundArticleId.Value;
        }

        // Found the article, now get full details
        if (!articleId.HasValue)
            return null;

        var article = await _context.Articles
            .AsNoTracking()
            .Where(a => a.Id == articleId.Value && a.Visibility == ArticleVisibility.Public)
            .Select(a => new ArticleDto
            {
                Id = a.Id,
                Title = a.Title,
                Slug = a.Slug,
                ParentId = a.ParentId,
                WorldId = a.WorldId,
                CampaignId = a.CampaignId,
                ArcId = a.ArcId,
                Body = a.Body ?? string.Empty,
                Type = a.Type,
                Visibility = a.Visibility,
                CreatedAt = a.CreatedAt,
                ModifiedAt = a.ModifiedAt,
                EffectiveDate = a.EffectiveDate,
                CreatedBy = a.CreatedBy,
                LastModifiedBy = a.LastModifiedBy,
                IconEmoji = a.IconEmoji,
                SessionDate = a.SessionDate,
                InGameDate = a.InGameDate,
                PlayerId = a.PlayerId,
                AISummary = a.AISummary,
                AISummaryGeneratedAt = a.AISummaryGeneratedAt,
                Breadcrumbs = new List<BreadcrumbDto>()
            })
            .FirstOrDefaultAsync();

        if (article == null)
            return null;

        // Build breadcrumbs (only including public articles)
        article.Breadcrumbs = await BuildPublicBreadcrumbsAsync(articleId.Value, world.Id, world.Name, world.Slug);

        _logger.LogInformation("Public article '{Title}' accessed in world '{PublicSlug}'", 
            article.Title, normalizedSlug);

        return article;
    }

    /// <summary>
    /// Build breadcrumb trail for public article viewing.
    /// Only includes articles that are publicly visible.
    /// </summary>
    private async Task<List<BreadcrumbDto>> BuildPublicBreadcrumbsAsync(
        Guid articleId, 
        Guid worldId, 
        string worldName, 
        string worldSlug)
    {
        var breadcrumbs = new List<BreadcrumbDto>();
        var currentId = (Guid?)articleId;

        // Walk up the tree to build article path
        while (currentId.HasValue)
        {
            var article = await _context.Articles
                .AsNoTracking()
                .Where(a => a.Id == currentId && a.Visibility == ArticleVisibility.Public)
                .Select(a => new { a.Id, a.Title, a.Slug, a.ParentId, a.Type })
                .FirstOrDefaultAsync();

            if (article == null)
                break;

            breadcrumbs.Insert(0, new BreadcrumbDto
            {
                Id = article.Id,
                Title = article.Title ?? "(Untitled)",
                Slug = article.Slug,
                Type = article.Type,
                IsWorld = false
            });

            currentId = article.ParentId;
        }

        // Prepend world breadcrumb
        breadcrumbs.Insert(0, new BreadcrumbDto
        {
            Id = worldId,
            Title = worldName,
            Slug = worldSlug,
            Type = default,
            IsWorld = true
        });

        return breadcrumbs;
    }
}
