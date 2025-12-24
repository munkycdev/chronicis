namespace Chronicis.Shared.Models;

/// <summary>
/// Represents a story arc within a Campaign.
/// Arcs are the organizational containers for Sessions (e.g., "Act 1", "The Beginning").
/// Each Campaign must have at least one Arc to contain Sessions.
/// </summary>
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

    // ===== Navigation Properties =====

    /// <summary>
    /// The campaign this arc belongs to.
    /// </summary>
    public Campaign Campaign { get; set; } = null!;

    /// <summary>
    /// User who created this arc.
    /// </summary>
    public User Creator { get; set; } = null!;

    /// <summary>
    /// All session articles within this arc.
    /// </summary>
    public ICollection<Article> Sessions { get; set; } = new List<Article>();
}
