namespace Chronicis.Shared.Enums;

/// <summary>
/// Defines the scope of a WorldMap for grouping on the Maps Detail page.
/// </summary>
public enum MapScope
{
    /// <summary>
    /// Map has no campaign or arc associations.
    /// </summary>
    WorldScoped = 0,

    /// <summary>
    /// Map is associated with one or more campaigns.
    /// </summary>
    CampaignScoped = 1,

    /// <summary>
    /// Map is associated with one or more arcs.
    /// </summary>
    ArcScoped = 2,
}
