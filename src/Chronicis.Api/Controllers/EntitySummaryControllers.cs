using Chronicis.Api.Infrastructure;
using Chronicis.Api.Services;
using Chronicis.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Chronicis.Api.Controllers;

/// <summary>
/// API endpoints for Article Summary operations.
/// These are nested under /api/articles/{id}/summary/* as expected by the client.
/// </summary>
[ApiController]
[Route("articles/{articleId:guid}/summary")]
[Authorize]
public class ArticleSummaryController : ControllerBase
{
    private readonly ISummaryService _summaryService;
    private readonly ISummaryAccessService _summaryAccessService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<ArticleSummaryController> _logger;

    public ArticleSummaryController(
        ISummaryService summaryService,
        ISummaryAccessService summaryAccessService,
        ICurrentUserService currentUserService,
        ILogger<ArticleSummaryController> logger)
    {
        _summaryService = summaryService;
        _summaryAccessService = summaryAccessService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    /// <summary>
    /// GET /api/articles/{articleId}/summary - Get the current summary for an article.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ArticleSummaryDto>> GetSummary(Guid articleId)
    {
        var user = await _currentUserService.GetRequiredUserAsync();

        // Verify user has access
        if (!await HasAccessAsync(articleId, user.Id))
        {
            return NotFound(new { error = "Article not found or access denied" });
        }

        _logger.LogDebug("Getting summary for article {ArticleId}", articleId);

        var summary = await _summaryService.GetArticleSummaryAsync(articleId);

        if (summary == null)
        {
            return NoContent();
        }

        return Ok(summary);
    }

    /// <summary>
    /// GET /api/articles/{articleId}/summary/preview - Get a lightweight summary preview for tooltip display.
    /// </summary>
    [HttpGet("preview")]
    public async Task<ActionResult<SummaryPreviewDto>> GetSummaryPreview(Guid articleId)
    {
        var user = await _currentUserService.GetRequiredUserAsync();

        // Verify user has access
        if (!await HasAccessAsync(articleId, user.Id))
        {
            return NotFound(new { error = "Article not found or access denied" });
        }

        _logger.LogDebug("Getting summary preview for article {ArticleId}", articleId);

        var preview = await _summaryService.GetArticleSummaryPreviewAsync(articleId);

        if (preview == null)
        {
            return NoContent();
        }

        return Ok(preview);
    }

    /// <summary>
    /// GET /api/articles/{articleId}/summary/estimate - Estimate the cost of generating a summary.
    /// </summary>
    [HttpGet("estimate")]
    public async Task<ActionResult<SummaryEstimateDto>> GetEstimate(Guid articleId)
    {
        var user = await _currentUserService.GetRequiredUserAsync();

        // Verify user has access
        if (!await HasAccessAsync(articleId, user.Id))
        {
            return NotFound(new { error = "Article not found or access denied" });
        }

        _logger.LogDebug("Getting summary estimate for article {ArticleId}", articleId);

        var estimate = await _summaryService.EstimateArticleSummaryAsync(articleId);
        return Ok(estimate);
    }

    /// <summary>
    /// POST /api/articles/{articleId}/summary/generate - Generate a summary for an article.
    /// </summary>
    [HttpPost("generate")]
    public async Task<ActionResult<SummaryGenerationDto>> GenerateSummary(
        Guid articleId,
        [FromBody] GenerateSummaryRequestDto? request)
    {
        var user = await _currentUserService.GetRequiredUserAsync();

        // Verify user has access
        if (!await HasAccessAsync(articleId, user.Id))
        {
            return NotFound(new { error = "Article not found or access denied" });
        }

        _logger.LogDebug("Generating summary for article {ArticleId}", articleId);

        var result = await _summaryService.GenerateArticleSummaryAsync(articleId, request);

        if (!result.Success)
        {
            return BadRequest(new { error = result.ErrorMessage });
        }

        return Ok(result);
    }

    /// <summary>
    /// DELETE /api/articles/{articleId}/summary - Clear the summary for an article.
    /// </summary>
    [HttpDelete]
    public async Task<IActionResult> ClearSummary(Guid articleId)
    {
        var user = await _currentUserService.GetRequiredUserAsync();

        // Verify user has access
        if (!await HasAccessAsync(articleId, user.Id))
        {
            return NotFound(new { error = "Article not found or access denied" });
        }

        _logger.LogDebug("Clearing summary for article {ArticleId}", articleId);

        var success = await _summaryService.ClearArticleSummaryAsync(articleId);

        if (!success)
        {
            return NotFound(new { error = "Article not found" });
        }

        return NoContent();
    }

    private async Task<bool> HasAccessAsync(Guid articleId, Guid userId)
    {
        return await _summaryAccessService.CanAccessArticleAsync(articleId, userId);
    }
}

/// <summary>
/// API endpoints for Campaign Summary operations.
/// </summary>
[ApiController]
[Route("campaigns/{campaignId:guid}/summary")]
[Authorize]
public class CampaignSummaryController : ControllerBase
{
    private readonly ISummaryService _summaryService;
    private readonly ISummaryAccessService _summaryAccessService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<CampaignSummaryController> _logger;

    public CampaignSummaryController(
        ISummaryService summaryService,
        ISummaryAccessService summaryAccessService,
        ICurrentUserService currentUserService,
        ILogger<CampaignSummaryController> logger)
    {
        _summaryService = summaryService;
        _summaryAccessService = summaryAccessService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    /// <summary>
    /// GET /api/campaigns/{campaignId}/summary - Get the current summary for a campaign.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<EntitySummaryDto>> GetSummary(Guid campaignId)
    {
        var user = await _currentUserService.GetRequiredUserAsync();

        if (!await HasAccessAsync(campaignId, user.Id))
        {
            return NotFound(new { error = "Campaign not found or access denied" });
        }

        _logger.LogDebug("Getting summary for campaign {CampaignId}", campaignId);

        var summary = await _summaryService.GetCampaignSummaryAsync(campaignId);

        if (summary == null)
        {
            return NoContent();
        }

        return Ok(summary);
    }

    /// <summary>
    /// GET /api/campaigns/{campaignId}/summary/estimate - Estimate the cost of generating a summary.
    /// </summary>
    [HttpGet("estimate")]
    public async Task<ActionResult<SummaryEstimateDto>> GetEstimate(Guid campaignId)
    {
        var user = await _currentUserService.GetRequiredUserAsync();

        if (!await HasAccessAsync(campaignId, user.Id))
        {
            return NotFound(new { error = "Campaign not found or access denied" });
        }

        _logger.LogDebug("Getting summary estimate for campaign {CampaignId}", campaignId);

        var estimate = await _summaryService.EstimateCampaignSummaryAsync(campaignId);
        return Ok(estimate);
    }

    /// <summary>
    /// POST /api/campaigns/{campaignId}/summary/generate - Generate a summary for a campaign.
    /// </summary>
    [HttpPost("generate")]
    public async Task<ActionResult<SummaryGenerationDto>> GenerateSummary(
        Guid campaignId,
        [FromBody] GenerateSummaryRequestDto? request)
    {
        var user = await _currentUserService.GetRequiredUserAsync();

        if (!await HasAccessAsync(campaignId, user.Id))
        {
            return NotFound(new { error = "Campaign not found or access denied" });
        }

        _logger.LogDebug("Generating summary for campaign {CampaignId}", campaignId);

        var result = await _summaryService.GenerateCampaignSummaryAsync(campaignId, request);

        if (!result.Success)
        {
            return BadRequest(new { error = result.ErrorMessage });
        }

        return Ok(result);
    }

    /// <summary>
    /// DELETE /api/campaigns/{campaignId}/summary - Clear the summary for a campaign.
    /// </summary>
    [HttpDelete]
    public async Task<IActionResult> ClearSummary(Guid campaignId)
    {
        var user = await _currentUserService.GetRequiredUserAsync();

        if (!await HasAccessAsync(campaignId, user.Id))
        {
            return NotFound(new { error = "Campaign not found or access denied" });
        }

        _logger.LogDebug("Clearing summary for campaign {CampaignId}", campaignId);

        var success = await _summaryService.ClearCampaignSummaryAsync(campaignId);

        if (!success)
        {
            return NotFound(new { error = "Campaign not found" });
        }

        return NoContent();
    }

    private async Task<bool> HasAccessAsync(Guid campaignId, Guid userId)
    {
        return await _summaryAccessService.CanAccessCampaignAsync(campaignId, userId);
    }
}

/// <summary>
/// API endpoints for Arc Summary operations.
/// </summary>
[ApiController]
[Route("arcs/{arcId:guid}/summary")]
[Authorize]
public class ArcSummaryController : ControllerBase
{
    private readonly ISummaryService _summaryService;
    private readonly ISummaryAccessService _summaryAccessService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<ArcSummaryController> _logger;

    public ArcSummaryController(
        ISummaryService summaryService,
        ISummaryAccessService summaryAccessService,
        ICurrentUserService currentUserService,
        ILogger<ArcSummaryController> logger)
    {
        _summaryService = summaryService;
        _summaryAccessService = summaryAccessService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    /// <summary>
    /// GET /api/arcs/{arcId}/summary - Get the current summary for an arc.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<EntitySummaryDto>> GetSummary(Guid arcId)
    {
        var user = await _currentUserService.GetRequiredUserAsync();

        if (!await HasAccessAsync(arcId, user.Id))
        {
            return NotFound(new { error = "Arc not found or access denied" });
        }

        _logger.LogDebug("Getting summary for arc {ArcId}", arcId);

        var summary = await _summaryService.GetArcSummaryAsync(arcId);

        if (summary == null)
        {
            return NoContent();
        }

        return Ok(summary);
    }

    /// <summary>
    /// GET /api/arcs/{arcId}/summary/estimate - Estimate the cost of generating a summary.
    /// </summary>
    [HttpGet("estimate")]
    public async Task<ActionResult<SummaryEstimateDto>> GetEstimate(Guid arcId)
    {
        var user = await _currentUserService.GetRequiredUserAsync();

        if (!await HasAccessAsync(arcId, user.Id))
        {
            return NotFound(new { error = "Arc not found or access denied" });
        }

        _logger.LogDebug("Getting summary estimate for arc {ArcId}", arcId);

        var estimate = await _summaryService.EstimateArcSummaryAsync(arcId);
        return Ok(estimate);
    }

    /// <summary>
    /// POST /api/arcs/{arcId}/summary/generate - Generate a summary for an arc.
    /// </summary>
    [HttpPost("generate")]
    public async Task<ActionResult<SummaryGenerationDto>> GenerateSummary(
        Guid arcId,
        [FromBody] GenerateSummaryRequestDto? request)
    {
        var user = await _currentUserService.GetRequiredUserAsync();

        if (!await HasAccessAsync(arcId, user.Id))
        {
            return NotFound(new { error = "Arc not found or access denied" });
        }

        _logger.LogDebug("Generating summary for arc {ArcId}", arcId);

        var result = await _summaryService.GenerateArcSummaryAsync(arcId, request);

        if (!result.Success)
        {
            return BadRequest(new { error = result.ErrorMessage });
        }

        return Ok(result);
    }

    /// <summary>
    /// DELETE /api/arcs/{arcId}/summary - Clear the summary for an arc.
    /// </summary>
    [HttpDelete]
    public async Task<IActionResult> ClearSummary(Guid arcId)
    {
        var user = await _currentUserService.GetRequiredUserAsync();

        if (!await HasAccessAsync(arcId, user.Id))
        {
            return NotFound(new { error = "Arc not found or access denied" });
        }

        _logger.LogDebug("Clearing summary for arc {ArcId}", arcId);

        var success = await _summaryService.ClearArcSummaryAsync(arcId);

        if (!success)
        {
            return NotFound(new { error = "Arc not found" });
        }

        return NoContent();
    }

    private async Task<bool> HasAccessAsync(Guid arcId, Guid userId)
    {
        return await _summaryAccessService.CanAccessArcAsync(arcId, userId);
    }
}
