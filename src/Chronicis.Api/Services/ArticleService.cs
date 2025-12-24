using Chronicis.Api.Data;
using Chronicis.Shared.DTOs;
using Chronicis.Shared.Enums;
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
        /// Get all root-level articles (ParentId is null) for a specific user.
        /// Optionally filter by WorldId.
        /// </summary>
        public async Task<List<ArticleTreeDto>> GetRootArticlesAsync(Guid userId, Guid? worldId = null)
        {
            var query = _context.Articles
                .AsNoTracking()
                .Where(a => a.ParentId == null && a.CreatedBy == userId);

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
                    CreatedBy = a.CreatedBy
                })
                .OrderBy(a => a.Title)
                .ToListAsync();

            return rootArticles;
        }

        /// <summary>
        /// Get all articles for a user in a flat list (no hierarchy).
        /// Useful for dropdowns, linking dialogs, etc.
        /// Optionally filter by WorldId.
        /// </summary>
        public async Task<List<ArticleTreeDto>> GetAllArticlesAsync(Guid userId, Guid? worldId = null)
        {
            var query = _context.Articles
                .AsNoTracking()
                .Where(a => a.CreatedBy == userId);

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
                    CreatedBy = a.CreatedBy
                })
                .OrderBy(a => a.Title)
                .ToListAsync();

            return articles;
        }

        /// <summary>
        /// Get all child articles of a specific parent.
        /// </summary>
        public async Task<List<ArticleTreeDto>> GetChildrenAsync(Guid parentId, Guid userId)
        {
            var children = await _context.Articles
                .AsNoTracking()
                .Where(a => a.ParentId == parentId && a.CreatedBy == userId)
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
                    CreatedBy = a.CreatedBy
                })
                .OrderBy(a => a.Title)
                .ToListAsync();

            return children;
        }

        /// <summary>
        /// Get full article details including breadcrumb path from root.
        /// </summary>
        public async Task<ArticleDto?> GetArticleDetailAsync(Guid id, Guid userId)
        {
            var article = await _context.Articles
                .AsNoTracking()
                .Where(a => a.Id == id && a.CreatedBy == userId)
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
                    Breadcrumbs = new List<BreadcrumbDto>()  // Will populate separately
                })
                .FirstOrDefaultAsync();

            if (article == null)
            {
                _logger.LogWarning("Article {ArticleId} not found for user {UserId}", id, userId);
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
            // 1. Get the article to move
            var article = await _context.Articles
                .FirstOrDefaultAsync(a => a.Id == articleId && a.CreatedBy == userId);

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

            // 3. If newParentId is specified, validate the target exists and belongs to user
            if (newParentId.HasValue)
            {
                var targetParent = await _context.Articles
                    .AsNoTracking()
                    .FirstOrDefaultAsync(a => a.Id == newParentId.Value && a.CreatedBy == userId);

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
                var parent = await _context.Articles
                    .AsNoTracking()
                    .Where(a => a.Id == currentId.Value && a.CreatedBy == userId)
                    .Select(a => new { a.ParentId })
                    .FirstOrDefaultAsync();

                currentId = parent?.ParentId;
            }

            return false;
        }

        /// <summary>
        /// Recursively build breadcrumb trail from root to current article.
        /// </summary>
        private async Task<List<BreadcrumbDto>> BuildBreadcrumbsAsync(Guid articleId, Guid userId)
        {
            var breadcrumbs = new List<BreadcrumbDto>();
            var currentId = (Guid?)articleId;

            // Walk up the tree to build path
            while (currentId.HasValue)
            {
                var article = await _context.Articles
                    .AsNoTracking()
                    .Where(a => a.Id == currentId && a.CreatedBy == userId)
                    .Select(a => new { a.Id, a.Title, a.Slug, a.ParentId, a.Type })
                    .FirstOrDefaultAsync();

                if (article == null)
                    break;

                breadcrumbs.Insert(0, new BreadcrumbDto
                {
                    Id = article.Id,
                    Title = article.Title ?? "(Untitled)",
                    Slug = article.Slug,
                    Type = article.Type
                });

                currentId = article.ParentId;
            }

            return breadcrumbs;
        }

        /// <summary>
        /// Get article by hierarchical path (e.g., "sword-coast/waterdeep/castle-ward").
        /// </summary>
        public async Task<ArticleDto?> GetArticleByPathAsync(string path, Guid userId)
        {
            if (string.IsNullOrWhiteSpace(path))
                return null;

            var slugs = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (slugs.Length == 0)
                return null;

            Guid? currentParentId = null;
            Guid? articleId = null;

            // Walk down the tree using slugs
            foreach (var slug in slugs)
            {
                var article = await _context.Articles
                    .AsNoTracking()
                    .Where(a => a.Slug == slug &&
                                a.ParentId == currentParentId &&
                                a.CreatedBy == userId)
                    .Select(a => new { a.Id, a.ParentId })
                    .FirstOrDefaultAsync();

                if (article == null)
                {
                    _logger.LogWarning("Article not found for slug '{Slug}' under parent {ParentId} for user {UserId}",
                        slug, currentParentId, userId);
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
        /// Check if a slug is unique within its parent scope.
        /// </summary>
        public async Task<bool> IsSlugUniqueAsync(string slug, Guid? parentId, Guid userId, Guid? excludeArticleId = null)
        {
            var query = _context.Articles
                .AsNoTracking()
                .Where(a => a.Slug == slug &&
                            a.ParentId == parentId &&
                            a.CreatedBy == userId);

            if (excludeArticleId.HasValue)
            {
                query = query.Where(a => a.Id != excludeArticleId.Value);
            }

            return !await query.AnyAsync();
        }

        /// <summary>
        /// Generate a unique slug for an article within its parent scope.
        /// </summary>
        public async Task<string> GenerateUniqueSlugAsync(string title, Guid? parentId, Guid userId, Guid? excludeArticleId = null)
        {
            var baseSlug = SlugGenerator.GenerateSlug(title);

            // Get all existing slugs in the same parent scope
            var existingSlugs = await _context.Articles
                .AsNoTracking()
                .Where(a => a.ParentId == parentId && a.CreatedBy == userId)
                .Where(a => !excludeArticleId.HasValue || a.Id != excludeArticleId.Value)
                .Select(a => a.Slug)
                .ToHashSetAsync();

            return SlugGenerator.GenerateUniqueSlug(baseSlug, existingSlugs);
        }

        /// <summary>
        /// Build the full hierarchical path for an article.
        /// </summary>
        public async Task<string> BuildArticlePathAsync(Guid articleId, Guid userId)
        {
            var breadcrumbs = await BuildBreadcrumbsAsync(articleId, userId);
            return string.Join("/", breadcrumbs.Select(b => b.Slug));
        }
    }
}
