using Chronicis.Api.Data;
using Chronicis.Api.Services;
using Chronicis.Shared.DTOs;
using Chronicis.Shared.Enums;
using Chronicis.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Chronicis.Api.Controllers;

/// <summary>
/// API endpoints for anonymous public access to worlds and articles.
/// These endpoints do NOT require authentication.
/// </summary>
[ApiController]
[Route("api/public")]
public class PublicController : ControllerBase
{
    private readonly ChronicisDbContext _context;
    private readonly IWorldService _worldService;
    private readonly ILogger<PublicController> _logger;

    public PublicController(
        ChronicisDbContext context,
        IWorldService worldService,
        ILogger<PublicController> logger)
    {
        _context = context;
        _worldService = worldService;
        _logger = logger;
    }

    /// <summary>
    /// GET /api/public/worlds/{publicSlug} - Get a public world by its public slug.
    /// </summary>
    [HttpGet("worlds/{publicSlug}")]
    public async Task<ActionResult<WorldDetailDto>> GetPublicWorld(string publicSlug)
    {
        if (string.IsNullOrWhiteSpace(publicSlug))
        {
            return BadRequest(new { error = "Public slug is required" });
        }

        _logger.LogInformation("Getting public world with slug '{PublicSlug}'", publicSlug);

        // Find the world by public slug
        var world = await _context.Worlds
            .Where(w => w.IsPublic && w.PublicSlug == publicSlug)
            .Include(w => w.Owner)
            .Include(w => w.Campaigns)
            .FirstOrDefaultAsync();

        if (world == null)
        {
            return NotFound(new { error = "World not found or not public" });
        }

        // Build the DTO
        var dto = new WorldDetailDto
        {
            Id = world.Id,
            Name = world.Name,
            Slug = world.Slug,
            Description = world.Description,
            OwnerId = world.OwnerId,
            OwnerName = world.Owner?.DisplayName ?? "Unknown",
            CreatedAt = world.CreatedAt,
            IsPublic = world.IsPublic,
            PublicSlug = world.PublicSlug,
            Campaigns = world.Campaigns?
                .Where(c => c.IsActive) // Only show active campaigns publicly
                .Select(c => new CampaignDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Description = c.Description,
                    WorldId = c.WorldId,
                    IsActive = c.IsActive,
                    CreatedAt = c.CreatedAt,
                    StartedAt = c.StartedAt
                })
                .ToList() ?? new List<CampaignDto>()
        };

        return Ok(dto);
    }

    /// <summary>
    /// GET /api/public/worlds/{publicSlug}/articles - Get the article tree for a public world.
    /// Only returns articles with Public visibility.
    /// </summary>
    [HttpGet("worlds/{publicSlug}/articles")]
    public async Task<ActionResult<List<ArticleTreeDto>>> GetPublicArticleTree(string publicSlug)
    {
        if (string.IsNullOrWhiteSpace(publicSlug))
        {
            return BadRequest(new { error = "Public slug is required" });
        }

        _logger.LogInformation("Getting public article tree for world '{PublicSlug}'", publicSlug);

        // Find the world by public slug
        var world = await _context.Worlds
            .Where(w => w.IsPublic && w.PublicSlug == publicSlug)
            .Select(w => new { w.Id })
            .FirstOrDefaultAsync();

        if (world == null)
        {
            return NotFound(new { error = "World not found or not public" });
        }

        // Get all public articles in this world
        var articles = await _context.Articles
            .Where(a => a.WorldId == world.Id)
            .Where(a => a.Visibility == ArticleVisibility.Public)
            .OrderBy(a => a.Title)
            .Select(a => new ArticleTreeDto
            {
                Id = a.Id,
                Title = a.Title ?? "Untitled",
                Slug = a.Slug,
                ParentId = a.ParentId,
                WorldId = a.WorldId,
                CampaignId = a.CampaignId,
                ArcId = a.ArcId,
                Type = a.Type,
                Visibility = a.Visibility,
                HasChildren = a.Children.Any(c => c.Visibility == ArticleVisibility.Public),
                ChildCount = a.Children.Count(c => c.Visibility == ArticleVisibility.Public),
                IconEmoji = a.IconEmoji,
                CreatedAt = a.CreatedAt,
                EffectiveDate = a.EffectiveDate,
                CreatedBy = a.CreatedBy
            })
            .ToListAsync();

        // Filter to only include articles whose entire ancestor chain is also public
        var publicArticleIds = new HashSet<Guid>(articles.Select(a => a.Id));
        var filteredArticles = articles
            .Where(a => IsAncestorChainPublic(a, articles, publicArticleIds))
            .ToList();

        // Build hierarchical structure - return only root-level articles
        var rootArticles = filteredArticles
            .Where(a => a.ParentId == null || !publicArticleIds.Contains(a.ParentId.Value))
            .ToList();

        return Ok(rootArticles);
    }

    /// <summary>
    /// GET /api/public/worlds/{publicSlug}/articles/{*articlePath} - Get a specific public article by path.
    /// </summary>
    [HttpGet("worlds/{publicSlug}/articles/{*articlePath}")]
    public async Task<ActionResult<ArticleDto>> GetPublicArticle(string publicSlug, string articlePath)
    {
        if (string.IsNullOrWhiteSpace(publicSlug))
        {
            return BadRequest(new { error = "Public slug is required" });
        }

        if (string.IsNullOrWhiteSpace(articlePath))
        {
            return BadRequest(new { error = "Article path is required" });
        }

        _logger.LogInformation("Getting public article '{ArticlePath}' in world '{PublicSlug}'", articlePath, publicSlug);

        // Find the world by public slug
        var world = await _context.Worlds
            .Where(w => w.IsPublic && w.PublicSlug == publicSlug)
            .Select(w => new { w.Id })
            .FirstOrDefaultAsync();

        if (world == null)
        {
            return NotFound(new { error = "World not found or not public" });
        }

        // Parse the article path (e.g., "lore/magic/spells" -> ["lore", "magic", "spells"])
        var pathSegments = articlePath.Split('/', StringSplitOptions.RemoveEmptyEntries);

        if (pathSegments.Length == 0)
        {
            return BadRequest(new { error = "Invalid article path" });
        }

        // Navigate through the path to find the article
        Guid? currentParentId = null;
        Article? targetArticle = null;

        foreach (var segment in pathSegments)
        {
            targetArticle = await _context.Articles
                .Where(a => a.WorldId == world.Id)
                .Where(a => a.Slug == segment)
                .Where(a => a.ParentId == currentParentId)
                .Where(a => a.Visibility == ArticleVisibility.Public)
                .FirstOrDefaultAsync();

            if (targetArticle == null)
            {
                return NotFound(new { error = "Article not found or not public" });
            }

            currentParentId = targetArticle.Id;
        }

        if (targetArticle == null)
        {
            return NotFound(new { error = "Article not found" });
        }

        // Build the article DTO
        var hasChildren = await _context.Articles
            .AnyAsync(a => a.ParentId == targetArticle.Id && a.Visibility == ArticleVisibility.Public);

        var childCount = await _context.Articles
            .CountAsync(a => a.ParentId == targetArticle.Id && a.Visibility == ArticleVisibility.Public);

        // Build breadcrumbs
        var breadcrumbs = await BuildPublicBreadcrumbsAsync(targetArticle.Id, world.Id);

        var dto = new ArticleDto
        {
            Id = targetArticle.Id,
            Title = targetArticle.Title ?? "Untitled",
            Slug = targetArticle.Slug,
            ParentId = targetArticle.ParentId,
            WorldId = targetArticle.WorldId,
            CampaignId = targetArticle.CampaignId,
            ArcId = targetArticle.ArcId,
            Body = targetArticle.Body ?? string.Empty,
            Type = targetArticle.Type,
            Visibility = targetArticle.Visibility,
            CreatedAt = targetArticle.CreatedAt,
            ModifiedAt = targetArticle.ModifiedAt,
            EffectiveDate = targetArticle.EffectiveDate,
            IconEmoji = targetArticle.IconEmoji,
            HasChildren = hasChildren,
            ChildCount = childCount,
            Breadcrumbs = breadcrumbs,
            SessionDate = targetArticle.SessionDate,
            InGameDate = targetArticle.InGameDate,
            CreatedBy = targetArticle.CreatedBy
        };

        return Ok(dto);
    }

    /// <summary>
    /// Checks if an article's entire ancestor chain consists of public articles.
    /// </summary>
    private static bool IsAncestorChainPublic(
        ArticleTreeDto article,
        List<ArticleTreeDto> allArticles,
        HashSet<Guid> publicArticleIds)
    {
        if (article.ParentId == null)
        {
            return true; // Root article
        }

        // If parent is not in the public set, the chain is broken
        if (!publicArticleIds.Contains(article.ParentId.Value))
        {
            return false;
        }

        // Recursively check parent
        var parent = allArticles.FirstOrDefault(a => a.Id == article.ParentId.Value);
        if (parent == null)
        {
            return false;
        }

        return IsAncestorChainPublic(parent, allArticles, publicArticleIds);
    }

    /// <summary>
    /// Builds breadcrumbs for a public article.
    /// </summary>
    private async Task<List<BreadcrumbDto>> BuildPublicBreadcrumbsAsync(Guid articleId, Guid worldId)
    {
        var breadcrumbs = new List<BreadcrumbDto>();
        var currentId = articleId;
        var visited = new HashSet<Guid>();

        // First, get the current article's parent
        var article = await _context.Articles
            .Where(a => a.Id == currentId)
            .Select(a => new { a.ParentId })
            .FirstOrDefaultAsync();

        if (article?.ParentId == null)
        {
            return breadcrumbs;
        }

        currentId = article.ParentId.Value;

        // Walk up the tree
        while (!visited.Contains(currentId))
        {
            visited.Add(currentId);

            var ancestor = await _context.Articles
                .Where(a => a.Id == currentId && a.Visibility == ArticleVisibility.Public)
                .Select(a => new BreadcrumbDto
                {
                    Id = a.Id,
                    Title = a.Title ?? "Untitled",
                    Slug = a.Slug,
                    Type = a.Type,
                    IsWorld = false
                })
                .FirstOrDefaultAsync();

            if (ancestor == null)
            {
                break;
            }

            breadcrumbs.Insert(0, ancestor);

            var parent = await _context.Articles
                .Where(a => a.Id == currentId)
                .Select(a => a.ParentId)
                .FirstOrDefaultAsync();

            if (parent == null)
            {
                break;
            }

            currentId = parent.Value;
        }

        return breadcrumbs;
    }
}
