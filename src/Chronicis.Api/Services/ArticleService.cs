using Chronicis.Api.Data;
using Chronicis.Shared.Extensions;
using Chronicis.Shared.DTOs;
using Chronicis.Shared.Enums;
using Chronicis.Shared.Models;
using Chronicis.Shared.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Chronicis.Api.Services
{
    public class ArticleService : IArticleService
    {
        private readonly ChronicisDbContext _context;
        private readonly ILogger<ArticleService> _logger;

        public ArticleService(ChronicisDbContext context, ILogger<ArticleService> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Get all articles the user has access to via WorldMembers.
        /// Private articles are only visible to their creator.
        /// This is the base query for all article access - use this instead of filtering by CreatedBy.
        /// </summary>
        private IQueryable<Article> GetAccessibleArticles(Guid userId)
        {
            return from a in _context.Articles
                   join wm in _context.WorldMembers on a.WorldId equals wm.WorldId
                   where wm.UserId == userId
                   // Private articles only visible to creator
                   where a.Visibility != ArticleVisibility.Private || a.CreatedBy == userId
                   select a;
        }

        /// <summary>
        /// Get all root-level articles (ParentId is null) for worlds the user has access to.
        /// Optionally filter by WorldId.
        /// </summary>
        public async Task<List<ArticleTreeDto>> GetRootArticlesAsync(Guid userId, Guid? worldId = null)
        {
            var query = GetAccessibleArticles(userId)
                .AsNoTracking()
                .Where(a => a.ParentId == null);

            if (worldId.HasValue)
            {
                query = query.Where(a => a.WorldId == worldId.Value);
            }

            var rootArticles = await query
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
                    HasChildren = _context.Articles.Any(c => c.ParentId == a.Id),
                    ChildCount = _context.Articles.Count(c => c.ParentId == a.Id),
                    Children = new List<ArticleTreeDto>(),
                    CreatedAt = a.CreatedAt,
                    EffectiveDate = a.EffectiveDate,
                    IconEmoji = a.IconEmoji,
                    CreatedBy = a.CreatedBy,
                    HasAISummary = a.AISummary != null
                })
                .OrderBy(a => a.Title)
                .ToListAsync();

            return rootArticles;
        }

        /// <summary>
        /// Get all articles for worlds the user has access to, in a flat list (no hierarchy).
        /// Useful for dropdowns, linking dialogs, etc.
        /// Optionally filter by WorldId.
        /// </summary>
        public async Task<List<ArticleTreeDto>> GetAllArticlesAsync(Guid userId, Guid? worldId = null)
        {
            var query = GetAccessibleArticles(userId).AsNoTracking();

            if (worldId.HasValue)
            {
                query = query.Where(a => a.WorldId == worldId.Value);
            }

            var articles = await query
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
                    HasChildren = false, // Not relevant in flat list
                    ChildCount = 0,      // Not relevant in flat list
                    Children = new List<ArticleTreeDto>(),
                    CreatedAt = a.CreatedAt,
                    EffectiveDate = a.EffectiveDate,
                    IconEmoji = a.IconEmoji,
                    CreatedBy = a.CreatedBy,
                    HasAISummary = a.AISummary != null
                    // Note: Aliases intentionally not loaded here - loaded separately via GetLinkSuggestions
                })
                .OrderBy(a => a.Title)
                .ToListAsync();

            return articles;
        }

        /// <summary>
        /// Get all child articles of a specific parent.
        /// User must have access to the article's world via WorldMembers.
        /// </summary>
        public async Task<List<ArticleTreeDto>> GetChildrenAsync(Guid parentId, Guid userId)
        {
            var children = await GetAccessibleArticles(userId)
                .AsNoTracking()
                .Where(a => a.ParentId == parentId)
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
                    HasChildren = _context.Articles.Any(c => c.ParentId == a.Id),
                    ChildCount = _context.Articles.Count(c => c.ParentId == a.Id),
                    Children = new List<ArticleTreeDto>(),
                    CreatedAt = a.CreatedAt,
                    EffectiveDate = a.EffectiveDate,
                    IconEmoji = a.IconEmoji,
                    CreatedBy = a.CreatedBy,
                    HasAISummary = a.AISummary != null
                })
                .OrderBy(a => a.Title)
                .ToListAsync();

            return children;
        }

        /// <summary>
        /// Get full article details including breadcrumb path from root.
        /// User must have access to the article's world via WorldMembers.
        /// </summary>
        public async Task<ArticleDto?> GetArticleDetailAsync(Guid id, Guid userId)
        {
            var article = await GetAccessibleArticles(userId)
                .AsNoTracking()
                .Where(a => a.Id == id)
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
                    Breadcrumbs = new List<BreadcrumbDto>(),  // Will populate separately
                    Aliases = a.Aliases.Select(al => new ArticleAliasDto
                    {
                        Id = al.Id,
                        AliasText = al.AliasText,
                        AliasType = al.AliasType,
                        EffectiveDate = al.EffectiveDate,
                        CreatedAt = al.CreatedAt
                    }).ToList()
                })
                .FirstOrDefaultAsync();

            if (article == null)
            {
                _logger.LogWarning("Article {ArticleId} not found", id);
                return null;
            }

            // Build breadcrumb path by walking up the hierarchy
            article.Breadcrumbs = await BuildBreadcrumbsAsync(id, userId);

            return article;
        }

        /// <summary>
        /// Move an article to a new parent (or to root if newParentId is null).
        /// </summary>
        public async Task<(bool Success, string? ErrorMessage)> MoveArticleAsync(Guid articleId, Guid? newParentId, Guid userId)
        {
            // 1. Get the article to move (must be in a world user has access to)
            var article = await GetAccessibleArticles(userId)
                .FirstOrDefaultAsync(a => a.Id == articleId);

            if (article == null)
            {
                _logger.LogWarning("Article {ArticleId} not found for user {UserId}", articleId, userId);
                return (false, "Article not found");
            }

            // 2. If moving to same parent, nothing to do
            if (article.ParentId == newParentId)
            {
                return (true, null);
            }

            // 3. If newParentId is specified, validate the target exists and user has access
            if (newParentId.HasValue)
            {
                var targetParent = await GetAccessibleArticles(userId)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(a => a.Id == newParentId.Value);

                if (targetParent == null)
                {
                    _logger.LogWarning("Target parent {NewParentId} not found for user {UserId}", newParentId, userId);
                    return (false, "Target parent article not found");
                }

                // 4. Check for circular reference - cannot move an article to be a child of itself or its descendants
                if (await WouldCreateCircularReferenceAsync(articleId, newParentId.Value, userId))
                {
                    _logger.LogWarning("Moving article {ArticleId} to {NewParentId} would create circular reference",
                        articleId, newParentId);
                    return (false, "Cannot move an article to be a child of itself or its descendants");
                }
            }

            // 5. Perform the move
            article.ParentId = newParentId;
            article.ModifiedAt = DateTime.UtcNow;
            article.LastModifiedBy = userId;

            await _context.SaveChangesAsync();

            return (true, null);
        }

        /// <summary>
        /// Check if moving articleId to become a child of targetParentId would create a circular reference.
        /// </summary>
        private async Task<bool> WouldCreateCircularReferenceAsync(Guid articleId, Guid targetParentId, Guid userId)
        {
            // If trying to move to self, that's circular
            if (articleId == targetParentId)
            {
                return true;
            }

            // Walk up from targetParentId to root, checking if we encounter articleId
            var currentId = (Guid?)targetParentId;
            var visited = new HashSet<Guid>();

            while (currentId.HasValue)
            {
                // Prevent infinite loops (shouldn't happen with valid data, but safety first)
                if (visited.Contains(currentId.Value))
                {
                    _logger.LogError("Detected existing circular reference in hierarchy at article {ArticleId}", currentId.Value);
                    return true;
                }
                visited.Add(currentId.Value);

                // If we find the article we're trying to move in the ancestor chain, it's circular
                if (currentId.Value == articleId)
                {
                    return true;
                }

                // Move up to parent
                var parent = await GetAccessibleArticles(userId)
                    .AsNoTracking()
                    .Where(a => a.Id == currentId.Value)
                    .Select(a => new { a.ParentId })
                    .FirstOrDefaultAsync();

                currentId = parent?.ParentId;
            }

            return false;
        }

        /// <summary>
        /// Recursively build breadcrumb trail from world to current article.
        /// The first breadcrumb is always the world, followed by the article hierarchy.
        /// </summary>
        private async Task<List<BreadcrumbDto>> BuildBreadcrumbsAsync(Guid articleId, Guid userId)
        {
            var breadcrumbs = new List<BreadcrumbDto>();
            var currentId = (Guid?)articleId;
            Guid? worldId = null;

            // Walk up the tree to build article path
            while (currentId.HasValue)
            {
                var article = await GetAccessibleArticles(userId)
                    .AsNoTracking()
                    .Where(a => a.Id == currentId)
                    .Select(a => new { a.Id, a.Title, a.Slug, a.ParentId, a.Type, a.WorldId })
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

                // Capture the world ID from the first article we find it on
                if (worldId == null && article.WorldId.HasValue)
                {
                    worldId = article.WorldId;
                }

                currentId = article.ParentId;
            }

            // Prepend world breadcrumb if we found a world
            if (worldId.HasValue)
            {
                var world = await _context.Worlds
                    .AsNoTracking()
                    .Where(w => w.Id == worldId.Value)
                    .Select(w => new { w.Id, w.Name, w.Slug })
                    .FirstOrDefaultAsync();

                if (world != null)
                {
                    breadcrumbs.Insert(0, new BreadcrumbDto
                    {
                        Id = world.Id,
                        Title = world.Name,
                        Slug = world.Slug,
                        Type = default, // Not applicable for worlds
                        IsWorld = true
                    });
                }
            }

            return breadcrumbs;
        }

        /// <summary>
        /// Get article by hierarchical path.
        /// Path format: "world-slug/article-slug/child-slug" (e.g., "stormlight/wiki/characters").
        /// The first segment is the world slug, remaining segments are article hierarchy.
        /// </summary>
        public async Task<ArticleDto?> GetArticleByPathAsync(string path, Guid userId)
        {
            if (string.IsNullOrWhiteSpace(path))
                return null;

            var slugs = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (slugs.Length == 0)
                return null;

            // First segment is the world slug
            var worldSlug = slugs[0];
            
            // Look up the world by slug - user must be a member of the world
            var world = await _context.Worlds
                .AsNoTracking()
                .Where(w => w.Slug == worldSlug && w.Members.Any(m => m.UserId == userId))
                .Select(w => new { w.Id })
                .FirstOrDefaultAsync();

            if (world == null)
            {
                _logger.LogWarningSanitized("World not found for slug '{WorldSlug}' or user {UserId} doesn't have access", worldSlug, userId);
                return null;
            }

            // If only world slug provided, no article to return
            if (slugs.Length == 1)
            {
                _logger.LogWarningSanitized("Path '{Path}' contains only world slug, no article path", path);
                return null;
            }

            // Remaining segments are the article path within the world
            Guid? currentParentId = null;
            Guid? articleId = null;

            // Walk down the tree using slugs (starting from index 1, skipping world slug)
            for (int i = 1; i < slugs.Length; i++)
            {
                var slug = slugs[i];
                var isRootLevel = (i == 1); // First article slug (index 1) is at root level

                Article? article;
                if (isRootLevel)
                {
                    // Root-level article: filter by WorldId and ParentId = null
                    article = await GetAccessibleArticles(userId)
                        .AsNoTracking()
                        .Where(a => a.Slug == slug &&
                                    a.ParentId == null &&
                                    a.WorldId == world.Id)
                        .FirstOrDefaultAsync();
                }
                else
                {
                    // Child article: filter by ParentId
                    article = await GetAccessibleArticles(userId)
                        .AsNoTracking()
                        .Where(a => a.Slug == slug &&
                                    a.ParentId == currentParentId)
                        .FirstOrDefaultAsync();
                }

                if (article == null)
                {
                    _logger.LogWarningSanitized("Article not found for slug '{Slug}' under parent {ParentId} in world {WorldId} for user {UserId}",
                        slug, currentParentId, world.Id, userId);
                    return null;
                }

                articleId = article.Id;
                currentParentId = article.Id; // Next iteration looks for children of this article
            }

            // Found the article, now get full details
            return articleId.HasValue
                ? await GetArticleDetailAsync(articleId.Value, userId)
                : null;
        }

        /// <summary>
        /// Check if a slug is unique among siblings.
        /// For root articles (ParentId is null), checks uniqueness within the World.
        /// For child articles, checks uniqueness within the same parent.
        /// </summary>
        public async Task<bool> IsSlugUniqueAsync(string slug, Guid? parentId, Guid? worldId, Guid userId, Guid? excludeArticleId = null)
        {
            IQueryable<Article> query;

            if (parentId.HasValue)
            {
                // Child article: unique among siblings with same parent
                query = GetAccessibleArticles(userId)
                    .AsNoTracking()
                    .Where(a => a.Slug == slug &&
                                a.ParentId == parentId);
            }
            else
            {
                // Root article: unique among root articles in the same world
                query = GetAccessibleArticles(userId)
                    .AsNoTracking()
                    .Where(a => a.Slug == slug &&
                                a.ParentId == null &&
                                a.WorldId == worldId);
            }

            if (excludeArticleId.HasValue)
            {
                query = query.Where(a => a.Id != excludeArticleId.Value);
            }

            return !await query.AnyAsync();
        }

        /// <summary>
        /// Generate a unique slug for an article among its siblings.
        /// For root articles (ParentId is null), checks uniqueness within the World.
        /// For child articles, checks uniqueness within the same parent.
        /// </summary>
        public async Task<string> GenerateUniqueSlugAsync(string title, Guid? parentId, Guid? worldId, Guid userId, Guid? excludeArticleId = null)
        {
            var baseSlug = SlugGenerator.GenerateSlug(title);

            IQueryable<Article> query;

            if (parentId.HasValue)
            {
                // Child article: get slugs from siblings with same parent
                query = GetAccessibleArticles(userId)
                    .AsNoTracking()
                    .Where(a => a.ParentId == parentId);
            }
            else
            {
                // Root article: get slugs from root articles in the same world
                query = GetAccessibleArticles(userId)
                    .AsNoTracking()
                    .Where(a => a.ParentId == null && a.WorldId == worldId);
            }

            var existingSlugs = await query
                .Where(a => !excludeArticleId.HasValue || a.Id != excludeArticleId.Value)
                .Select(a => a.Slug)
                .ToHashSetAsync();

            return SlugGenerator.GenerateUniqueSlug(baseSlug, existingSlugs);
        }

        /// <summary>
        /// Build the full hierarchical path for an article, including world slug.
        /// Returns format: "world-slug/article-slug/child-slug"
        /// </summary>
        public async Task<string> BuildArticlePathAsync(Guid articleId, Guid userId)
        {
            var breadcrumbs = await BuildBreadcrumbsAsync(articleId, userId);
            
            // Breadcrumbs now include world as first element
            // Just join all slugs together
            return string.Join("/", breadcrumbs.Select(b => b.Slug));
        }
    }
}
