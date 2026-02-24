using Chronicis.Shared.DTOs;

namespace Chronicis.Api.Services;

/// <summary>
/// Service for AI-powered summary generation across entity types.
/// </summary>
public interface ISummaryService
{
    /// <summary>
    /// Get all available summary templates.
    /// </summary>
    Task<List<SummaryTemplateDto>> GetTemplatesAsync();

    /// <summary>
    /// Estimate the cost of generating a summary for an article.
    /// </summary>
    Task<SummaryEstimateDto> EstimateArticleSummaryAsync(Guid articleId);

    /// <summary>
    /// Generate a summary for an article.
    /// </summary>
    Task<SummaryGenerationDto> GenerateArticleSummaryAsync(Guid articleId, GenerateSummaryRequestDto? request = null);

    /// <summary>
    /// Get the current summary for an article.
    /// </summary>
    Task<ArticleSummaryDto?> GetArticleSummaryAsync(Guid articleId);

    /// <summary>
    /// Get a lightweight summary preview for tooltip display.
    /// </summary>
    Task<SummaryPreviewDto?> GetArticleSummaryPreviewAsync(Guid articleId);

    /// <summary>
    /// Clear the summary for an article.
    /// </summary>
    Task<bool> ClearArticleSummaryAsync(Guid articleId);

    /// <summary>
    /// Estimate the cost of generating a summary for a campaign.
    /// </summary>
    Task<SummaryEstimateDto> EstimateCampaignSummaryAsync(Guid campaignId);

    /// <summary>
    /// Generate a summary for a campaign (aggregates all public sessions).
    /// </summary>
    Task<SummaryGenerationDto> GenerateCampaignSummaryAsync(Guid campaignId, GenerateSummaryRequestDto? request = null);

    /// <summary>
    /// Get the current summary for a campaign.
    /// </summary>
    Task<EntitySummaryDto?> GetCampaignSummaryAsync(Guid campaignId);

    /// <summary>
    /// Clear the summary for a campaign.
    /// </summary>
    Task<bool> ClearCampaignSummaryAsync(Guid campaignId);

    /// <summary>
    /// Estimate the cost of generating a summary for an arc.
    /// </summary>
    Task<SummaryEstimateDto> EstimateArcSummaryAsync(Guid arcId);

    /// <summary>
    /// Generate a summary for an arc (aggregates all public sessions in the arc).
    /// </summary>
    Task<SummaryGenerationDto> GenerateArcSummaryAsync(Guid arcId, GenerateSummaryRequestDto? request = null);

    /// <summary>
    /// Get the current summary for an arc.
    /// </summary>
    Task<EntitySummaryDto?> GetArcSummaryAsync(Guid arcId);

    /// <summary>
    /// Clear the summary for an arc.
    /// </summary>
    Task<bool> ClearArcSummaryAsync(Guid arcId);

    /// <summary>
    /// Generate a session summary from pre-filtered public-safe source content.
    /// </summary>
    Task<SummaryGenerationDto> GenerateSessionSummaryFromSourcesAsync(
        string sessionName,
        string sourceContent,
        IReadOnlyList<SummarySourceDto> sources,
        int maxOutputTokens = 1500);
}
