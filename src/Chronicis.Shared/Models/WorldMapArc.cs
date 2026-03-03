using System.Diagnostics.CodeAnalysis;

namespace Chronicis.Shared.Models;

/// <summary>
/// Join table associating a WorldMap with an Arc.
/// Maps with arc rows are considered arc-scoped.
/// </summary>
[ExcludeFromCodeCoverage]
public class WorldMapArc
{
    /// <summary>
    /// The map in this association.
    /// </summary>
    public Guid WorldMapId { get; set; }

    /// <summary>
    /// The arc in this association.
    /// </summary>
    public Guid ArcId { get; set; }

    // ===== Navigation Properties =====

    /// <summary>
    /// The map in this association.
    /// </summary>
    public WorldMap WorldMap { get; set; } = null!;

    /// <summary>
    /// The arc in this association.
    /// </summary>
    public Arc Arc { get; set; } = null!;
}
