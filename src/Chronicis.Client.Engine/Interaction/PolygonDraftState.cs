using Chronicis.Client.Engine.Geometry;

namespace Chronicis.Client.Engine.Interaction;

/// <summary>
/// Mutable helper for polygon draft workflows.
/// </summary>
public sealed class PolygonDraftState
{
    private const float TerminalDuplicateEpsilon = 0.001f;
    private readonly List<NormalizedMapPoint> _vertices = [];

    public IReadOnlyList<NormalizedMapPoint> Vertices => _vertices;

    public bool IsEmpty => _vertices.Count == 0;

    public bool CanComplete => CountDistinctVertices(GetNormalizedTerminalVertices()) >= 3;

    public void AddVertex(NormalizedMapPoint point) => _vertices.Add(point);

    public bool RemoveLastVertex()
    {
        if (_vertices.Count == 0)
        {
            return false;
        }

        _vertices.RemoveAt(_vertices.Count - 1);
        return true;
    }

    public void MoveVertex(int index, NormalizedMapPoint point) => _vertices[index] = point;

    public void Clear() => _vertices.Clear();

    public PolygonGeometry BuildPolygon() => new([.. _vertices]);

    public bool EndsWith(NormalizedMapPoint point)
    {
        if (_vertices.Count == 0)
        {
            return false;
        }

        return AreWithinEpsilon(_vertices[^1], point, TerminalDuplicateEpsilon);
    }

    public IReadOnlyList<NormalizedMapPoint> BuildClosedRing()
    {
        var vertices = GetNormalizedTerminalVertices();
        if (vertices.Count == 0)
        {
            return [];
        }

        if (!vertices[0].Equals(vertices[^1]))
        {
            vertices.Add(vertices[0]);
        }

        return vertices;
    }

    public PolygonGeometry BuildClosedPolygon() => new(BuildClosedRing());

    private List<NormalizedMapPoint> GetNormalizedTerminalVertices()
    {
        var vertices = new List<NormalizedMapPoint>(_vertices);
        if (vertices.Count < 2)
        {
            return vertices;
        }

        if (AreWithinEpsilon(vertices[^1], vertices[^2], TerminalDuplicateEpsilon))
        {
            vertices.RemoveAt(vertices.Count - 1);
        }

        return vertices;
    }

    private static int CountDistinctVertices(IReadOnlyList<NormalizedMapPoint> vertices)
    {
        var distinct = new HashSet<NormalizedMapPoint>();
        foreach (var vertex in vertices)
        {
            distinct.Add(vertex);
        }

        return distinct.Count;
    }

    private static bool AreWithinEpsilon(NormalizedMapPoint left, NormalizedMapPoint right, float epsilon) =>
        MathF.Abs(left.X - right.X) <= epsilon
        && MathF.Abs(left.Y - right.Y) <= epsilon;
}
