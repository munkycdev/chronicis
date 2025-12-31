namespace Chronicis.Shared.DTOs;

/// <summary>
/// Aggregated dashboard data for the current user.
/// </summary>
public class DashboardDto
{
    /// <summary>
    /// User's display name for personalized greeting.
    /// </summary>
    public string UserDisplayName { get; set; } = string.Empty;
    
    /// <summary>
    /// All worlds the user has access to.
    /// </summary>
    public List<DashboardWorldDto> Worlds { get; set; } = new();
    
    /// <summary>
    /// Characters claimed by the user across all worlds.
    /// </summary>
    public List<ClaimedCharacterDto> ClaimedCharacters { get; set; } = new();
    
    /// <summary>
    /// Contextual prompts/suggestions for the user.
    /// </summary>
    public List<PromptDto> Prompts { get; set; } = new();
}

/// <summary>
/// World information for dashboard display.
/// </summary>
public class DashboardWorldDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid? WorldRootArticleId { get; set; }
    
    /// <summary>
    /// Total article count in this world.
    /// </summary>
    public int ArticleCount { get; set; }
    
    /// <summary>
    /// Campaigns in this world.
    /// </summary>
    public List<DashboardCampaignDto> Campaigns { get; set; } = new();
    
    /// <summary>
    /// Characters the current user has claimed in this world.
    /// </summary>
    public List<DashboardCharacterDto> MyCharacters { get; set; } = new();
}

/// <summary>
/// Campaign information for dashboard display.
/// </summary>
public class DashboardCampaignDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public bool IsActive { get; set; }
    public int SessionCount { get; set; }
    public int ArcCount { get; set; }
    
    /// <summary>
    /// The current/most recent arc.
    /// </summary>
    public DashboardArcDto? CurrentArc { get; set; }
}

/// <summary>
/// Arc information for dashboard display.
/// </summary>
public class DashboardArcDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int SessionCount { get; set; }
    public DateTime? LatestSessionDate { get; set; }
}

/// <summary>
/// Character information for dashboard display.
/// </summary>
public class DashboardCharacterDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? IconEmoji { get; set; }
    public DateTime? ModifiedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
