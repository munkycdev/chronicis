namespace Chronicis.Client.Engine.Geometry;

/// <summary>
/// Single-ring polygon geometry in normalized image space.
/// </summary>
public sealed class PolygonGeometry
{
    public PolygonGeometry(IReadOnlyList<NormalizedMapPoint> vertices)
    {
        Vertices = vertices;
    }

    public IReadOnlyList<NormalizedMapPoint> Vertices { get; }

    public bool IsClosed =>
        Vertices.Count > 1
        && Vertices[0].Equals(Vertices[^1]);
}
