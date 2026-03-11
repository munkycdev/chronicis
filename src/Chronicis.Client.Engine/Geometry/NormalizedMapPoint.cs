namespace Chronicis.Client.Engine.Geometry;

/// <summary>
/// Normalized image-space coordinate.
/// </summary>
public readonly record struct NormalizedMapPoint(float X, float Y)
{
    public bool IsInBounds => X >= 0f && X <= 1f && Y >= 0f && Y <= 1f;
}
