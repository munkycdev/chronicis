using System.Diagnostics.CodeAnalysis;

namespace Chronicis.Shared.Models;

/// <summary>
/// Represents a Campaign - a gaming group's collaborative space within a World.
/// Multiple users collaborate within a Campaign with role-based access.
/// Sequential campaigns within a World share resources (Wiki, Characters).
/// </summary>
[ExcludeFromCodeCoverage]
public class Campaign
{
    /// <summary>
    /// Unique identifier for the campaign.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The world this campaign belongs to.
    /// </summary>
    public Guid WorldId { get; set; }

    /// <summary>
    /// Display name of the campaign (e.g., "Dragon Heist", "Curse of Strahd").
    /// Max 200 characters.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optional description of the campaign.
    /// Max 1000 characters.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Private notes for the campaign, visible only to the world owner and GMs.
    /// </summary>
    public string? PrivateNotes { get; set; }

    /// <summary>
    /// The user who owns this campaign (the DM).
    /// </summary>
    public Guid OwnerId { get; set; }

    /// <summary>
    /// When the campaign was created in Chronicis.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the campaign actually started (first session IRL).
    /// Null if not yet started.
    /// </summary>
    public DateTime? StartedAt { get; set; }

    /// <summary>
    /// When the campaign ended.
    /// Null if still ongoing.
    /// </summary>
    public DateTime? EndedAt { get; set; }

    /// <summary>
    /// Whether this is the active campaign for quick session creation.
    /// Only one campaign per world should be active at a time.
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
    /// AI-generated summary of the campaign.
    /// </summary>
    public string? AISummary { get; set; }

    /// <summary>
    /// When the AI summary was last generated.
    /// </summary>
    public DateTime? AISummaryGeneratedAt { get; set; }

    // ===== Navigation Properties =====

    /// <summary>
    /// The world this campaign belongs to.
    /// </summary>
    public World World { get; set; } = null!;

    /// <summary>
    /// The user who owns this campaign.
    /// </summary>
    public User Owner { get; set; } = null!;

    /// <summary>
    /// Template used for AI summary generation.
    /// </summary>
    public SummaryTemplate? SummaryTemplate { get; set; }

    /// <summary>
    /// All story arcs within this campaign.
    /// </summary>
    public ICollection<Arc> Arcs { get; set; } = new List<Arc>();

    /// <summary>
    /// All articles scoped to this campaign (Sessions, etc.).
    /// </summary>
    public ICollection<Article> Articles { get; set; } = new List<Article>();
}
