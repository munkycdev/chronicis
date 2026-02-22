using System.Diagnostics.CodeAnalysis;

namespace Chronicis.Shared.DTOs;

/// <summary>
/// Summary of a world as seen by a system administrator.
/// Includes aggregate counts across all campaigns.
/// </summary>
[ExcludeFromCodeCoverage]
public class AdminWorldSummaryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string OwnerName { get; set; } = string.Empty;
    public string OwnerEmail { get; set; } = string.Empty;
    public int CampaignCount { get; set; }
    public int ArcCount { get; set; }
    public int ArticleCount { get; set; }
    public DateTime CreatedAt { get; set; }
}
