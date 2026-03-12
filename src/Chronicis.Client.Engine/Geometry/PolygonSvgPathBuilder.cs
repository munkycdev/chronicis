using System.Globalization;
using Chronicis.Shared.DTOs.Maps;

namespace Chronicis.Client.Engine.Geometry;

public static class PolygonSvgPathBuilder
{
    public static bool TryBuildPath(PolygonGeometryDto? polygon, out string pathData)
    {
        pathData = string.Empty;

        if (polygon == null
            || !string.Equals(polygon.Type, "Polygon", StringComparison.Ordinal)
            || polygon.Coordinates.Count != 1)
        {
            return false;
        }

        var ring = polygon.Coordinates[0];
        if (ring.Count < 4)
        {
            return false;
        }

        var distinctVertices = new HashSet<NormalizedMapPoint>();
        var builder = new System.Text.StringBuilder();

        for (var index = 0; index < ring.Count; index++)
        {
            var vertex = ring[index];
            if (vertex.Count != 2)
            {
                return false;
            }

            var point = new NormalizedMapPoint(vertex[0], vertex[1]);
            if (!point.IsInBounds)
            {
                return false;
            }

            if (index < ring.Count - 1)
            {
                distinctVertices.Add(point);
            }

            if (index == 0)
            {
                builder.Append("M ");
            }
            else
            {
                builder.Append(" L ");
            }

            builder.Append(point.X.ToString("0.####", CultureInfo.InvariantCulture));
            builder.Append(' ');
            builder.Append(point.Y.ToString("0.####", CultureInfo.InvariantCulture));
        }

        var first = new NormalizedMapPoint(ring[0][0], ring[0][1]);
        var last = new NormalizedMapPoint(ring[^1][0], ring[^1][1]);
        if (first != last || distinctVertices.Count < 3)
        {
            return false;
        }

        builder.Append(" Z");
        pathData = builder.ToString();
        return true;
    }

    public static bool TryBuildDraftPath(
        IReadOnlyList<NormalizedMapPoint> vertices,
        NormalizedMapPoint? hoverPoint,
        out string pathData)
    {
        pathData = string.Empty;

        if (vertices.Count == 0)
        {
            return false;
        }

        var builder = new System.Text.StringBuilder();
        AppendPoint(builder, vertices[0], moveTo: true);

        for (var index = 1; index < vertices.Count; index++)
        {
            if (!vertices[index].IsInBounds)
            {
                return false;
            }

            AppendPoint(builder, vertices[index], moveTo: false);
        }

        if (hoverPoint is { IsInBounds: true } previewPoint)
        {
            AppendPoint(builder, previewPoint, moveTo: false);
        }

        pathData = builder.ToString();
        return true;
    }

    private static void AppendPoint(System.Text.StringBuilder builder, NormalizedMapPoint point, bool moveTo)
    {
        if (!point.IsInBounds)
        {
            throw new InvalidOperationException("SVG draft path received an out-of-bounds point.");
        }

        builder.Append(moveTo ? "M " : " L ");
        builder.Append(point.X.ToString("0.####", CultureInfo.InvariantCulture));
        builder.Append(' ');
        builder.Append(point.Y.ToString("0.####", CultureInfo.InvariantCulture));
    }
}
