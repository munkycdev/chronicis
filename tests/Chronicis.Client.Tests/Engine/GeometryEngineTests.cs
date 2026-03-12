using Chronicis.Client.Engine.Geometry;
using Chronicis.Client.Engine.Interaction;
using Xunit;

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

    [Fact]
    public void PolygonSvgPathBuilder_ValidPolygon_ReturnsSvgPath()
    {
        var polygon = new Chronicis.Shared.DTOs.Maps.PolygonGeometryDto
        {
            Type = "Polygon",
            Coordinates =
            [
                [
                    [0.1f, 0.2f],
                    [0.8f, 0.2f],
                    [0.4f, 0.7f],
                    [0.1f, 0.2f],
                ]
            ]
        };

        var result = PolygonSvgPathBuilder.TryBuildPath(polygon, out var path);

        Assert.True(result);
        Assert.Equal("M 0.1 0.2 L 0.8 0.2 L 0.4 0.7 L 0.1 0.2 Z", path);
    }

    [Fact]
    public void PolygonSvgPathBuilder_OutOfBoundsCoordinate_ReturnsFalse()
    {
        var polygon = new Chronicis.Shared.DTOs.Maps.PolygonGeometryDto
        {
            Type = "Polygon",
            Coordinates =
            [
                [
                    [0.1f, 0.2f],
                    [1.1f, 0.2f],
                    [0.4f, 0.7f],
                    [0.1f, 0.2f],
                ]
            ]
        };

        Assert.False(PolygonSvgPathBuilder.TryBuildPath(polygon, out _));
    }

    [Fact]
    public void PolygonSvgPathBuilder_FewerThanThreeDistinctVertices_ReturnsFalse()
    {
        var polygon = new Chronicis.Shared.DTOs.Maps.PolygonGeometryDto
        {
            Type = "Polygon",
            Coordinates =
            [
                [
                    [0.1f, 0.2f],
                    [0.8f, 0.2f],
                    [0.1f, 0.2f],
                    [0.1f, 0.2f],
                ]
            ]
        };

        Assert.False(PolygonSvgPathBuilder.TryBuildPath(polygon, out _));
    }

    [Fact]
    public void PolygonSvgPathBuilder_NonClosedRing_ReturnsFalse()
    {
        var polygon = new Chronicis.Shared.DTOs.Maps.PolygonGeometryDto
        {
            Type = "Polygon",
            Coordinates =
            [
                [
                    [0.1f, 0.2f],
                    [0.8f, 0.2f],
                    [0.4f, 0.7f],
                    [0.2f, 0.3f],
                ]
            ]
        };

        Assert.False(PolygonSvgPathBuilder.TryBuildPath(polygon, out _));
    }
}
