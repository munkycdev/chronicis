using Chronicis.Api.Services;
using Chronicis.Shared.DTOs;
using Chronicis.Shared.DTOs.Maps;
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

        _logger.LogTraceSanitized("Getting public world with slug '{PublicSlug}'", publicSlug);

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

        _logger.LogTraceSanitized("Getting public article tree for world '{PublicSlug}'", publicSlug);

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

        _logger.LogTraceSanitized("Getting public article '{ArticlePath}' in world '{PublicSlug}'", articlePath, publicSlug);

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

        _logger.LogTraceSanitized("Resolving article path for {ArticleId} in world '{PublicSlug}'", articleId, publicSlug);

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

    /// <summary>
    /// GET /api/public/worlds/{publicSlug}/maps/{mapId}/basemap - Get a public basemap read URL.
    /// </summary>
    [HttpGet("worlds/{publicSlug}/maps/{mapId:guid}/basemap")]
    public async Task<ActionResult<GetBasemapReadUrlResponseDto>> GetPublicMapBasemap(string publicSlug, Guid mapId)
    {
        if (string.IsNullOrWhiteSpace(publicSlug))
        {
            return BadRequest(new { error = "Public slug is required" });
        }

        var (basemap, error) = await _publicWorldService.GetPublicMapBasemapReadUrlAsync(publicSlug, mapId);
        if (basemap == null)
        {
            return NotFound(new { error = error ?? "Map not found or not public" });
        }

        return Ok(basemap);
    }

    /// <summary>
    /// GET /api/public/worlds/{publicSlug}/maps/{mapId}/layers - List public map layers.
    /// </summary>
    [HttpGet("worlds/{publicSlug}/maps/{mapId:guid}/layers")]
    public async Task<ActionResult<IEnumerable<MapLayerDto>>> GetPublicMapLayers(string publicSlug, Guid mapId)
    {
        if (string.IsNullOrWhiteSpace(publicSlug))
        {
            return BadRequest(new { error = "Public slug is required" });
        }

        var layers = await _publicWorldService.GetPublicMapLayersAsync(publicSlug, mapId);
        if (layers == null)
        {
            return NotFound(new { error = "Map not found or not public" });
        }

        return Ok(layers);
    }

    /// <summary>
    /// GET /api/public/worlds/{publicSlug}/maps/{mapId}/pins - List public map pins.
    /// </summary>
    [HttpGet("worlds/{publicSlug}/maps/{mapId:guid}/pins")]
    public async Task<ActionResult<IEnumerable<MapPinResponseDto>>> GetPublicMapPins(string publicSlug, Guid mapId)
    {
        if (string.IsNullOrWhiteSpace(publicSlug))
        {
            return BadRequest(new { error = "Public slug is required" });
        }

        var pins = await _publicWorldService.GetPublicMapPinsAsync(publicSlug, mapId);
        if (pins == null)
        {
            return NotFound(new { error = "Map not found or not public" });
        }

        return Ok(pins);
    }

    /// <summary>
    /// GET /api/public/worlds/{publicSlug}/maps/{mapId}/features - List public map features.
    /// </summary>
    [HttpGet("worlds/{publicSlug}/maps/{mapId:guid}/features")]
    public async Task<ActionResult<IEnumerable<MapFeatureDto>>> GetPublicMapFeatures(string publicSlug, Guid mapId)
    {
        if (string.IsNullOrWhiteSpace(publicSlug))
        {
            return BadRequest(new { error = "Public slug is required" });
        }

        var features = await _publicWorldService.GetPublicMapFeaturesAsync(publicSlug, mapId);
        if (features == null)
        {
            return NotFound(new { error = "Map not found or not public" });
        }

        return Ok(features);
    }
}
