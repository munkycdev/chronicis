using Chronicis.Api.Services;
using Chronicis.Shared.DTOs;
using Chronicis.Shared.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace Chronicis.Api.Controllers;

/// <summary>
/// API endpoints for anonymous public access to worlds and articles.
/// These endpoints do NOT require authentication.
/// </summary>
[ApiController]
[Route("public")]
public class PublicController : ControllerBase
{
    private readonly IPublicWorldService _publicWorldService;
    private readonly ILogger<PublicController> _logger;

    public PublicController(
        IPublicWorldService publicWorldService,
        ILogger<PublicController> logger)
    {
        _publicWorldService = publicWorldService;
        _logger = logger;
    }

    /// <summary>
    /// GET /api/public/worlds/{publicSlug} - Get a public world by its public slug.
    /// </summary>
    [HttpGet("worlds/{publicSlug}")]
    public async Task<ActionResult<WorldDetailDto>> GetPublicWorld(string publicSlug)
    {
        if (string.IsNullOrWhiteSpace(publicSlug))
        {
            return BadRequest(new { error = "Public slug is required" });
        }

        _logger.LogDebugSanitized("Getting public world with slug '{PublicSlug}'", publicSlug);

        var world = await _publicWorldService.GetPublicWorldAsync(publicSlug);

        if (world == null)
        {
            return NotFound(new { error = "World not found or not public" });
        }

        return Ok(world);
    }

    /// <summary>
    /// GET /api/public/worlds/{publicSlug}/articles - Get the article tree for a public world.
    /// Returns a hierarchical tree structure with virtual groups (Campaigns, Player Characters, Wiki).
    /// Only returns articles with Public visibility.
    /// </summary>
    [HttpGet("worlds/{publicSlug}/articles")]
    public async Task<ActionResult<List<ArticleTreeDto>>> GetPublicArticleTree(string publicSlug)
    {
        if (string.IsNullOrWhiteSpace(publicSlug))
        {
            return BadRequest(new { error = "Public slug is required" });
        }

        _logger.LogDebugSanitized("Getting public article tree for world '{PublicSlug}'", publicSlug);

        var tree = await _publicWorldService.GetPublicArticleTreeAsync(publicSlug);

        // If tree is empty, check if world exists
        if (!tree.Any())
        {
            var world = await _publicWorldService.GetPublicWorldAsync(publicSlug);
            if (world == null)
            {
                return NotFound(new { error = "World not found or not public" });
            }
        }

        return Ok(tree);
    }

    /// <summary>
    /// GET /api/public/worlds/{publicSlug}/articles/{*articlePath} - Get a specific public article by path.
    /// </summary>
    [HttpGet("worlds/{publicSlug}/articles/{*articlePath}")]
    public async Task<ActionResult<ArticleDto>> GetPublicArticle(string publicSlug, string articlePath)
    {
        if (string.IsNullOrWhiteSpace(publicSlug))
        {
            return BadRequest(new { error = "Public slug is required" });
        }

        if (string.IsNullOrWhiteSpace(articlePath))
        {
            return BadRequest(new { error = "Article path is required" });
        }

        _logger.LogDebugSanitized("Getting public article '{ArticlePath}' in world '{PublicSlug}'", articlePath, publicSlug);

        var article = await _publicWorldService.GetPublicArticleAsync(publicSlug, articlePath);

        if (article == null)
        {
            return NotFound(new { error = "Article not found or not public" });
        }

        return Ok(article);
    }

    /// <summary>
    /// GET /api/public/worlds/{publicSlug}/articles/resolve/{articleId} - Resolve an article ID to its public URL path.
    /// </summary>
    [HttpGet("worlds/{publicSlug}/articles/resolve/{articleId:guid}")]
    public async Task<ActionResult<string>> ResolveArticlePath(string publicSlug, Guid articleId)
    {
        if (string.IsNullOrWhiteSpace(publicSlug))
        {
            return BadRequest(new { error = "Public slug is required" });
        }

        _logger.LogDebugSanitized("Resolving article path for {ArticleId} in world '{PublicSlug}'", articleId, publicSlug);

        var path = await _publicWorldService.GetPublicArticlePathAsync(publicSlug, articleId);

        if (path == null)
        {
            return NotFound(new { error = "Article not found or not public" });
        }

        return Ok(path);
    }

    /// <summary>
    /// GET /api/public/documents/{documentId} - Resolve a public inline image document to a fresh download URL.
    /// Only documents attached to public articles in public worlds are accessible.
    /// </summary>
    [HttpGet("documents/{documentId:guid}")]
    public async Task<IActionResult> GetPublicDocumentContent(Guid documentId)
    {
        var downloadUrl = await _publicWorldService.GetPublicDocumentDownloadUrlAsync(documentId);

        if (string.IsNullOrWhiteSpace(downloadUrl))
        {
            return NotFound(new { error = "Document not found or not publicly accessible" });
        }

        return Redirect(downloadUrl);
    }
}
