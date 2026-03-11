using System.Diagnostics.CodeAnalysis;
using Chronicis.Shared.Enums;

namespace Chronicis.Shared.Models;

/// <summary>
/// Represents a map feature on a map layer.
/// Point coordinates are normalized values between 0 and 1.
/// </summary>
[ExcludeFromCodeCoverage]
public class MapFeature
{
    public Guid MapFeatureId { get; set; }

    public Guid WorldMapId { get; set; }

    public Guid MapLayerId { get; set; }

    public MapFeatureType FeatureType { get; set; }

    public string? Name { get; set; }

    public float X { get; set; }

    public float Y { get; set; }

    public string? GeometryBlobKey { get; set; }

    public string? GeometryETag { get; set; }

    public Guid? LinkedArticleId { get; set; }
}
