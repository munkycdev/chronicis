namespace Chronicis.Shared.DTOs;

/// <summary>
/// Basic world information for lists
/// </summary>
public class WorldDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid OwnerId { get; set; }
    public string OwnerName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int CampaignCount { get; set; }
    
    /// <summary>
    /// The ID of the WorldRoot article (top-level article for this world)
    /// </summary>
    public Guid? WorldRootArticleId { get; set; }
}

/// <summary>
/// Detailed world information including campaigns
/// </summary>
public class WorldDetailDto : WorldDto
{
    public List<CampaignDto> Campaigns { get; set; } = new();
}

/// <summary>
/// DTO for creating a new world
/// </summary>
public class WorldCreateDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

/// <summary>
/// DTO for updating a world
/// </summary>
public class WorldUpdateDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}
