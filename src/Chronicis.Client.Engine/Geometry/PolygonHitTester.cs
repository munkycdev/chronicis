namespace Chronicis.Client.Engine.Geometry;

/// <summary>
/// Minimal point-in-polygon hit testing.
/// </summary>
public static class PolygonHitTester
{
    private const float BoundaryEpsilon = 0.0001f;

    public static bool ContainsPoint(PolygonGeometry polygon, NormalizedMapPoint point)
    {
        var vertices = GetRingVertices(polygon);
        if (vertices.Count < 3)
        {
            return false;
        }

        var inside = false;

        for (var i = 0; i < vertices.Count; i++)
        {
            var current = vertices[i];
            var next = vertices[(i + 1) % vertices.Count];

            if (IsPointOnSegment(current, next, point))
            {
                return true;
            }

            var intersects = ((current.Y > point.Y) != (next.Y > point.Y))
                && (point.X < ((next.X - current.X) * (point.Y - current.Y) / ((next.Y - current.Y) + float.Epsilon)) + current.X);

            if (intersects)
            {
                inside = !inside;
            }
        }

        return inside;
    }

    private static IReadOnlyList<NormalizedMapPoint> GetRingVertices(PolygonGeometry polygon)
    {
        if (polygon.Vertices.Count > 1 && polygon.IsClosed)
        {
            return polygon.Vertices.Take(polygon.Vertices.Count - 1).ToList();
        }

        return polygon.Vertices;
    }

    private static bool IsPointOnSegment(
        NormalizedMapPoint start,
        NormalizedMapPoint end,
        NormalizedMapPoint point)
    {
        var cross = ((point.Y - start.Y) * (end.X - start.X)) - ((point.X - start.X) * (end.Y - start.Y));
        if (MathF.Abs(cross) > BoundaryEpsilon)
        {
            return false;
        }

        var dot = ((point.X - start.X) * (end.X - start.X)) + ((point.Y - start.Y) * (end.Y - start.Y));
        if (dot < -BoundaryEpsilon)
        {
            return false;
        }

        var lengthSquared = ((end.X - start.X) * (end.X - start.X)) + ((end.Y - start.Y) * (end.Y - start.Y));
        return dot <= lengthSquared + BoundaryEpsilon;
    }
}
