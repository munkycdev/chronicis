using System.Diagnostics.CodeAnalysis;
using Chronicis.Shared.Enums;

namespace Chronicis.Shared.DTOs.Maps;

/// <summary>
/// Request DTO for creating a map feature.
/// </summary>
[ExcludeFromCodeCoverage]
public class MapFeatureCreateDto
{
    public MapFeatureType FeatureType { get; set; }
    public Guid LayerId { get; set; }
    public string? Name { get; set; }
    public string? Color { get; set; }
    public Guid? LinkedArticleId { get; set; }
    public MapFeaturePointDto? Point { get; set; }
    public PolygonGeometryDto? Polygon { get; set; }
}

/// <summary>
/// Request DTO for replacing a map feature.
/// </summary>
[ExcludeFromCodeCoverage]
public class MapFeatureUpdateDto
{
    public string? Name { get; set; }
    public string? Color { get; set; }
    public Guid LayerId { get; set; }
    public Guid? LinkedArticleId { get; set; }
    public MapFeaturePointDto? Point { get; set; }
    public PolygonGeometryDto? Polygon { get; set; }
}

/// <summary>
/// Generic map feature response DTO.
/// </summary>
[ExcludeFromCodeCoverage]
public class MapFeatureDto
{
    public Guid FeatureId { get; set; }
    public Guid MapId { get; set; }
    public Guid LayerId { get; set; }
    public MapFeatureType FeatureType { get; set; }
    public string? Name { get; set; }
    public string? Color { get; set; }
    public Guid? LinkedArticleId { get; set; }
    public LinkedArticleSummaryDto? LinkedArticle { get; set; }
    public MapFeaturePointDto? Point { get; set; }
    public PolygonGeometryDto? Polygon { get; set; }
    public MapFeatureGeometryReferenceDto? Geometry { get; set; }
}

/// <summary>
/// Minimal map feature suggestion payload for editor autocomplete.
/// </summary>
[ExcludeFromCodeCoverage]
public class MapFeatureAutocompleteDto
{
    public Guid MapFeatureId { get; set; }
    public Guid MapId { get; set; }
    public string MapName { get; set; } = string.Empty;
    public string DisplayText { get; set; } = string.Empty;
    public string? FeatureName { get; set; }
    public string? LinkedArticleTitle { get; set; }
}

/// <summary>
/// Normalized point geometry for point features.
/// </summary>
[ExcludeFromCodeCoverage]
public class MapFeaturePointDto
{
    public float X { get; set; }
    public float Y { get; set; }
}

/// <summary>
/// GeoJSON-shaped polygon payload used for geometry blob transport.
/// </summary>
[ExcludeFromCodeCoverage]
public class PolygonGeometryDto
{
    public string Type { get; set; } = "Polygon";

    /// <summary>
    /// Single closed outer ring only.
    /// </summary>
    public List<List<List<float>>> Coordinates { get; set; } = [];
}

/// <summary>
/// Geometry blob metadata for polygon features.
/// </summary>
[ExcludeFromCodeCoverage]
public class MapFeatureGeometryReferenceDto
{
    public string BlobKey { get; set; } = string.Empty;
    public string? ETag { get; set; }
    public string ContentEncoding { get; set; } = "gzip";
}
