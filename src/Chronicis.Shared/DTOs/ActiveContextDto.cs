namespace Chronicis.Shared.DTOs;

/// <summary>
/// Response DTO for the active campaign/arc context used for quick session creation.
/// </summary>
public class ActiveContextDto
{
    /// <summary>
    /// The world ID this context belongs to.
    /// </summary>
    public Guid? WorldId { get; set; }

    /// <summary>
    /// The explicitly active campaign ID (where IsActive = true).
    /// </summary>
    public Guid? CampaignId { get; set; }

    /// <summary>
    /// The active campaign name.
    /// </summary>
    public string? CampaignName { get; set; }

    /// <summary>
    /// The explicitly active arc ID (where IsActive = true).
    /// </summary>
    public Guid? ArcId { get; set; }

    /// <summary>
    /// The active arc name.
    /// </summary>
    public string? ArcName { get; set; }

    /// <summary>
    /// Whether an active context is available for quick session creation.
    /// True if both CampaignId and ArcId are set.
    /// </summary>
    public bool HasActiveContext => CampaignId.HasValue && ArcId.HasValue;
}
