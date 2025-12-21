namespace Chronicis.Shared.DTOs;

/// <summary>
/// Estimate for AI summary generation cost and scope
/// </summary>
public class SummaryEstimateDto
{
    public Guid ArticleId { get; set; }
    public string ArticleTitle { get; set; } = string.Empty;
    public int BacklinkCount { get; set; }
    public int EstimatedInputTokens { get; set; }
    public int EstimatedOutputTokens { get; set; }
    public decimal EstimatedCostUSD { get; set; }
    public bool HasExistingSummary { get; set; }
    public DateTime? ExistingSummaryDate { get; set; }
}

/// <summary>
/// Response from AI summary generation
/// </summary>
public class SummaryGenerationDto
{
    public bool Success { get; set; }
    public string Summary { get; set; } = string.Empty;
    public DateTime GeneratedDate { get; set; }
    public int TokensUsed { get; set; }
    public decimal ActualCostUSD { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Request to generate AI summary
/// </summary>
public class GenerateSummaryRequestDto
{
    public Guid ArticleId { get; set; }
    public int MaxOutputTokens { get; set; } = 1500;
}

/// <summary>
/// Summary data for display
/// </summary>
public class ArticleSummaryDto
{
    public Guid ArticleId { get; set; }
    public string? Summary { get; set; }
    public DateTime? GeneratedAt { get; set; }
    public bool HasSummary => !string.IsNullOrEmpty(Summary);
}
