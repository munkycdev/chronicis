using Chronicis.Client.Engine.Geometry;
using Chronicis.Client.Engine.Interaction;

namespace Chronicis.Client.Tests.Engine;

public class GeometryEngineTests
{
    [Fact]
    public void NormalizedMapPoint_IsInBounds_TracksNormalizedRange()
    {
        Assert.True(new NormalizedMapPoint(0.5f, 0.25f).IsInBounds);
        Assert.False(new NormalizedMapPoint(1.1f, 0.25f).IsInBounds);
    }

    [Fact]
    public void GeoJsonPolygonSerializer_RoundTripsPolygon()
    {
        var polygon = new PolygonGeometry(
        [
            new NormalizedMapPoint(0.1f, 0.2f),
            new NormalizedMapPoint(0.8f, 0.2f),
            new NormalizedMapPoint(0.8f, 0.9f),
            new NormalizedMapPoint(0.1f, 0.2f),
        ]);

        var json = GeoJsonPolygonSerializer.Serialize(polygon);
        var roundTrip = GeoJsonPolygonSerializer.Deserialize(json);

        Assert.Equal(4, roundTrip.Vertices.Count);
        Assert.True(roundTrip.IsClosed);
    }

    [Fact]
    public void PolygonHitTester_ContainsPoint_UsesPolygonArea()
    {
        var polygon = new PolygonGeometry(
        [
            new NormalizedMapPoint(0.1f, 0.1f),
            new NormalizedMapPoint(0.9f, 0.1f),
            new NormalizedMapPoint(0.9f, 0.9f),
            new NormalizedMapPoint(0.1f, 0.9f),
            new NormalizedMapPoint(0.1f, 0.1f),
        ]);

        Assert.True(PolygonHitTester.ContainsPoint(polygon, new NormalizedMapPoint(0.4f, 0.4f)));
        Assert.False(PolygonHitTester.ContainsPoint(polygon, new NormalizedMapPoint(0.95f, 0.95f)));
    }

    [Fact]
    public void PolygonDraftState_SupportsAddMoveRemoveAndBuild()
    {
        var draft = new PolygonDraftState();
        draft.AddVertex(new NormalizedMapPoint(0.1f, 0.1f));
        draft.AddVertex(new NormalizedMapPoint(0.5f, 0.1f));
        draft.AddVertex(new NormalizedMapPoint(0.5f, 0.5f));
        draft.MoveVertex(1, new NormalizedMapPoint(0.6f, 0.1f));

        var polygon = draft.BuildPolygon();

        Assert.Equal(3, polygon.Vertices.Count);
        Assert.Equal(0.6f, polygon.Vertices[1].X);
        Assert.True(draft.RemoveLastVertex());
        Assert.Equal(2, draft.Vertices.Count);
    }
}
