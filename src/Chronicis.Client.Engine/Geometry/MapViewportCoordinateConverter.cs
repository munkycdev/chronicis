namespace Chronicis.Client.Engine.Geometry;

public static class MapViewportCoordinateConverter
{
    public static bool TryConvertToNormalizedPoint(
        double viewportLocalX,
        double viewportLocalY,
        double viewportWidth,
        double viewportHeight,
        double baseWidth,
        double baseHeight,
        double panX,
        double panY,
        double zoom,
        out NormalizedMapPoint point)
    {
        point = default;

        if (viewportWidth <= 0d
            || viewportHeight <= 0d
            || baseWidth <= 0d
            || baseHeight <= 0d
            || zoom <= 0d)
        {
            return false;
        }

        if (viewportLocalX < 0d
            || viewportLocalY < 0d
            || viewportLocalX > viewportWidth
            || viewportLocalY > viewportHeight)
        {
            return false;
        }

        var normalizedX = (float)(((viewportLocalX - panX) / zoom) / baseWidth);
        var normalizedY = (float)(((viewportLocalY - panY) / zoom) / baseHeight);
        point = new NormalizedMapPoint(normalizedX, normalizedY);
        return point.IsInBounds;
    }
}
