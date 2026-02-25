using Chronicis.Api.Data;
using Chronicis.Api.Infrastructure;
using Chronicis.Api.Services;
using Chronicis.Api.Services.Articles;
using Chronicis.Shared.DTOs;
using Chronicis.Shared.Enums;
using Chronicis.Shared.Extensions;
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
    private readonly IAutoLinkService _autoLinkService;
    private readonly IArticleExternalLinkService _externalLinkService;
    private readonly IArticleHierarchyService _hierarchyService;
    private readonly ChronicisDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IWorldDocumentService _worldDocumentService;
    private readonly ILogger<ArticlesController> _logger;

    public ArticlesController(
        IArticleService articleService,
        IArticleValidationService validationService,
        ILinkSyncService linkSyncService,
        IAutoLinkService autoLinkService,
        IArticleExternalLinkService externalLinkService,
        IArticleHierarchyService hierarchyService,
        ChronicisDbContext context,
        ICurrentUserService currentUserService,
        IWorldDocumentService worldDocumentService,
        ILogger<ArticlesController> logger)
    {
        _articleService = articleService;
        _validationService = validationService;
        _linkSyncService = linkSyncService;
        _autoLinkService = autoLinkService;
        _externalLinkService = externalLinkService;
        _hierarchyService = hierarchyService;
        _context = context;
        _currentUserService = currentUserService;
        _worldDocumentService = worldDocumentService;
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
            _logger.LogErrorSanitized(ex, "Error fetching article by path: {Path}", path);
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

            if (dto.Type == ArticleType.Tutorial && !await _currentUserService.IsSysAdminAsync())
            {
                return Forbid();
            }

            var normalizedWorldId = dto.Type == ArticleType.Tutorial
                ? Guid.Empty
                : dto.WorldId;

            // Generate slug
            string slug;
            if (!string.IsNullOrWhiteSpace(dto.Slug))
            {
                if (!SlugGenerator.IsValidSlug(dto.Slug))
                {
                    return BadRequest("Slug must contain only lowercase letters, numbers, and hyphens");
                }

                var isSlugUnique = dto.Type == ArticleType.Tutorial
                    ? await IsTutorialSlugUniqueAsync(dto.Slug, dto.ParentId)
                    : await _articleService.IsSlugUniqueAsync(dto.Slug, dto.ParentId, normalizedWorldId, user.Id);

                if (!isSlugUnique)
                {
                    return Conflict($"An article with slug '{dto.Slug}' already exists in this location");
                }

                slug = dto.Slug;
            }
            else
            {
                slug = dto.Type == ArticleType.Tutorial
                    ? await GenerateTutorialSlugAsync(dto.Title, dto.ParentId)
                    : await _articleService.GenerateUniqueSlugAsync(dto.Title, dto.ParentId, normalizedWorldId, user.Id);
            }

            var article = new Article
            {
                Id = Guid.NewGuid(),
                Title = dto.Title,
                Slug = slug,
                ParentId = dto.ParentId,
                WorldId = normalizedWorldId,
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

            var article = await FindReadableArticleAsync(id, user.Id);

            if (article == null)
            {
                return NotFound($"Article {id} not found");
            }

            var isTutorialArticle = article.Type == ArticleType.Tutorial;
            var targetType = dto.Type ?? article.Type;
            var targetIsTutorial = targetType == ArticleType.Tutorial;

            if ((isTutorialArticle || targetIsTutorial) && !await _currentUserService.IsSysAdminAsync())
            {
                return Forbid();
            }

            if (isTutorialArticle && dto.Type.HasValue && dto.Type.Value != ArticleType.Tutorial)
            {
                return BadRequest("Tutorial articles cannot be recategorized.");
            }

            // Handle slug update if provided
            if (!string.IsNullOrWhiteSpace(dto.Slug) && dto.Slug != article.Slug)
            {
                if (!SlugGenerator.IsValidSlug(dto.Slug))
                {
                    return BadRequest("Slug must contain only lowercase letters, numbers, and hyphens");
                }

                var isSlugUnique = targetIsTutorial
                    ? await IsTutorialSlugUniqueAsync(dto.Slug, article.ParentId, id)
                    : await _articleService.IsSlugUniqueAsync(dto.Slug, article.ParentId, article.WorldId, user.Id, id);

                if (!isSlugUnique)
                {
                    return Conflict($"An article with slug '{dto.Slug}' already exists in this location");
                }

                article.Slug = dto.Slug;
            }

            // Update fields
            if (dto.Title != null)
                article.Title = dto.Title;
            if (dto.Body != null)
                article.Body = dto.Body;
            if (dto.EffectiveDate.HasValue)
                article.EffectiveDate = dto.EffectiveDate.Value;
            if (dto.IconEmoji != null)
                article.IconEmoji = dto.IconEmoji;
            if (dto.SessionDate.HasValue)
                article.SessionDate = dto.SessionDate;
            if (dto.InGameDate != null)
                article.InGameDate = dto.InGameDate;
            if (dto.Visibility.HasValue)
                article.Visibility = dto.Visibility.Value;
            if (dto.Type.HasValue)
                article.Type = dto.Type.Value;

            if (targetIsTutorial)
            {
                article.WorldId = Guid.Empty;
                article.CampaignId = null;
                article.ArcId = null;
                article.SessionId = null;
                if (!isTutorialArticle)
                {
                    article.ParentId = null;
                }
            }

            article.ModifiedAt = DateTime.UtcNow;
            article.LastModifiedBy = user.Id;

            await _context.SaveChangesAsync();

            // Sync wiki links after update
            if (!string.IsNullOrEmpty(dto.Body))
            {
                await _linkSyncService.SyncLinksAsync(id, dto.Body);
            }

            // Sync external links after update
            await _externalLinkService.SyncExternalLinksAsync(id, dto.Body);

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
            var article = await FindReadableArticleAsync(id, user.Id);

            if (article == null)
            {
                return NotFound($"Article {id} not found");
            }

            if (article.Type == ArticleType.Tutorial && !await _currentUserService.IsSysAdminAsync())
            {
                return Forbid();
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

            var (success, errorMessage) = await _articleService.MoveArticleAsync(id, dto.NewParentId, dto.NewSessionId, user.Id);

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

    #region Aliases

    /// <summary>
    /// PUT /api/articles/{id}/aliases - Updates all aliases for an article.
    /// Accepts a comma-delimited string that replaces all existing aliases.
    /// </summary>
    [HttpPut("{id:guid}/aliases")]
    public async Task<ActionResult<ArticleDto>> UpdateAliases(Guid id, [FromBody] ArticleAliasesUpdateDto dto)
    {
        var user = await _currentUserService.GetRequiredUserAsync();

        try
        {
            if (dto == null)
            {
                return BadRequest("Invalid request body");
            }

            var article = await _context.Articles
                .Include(a => a.Aliases)
                .Where(a => a.Id == id)
                .Where(a => (a.Type == ArticleType.Tutorial && a.WorldId == Guid.Empty) ||
                            (a.World != null && a.World.Members.Any(m => m.UserId == user.Id)))
                .FirstOrDefaultAsync();

            if (article == null)
            {
                return NotFound($"Article {id} not found");
            }

            if (article.Type == ArticleType.Tutorial && !await _currentUserService.IsSysAdminAsync())
            {
                return Forbid();
            }

            // Parse the comma-delimited aliases
            var newAliases = ParseAliases(dto.Aliases);

            // Validate: aliases cannot match the article's own title
            var titleLower = article.Title?.ToLowerInvariant() ?? string.Empty;
            var invalidAliases = newAliases.Where(a => a.ToLowerInvariant() == titleLower).ToList();
            if (invalidAliases.Any())
            {
                return BadRequest($"Alias cannot match the article's title: {string.Join(", ", invalidAliases)}");
            }

            // Remove aliases that are no longer in the list
            var aliasesToRemove = article.Aliases
                .Where(existing => !newAliases.Contains(existing.AliasText, StringComparer.OrdinalIgnoreCase))
                .ToList();
            foreach (var alias in aliasesToRemove)
            {
                _context.ArticleAliases.Remove(alias);
            }

            // Add new aliases that don't already exist
            var existingAliasTexts = article.Aliases
                .Select(a => a.AliasText.ToLowerInvariant())
                .ToHashSet();

            foreach (var aliasText in newAliases)
            {
                if (!existingAliasTexts.Contains(aliasText.ToLowerInvariant()))
                {
                    var newAlias = new ArticleAlias
                    {
                        Id = Guid.NewGuid(),
                        ArticleId = id,
                        AliasText = aliasText,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.ArticleAliases.Add(newAlias);
                }
            }

            article.ModifiedAt = DateTime.UtcNow;
            article.LastModifiedBy = user.Id;

            await _context.SaveChangesAsync();

            // Return updated article with aliases
            var updatedArticle = await _articleService.GetArticleDetailAsync(id, user.Id);
            return Ok(updatedArticle);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating aliases for article {ArticleId}", id);
            return StatusCode(500, $"Error updating aliases: {ex.Message}");
        }
    }

    /// <summary>
    /// Parses a comma-delimited string into a list of trimmed, non-empty, unique aliases.
    /// </summary>
    private static List<string> ParseAliases(string? aliasesString)
    {
        if (string.IsNullOrWhiteSpace(aliasesString))
        {
            return new List<string>();
        }

        return aliasesString
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(a => a.Trim())
            .Where(a => !string.IsNullOrWhiteSpace(a) && a.Length <= 200)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    #endregion

    #region Wiki Links

    /// <summary>
    /// GET /articles/{id}/backlinks - Gets all articles that link to this article.
    /// </summary>
    [HttpGet("{id:guid}/backlinks")]
    public async Task<ActionResult<BacklinksResponseDto>> GetBacklinks(Guid id)
    {
        var user = await _currentUserService.GetRequiredUserAsync();
        _logger.LogDebug("Getting backlinks for article {ArticleId}", id);

        var article = await FindReadableArticleAsync(id, user.Id);

        if (article == null)
        {
            return NotFound(new { error = "Article not found or access denied" });
        }

        // Get all articles that link TO this article
        var backlinks = await _context.ArticleLinks
            .Where(l => l.TargetArticleId == id)
            .Select(l => new BacklinkDto
            {
                ArticleId = l.SourceArticleId,
                Title = l.SourceArticle.Title ?? "Untitled",
                Slug = l.SourceArticle.Slug,
                Snippet = l.DisplayText,
                DisplayPath = ""
            })
            .Distinct()
            .ToListAsync();

        // Build display paths using centralised hierarchy service
        foreach (var backlink in backlinks)
        {
            backlink.DisplayPath = await _hierarchyService.BuildDisplayPathAsync(backlink.ArticleId);
        }

        return Ok(new BacklinksResponseDto { Backlinks = backlinks });
    }

    /// <summary>
    /// GET /articles/{id}/outgoing-links - Gets all articles that this article links to.
    /// </summary>
    [HttpGet("{id:guid}/outgoing-links")]
    public async Task<ActionResult<BacklinksResponseDto>> GetOutgoingLinks(Guid id)
    {
        var user = await _currentUserService.GetRequiredUserAsync();
        _logger.LogDebug("Getting outgoing links for article {ArticleId}", id);

        var article = await FindReadableArticleAsync(id, user.Id);

        if (article == null)
        {
            return NotFound(new { error = "Article not found or access denied" });
        }

        // Get all articles that this article links TO
        var outgoingLinks = await _context.ArticleLinks
            .Where(l => l.SourceArticleId == id)
            .Select(l => new BacklinkDto
            {
                ArticleId = l.TargetArticleId,
                Title = l.TargetArticle.Title ?? "Untitled",
                Slug = l.TargetArticle.Slug,
                Snippet = l.DisplayText,
                DisplayPath = ""
            })
            .Distinct()
            .ToListAsync();

        // Build display paths using centralised hierarchy service
        foreach (var link in outgoingLinks)
        {
            link.DisplayPath = await _hierarchyService.BuildDisplayPathAsync(link.ArticleId);
        }

        return Ok(new BacklinksResponseDto { Backlinks = outgoingLinks });
    }

    /// <summary>
    /// POST /articles/resolve-links - Resolves multiple article IDs to check if they exist.
    /// </summary>
    [HttpPost("resolve-links")]
    public async Task<ActionResult<LinkResolutionResponseDto>> ResolveLinks([FromBody] LinkResolutionRequestDto request)
    {
        var user = await _currentUserService.GetRequiredUserAsync();

        if (request?.ArticleIds == null || !request.ArticleIds.Any())
        {
            return Ok(new LinkResolutionResponseDto { Articles = new Dictionary<Guid, ResolvedLinkDto>() });
        }

        _logger.LogDebug("Resolving {Count} article links", request.ArticleIds.Count);

        // Get all requested articles that the user has access to
        var articles = await GetReadableArticlesQuery(user.Id)
            .Where(a => request.ArticleIds.Contains(a.Id))
            .Select(a => new ResolvedLinkDto
            {
                ArticleId = a.Id,
                Exists = true,
                Title = a.Title,
                Slug = a.Slug
            })
            .ToListAsync();

        // Build response dictionary
        var result = new LinkResolutionResponseDto
        {
            Articles = new Dictionary<Guid, ResolvedLinkDto>()
        };

        // Add found articles
        foreach (var article in articles)
        {
            result.Articles[article.ArticleId] = article;
        }

        // Add missing articles as non-existent
        foreach (var requestedId in request.ArticleIds)
        {
            if (!result.Articles.ContainsKey(requestedId))
            {
                result.Articles[requestedId] = new ResolvedLinkDto
                {
                    ArticleId = requestedId,
                    Exists = false,
                    Title = null,
                    Slug = null
                };
            }
        }

        return Ok(result);
    }

    /// <summary>
    /// POST /articles/{id}/auto-link - Scans article content and returns match positions for wiki links.
    /// </summary>
    [HttpPost("{id:guid}/auto-link")]
    public async Task<ActionResult<AutoLinkResponseDto>> AutoLink(Guid id, [FromBody] AutoLinkRequestDto request)
    {
        var user = await _currentUserService.GetRequiredUserAsync();

        if (request == null || string.IsNullOrEmpty(request.Body))
        {
            return BadRequest(new { error = "Body content is required" });
        }

        _logger.LogDebug("Auto-linking article {ArticleId}", id);

        // Get article and verify access
        var article = await GetReadableArticlesQuery(user.Id)
            .Where(a => a.Id == id)
            .Select(a => new { a.Id, a.WorldId })
            .FirstOrDefaultAsync();

        if (article == null)
        {
            return NotFound(new { error = "Article not found or access denied" });
        }

        if (!article.WorldId.HasValue)
        {
            return BadRequest(new { error = "Article must belong to a world" });
        }

        var result = await _autoLinkService.FindLinksAsync(
            id,
            article.WorldId.Value,
            request.Body,
            user.Id);

        return Ok(result);
    }

    #endregion

    #region Private Helpers

    private IQueryable<Article> GetReadableArticlesQuery(Guid userId)
    {
        var worldScoped = _context.Articles
            .Where(a => a.Type != ArticleType.Tutorial && a.WorldId != Guid.Empty)
            .Where(a => a.World != null && a.World.Members.Any(m => m.UserId == userId));

        var tutorials = _context.Articles
            .Where(a => a.Type == ArticleType.Tutorial && a.WorldId == Guid.Empty);

        return worldScoped.Concat(tutorials);
    }

    private async Task<Article?> FindReadableArticleAsync(Guid articleId, Guid userId)
    {
        return await GetReadableArticlesQuery(userId)
            .Where(a => a.Id == articleId)
            .FirstOrDefaultAsync();
    }

    private async Task<bool> IsTutorialSlugUniqueAsync(string slug, Guid? parentId, Guid? excludeArticleId = null)
    {
        var query = _context.Articles
            .AsNoTracking()
            .Where(a => a.Type == ArticleType.Tutorial && a.WorldId == Guid.Empty && a.Slug == slug);

        if (parentId.HasValue)
        {
            query = query.Where(a => a.ParentId == parentId.Value);
        }
        else
        {
            query = query.Where(a => a.ParentId == null);
        }

        if (excludeArticleId.HasValue)
        {
            query = query.Where(a => a.Id != excludeArticleId.Value);
        }

        return !await query.AnyAsync();
    }

    private async Task<string> GenerateTutorialSlugAsync(string title, Guid? parentId, Guid? excludeArticleId = null)
    {
        var baseSlug = SlugGenerator.GenerateSlug(title);

        var query = _context.Articles
            .AsNoTracking()
            .Where(a => a.Type == ArticleType.Tutorial && a.WorldId == Guid.Empty);

        if (parentId.HasValue)
        {
            query = query.Where(a => a.ParentId == parentId.Value);
        }
        else
        {
            query = query.Where(a => a.ParentId == null);
        }

        if (excludeArticleId.HasValue)
        {
            query = query.Where(a => a.Id != excludeArticleId.Value);
        }

        var existingSlugs = await query
            .Select(a => a.Slug)
            .ToHashSetAsync();

        return SlugGenerator.GenerateUniqueSlug(baseSlug, existingSlugs);
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

        // Delete inline images associated with this article
        await _worldDocumentService.DeleteArticleImagesAsync(articleId);

        // Delete the article itself
        var article = await _context.Articles.FindAsync(articleId);
        if (article != null)
        {
            _context.Articles.Remove(article);
        }

        await _context.SaveChangesAsync();
    }

    #endregion
}
