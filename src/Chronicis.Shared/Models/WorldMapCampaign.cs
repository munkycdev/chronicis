using System.Diagnostics.CodeAnalysis;

namespace Chronicis.Shared.Models;

/// <summary>
/// Join table associating a WorldMap with a Campaign.
/// Maps with campaign rows are considered campaign-scoped.
/// </summary>
[ExcludeFromCodeCoverage]
public class WorldMapCampaign
{
    /// <summary>
    /// The map in this association.
    /// </summary>
    public Guid WorldMapId { get; set; }

    /// <summary>
    /// The campaign in this association.
    /// </summary>
    public Guid CampaignId { get; set; }

    // ===== Navigation Properties =====

    /// <summary>
    /// The map in this association.
    /// </summary>
    public WorldMap WorldMap { get; set; } = null!;

    /// <summary>
    /// The campaign in this association.
    /// </summary>
    public Campaign Campaign { get; set; } = null!;
}
