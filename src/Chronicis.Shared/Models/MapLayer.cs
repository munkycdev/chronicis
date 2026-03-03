using System.Diagnostics.CodeAnalysis;

namespace Chronicis.Shared.Models;

/// <summary>
/// Represents a named layer within a WorldMap (e.g., "World", "Campaign", "Arc").
/// Layers are hidden in MVP and created automatically when a map is created.
/// </summary>
[ExcludeFromCodeCoverage]
public class MapLayer
{
    /// <summary>
    /// Unique identifier for the layer.
    /// </summary>
    public Guid MapLayerId { get; set; }

    /// <summary>
    /// The map this layer belongs to.
    /// </summary>
    public Guid WorldMapId { get; set; }

    /// <summary>
    /// Optional parent layer identifier for nested layers.
    /// Null for top-level layers.
    /// </summary>
    public Guid? ParentLayerId { get; set; }

    /// <summary>
    /// Display name of the layer (e.g., "World", "Campaign", "Arc").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Sort order for displaying layers within a map. Lower numbers appear first.
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// Whether this layer is currently enabled and visible.
    /// </summary>
    public bool IsEnabled { get; set; }

    // ===== Navigation Properties =====

    /// <summary>
    /// The map this layer belongs to.
    /// </summary>
    public WorldMap WorldMap { get; set; } = null!;

    /// <summary>
    /// The parent layer, if this is a nested layer.
    /// </summary>
    public MapLayer? Parent { get; set; }

    /// <summary>
    /// Child layers nested beneath this layer.
    /// </summary>
    public ICollection<MapLayer> Children { get; set; } = new List<MapLayer>();
}
