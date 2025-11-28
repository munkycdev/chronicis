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
        Task<List<ArticleTreeDto>> GetRootArticlesAsync();
        Task<List<ArticleTreeDto>> GetChildrenAsync(int parentId);
        Task<ArticleDto?> GetArticleDetailAsync(int id);
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
        /// Get all root-level articles (ParentId is null).
        /// </summary>
        public async Task<List<ArticleTreeDto>> GetRootArticlesAsync()
        {
            _logger.LogInformation("Fetching root articles");

            var rootArticles = await _context.Articles
            .Where(a => a.ParentId == null)
            .Include(a => a.Children)
            .OrderBy(a => a.Title)
            .ToListAsync();

            return rootArticles.Select(a => MapToDto(a)).ToList();
        }

        /// <summary>
        /// Get all child articles of a specific parent.
        /// </summary>
        public async Task<List<ArticleTreeDto>> GetChildrenAsync(int parentId)
        {
            _logger.LogInformation("Fetching children for article {ParentId}", parentId);

            var children = await _context.Articles
                .Where(a => a.ParentId == parentId)
                .Include(a => a.Children)  // Load children for recursive mapping
                .OrderBy(a => a.Title)
                .ToListAsync();

            return children.Select(a => MapToDtoWithChildCount(a)).ToList();
        }

        /// <summary>
        /// Get full article details including breadcrumb path from root.
        /// </summary>
        public async Task<ArticleDto?> GetArticleDetailAsync(int id)
        {
            _logger.LogInformation("Fetching article detail for {ArticleId}", id);

            var article = await _context.Articles
                .Include(a => a.Parent)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (article == null)
            {
                _logger.LogWarning("Article {ArticleId} not found", id);
                return null;
            }

            // Build breadcrumb path by walking up the hierarchy
            var breadcrumbs = await BuildBreadcrumbsAsync(article);

            return new ArticleDto
            {
                Id = article.Id,
                Title = article.Title,
                ParentId = article.ParentId,
                Body = article?.Body ?? string.Empty,
                CreatedDate = article?.CreatedDate ?? DateTime.UtcNow,
                ModifiedDate = article?.ModifiedDate ?? DateTime.UtcNow,
                Breadcrumbs = breadcrumbs
            };
        }

        /// <summary>
        /// Recursively build breadcrumb trail from root to current article.
        /// </summary>
        private async Task<List<BreadcrumbDto>> BuildBreadcrumbsAsync(Article article)
        {
            var breadcrumbs = new List<BreadcrumbDto>();
            var current = article;

            // Walk up the tree to build path
            while (current != null)
            {
                breadcrumbs.Insert(0, new BreadcrumbDto
                {
                    Id = current.Id,
                    Title = current.Title
                });

                if (current.ParentId.HasValue)
                {
                    current = await _context.Articles
                        .FirstOrDefaultAsync(a => a.Id == current.ParentId.Value);
                }
                else
                {
                    current = null;
                }
            }

            return breadcrumbs;
        }

        private static ArticleTreeDto MapToDto(Article article)
        {
            return new ArticleTreeDto
            {
                Id = article.Id,
                Title = article.Title,
                ParentId = article.ParentId,
                HasChildren = article.Children?.Any() ?? false,
                ChildCount = article.Children?.Count ?? 0,  // ADD THIS LINE
                Children = article.Children?.Select(c => MapToDto(c)).ToList() ?? new List<ArticleTreeDto>(),
                CreatedDate = article.CreatedDate,
                EffectiveDate = article.EffectiveDate,  // ADD THIS TOO
                IconEmoji = article.IconEmoji  // AND THIS
            };
        }

        private ArticleTreeDto MapToDtoWithChildCount(Article article)
        {
            // Count children from database, not from loaded collection
            var childCount = _context.Articles.Count(a => a.ParentId == article.Id);

            return new ArticleTreeDto
            {
                Id = article.Id,
                Title = article.Title,
                ParentId = article.ParentId,
                HasChildren = childCount > 0,
                ChildCount = childCount,
                Children = article.Children?.Select(c => MapToDtoWithChildCount(c)).ToList() ?? new List<ArticleTreeDto>(),
                CreatedDate = article.CreatedDate,
                EffectiveDate = article.EffectiveDate,
                IconEmoji = article.IconEmoji
            };
        }
    }
}
