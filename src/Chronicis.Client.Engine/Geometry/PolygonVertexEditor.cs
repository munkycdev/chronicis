namespace Chronicis.Client.Engine.Geometry;

/// <summary>
/// Minimal vertex editing helpers for single-ring polygons in normalized image space.
/// </summary>
public static class PolygonVertexEditor
{
    public static int FindNearestVertexIndex(PolygonGeometry polygon, NormalizedMapPoint point, float maxDistance)
    {
        if (maxDistance < 0f)
        {
            return -1;
        }

        var vertices = GetEditableVertices(polygon);
        var maxDistanceSquared = maxDistance * maxDistance;
        var nearestIndex = -1;
        var nearestDistanceSquared = maxDistanceSquared;

        for (var i = 0; i < vertices.Count; i++)
        {
            var vertex = vertices[i];
            var distanceSquared = GetDistanceSquared(vertex, point);
            if (distanceSquared > nearestDistanceSquared)
            {
                continue;
            }

            nearestIndex = i;
            nearestDistanceSquared = distanceSquared;
        }

        return nearestIndex;
    }

    public static bool TryMoveVertex(
        PolygonGeometry polygon,
        int vertexIndex,
        NormalizedMapPoint point,
        out PolygonGeometry updatedPolygon)
    {
        var vertices = polygon.Vertices.ToList();
        updatedPolygon = polygon;

        if (vertices.Count == 0)
        {
            return false;
        }

        var editableVertexCount = polygon.IsClosed && vertices.Count > 1
            ? vertices.Count - 1
            : vertices.Count;

        if (vertexIndex < 0 || vertexIndex >= editableVertexCount)
        {
            return false;
        }

        var normalizedPoint = ClampToBounds(point);
        vertices[vertexIndex] = normalizedPoint;

        if (polygon.IsClosed && vertexIndex == 0)
        {
            vertices[^1] = normalizedPoint;
        }

        updatedPolygon = new PolygonGeometry(vertices);
        return true;
    }

    public static NormalizedMapPoint ClampToBounds(NormalizedMapPoint point) =>
        new(Clamp(point.X), Clamp(point.Y));

    private static IReadOnlyList<NormalizedMapPoint> GetEditableVertices(PolygonGeometry polygon) =>
        polygon.IsClosed && polygon.Vertices.Count > 1
            ? polygon.Vertices.Take(polygon.Vertices.Count - 1).ToList()
            : polygon.Vertices;

    private static float GetDistanceSquared(NormalizedMapPoint left, NormalizedMapPoint right)
    {
        var deltaX = left.X - right.X;
        var deltaY = left.Y - right.Y;
        return (deltaX * deltaX) + (deltaY * deltaY);
    }

    private static float Clamp(float value) =>
        value < 0f ? 0f : value > 1f ? 1f : value;
}
