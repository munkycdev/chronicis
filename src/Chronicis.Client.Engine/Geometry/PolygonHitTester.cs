namespace Chronicis.Client.Engine.Geometry;

/// <summary>
/// Minimal point-in-polygon hit testing.
/// </summary>
public static class PolygonHitTester
{
    public static bool ContainsPoint(PolygonGeometry polygon, NormalizedMapPoint point)
    {
        if (polygon.Vertices.Count < 3)
        {
            return false;
        }

        var inside = false;
        var vertices = polygon.Vertices;

        for (var i = 0; i < vertices.Count; i++)
        {
            var current = vertices[i];
            var previous = vertices[(i + vertices.Count - 1) % vertices.Count];
            var intersects = ((current.Y > point.Y) != (previous.Y > point.Y))
                && (point.X < ((previous.X - current.X) * (point.Y - current.Y) / ((previous.Y - current.Y) + float.Epsilon)) + current.X);

            if (intersects)
            {
                inside = !inside;
            }
        }

        return inside;
    }
}
