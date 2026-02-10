using Chronicis.Api.Data;
using Chronicis.Shared.DTOs;
using Chronicis.Shared.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Chronicis.Api.Services;

/// <inheritdoc />
public class ArticleHierarchyService : IArticleHierarchyService
{
    private readonly ChronicisDbContext _context;
    private readonly ILogger<ArticleHierarchyService> _logger;

    /// <summary>
    /// Safety limit to prevent runaway walks in case of corrupted data.
    /// No realistic hierarchy should exceed this depth.
    /// </summary>
    private const int MaxDepth = 200;

    public ArticleHierarchyService(ChronicisDbContext context, ILogger<ArticleHierarchyService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<List<BreadcrumbDto>> BuildBreadcrumbsAsync(Guid articleId, HierarchyWalkOptions? options = null)
    {
        options ??= new HierarchyWalkOptions();

        // 1. Walk up the parent chain
        var articleBreadcrumbs = await WalkAncestorsAsync(articleId, options);

        // 2. Resolve world breadcrumb
        var result = new List<BreadcrumbDto>();

        if (options.IncludeWorldBreadcrumb)
        {
            var worldBreadcrumb = await ResolveWorldBreadcrumbAsync(articleId, options);
            if (worldBreadcrumb != null)
            {
                result.Add(worldBreadcrumb);
            }
        }

        // 3. Insert virtual group breadcrumbs if requested
        if (options.IncludeVirtualGroups)
        {
            var virtualGroups = await ResolveVirtualGroupsAsync(articleId, articleBreadcrumbs, options);
            result.AddRange(virtualGroups);
        }

        // 4. Append the article chain
        result.AddRange(articleBreadcrumbs);

        return result;
    }

    /// <inheritdoc />
    public async Task<string> BuildPathAsync(Guid articleId, HierarchyWalkOptions? options = null)
    {
        var breadcrumbs = await BuildBreadcrumbsAsync(articleId, options);
        return string.Join("/", breadcrumbs.Select(b => b.Slug));
    }

    /// <inheritdoc />
    public async Task<string> BuildDisplayPathAsync(Guid articleId, bool stripFirstLevel = true)
    {
        // Display paths: walk up collecting titles, no world breadcrumb, no virtual groups
        var options = new HierarchyWalkOptions
        {
            PublicOnly = false,
            IncludeWorldBreadcrumb = false,
            IncludeVirtualGroups = false,
            IncludeCurrentArticle = true
        };

        var breadcrumbs = await WalkAncestorsAsync(articleId, options);

        var titles = breadcrumbs.Select(b => b.Title).ToList();

        // Strip the first level (top-level article / world root) when requested
        if (stripFirstLevel && titles.Count > 1)
        {
            titles.RemoveAt(0);
        }

        return string.Join(" / ", titles);
    }

    // ────────────────────────────────────────────────────────────────
    //  Core walk algorithm
    // ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Walks up the parent chain from <paramref name="articleId"/> to the root,
    /// collecting breadcrumbs in root-to-leaf order.
    /// Includes cycle protection via a visited set and a hard depth limit.
    /// </summary>
    private async Task<List<BreadcrumbDto>> WalkAncestorsAsync(Guid articleId, HierarchyWalkOptions options)
    {
        var breadcrumbs = new List<BreadcrumbDto>();
        var visited = new HashSet<Guid>();
        var currentId = (Guid?)articleId;

        while (currentId.HasValue)
        {
            // Cycle detection
            if (!visited.Add(currentId.Value))
            {
                _logger.LogError(
                    "Cycle detected in article hierarchy at {ArticleId}. Visited: {Visited}",
                    currentId.Value, string.Join(", ", visited));
                break;
            }

            // Hard depth limit
            if (visited.Count > MaxDepth)
            {
                _logger.LogError(
                    "Max hierarchy depth ({MaxDepth}) exceeded walking from article {ArticleId}",
                    MaxDepth, articleId);
                break;
            }

            // Build the base query
            IQueryable<Chronicis.Shared.Models.Article> query = _context.Articles
                .AsNoTracking()
                .Where(a => a.Id == currentId);

            if (options.PublicOnly)
            {
                query = query.Where(a => a.Visibility == ArticleVisibility.Public);
            }

            var article = await query
                .Select(a => new
                {
                    a.Id,
                    a.Title,
                    a.Slug,
                    a.ParentId,
                    a.Type,
                    a.WorldId,
                    a.CampaignId,
                    a.ArcId
                })
                .FirstOrDefaultAsync();

            if (article == null)
                break;

            // Skip the current article if the caller only wants ancestors
            var isTarget = (article.Id == articleId);
            if (!isTarget || options.IncludeCurrentArticle)
            {
                breadcrumbs.Insert(0, new BreadcrumbDto
                {
                    Id = article.Id,
                    Title = article.Title ?? "(Untitled)",
                    Slug = article.Slug,
                    Type = article.Type,
                    IsWorld = false
                });
            }

            currentId = article.ParentId;
        }

        return breadcrumbs;
    }

    // ────────────────────────────────────────────────────────────────
    //  World breadcrumb resolution
    // ────────────────────────────────────────────────────────────────

    private async Task<BreadcrumbDto?> ResolveWorldBreadcrumbAsync(Guid articleId, HierarchyWalkOptions options)
    {
        // Use pre-resolved world if supplied
        if (options.World != null)
        {
            return new BreadcrumbDto
            {
                Id = options.World.Id,
                Title = options.World.Name,
                Slug = options.World.Slug,
                Type = default,
                IsWorld = true
            };
        }

        // Otherwise look it up from the article's WorldId
        var worldId = await _context.Articles
            .AsNoTracking()
            .Where(a => a.Id == articleId)
            .Select(a => a.WorldId)
            .FirstOrDefaultAsync();

        if (!worldId.HasValue)
            return null;

        var world = await _context.Worlds
            .AsNoTracking()
            .Where(w => w.Id == worldId.Value)
            .Select(w => new { w.Id, w.Name, w.Slug })
            .FirstOrDefaultAsync();

        if (world == null)
            return null;

        return new BreadcrumbDto
        {
            Id = world.Id,
            Title = world.Name,
            Slug = world.Slug,
            Type = default,
            IsWorld = true
        };
    }

    // ────────────────────────────────────────────────────────────────
    //  Virtual group resolution (public world breadcrumbs)
    // ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Resolves virtual group breadcrumbs (Campaigns/Arc or Player Characters/Wiki)
    /// based on the article's type and campaign/arc associations.
    /// Mirrors the prior behaviour of PublicWorldService.BuildPublicBreadcrumbsAsync.
    /// </summary>
    private async Task<List<BreadcrumbDto>> ResolveVirtualGroupsAsync(
        Guid articleId,
        List<BreadcrumbDto> articleBreadcrumbs,
        HierarchyWalkOptions options)
    {
        var groups = new List<BreadcrumbDto>();

        var targetArticle = await _context.Articles
            .AsNoTracking()
            .Where(a => a.Id == articleId)
            .Select(a => new { a.CampaignId, a.ArcId, a.ParentId, a.Type })
            .FirstOrDefaultAsync();

        if (targetArticle == null)
            return groups;

        if (targetArticle.CampaignId.HasValue)
        {
            // Session-type article: add Campaign breadcrumb
            var campaign = await _context.Campaigns
                .AsNoTracking()
                .Where(c => c.Id == targetArticle.CampaignId.Value)
                .Select(c => new { c.Id, c.Name })
                .FirstOrDefaultAsync();

            if (campaign != null)
            {
                groups.Add(new BreadcrumbDto
                {
                    Id = campaign.Id,
                    Title = campaign.Name,
                    Slug = campaign.Name.ToLowerInvariant().Replace(" ", "-"),
                    Type = default,
                    IsWorld = false
                });
            }

            // Add Arc breadcrumb if present
            if (targetArticle.ArcId.HasValue)
            {
                var arc = await _context.Arcs
                    .AsNoTracking()
                    .Where(a => a.Id == targetArticle.ArcId.Value)
                    .Select(a => new { a.Id, a.Name })
                    .FirstOrDefaultAsync();

                if (arc != null)
                {
                    groups.Add(new BreadcrumbDto
                    {
                        Id = arc.Id,
                        Title = arc.Name,
                        Slug = arc.Name.ToLowerInvariant().Replace(" ", "-"),
                        Type = default,
                        IsWorld = false
                    });
                }
            }
        }
        else
        {
            // Non-session: determine root article type by walking up the collected breadcrumbs
            var rootArticleType = targetArticle.Type;
            if (articleBreadcrumbs.Count > 0)
            {
                rootArticleType = articleBreadcrumbs[0].Type;
            }
            else
            {
                // Breadcrumbs may be empty if IncludeCurrentArticle was false;
                // walk up manually to find root type
                var currentParentId = targetArticle.ParentId;
                while (currentParentId.HasValue)
                {
                    var parentArticle = await _context.Articles
                        .AsNoTracking()
                        .Where(a => a.Id == currentParentId.Value)
                        .Select(a => new { a.Type, a.ParentId })
                        .FirstOrDefaultAsync();

                    if (parentArticle == null)
                        break;

                    rootArticleType = parentArticle.Type;
                    currentParentId = parentArticle.ParentId;
                }
            }

            if (rootArticleType == ArticleType.Character)
            {
                groups.Add(new BreadcrumbDto
                {
                    Id = Guid.Empty,
                    Title = "Player Characters",
                    Slug = "characters",
                    Type = default,
                    IsWorld = false
                });
            }
            else if (rootArticleType == ArticleType.WikiArticle)
            {
                groups.Add(new BreadcrumbDto
                {
                    Id = Guid.Empty,
                    Title = "Wiki",
                    Slug = "wiki",
                    Type = default,
                    IsWorld = false
                });
            }
        }

        return groups;
    }
}
