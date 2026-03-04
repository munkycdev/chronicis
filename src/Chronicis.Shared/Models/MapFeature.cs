using System.Diagnostics.CodeAnalysis;

namespace Chronicis.Shared.Models;

/// <summary>
/// Represents a point feature (pin) on a map.
/// Coordinates are normalized values between 0 and 1.
/// </summary>
[ExcludeFromCodeCoverage]
public class MapFeature
{
    public Guid MapFeatureId { get; set; }

    public Guid WorldMapId { get; set; }

    public Guid MapLayerId { get; set; }

    public string? Name { get; set; }

    public float X { get; set; }

    public float Y { get; set; }

    public Guid? LinkedArticleId { get; set; }
}
