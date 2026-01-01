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
            // Include public campaigns
            Campaigns = world.Campaigns?.Select(c => new CampaignDto
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                WorldId = c.WorldId,
                IsActive = c.IsActive,
                StartedAt = c.StartedAt
            }).ToList() ?? new List<CampaignDto>()
        };
    }

    /// <summary>
    /// Get the article tree for a public world.
    /// Only returns articles with Public visibility.
    /// Returns a hierarchical tree structure organized by virtual groups (Campaigns, Characters, Wiki).
    /// </summary>
    public async Task<List<ArticleTreeDto>> GetPublicArticleTreeAsync(string publicSlug)
    {
        var normalizedSlug = publicSlug.Trim().ToLowerInvariant();

        // First, verify the world exists and is public
        var world = await _context.Worlds
            .AsNoTracking()
            .Include(w => w.Campaigns)
                .ThenInclude(c => c.Arcs)
            .Where(w => w.PublicSlug == normalizedSlug && w.IsPublic)
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
            .ToListAsync();

        // Build article index and children relationships
        var articleIndex = allPublicArticles.ToDictionary(a => a.Id);
        var publicArticleIds = new HashSet<Guid>(allPublicArticles.Select(a => a.Id));

        // Link children to parents
        foreach (var article in allPublicArticles)
        {
            if (article.ParentId.HasValue && articleIndex.TryGetValue(article.ParentId.Value, out var parent))
            {
                parent.Children ??= new List<ArticleTreeDto>();
                parent.Children.Add(article);
                parent.HasChildren = true;
                parent.ChildCount++;
            }
        }

        // Sort children by title
        foreach (var article in allPublicArticles.Where(a => a.Children?.Any() == true))
        {
            article.Children = article.Children!.OrderBy(c => c.Title).ToList();
        }

        // Build virtual groups
        var result = new List<ArticleTreeDto>();

        // 1. Campaigns group - contains campaigns with their arcs and sessions
        var campaignsGroup = CreateVirtualGroup("campaigns", "Campaigns", "fa-solid fa-dungeon");
        foreach (var campaign in world.Campaigns?.OrderBy(c => c.Name) ?? Enumerable.Empty<Chronicis.Shared.Models.Campaign>())
        {
            var campaignNode = new ArticleTreeDto
            {
                Id = campaign.Id,
                Title = campaign.Name,
                Slug = campaign.Name.ToLowerInvariant().Replace(" ", "-"),
                Type = ArticleType.WikiArticle, // Use WikiArticle as placeholder
                IconEmoji = "fa-solid fa-dungeon",
                Children = new List<ArticleTreeDto>(),
                IsVirtualGroup = true
            };

            // Add arcs under campaign
            foreach (var arc in campaign.Arcs?.OrderBy(a => a.SortOrder).ThenBy(a => a.Name) ?? Enumerable.Empty<Chronicis.Shared.Models.Arc>())
            {
                var arcNode = new ArticleTreeDto
                {
                    Id = arc.Id,
                    Title = arc.Name,
                    Slug = arc.Name.ToLowerInvariant().Replace(" ", "-"),
                    Type = ArticleType.WikiArticle,
                    IconEmoji = "fa-solid fa-book-open",
                    Children = new List<ArticleTreeDto>(),
                    IsVirtualGroup = true
                };

                // Find session articles for this arc
                var sessionArticles = allPublicArticles
                    .Where(a => a.ArcId == arc.Id && a.Type == ArticleType.Session && a.ParentId == null)
                    .OrderBy(a => a.Title)
                    .ToList();

                foreach (var session in sessionArticles)
                {
                    arcNode.Children.Add(session);
                    arcNode.HasChildren = true;
                    arcNode.ChildCount++;
                }

                if (arcNode.Children.Any())
                {
                    campaignNode.Children.Add(arcNode);
                    campaignNode.HasChildren = true;
                    campaignNode.ChildCount++;
                }
            }

            if (campaignNode.Children.Any())
            {
                campaignsGroup.Children!.Add(campaignNode);
                campaignsGroup.HasChildren = true;
                campaignsGroup.ChildCount++;
            }
        }

        if (campaignsGroup.Children!.Any())
        {
            result.Add(campaignsGroup);
        }

        // 2. Player Characters group
        var charactersGroup = CreateVirtualGroup("characters", "Player Characters", "fa-solid fa-user-ninja");
        var characterArticles = allPublicArticles
            .Where(a => a.Type == ArticleType.Character && a.ParentId == null)
            .OrderBy(a => a.Title)
            .ToList();

        foreach (var article in characterArticles)
        {
            charactersGroup.Children!.Add(article);
            charactersGroup.HasChildren = true;
            charactersGroup.ChildCount++;
        }

        if (charactersGroup.Children!.Any())
        {
            result.Add(charactersGroup);
        }

        // 3. Wiki group
        var wikiGroup = CreateVirtualGroup("wiki", "Wiki", "fa-solid fa-book");
        var wikiArticles = allPublicArticles
            .Where(a => a.Type == ArticleType.WikiArticle && a.ParentId == null)
            .OrderBy(a => a.Title)
            .ToList();

        foreach (var article in wikiArticles)
        {
            wikiGroup.Children!.Add(article);
            wikiGroup.HasChildren = true;
            wikiGroup.ChildCount++;
        }

        if (wikiGroup.Children!.Any())
        {
            result.Add(wikiGroup);
        }

        // 4. Uncategorized (Legacy and other types without parents not already included)
        var includedIds = new HashSet<Guid>();
        
        // Collect all IDs from campaigns/arcs/sessions
        foreach (var campaign in result.FirstOrDefault(r => r.Slug == "campaigns")?.Children ?? new List<ArticleTreeDto>())
        {
            foreach (var arc in campaign.Children ?? new List<ArticleTreeDto>())
            {
                foreach (var session in arc.Children ?? new List<ArticleTreeDto>())
                {
                    CollectArticleIds(session, includedIds);
                }
            }
        }
        
        // Collect character and wiki IDs
        foreach (var article in characterArticles)
        {
            CollectArticleIds(article, includedIds);
        }
        foreach (var article in wikiArticles)
        {
            CollectArticleIds(article, includedIds);
        }

        var uncategorizedArticles = allPublicArticles
            .Where(a => a.ParentId == null && !includedIds.Contains(a.Id))
            .OrderBy(a => a.Title)
            .ToList();

        if (uncategorizedArticles.Any())
        {
            var uncategorizedGroup = CreateVirtualGroup("uncategorized", "Uncategorized", "fa-solid fa-folder");
            foreach (var article in uncategorizedArticles)
            {
                uncategorizedGroup.Children!.Add(article);
                uncategorizedGroup.HasChildren = true;
                uncategorizedGroup.ChildCount++;
            }
            result.Add(uncategorizedGroup);
        }

        _logger.LogInformation("Retrieved {Count} public articles for world '{PublicSlug}' in {GroupCount} groups", 
            allPublicArticles.Count, normalizedSlug, result.Count);

        return result;
    }

    private static ArticleTreeDto CreateVirtualGroup(string slug, string title, string icon)
    {
        return new ArticleTreeDto
        {
            Id = Guid.NewGuid(), // Virtual ID
            Title = title,
            Slug = slug,
            Type = ArticleType.WikiArticle,
            IconEmoji = icon,
            HasChildren = false,
            ChildCount = 0,
            Children = new List<ArticleTreeDto>(),
            IsVirtualGroup = true
        };
    }

    private static void CollectArticleIds(ArticleTreeDto article, HashSet<Guid> ids)
    {
        ids.Add(article.Id);
        if (article.Children != null)
        {
            foreach (var child in article.Children)
            {
                CollectArticleIds(child, ids);
            }
        }
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
    /// Includes Campaign and Arc for session articles.
    /// Only includes articles that are publicly visible.
    /// </summary>
    private async Task<List<BreadcrumbDto>> BuildPublicBreadcrumbsAsync(
        Guid articleId, 
        Guid worldId, 
        string worldName, 
        string worldSlug)
    {
        var breadcrumbs = new List<BreadcrumbDto>();
        
        // Get the article to check for Campaign/Arc
        var targetArticle = await _context.Articles
            .AsNoTracking()
            .Where(a => a.Id == articleId)
            .Select(a => new { a.CampaignId, a.ArcId, a.ParentId })
            .FirstOrDefaultAsync();

        // Start with World breadcrumb
        breadcrumbs.Add(new BreadcrumbDto
        {
            Id = worldId,
            Title = worldName,
            Slug = worldSlug,
            Type = default,
            IsWorld = true
        });

        // Add Campaign breadcrumb if article belongs to a campaign
        if (targetArticle?.CampaignId.HasValue == true)
        {
            var campaign = await _context.Campaigns
                .AsNoTracking()
                .Where(c => c.Id == targetArticle.CampaignId.Value)
                .Select(c => new { c.Id, c.Name })
                .FirstOrDefaultAsync();

            if (campaign != null)
            {
                breadcrumbs.Add(new BreadcrumbDto
                {
                    Id = campaign.Id,
                    Title = campaign.Name,
                    Slug = campaign.Name.ToLowerInvariant().Replace(" ", "-"),
                    Type = default,
                    IsWorld = false
                });
            }
        }

        // Add Arc breadcrumb if article belongs to an arc
        if (targetArticle?.ArcId.HasValue == true)
        {
            var arc = await _context.Arcs
                .AsNoTracking()
                .Where(a => a.Id == targetArticle.ArcId.Value)
                .Select(a => new { a.Id, a.Name })
                .FirstOrDefaultAsync();

            if (arc != null)
            {
                breadcrumbs.Add(new BreadcrumbDto
                {
                    Id = arc.Id,
                    Title = arc.Name,
                    Slug = arc.Name.ToLowerInvariant().Replace(" ", "-"),
                    Type = default,
                    IsWorld = false
                });
            }
        }

        // Walk up the article parent tree
        var currentId = (Guid?)articleId;
        var articleBreadcrumbs = new List<BreadcrumbDto>();

        while (currentId.HasValue)
        {
            var article = await _context.Articles
                .AsNoTracking()
                .Where(a => a.Id == currentId && a.Visibility == ArticleVisibility.Public)
                .Select(a => new { a.Id, a.Title, a.Slug, a.ParentId, a.Type })
                .FirstOrDefaultAsync();

            if (article == null)
                break;

            articleBreadcrumbs.Insert(0, new BreadcrumbDto
            {
                Id = article.Id,
                Title = article.Title ?? "(Untitled)",
                Slug = article.Slug,
                Type = article.Type,
                IsWorld = false
            });

            currentId = article.ParentId;
        }

        // Add article breadcrumbs after world/campaign/arc
        breadcrumbs.AddRange(articleBreadcrumbs);

        return breadcrumbs;
    }
}
