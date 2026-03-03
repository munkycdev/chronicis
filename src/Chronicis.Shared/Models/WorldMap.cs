using System.Diagnostics.CodeAnalysis;

namespace Chronicis.Shared.Models;

/// <summary>
/// Represents a map within a World, with an optional basemap image stored in blob storage.
/// Maps can be scoped to campaigns or arcs via join tables.
/// </summary>
[ExcludeFromCodeCoverage]
public class WorldMap
{
    /// <summary>
    /// Unique identifier for the map.
    /// </summary>
    public Guid WorldMapId { get; set; }

    /// <summary>
    /// The world this map belongs to.
    /// </summary>
    public Guid WorldId { get; set; }

    /// <summary>
    /// Display name of the map (e.g., "Faerûn", "Neverwinter Region").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Blob storage key for the basemap image. Null until the basemap has been uploaded.
    /// </summary>
    public string? BasemapBlobKey { get; set; }

    /// <summary>
    /// MIME type of the basemap image (e.g., image/png, image/jpeg).
    /// </summary>
    public string? BasemapContentType { get; set; }

    /// <summary>
    /// Original filename of the uploaded basemap image.
    /// </summary>
    public string? BasemapOriginalFilename { get; set; }

    /// <summary>
    /// When the map was created (UTC).
    /// </summary>
    public DateTime CreatedUtc { get; set; }

    /// <summary>
    /// When the map was last updated (UTC).
    /// </summary>
    public DateTime UpdatedUtc { get; set; }

    // ===== Navigation Properties =====

    /// <summary>
    /// The world this map belongs to.
    /// </summary>
    public World World { get; set; } = null!;

    /// <summary>
    /// All layers belonging to this map.
    /// </summary>
    public ICollection<MapLayer> Layers { get; set; } = new List<MapLayer>();

    /// <summary>
    /// Campaigns this map is associated with.
    /// </summary>
    public ICollection<WorldMapCampaign> WorldMapCampaigns { get; set; } = new List<WorldMapCampaign>();

    /// <summary>
    /// Arcs this map is associated with.
    /// </summary>
    public ICollection<WorldMapArc> WorldMapArcs { get; set; } = new List<WorldMapArc>();
}
