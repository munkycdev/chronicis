using Chronicis.Api.Data;
using Chronicis.Shared.DTOs;
using Chronicis.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace Chronicis.Api.Services;

/// <inheritdoc />
public sealed class ArticleHierarchyService : IArticleHierarchyService
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
    //  Batch breadcrumb resolution
    // ────────────────────────────────────────────────────────────────

    /// <inheritdoc />
    public async Task<Dictionary<Guid, List<BreadcrumbDto>>> BuildBreadcrumbsBatchAsync(
        IEnumerable<Guid> articleIds,
        HierarchyWalkOptions? options = null)
    {
        options ??= new HierarchyWalkOptions();

        var requestedIds = articleIds.Distinct().ToList();
        var result = requestedIds.ToDictionary(id => id, _ => new List<BreadcrumbDto>());

        if (requestedIds.Count == 0)
            return result;

        // Phase 1: Load all article data needed for breadcrumbs in O(depth) bulk queries.
        // Each iteration loads one "level" of parents that has not been fetched yet.
        var cache = new Dictionary<Guid, (string? Title, string Slug, Guid? ParentId, ArticleType Type)>();
        var pending = new HashSet<Guid>(requestedIds);

        while (pending.Count > 0)
        {
            if (cache.Count > 10_000)
            {
                _logger.LogErrorSanitized(
                    "BuildBreadcrumbsBatchAsync: ancestor cache exceeded 10,000 entries, stopping walk");
                break;
            }

            var pendingList = pending.ToList();
            var batch = await _context.Articles
                .AsNoTracking()
                .Where(a => pendingList.Contains(a.Id))
                .Select(a => new { a.Id, a.Title, a.Slug, a.ParentId, a.Type })
                .ToListAsync();

            pending.Clear();

            foreach (var node in batch)
            {
                cache[node.Id] = (node.Title, node.Slug, node.ParentId, node.Type);

                if (node.ParentId.HasValue && !cache.ContainsKey(node.ParentId.Value))
                    pending.Add(node.ParentId.Value);
            }
        }

        // Phase 2: Build breadcrumbs in memory for each requested article ID.
        foreach (var articleId in requestedIds)
        {
            if (!cache.TryGetValue(articleId, out var article))
                continue;

            var breadcrumbs = new List<BreadcrumbDto>();
            var visited = new HashSet<Guid> { articleId };

            // Honour IncludeCurrentArticle: start from the article itself or skip to its parent.
            var currentId = options.IncludeCurrentArticle
                ? (Guid?)articleId
                : article.ParentId;

            while (currentId.HasValue)
            {
                if (!visited.Add(currentId.Value))
                    break; // cycle detected

                if (!cache.TryGetValue(currentId.Value, out var node))
                    break; // parent was not loaded (shouldn't happen within safety cap)

                breadcrumbs.Insert(0, new BreadcrumbDto
                {
                    Id = currentId.Value,
                    Title = node.Title ?? "(Untitled)",
                    Slug = node.Slug,
                    Type = node.Type,
                    IsWorld = false
                });

                currentId = node.ParentId;
            }

            result[articleId] = breadcrumbs;
        }

        return result;
    }

    // ────────────────────────────────────────────────────────────────
    //  Core walk algorithm
    // ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Walks up the parent chain from <paramref name="articleId"/> to the root,
    /// collecting breadcrumbs in root-to-leaf order.
    /// Issues one query per ancestor level; depth is typically 2–5 for real hierarchies.
    /// </summary>
    private async Task<List<BreadcrumbDto>> WalkAncestorsAsync(Guid articleId, HierarchyWalkOptions options)
    {
        var breadcrumbs = new List<BreadcrumbDto>();
        var visited = new HashSet<Guid>();
        var currentId = (Guid?)articleId;

        while (currentId.HasValue && visited.Count <= MaxDepth)
        {
            if (!visited.Add(currentId.Value))
            {
                _logger.LogErrorSanitized(
                    "Cycle detected in article hierarchy at {ArticleId}. Visited: {Visited}",
                    currentId.Value, string.Join(", ", visited));
                break;
            }

            IQueryable<Chronicis.Shared.Models.Article> query = _context.Articles
                .AsNoTracking()
                .Where(a => a.Id == currentId);

            if (options.PublicOnly)
                query = query.Where(a => a.Visibility == ArticleVisibility.Public);

            var article = await query
                .Select(a => new { a.Id, a.Title, a.Slug, a.ParentId, a.Type })
                .FirstOrDefaultAsync();

            if (article == null)
                break;

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
