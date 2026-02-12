namespace Chronicis.Shared.DTOs;

/// <summary>
/// Represents a summary template available for selection
/// </summary>
public class SummaryTemplateDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsSystem { get; set; }
}

/// <summary>
/// Estimate for AI summary generation cost and scope
/// </summary>
public class SummaryEstimateDto
{
    public Guid EntityId { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public int SourceCount { get; set; }
    public int EstimatedInputTokens { get; set; }
    public int EstimatedOutputTokens { get; set; }
    public decimal EstimatedCostUSD { get; set; }
    public bool HasExistingSummary { get; set; }
    public DateTime? ExistingSummaryDate { get; set; }

    // Current configuration
    public Guid? TemplateId { get; set; }
    public string? TemplateName { get; set; }
    public string? CustomPrompt { get; set; }
    public bool IncludeWebSources { get; set; }
}

/// <summary>
/// Request to generate AI summary with configuration options
/// </summary>
public class GenerateSummaryRequestDto
{
    /// <summary>
    /// Template to use for generation. Null uses entity's saved template or default.
    /// </summary>
    public Guid? TemplateId { get; set; }

    /// <summary>
    /// Custom prompt that overrides the template. Takes precedence over TemplateId.
    /// </summary>
    public string? CustomPrompt { get; set; }

    /// <summary>
    /// Whether to include web search results in the summary.
    /// </summary>
    public bool IncludeWebSources { get; set; }

    /// <summary>
    /// Whether to save these settings to the entity for future use.
    /// </summary>
    public bool SaveConfiguration { get; set; }

    /// <summary>
    /// Maximum output tokens for the generated summary.
    /// </summary>
    public int MaxOutputTokens { get; set; } = 1500;
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

    /// <summary>
    /// Sources that were used to generate this summary
    /// </summary>
    public List<SummarySourceDto> Sources { get; set; } = new();
}

/// <summary>
/// Represents a source used in summary generation
/// </summary>
public class SummarySourceDto
{
    public string Type { get; set; } = string.Empty; // "Backlink", "Session", "Web"
    public string Title { get; set; } = string.Empty;
    public Guid? ArticleId { get; set; }
    public string? Url { get; set; } // For web sources
}

/// <summary>
/// Summary data for display (for Campaign/Arc)
/// </summary>
public class EntitySummaryDto
{
    public Guid EntityId { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public DateTime? GeneratedAt { get; set; }
    public bool HasSummary => !string.IsNullOrEmpty(Summary);

    // Current configuration
    public Guid? TemplateId { get; set; }
    public string? TemplateName { get; set; }
    public string? CustomPrompt { get; set; }
    public bool IncludeWebSources { get; set; }
}

/// <summary>
/// Summary data for Article display
/// </summary>
public class ArticleSummaryDto
{
    public Guid ArticleId { get; set; }
    public string? Summary { get; set; }
    public DateTime? GeneratedAt { get; set; }
    public bool HasSummary => !string.IsNullOrEmpty(Summary);

    // Configuration
    public Guid? TemplateId { get; set; }
    public string? TemplateName { get; set; }
    public string? CustomPrompt { get; set; }
    public bool IncludeWebSources { get; set; }
}

/// <summary>
/// Lightweight summary preview for tooltip display
/// </summary>
public class SummaryPreviewDto
{
    public string Title { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public string? TemplateName { get; set; }
    public bool HasSummary => !string.IsNullOrEmpty(Summary);
}
