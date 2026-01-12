using Chronicis.Api.Data;
using Chronicis.Api.Infrastructure;
using Chronicis.Api.Services;
using Chronicis.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Chronicis.Api.Controllers;

/// <summary>
/// API endpoints for Wiki Link operations.
/// </summary>
[ApiController]
[Route("links")]
[Authorize]
public class LinksController : ControllerBase
{
    private readonly ChronicisDbContext _context;
    private readonly IAutoLinkService _autoLinkService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<LinksController> _logger;

    public LinksController(
        ChronicisDbContext context,
        IAutoLinkService autoLinkService,
        ICurrentUserService currentUserService,
        ILogger<LinksController> logger)
    {
        _context = context;
        _autoLinkService = autoLinkService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    /// <summary>
    /// GET /api/links/suggestions?worldId={worldId}&query={query}
    /// Gets link suggestions for autocomplete based on a search query.
    /// </summary>
    [HttpGet("suggestions")]
    public async Task<ActionResult<List<LinkSuggestionDto>>> GetSuggestions(
        [FromQuery] Guid worldId,
        [FromQuery] string query)
    {
        var user = await _currentUserService.GetRequiredUserAsync();
        
        if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
        {
            return Ok(new List<LinkSuggestionDto>());
        }

        _logger.LogInformation("Getting link suggestions for query '{Query}' in world {WorldId}", query, worldId);

        // Verify user has access to the world
        var hasAccess = await _context.WorldMembers
            .AnyAsync(wm => wm.WorldId == worldId && wm.UserId == user.Id);

        if (!hasAccess)
        {
            return Forbid();
        }

        var normalizedQuery = query.ToLowerInvariant();

        // Search articles by title match
        var suggestions = await _context.Articles
            .Where(a => a.WorldId == worldId)
            .Where(a => a.Title != null && a.Title.ToLower().Contains(normalizedQuery))
            .OrderBy(a => a.Title)
            .Take(20)
            .Select(a => new LinkSuggestionDto
            {
                ArticleId = a.Id,
                Title = a.Title ?? "Untitled",
                Slug = a.Slug,
                ArticleType = a.Type,
                DisplayPath = "" // Will be populated below
            })
            .ToListAsync();

        // Build display paths for each suggestion
        foreach (var suggestion in suggestions)
        {
            suggestion.DisplayPath = await BuildDisplayPathAsync(suggestion.ArticleId);
        }

        return Ok(suggestions);
    }

    /// <summary>
    /// GET /api/links/backlinks/{articleId}
    /// Gets all articles that link to the specified article.
    /// </summary>
    [HttpGet("backlinks/{articleId:guid}")]
    public async Task<ActionResult<List<BacklinkDto>>> GetBacklinks(Guid articleId)
    {
        var user = await _currentUserService.GetRequiredUserAsync();
        _logger.LogInformation("Getting backlinks for article {ArticleId}", articleId);

        // Verify article exists and user has access
        var article = await _context.Articles
            .Where(a => a.Id == articleId)
            .Where(a => a.World != null && a.World.Members.Any(m => m.UserId == user.Id))
            .FirstOrDefaultAsync();

        if (article == null)
        {
            return NotFound(new { error = "Article not found or access denied" });
        }

        // Get all articles that link TO this article
        var backlinks = await _context.ArticleLinks
            .Where(l => l.TargetArticleId == articleId)
            .Select(l => new BacklinkDto
            {
                ArticleId = l.SourceArticleId,
                Title = l.SourceArticle.Title ?? "Untitled",
                Slug = l.SourceArticle.Slug,
                Snippet = l.DisplayText,
                DisplayPath = "" // Will be populated below
            })
            .Distinct()
            .ToListAsync();

        // Build display paths
        foreach (var backlink in backlinks)
        {
            backlink.DisplayPath = await BuildDisplayPathAsync(backlink.ArticleId);
        }

        return Ok(backlinks);
    }

    /// <summary>
    /// GET /api/links/outgoing/{articleId}
    /// Gets all articles that this article links to.
    /// </summary>
    [HttpGet("outgoing/{articleId:guid}")]
    public async Task<ActionResult<List<BacklinkDto>>> GetOutgoingLinks(Guid articleId)
    {
        var user = await _currentUserService.GetRequiredUserAsync();
        _logger.LogInformation("Getting outgoing links for article {ArticleId}", articleId);

        // Verify article exists and user has access
        var article = await _context.Articles
            .Where(a => a.Id == articleId)
            .Where(a => a.World != null && a.World.Members.Any(m => m.UserId == user.Id))
            .FirstOrDefaultAsync();

        if (article == null)
        {
            return NotFound(new { error = "Article not found or access denied" });
        }

        // Get all articles that this article links TO
        var outgoingLinks = await _context.ArticleLinks
            .Where(l => l.SourceArticleId == articleId)
            .Select(l => new BacklinkDto
            {
                ArticleId = l.TargetArticleId,
                Title = l.TargetArticle.Title ?? "Untitled",
                Slug = l.TargetArticle.Slug,
                Snippet = l.DisplayText,
                DisplayPath = "" // Will be populated below
            })
            .Distinct()
            .ToListAsync();

        // Build display paths
        foreach (var link in outgoingLinks)
        {
            link.DisplayPath = await BuildDisplayPathAsync(link.ArticleId);
        }

        return Ok(outgoingLinks);
    }

    /// <summary>
    /// POST /api/links/resolve
    /// Resolves multiple article IDs to check if they exist (for broken link detection).
    /// </summary>
    [HttpPost("resolve")]
    public async Task<ActionResult<LinkResolutionResponseDto>> ResolveLinks([FromBody] LinkResolutionRequestDto request)
    {
        var user = await _currentUserService.GetRequiredUserAsync();

        if (request?.ArticleIds == null || !request.ArticleIds.Any())
        {
            return Ok(new LinkResolutionResponseDto { Articles = new Dictionary<Guid, ResolvedLinkDto>() });
        }

        _logger.LogInformation("Resolving {Count} article links", request.ArticleIds.Count);

        // Get all requested articles that the user has access to
        var articles = await _context.Articles
            .Where(a => request.ArticleIds.Contains(a.Id))
            .Where(a => a.World != null && a.World.Members.Any(m => m.UserId == user.Id))
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
    /// POST /api/links/auto-link/{articleId}
    /// Scans article content and returns modified content with wiki links auto-inserted.
    /// </summary>
    [HttpPost("auto-link/{articleId:guid}")]
    public async Task<ActionResult<AutoLinkResponseDto>> AutoLink(Guid articleId, [FromBody] AutoLinkRequestDto request)
    {
        var user = await _currentUserService.GetRequiredUserAsync();

        if (request == null || string.IsNullOrEmpty(request.Body))
        {
            return BadRequest(new { error = "Body content is required" });
        }

        _logger.LogInformation("Auto-linking article {ArticleId}", articleId);

        // Get article and verify access
        var article = await _context.Articles
            .Where(a => a.Id == articleId)
            .Where(a => a.World != null && a.World.Members.Any(m => m.UserId == user.Id))
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

        var result = await _autoLinkService.FindAndInsertLinksAsync(
            articleId, 
            article.WorldId.Value, 
            request.Body, 
            user.Id);

        return Ok(result);
    }

    /// <summary>
    /// Builds a display path for an article (stripping the first level).
    /// </summary>
    private async Task<string> BuildDisplayPathAsync(Guid articleId)
    {
        var pathParts = new List<string>();
        var currentId = articleId;
        var visited = new HashSet<Guid>();

        // Walk up the tree
        while (currentId != Guid.Empty && !visited.Contains(currentId))
        {
            visited.Add(currentId);

            var article = await _context.Articles
                .Where(a => a.Id == currentId)
                .Select(a => new { a.Title, a.ParentId })
                .FirstOrDefaultAsync();

            if (article == null)
                break;

            pathParts.Insert(0, article.Title ?? "Untitled");
            currentId = article.ParentId ?? Guid.Empty;
        }

        // Strip the first level (world root) if there are multiple levels
        if (pathParts.Count > 1)
        {
            pathParts.RemoveAt(0);
        }

        return string.Join(" / ", pathParts);
    }
}
