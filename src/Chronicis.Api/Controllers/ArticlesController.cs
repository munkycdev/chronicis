using Chronicis.Api.Data;
using Chronicis.Api.Infrastructure;
using Chronicis.Api.Services;
using Chronicis.Shared.DTOs;
using Chronicis.Shared.Models;
using Chronicis.Shared.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Chronicis.Api.Controllers;

/// <summary>
/// API endpoints for Article operations.
/// </summary>
[ApiController]
[Route("articles")]
[Authorize]
public class ArticlesController : ControllerBase
{
    private readonly IArticleService _articleService;
    private readonly IArticleValidationService _validationService;
    private readonly ILinkSyncService _linkSyncService;
    private readonly ChronicisDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<ArticlesController> _logger;

    public ArticlesController(
        IArticleService articleService,
        IArticleValidationService validationService,
        ILinkSyncService linkSyncService,
        ChronicisDbContext context,
        ICurrentUserService currentUserService,
        ILogger<ArticlesController> logger)
    {
        _articleService = articleService;
        _validationService = validationService;
        _linkSyncService = linkSyncService;
        _context = context;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    /// <summary>
    /// GET /api/articles - Returns all root-level articles (those without a parent).
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ArticleTreeDto>>> GetRootArticles([FromQuery] Guid? worldId)
    {
        var user = await _currentUserService.GetRequiredUserAsync();

        try
        {
            var articles = await _articleService.GetRootArticlesAsync(user.Id, worldId);
            return Ok(articles);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching root articles");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// GET /api/articles/all - Returns all articles for the current user in a flat list.
    /// </summary>
    [HttpGet("all")]
    public async Task<ActionResult<IEnumerable<ArticleTreeDto>>> GetAllArticles([FromQuery] Guid? worldId)
    {
        var user = await _currentUserService.GetRequiredUserAsync();

        try
        {
            var articles = await _articleService.GetAllArticlesAsync(user.Id, worldId);
            return Ok(articles);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching all articles");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// GET /api/articles/{id} - Returns detailed information for a specific article.
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ArticleDto>> GetArticleDetail(Guid id)
    {
        var user = await _currentUserService.GetRequiredUserAsync();

        try
        {
            var article = await _articleService.GetArticleDetailAsync(id, user.Id);

            if (article == null)
            {
                return NotFound(new { message = $"Article {id} not found" });
            }

            return Ok(article);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching article {ArticleId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// GET /api/articles/{id}/children - Returns all child articles of the specified parent.
    /// </summary>
    [HttpGet("{id:guid}/children")]
    public async Task<ActionResult<IEnumerable<ArticleTreeDto>>> GetArticleChildren(Guid id)
    {
        var user = await _currentUserService.GetRequiredUserAsync();

        try
        {
            var children = await _articleService.GetChildrenAsync(id, user.Id);
            return Ok(children);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching children for article {ParentId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// GET /api/articles/by-path/{*path} - Gets an article by its URL path.
    /// </summary>
    [HttpGet("by-path/{*path}")]
    public async Task<ActionResult<ArticleDto>> GetArticleByPath(string path)
    {
        var user = await _currentUserService.GetRequiredUserAsync();

        try
        {
            var article = await _articleService.GetArticleByPathAsync(path, user.Id);

            if (article == null)
            {
                return NotFound(new { message = "Article not found" });
            }

            return Ok(article);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching article by path: {Path}", path);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// POST /api/articles - Creates a new article.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ArticleDto>> CreateArticle([FromBody] ArticleCreateDto dto)
    {
        var user = await _currentUserService.GetRequiredUserAsync();

        try
        {
            if (dto == null)
            {
                return BadRequest("Invalid request body");
            }

            var validationResult = await _validationService.ValidateCreateAsync(dto);
            if (!validationResult.IsValid)
            {
                return BadRequest(new { errors = validationResult.Errors });
            }

            // Generate slug
            string slug;
            if (!string.IsNullOrWhiteSpace(dto.Slug))
            {
                if (!SlugGenerator.IsValidSlug(dto.Slug))
                {
                    return BadRequest("Slug must contain only lowercase letters, numbers, and hyphens");
                }

                if (!await _articleService.IsSlugUniqueAsync(dto.Slug, dto.ParentId, dto.WorldId, user.Id))
                {
                    return Conflict($"An article with slug '{dto.Slug}' already exists in this location");
                }

                slug = dto.Slug;
            }
            else
            {
                slug = await _articleService.GenerateUniqueSlugAsync(dto.Title, dto.ParentId, dto.WorldId, user.Id);
            }

            var article = new Article
            {
                Id = Guid.NewGuid(),
                Title = dto.Title,
                Slug = slug,
                ParentId = dto.ParentId,
                WorldId = dto.WorldId,
                CampaignId = dto.CampaignId,
                ArcId = dto.ArcId,
                Body = dto.Body,
                Type = dto.Type,
                Visibility = dto.Visibility,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = user.Id,
                EffectiveDate = dto.EffectiveDate ?? DateTime.UtcNow,
                IconEmoji = dto.IconEmoji,
                SessionDate = dto.SessionDate,
                InGameDate = dto.InGameDate,
                PlayerId = dto.PlayerId
            };

            _context.Articles.Add(article);
            await _context.SaveChangesAsync();

            // Sync wiki links if body contains content
            if (!string.IsNullOrEmpty(dto.Body))
            {
                await _linkSyncService.SyncLinksAsync(article.Id, dto.Body);
            }

            var responseDto = new ArticleDto
            {
                Id = article.Id,
                Title = article.Title,
                Slug = article.Slug,
                ParentId = article.ParentId,
                WorldId = article.WorldId,
                CampaignId = article.CampaignId,
                ArcId = article.ArcId,
                Body = article.Body ?? string.Empty,
                Type = article.Type,
                Visibility = article.Visibility,
                CreatedAt = article.CreatedAt,
                ModifiedAt = article.ModifiedAt,
                EffectiveDate = article.EffectiveDate,
                CreatedBy = article.CreatedBy,
                IconEmoji = article.IconEmoji,
                HasChildren = false
            };

            return CreatedAtAction(nameof(GetArticleDetail), new { id = article.Id }, responseDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating article");
            return StatusCode(500, $"Error creating article: {ex.Message}");
        }
    }

    /// <summary>
    /// PUT /api/articles/{id} - Updates an existing article.
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ArticleDto>> UpdateArticle(Guid id, [FromBody] ArticleUpdateDto dto)
    {
        var user = await _currentUserService.GetRequiredUserAsync();

        try
        {
            if (dto == null)
            {
                return BadRequest("Invalid request body");
            }

            var validationResult = await _validationService.ValidateUpdateAsync(id, dto);
            if (!validationResult.IsValid)
            {
                return BadRequest(new { errors = validationResult.Errors });
            }

            // Get article - check user has access via world membership
            var article = await _context.Articles
                .Where(a => a.Id == id)
                .Where(a => a.World != null && a.World.Members.Any(m => m.UserId == user.Id))
                .FirstOrDefaultAsync();

            if (article == null)
            {
                return NotFound($"Article {id} not found");
            }

            // Handle slug update if provided
            if (!string.IsNullOrWhiteSpace(dto.Slug) && dto.Slug != article.Slug)
            {
                if (!SlugGenerator.IsValidSlug(dto.Slug))
                {
                    return BadRequest("Slug must contain only lowercase letters, numbers, and hyphens");
                }

                if (!await _articleService.IsSlugUniqueAsync(dto.Slug, article.ParentId, article.WorldId, user.Id, id))
                {
                    return Conflict($"An article with slug '{dto.Slug}' already exists in this location");
                }

                article.Slug = dto.Slug;
            }

            // Update fields
            if (dto.Title != null) article.Title = dto.Title;
            if (dto.Body != null) article.Body = dto.Body;
            if (dto.EffectiveDate.HasValue) article.EffectiveDate = dto.EffectiveDate.Value;
            if (dto.IconEmoji != null) article.IconEmoji = dto.IconEmoji;
            if (dto.SessionDate.HasValue) article.SessionDate = dto.SessionDate;
            if (dto.InGameDate != null) article.InGameDate = dto.InGameDate;
            if (dto.Visibility.HasValue) article.Visibility = dto.Visibility.Value;
            if (dto.Type.HasValue) article.Type = dto.Type.Value;

            article.ModifiedAt = DateTime.UtcNow;
            article.LastModifiedBy = user.Id;

            await _context.SaveChangesAsync();

            // Sync wiki links after update
            if (!string.IsNullOrEmpty(dto.Body))
            {
                await _linkSyncService.SyncLinksAsync(id, dto.Body);
            }

            // Return updated article
            var updatedArticle = await _articleService.GetArticleDetailAsync(id, user.Id);
            return Ok(updatedArticle);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating article {ArticleId}", id);
            return StatusCode(500, $"Error updating article: {ex.Message}");
        }
    }

    /// <summary>
    /// DELETE /api/articles/{id} - Deletes an article and all its children.
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteArticle(Guid id)
    {
        var user = await _currentUserService.GetRequiredUserAsync();

        try
        {
            // Get article - check user has access via world membership
            var article = await _context.Articles
                .Where(a => a.Id == id)
                .Where(a => a.World != null && a.World.Members.Any(m => m.UserId == user.Id))
                .FirstOrDefaultAsync();

            if (article == null)
            {
                return NotFound($"Article {id} not found");
            }

            // Delete all descendants recursively
            await DeleteArticleAndDescendantsAsync(id);

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting article {ArticleId}", id);
            return StatusCode(500, $"Error deleting article: {ex.Message}");
        }
    }

    /// <summary>
    /// PUT /api/articles/{id}/move - Moves an article to a new parent.
    /// </summary>
    [HttpPut("{id:guid}/move")]
    public async Task<IActionResult> MoveArticle(Guid id, [FromBody] ArticleMoveDto dto)
    {
        var user = await _currentUserService.GetRequiredUserAsync();

        try
        {
            if (dto == null)
            {
                return BadRequest("Invalid request body");
            }

            var (success, errorMessage) = await _articleService.MoveArticleAsync(id, dto.NewParentId, user.Id);

            if (!success)
            {
                return BadRequest(errorMessage);
            }

            // Return the updated article
            var article = await _articleService.GetArticleDetailAsync(id, user.Id);
            return Ok(article);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error moving article {ArticleId}", id);
            return StatusCode(500, $"Error moving article: {ex.Message}");
        }
    }

    /// <summary>
    /// Recursively deletes an article and all its descendants.
    /// </summary>
    private async Task DeleteArticleAndDescendantsAsync(Guid articleId)
    {
        // Get all children
        var children = await _context.Articles
            .Where(a => a.ParentId == articleId)
            .Select(a => a.Id)
            .ToListAsync();

        // Recursively delete children first
        foreach (var childId in children)
        {
            await DeleteArticleAndDescendantsAsync(childId);
        }

        // Delete article links pointing to/from this article
        var linksToDelete = await _context.ArticleLinks
            .Where(l => l.SourceArticleId == articleId || l.TargetArticleId == articleId)
            .ToListAsync();
        _context.ArticleLinks.RemoveRange(linksToDelete);

        // Delete the article itself
        var article = await _context.Articles.FindAsync(articleId);
        if (article != null)
        {
            _context.Articles.Remove(article);
        }

        await _context.SaveChangesAsync();
    }
}
