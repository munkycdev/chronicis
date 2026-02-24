using System.Diagnostics.CodeAnalysis;

namespace Chronicis.Shared.Models;

/// <summary>
/// Represents a story arc within a Campaign.
/// Arcs are the organizational containers for Sessions (e.g., "Act 1", "The Beginning").
/// Each Campaign must have at least one Arc to contain Sessions.
/// </summary>
[ExcludeFromCodeCoverage]
public class Arc
{
    /// <summary>
    /// Unique identifier for the arc.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The campaign this arc belongs to.
    /// </summary>
    public Guid CampaignId { get; set; }

    /// <summary>
    /// Display name of the arc (e.g., "Act 1", "The Dragon's Lair", "Prologue").
    /// Max 200 characters.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optional description of the arc's story or themes.
    /// Max 1000 characters.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Sort order for displaying arcs within a campaign.
    /// Lower numbers appear first.
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// When the arc was created in Chronicis.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// User who created this arc.
    /// </summary>
    public Guid CreatedBy { get; set; }

    /// <summary>
    /// Whether this is the active arc for quick session creation.
    /// Only one arc per campaign should be active at a time.
    /// </summary>
    public bool IsActive { get; set; }

    // ===== AI Features =====

    /// <summary>
    /// Template to use for AI summary generation. Null uses default behavior.
    /// </summary>
    public Guid? SummaryTemplateId { get; set; }

    /// <summary>
    /// Custom prompt that overrides the template when generating summaries.
    /// </summary>
    public string? SummaryCustomPrompt { get; set; }

    /// <summary>
    /// Whether to include web search results when generating summaries.
    /// </summary>
    public bool SummaryIncludeWebSources { get; set; }

    /// <summary>
    /// AI-generated summary of the arc.
    /// </summary>
    public string? AISummary { get; set; }

    /// <summary>
    /// When the AI summary was last generated.
    /// </summary>
    public DateTime? AISummaryGeneratedAt { get; set; }

    // ===== Navigation Properties =====

    /// <summary>
    /// The campaign this arc belongs to.
    /// </summary>
    public Campaign Campaign { get; set; } = null!;

    /// <summary>
    /// Template used for AI summary generation.
    /// </summary>
    public SummaryTemplate? SummaryTemplate { get; set; }

    /// <summary>
    /// User who created this arc.
    /// </summary>
    public User Creator { get; set; } = null!;

    /// <summary>
    /// Legacy session articles within this arc (ArticleType.Session).
    /// Retained for backward compatibility during migration to Session entities.
    /// </summary>
    public ICollection<Article> SessionArticles { get; set; } = new List<Article>();

    /// <summary>
    /// First-class Session entities within this arc.
    /// </summary>
    public ICollection<Session> SessionEntities { get; set; } = new List<Session>();
}
