using Chronicis.Client.Engine.Geometry;

namespace Chronicis.Client.Engine.Interaction;

/// <summary>
/// Mutable helper for polygon draft workflows.
/// </summary>
public sealed class PolygonDraftState
{
    private readonly List<NormalizedMapPoint> _vertices = [];

    public IReadOnlyList<NormalizedMapPoint> Vertices => _vertices;

    public bool IsEmpty => _vertices.Count == 0;

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
}
