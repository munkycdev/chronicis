using Chronicis.Api.Data;
using Chronicis.Shared.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Chronicis.Api.Services
{
    /// <summary>
    /// Repository service for Article entity operations.
    /// Handles all database queries and business logic for articles.
    /// </summary>
    public interface IArticleService
    {
        Task<List<ArticleTreeDto>> GetRootArticlesAsync(int userId);
        Task<List<ArticleTreeDto>> GetChildrenAsync(int parentId, int userId);
        Task<ArticleDto?> GetArticleDetailAsync(int id, int userId);

        /// <summary>
        /// Move an article to a new parent (or to root if newParentId is null).
        /// Validates ownership and prevents circular references.
        /// </summary>
        /// <param name="articleId">The article to move</param>
        /// <param name="newParentId">The new parent ID, or null to make root-level</param>
        /// <param name="userId">The user performing the operation</param>
        /// <returns>True if successful, false if validation failed</returns>
        Task<(bool Success, string? ErrorMessage)> MoveArticleAsync(int articleId, int? newParentId, int userId);
    }

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
        /// </summary>
        public async Task<List<ArticleTreeDto>> GetRootArticlesAsync(int userId)
        {
            // Use AsNoTracking and direct projection to avoid User navigation issues
            var rootArticles = await _context.Articles
                .AsNoTracking()
                .Where(a => a.ParentId == null && a.User.Id == userId)
                .Select(a => new ArticleTreeDto
                {
                    Id = a.Id,
                    Title = a.Title,
                    ParentId = a.ParentId,
                    HasChildren = _context.Articles.Any(c => c.ParentId == a.Id),
                    ChildCount = _context.Articles.Count(c => c.ParentId == a.Id),
                    Children = new List<ArticleTreeDto>(),
                    CreatedDate = a.CreatedDate,
                    EffectiveDate = a.EffectiveDate,
                    IconEmoji = a.IconEmoji
                })
                .OrderBy(a => a.Title)
                .ToListAsync();

            return rootArticles;
        }

        /// <summary>
        /// Get all child articles of a specific parent.
        /// </summary>
        public async Task<List<ArticleTreeDto>> GetChildrenAsync(int parentId, int userId)
        {
            // Use AsNoTracking and direct projection
            var children = await _context.Articles
                .AsNoTracking()
                .Where(a => a.ParentId == parentId && a.User.Id == userId)
                .Select(a => new ArticleTreeDto
                {
                    Id = a.Id,
                    Title = a.Title,
                    ParentId = a.ParentId,
                    HasChildren = _context.Articles.Any(c => c.ParentId == a.Id),
                    ChildCount = _context.Articles.Count(c => c.ParentId == a.Id),
                    Children = new List<ArticleTreeDto>(),
                    CreatedDate = a.CreatedDate,
                    EffectiveDate = a.EffectiveDate,
                    IconEmoji = a.IconEmoji
                })
                .OrderBy(a => a.Title)
                .ToListAsync();

            return children;
        }

        /// <summary>
        /// Get full article details including breadcrumb path from root.
        /// </summary>
        public async Task<ArticleDto?> GetArticleDetailAsync(int id, int userId)
        {
            // Use AsNoTracking and direct projection
            var article = await _context.Articles
                .AsNoTracking()
                .Where(a => a.Id == id && a.User.Id == userId)
                .Select(a => new ArticleDto
                {
                    Id = a.Id,
                    Title = a.Title,
                    ParentId = a.ParentId,
                    Body = a.Body ?? string.Empty,
                    CreatedDate = a.CreatedDate,
                    ModifiedDate = a.ModifiedDate,
                    EffectiveDate = a.EffectiveDate,
                    IconEmoji = a.IconEmoji,
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
        public async Task<(bool Success, string? ErrorMessage)> MoveArticleAsync(int articleId, int? newParentId, int userId)
        {
            // 1. Get the article to move
            var article = await _context.Articles
                .FirstOrDefaultAsync(a => a.Id == articleId && a.UserId == userId);

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
                    .FirstOrDefaultAsync(a => a.Id == newParentId.Value && a.UserId == userId);

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
            article.ModifiedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return (true, null);
        }

        /// <summary>
        /// Check if moving articleId to become a child of targetParentId would create a circular reference.
        /// This happens if targetParentId is the article itself or any of its descendants.
        /// </summary>
        private async Task<bool> WouldCreateCircularReferenceAsync(int articleId, int targetParentId, int userId)
        {
            // If trying to move to self, that's circular
            if (articleId == targetParentId)
            {
                return true;
            }

            // Walk up from targetParentId to root, checking if we encounter articleId
            var currentId = (int?)targetParentId;
            var visited = new HashSet<int>();

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
                    .Where(a => a.Id == currentId.Value && a.UserId == userId)
                    .Select(a => new { a.ParentId })
                    .FirstOrDefaultAsync();

                currentId = parent?.ParentId;
            }

            return false;
        }

        /// <summary>
        /// Recursively build breadcrumb trail from root to current article.
        /// </summary>
        private async Task<List<BreadcrumbDto>> BuildBreadcrumbsAsync(int articleId, int userId)
        {
            var breadcrumbs = new List<BreadcrumbDto>();
            var currentId = (int?)articleId;

            // Walk up the tree to build path
            while (currentId.HasValue)
            {
                var article = await _context.Articles
                    .AsNoTracking()
                    .Where(a => a.Id == currentId && a.User.Id == userId)
                    .Select(a => new { a.Id, a.Title, a.ParentId })
                    .FirstOrDefaultAsync();

                if (article == null)
                    break;

                breadcrumbs.Insert(0, new BreadcrumbDto
                {
                    Id = article.Id,
                    Title = article.Title ?? "(Untitled)"
                });

                currentId = article.ParentId;
            }

            return breadcrumbs;
        }
    }
}
