using System.Text.Json;
using Chronicis.Shared.DTOs.Maps;

namespace Chronicis.Client.Engine.Geometry;

/// <summary>
/// Converts between engine polygon types and shared GeoJSON-shaped DTOs.
/// </summary>
public static class GeoJsonPolygonSerializer
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static PolygonGeometryDto ToDto(PolygonGeometry geometry)
    {
        var ring = EnsureClosedRing(geometry.Vertices)
            .Select(vertex => new List<float> { vertex.X, vertex.Y })
            .ToList();

        return new PolygonGeometryDto
        {
            Type = "Polygon",
            Coordinates = [ring],
        };
    }

    public static PolygonGeometry FromDto(PolygonGeometryDto dto)
    {
        var ring = dto.Coordinates.Count == 0
            ? []
            : dto.Coordinates[0]
                .Select(vertex => new NormalizedMapPoint(vertex[0], vertex[1]))
                .ToList();

        return new PolygonGeometry(ring);
    }

    public static string Serialize(PolygonGeometry geometry) =>
        JsonSerializer.Serialize(ToDto(geometry), JsonOptions);

    public static PolygonGeometry Deserialize(string json)
    {
        var dto = JsonSerializer.Deserialize<PolygonGeometryDto>(json, JsonOptions)
            ?? throw new InvalidOperationException("Polygon payload could not be deserialized.");

        return FromDto(dto);
    }

    private static IReadOnlyList<NormalizedMapPoint> EnsureClosedRing(IReadOnlyList<NormalizedMapPoint> vertices)
    {
        if (vertices.Count == 0)
        {
            return [];
        }

        if (vertices[^1].Equals(vertices[0]))
        {
            return vertices;
        }

        return [.. vertices, vertices[0]];
    }
}
