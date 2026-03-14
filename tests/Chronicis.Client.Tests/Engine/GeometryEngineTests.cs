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
    public void PolygonHitTester_ContainsPoint_TreatsBoundaryAsHit()
    {
        var polygon = new PolygonGeometry(
        [
            new NormalizedMapPoint(0.1f, 0.1f),
            new NormalizedMapPoint(0.9f, 0.1f),
            new NormalizedMapPoint(0.9f, 0.9f),
            new NormalizedMapPoint(0.1f, 0.9f),
            new NormalizedMapPoint(0.1f, 0.1f),
        ]);

        Assert.True(PolygonHitTester.ContainsPoint(polygon, new NormalizedMapPoint(0.1f, 0.5f)));
    }

    [Fact]
    public void PolygonVertexEditor_FindNearestVertexIndex_ReturnsNearestEditableVertex()
    {
        var polygon = new PolygonGeometry(
        [
            new NormalizedMapPoint(0.1f, 0.2f),
            new NormalizedMapPoint(0.8f, 0.2f),
            new NormalizedMapPoint(0.4f, 0.7f),
            new NormalizedMapPoint(0.1f, 0.2f),
        ]);

        var index = PolygonVertexEditor.FindNearestVertexIndex(
            polygon,
            new NormalizedMapPoint(0.78f, 0.21f),
            maxDistance: 0.05f);

        Assert.Equal(1, index);
    }

    [Fact]
    public void PolygonVertexEditor_TryMoveVertex_ReplacesMiddleVertex()
    {
        var polygon = new PolygonGeometry(
        [
            new NormalizedMapPoint(0.1f, 0.2f),
            new NormalizedMapPoint(0.8f, 0.2f),
            new NormalizedMapPoint(0.4f, 0.7f),
            new NormalizedMapPoint(0.1f, 0.2f),
        ]);

        var moved = PolygonVertexEditor.TryMoveVertex(
            polygon,
            vertexIndex: 1,
            new NormalizedMapPoint(0.6f, 0.25f),
            out var updated);

        Assert.True(moved);
        Assert.Equal(new NormalizedMapPoint(0.6f, 0.25f), updated.Vertices[1]);
        Assert.Equal(updated.Vertices[0], updated.Vertices[^1]);
    }

    [Fact]
    public void PolygonVertexEditor_TryMoveVertex_PreservesClosureForFirstVertex()
    {
        var polygon = new PolygonGeometry(
        [
            new NormalizedMapPoint(0.1f, 0.2f),
            new NormalizedMapPoint(0.8f, 0.2f),
            new NormalizedMapPoint(0.4f, 0.7f),
            new NormalizedMapPoint(0.1f, 0.2f),
        ]);

        var moved = PolygonVertexEditor.TryMoveVertex(
            polygon,
            vertexIndex: 0,
            new NormalizedMapPoint(-0.2f, 1.2f),
            out var updated);

        Assert.True(moved);
        Assert.Equal(new NormalizedMapPoint(0f, 1f), updated.Vertices[0]);
        Assert.Equal(updated.Vertices[0], updated.Vertices[^1]);
    }

    [Fact]
    public void PolygonVertexEditor_TryInsertVertex_InsertsProjectedPointOnNearestEdge()
    {
        var polygon = new PolygonGeometry(
        [
            new NormalizedMapPoint(0.1f, 0.2f),
            new NormalizedMapPoint(0.8f, 0.2f),
            new NormalizedMapPoint(0.4f, 0.7f),
            new NormalizedMapPoint(0.1f, 0.2f),
        ]);

        var inserted = PolygonVertexEditor.TryInsertVertex(
            polygon,
            new NormalizedMapPoint(0.46f, 0.24f),
            maxDistance: 0.05f,
            out var updated,
            out var insertedIndex);

        Assert.True(inserted);
        Assert.Equal(1, insertedIndex);
        Assert.Equal(new NormalizedMapPoint(0.46f, 0.2f), updated.Vertices[1]);
        Assert.Equal(updated.Vertices[0], updated.Vertices[^1]);
    }

    [Fact]
    public void PolygonVertexEditor_TryInsertVertex_InsertsBeforeClosingVertexForLastEdge()
    {
        var polygon = new PolygonGeometry(
        [
            new NormalizedMapPoint(0.1f, 0.2f),
            new NormalizedMapPoint(0.8f, 0.2f),
            new NormalizedMapPoint(0.4f, 0.7f),
            new NormalizedMapPoint(0.1f, 0.2f),
        ]);

        var inserted = PolygonVertexEditor.TryInsertVertex(
            polygon,
            new NormalizedMapPoint(0.18f, 0.42f),
            maxDistance: 0.05f,
            out var updated,
            out var insertedIndex);

        Assert.True(inserted);
        Assert.Equal(3, insertedIndex);
        Assert.Equal(0.2182353f, updated.Vertices[3].X, 5);
        Assert.Equal(0.39705884f, updated.Vertices[3].Y, 5);
        Assert.Equal(updated.Vertices[0], updated.Vertices[^1]);
    }

    [Theory]
    [InlineData(-0.1f, 0.4f, 0.2f)]
    [InlineData(0.01f, 0.45f, 0.5f)]
    public void PolygonVertexEditor_TryInsertVertex_InvalidDistanceOrMiss_ReturnsFalse(
        float maxDistance,
        float pointX,
        float pointY)
    {
        var polygon = new PolygonGeometry(
        [
            new NormalizedMapPoint(0.1f, 0.2f),
            new NormalizedMapPoint(0.8f, 0.2f),
            new NormalizedMapPoint(0.4f, 0.7f),
            new NormalizedMapPoint(0.1f, 0.2f),
        ]);

        var inserted = PolygonVertexEditor.TryInsertVertex(
            polygon,
            new NormalizedMapPoint(pointX, pointY),
            maxDistance,
            out var updated,
            out var insertedIndex);

        Assert.False(inserted);
        Assert.Equal(-1, insertedIndex);
        Assert.Same(polygon, updated);
    }

    [Fact]
    public void PolygonVertexEditor_TryInsertVertex_WithTooFewVertices_ReturnsFalse()
    {
        var polygon = new PolygonGeometry(
        [
            new NormalizedMapPoint(0.1f, 0.2f),
        ]);

        var inserted = PolygonVertexEditor.TryInsertVertex(
            polygon,
            new NormalizedMapPoint(0.1f, 0.2f),
            maxDistance: 0.05f,
            out var updated,
            out var insertedIndex);

        Assert.False(inserted);
        Assert.Equal(-1, insertedIndex);
        Assert.Same(polygon, updated);
    }

    [Fact]
    public void GeoJsonPolygonSerializer_ToDto_ClosesOpenRing()
    {
        var polygon = new PolygonGeometry(
        [
            new NormalizedMapPoint(0.1f, 0.2f),
            new NormalizedMapPoint(0.8f, 0.2f),
            new NormalizedMapPoint(0.4f, 0.7f),
        ]);

        var dto = GeoJsonPolygonSerializer.ToDto(polygon);

        Assert.Equal(4, dto.Coordinates[0].Count);
        Assert.Equal(dto.Coordinates[0][0], dto.Coordinates[0][^1]);
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
    public void PolygonDraftState_CanComplete_RequiresThreeDistinctVertices()
    {
        var draft = new PolygonDraftState();
        draft.AddVertex(new NormalizedMapPoint(0.1f, 0.1f));
        draft.AddVertex(new NormalizedMapPoint(0.5f, 0.1f));

        Assert.False(draft.CanComplete);

        draft.AddVertex(new NormalizedMapPoint(0.5f, 0.5f));

        Assert.True(draft.CanComplete);
    }

    [Fact]
    public void PolygonDraftState_BuildClosedRing_AppendsStartVertex()
    {
        var draft = new PolygonDraftState();
        draft.AddVertex(new NormalizedMapPoint(0.1f, 0.1f));
        draft.AddVertex(new NormalizedMapPoint(0.5f, 0.1f));
        draft.AddVertex(new NormalizedMapPoint(0.5f, 0.5f));

        var ring = draft.BuildClosedRing();

        Assert.Equal(4, ring.Count);
        Assert.Equal(ring[0], ring[^1]);
    }

    [Fact]
    public void PolygonDraftState_BuildClosedRing_CollapsesAdjacentDuplicateTerminalVertex()
    {
        var draft = new PolygonDraftState();
        draft.AddVertex(new NormalizedMapPoint(0.1f, 0.1f));
        draft.AddVertex(new NormalizedMapPoint(0.5f, 0.1f));
        draft.AddVertex(new NormalizedMapPoint(0.5f, 0.5f));
        draft.AddVertex(new NormalizedMapPoint(0.5005f, 0.5004f));

        var ring = draft.BuildClosedRing();

        Assert.Equal(4, ring.Count);
        Assert.Equal(new NormalizedMapPoint(0.5f, 0.5f), ring[^2]);
        Assert.Equal(ring[0], ring[^1]);
    }

    [Fact]
    public void PolygonDraftState_ClearAndRemoveLastVertex_ResetLifecycle()
    {
        var draft = new PolygonDraftState();
        draft.AddVertex(new NormalizedMapPoint(0.1f, 0.1f));

        Assert.True(draft.RemoveLastVertex());
        Assert.False(draft.RemoveLastVertex());

        draft.AddVertex(new NormalizedMapPoint(0.2f, 0.2f));
        draft.Clear();

        Assert.True(draft.IsEmpty);
        Assert.Empty(draft.BuildClosedRing());
    }

    [Fact]
    public void MapViewportCoordinateConverter_ConvertsViewportPoint()
    {
        var result = MapViewportCoordinateConverter.TryConvertToNormalizedPoint(
            viewportLocalX: 250d,
            viewportLocalY: 100d,
            viewportWidth: 1000d,
            viewportHeight: 500d,
            baseWidth: 1000d,
            baseHeight: 500d,
            panX: 0d,
            panY: 0d,
            zoom: 1d,
            out var point);

        Assert.True(result);
        Assert.Equal(new NormalizedMapPoint(0.25f, 0.2f), point);
    }

    [Fact]
    public void MapViewportCoordinateConverter_AppliesPanAndZoom()
    {
        var result = MapViewportCoordinateConverter.TryConvertToNormalizedPoint(
            viewportLocalX: 450d,
            viewportLocalY: 275d,
            viewportWidth: 800d,
            viewportHeight: 600d,
            baseWidth: 1000d,
            baseHeight: 500d,
            panX: -50d,
            panY: 25d,
            zoom: 2d,
            out var point);

        Assert.True(result);
        Assert.Equal(new NormalizedMapPoint(0.25f, 0.25f), point);
    }

    [Theory]
    [InlineData(-1d, 50d, 100d, 100d, 100d, 100d, 0d, 0d, 1d)]
    [InlineData(50d, 50d, 0d, 100d, 100d, 100d, 0d, 0d, 1d)]
    [InlineData(150d, 50d, 100d, 100d, 100d, 100d, 0d, 0d, 1d)]
    [InlineData(50d, 50d, 100d, 100d, 100d, 100d, 100d, 0d, 1d)]
    public void MapViewportCoordinateConverter_InvalidInputs_ReturnFalse(
        double localX,
        double localY,
        double viewportWidth,
        double viewportHeight,
        double baseWidth,
        double baseHeight,
        double panX,
        double panY,
        double zoom)
    {
        var result = MapViewportCoordinateConverter.TryConvertToNormalizedPoint(
            localX,
            localY,
            viewportWidth,
            viewportHeight,
            baseWidth,
            baseHeight,
            panX,
            panY,
            zoom,
            out _);

        Assert.False(result);
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

    [Fact]
    public void PolygonSvgPathBuilder_DraftPath_IncludesHoverPoint()
    {
        var result = PolygonSvgPathBuilder.TryBuildDraftPath(
            [
                new NormalizedMapPoint(0.1f, 0.2f),
                new NormalizedMapPoint(0.8f, 0.2f)
            ],
            new NormalizedMapPoint(0.4f, 0.7f),
            out var path);

        Assert.True(result);
        Assert.Equal("M 0.1 0.2 L 0.8 0.2 L 0.4 0.7", path);
    }

    [Fact]
    public void PolygonSvgPathBuilder_DraftPath_WithoutHoverPoint_UsesCommittedVerticesOnly()
    {
        var result = PolygonSvgPathBuilder.TryBuildDraftPath(
            [
                new NormalizedMapPoint(0.1f, 0.2f),
                new NormalizedMapPoint(0.8f, 0.2f)
            ],
            hoverPoint: null,
            out var path);

        Assert.True(result);
        Assert.Equal("M 0.1 0.2 L 0.8 0.2", path);
    }
}
