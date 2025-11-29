using Chronicis.Api.Data;
using Chronicis.Shared.DTOs;
using Chronicis.Shared.Models;
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
            _logger.LogInformation("Fetching root articles for user {UserId}", userId);

            // Use AsNoTracking and direct projection to avoid User navigation issues
            var rootArticles = await _context.Articles
                .AsNoTracking()
                .Where(a => a.ParentId == null && a.UserId == userId)
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
            _logger.LogInformation("Fetching children for article {ParentId}, user {UserId}", parentId, userId);

            // Use AsNoTracking and direct projection
            var children = await _context.Articles
                .AsNoTracking()
                .Where(a => a.ParentId == parentId && a.UserId == userId)
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
            _logger.LogInformation("Fetching article detail for {ArticleId}, user {UserId}", id, userId);

            // Use AsNoTracking and direct projection
            var article = await _context.Articles
                .AsNoTracking()
                .Where(a => a.Id == id && a.UserId == userId)
                .Select(a => new ArticleDto
                {
                    Id = a.Id,
                    Title = a.Title,
                    ParentId = a.ParentId,
                    Body = a.Body ?? string.Empty,
                    CreatedDate = a.CreatedDate,
                    ModifiedDate = a.ModifiedDate,
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
                    .Where(a => a.Id == currentId && a.UserId == userId)
                    .Select(a => new { a.Id, a.Title, a.ParentId })
                    .FirstOrDefaultAsync();

                if (article == null) break;

                breadcrumbs.Insert(0, new BreadcrumbDto
                {
                    Id = article.Id,
                    Title = article.Title ?? "(Untitled)"
                });

                currentId = article.ParentId;
            }

            return breadcrumbs;
        }

        private static string CreateSlug(string? title)
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                return "untitled";
            }

            return title.ToLowerInvariant()
                .Replace(" ", "-")
                .Replace(":", "")
                .Replace("!", "")
                .Replace("?", "")
                .Replace("'", "")
                .Replace("\"", "");
        }
    }
}