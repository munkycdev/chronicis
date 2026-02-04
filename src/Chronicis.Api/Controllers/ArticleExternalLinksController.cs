using Chronicis.Api.Services.Articles;
using Chronicis.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Chronicis.Api.Controllers;

/// <summary>
/// API controller for article external links.
/// </summary>
[ApiController]
[Route("articles/{articleId:guid}/external-links")]
[Authorize]
public class ArticleExternalLinksController : ControllerBase
{
    private readonly IArticleExternalLinkService _externalLinkService;
    private readonly ILogger<ArticleExternalLinksController> _logger;

    public ArticleExternalLinksController(
        IArticleExternalLinkService externalLinkService,
        ILogger<ArticleExternalLinksController> logger)
    {
        _externalLinkService = externalLinkService;
        _logger = logger;
    }

    /// <summary>
    /// Gets all external links for a specific article.
    /// </summary>
    /// <param name="articleId">The article ID.</param>
    /// <returns>List of external links.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(List<ArticleExternalLinkDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ArticleExternalLinkDto>>> GetExternalLinks(Guid articleId)
    {
        try
        {
            var links = await _externalLinkService.GetExternalLinksForArticleAsync(articleId);
            return Ok(links);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving external links for article {ArticleId}", articleId);
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                "An error occurred while retrieving external links.");
        }
    }
}
