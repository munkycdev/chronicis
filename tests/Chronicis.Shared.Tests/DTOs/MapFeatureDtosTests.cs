using System.Text.Json;
using Chronicis.Shared.DTOs.Maps;

namespace Chronicis.Shared.Tests.DTOs;

public class MapFeatureDtosTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public void MapFeatureDto_SerializesAndDeserializes_PointFeature()
    {
        var dto = new MapFeatureDto
        {
            FeatureId = Guid.NewGuid(),
            MapId = Guid.NewGuid(),
            LayerId = Guid.NewGuid(),
            FeatureType = MapFeatureType.Point,
            Name = "Capital",
            Color = "teal",
            Point = new MapFeaturePointDto { X = 0.2f, Y = 0.4f },
        };

        var json = JsonSerializer.Serialize(dto, JsonOptions);
        var roundTrip = JsonSerializer.Deserialize<MapFeatureDto>(json, JsonOptions);

        Assert.NotNull(roundTrip);
        Assert.Equal(MapFeatureType.Point, roundTrip!.FeatureType);
        Assert.Equal("teal", roundTrip.Color);
        Assert.NotNull(roundTrip.Point);
        Assert.Equal(0.2f, roundTrip.Point!.X);
        Assert.Null(roundTrip.Polygon);
    }

    [Fact]
    public void MapFeatureDto_SerializesAndDeserializes_PolygonFeature()
    {
        var dto = new MapFeatureDto
        {
            FeatureId = Guid.NewGuid(),
            MapId = Guid.NewGuid(),
            LayerId = Guid.NewGuid(),
            FeatureType = MapFeatureType.Polygon,
            Name = "Region",
            Color = "amber",
            Polygon = new PolygonGeometryDto
            {
                Type = "Polygon",
                Coordinates =
                [
                    [
                        [0.1f, 0.2f],
                        [0.7f, 0.2f],
                        [0.7f, 0.8f],
                        [0.1f, 0.2f],
                    ],
                ],
            },
            Geometry = new MapFeatureGeometryReferenceDto
            {
                BlobKey = "maps/a/layers/b/features/c.geojson.gz",
                ETag = "\"etag\"",
            },
        };

        var json = JsonSerializer.Serialize(dto, JsonOptions);
        var roundTrip = JsonSerializer.Deserialize<MapFeatureDto>(json, JsonOptions);

        Assert.NotNull(roundTrip);
        Assert.Equal(MapFeatureType.Polygon, roundTrip!.FeatureType);
        Assert.NotNull(roundTrip.Polygon);
        Assert.Equal("Polygon", roundTrip.Polygon!.Type);
        Assert.Equal("amber", roundTrip.Color);
        Assert.Single(roundTrip.Polygon.Coordinates);
        Assert.Equal(4, roundTrip.Polygon.Coordinates[0].Count);
        Assert.Equal(0.1f, roundTrip.Polygon.Coordinates[0][0][0]);
        Assert.Equal("maps/a/layers/b/features/c.geojson.gz", roundTrip.Geometry!.BlobKey);
        Assert.Equal("gzip", roundTrip.Geometry.ContentEncoding);
    }

    [Fact]
    public void MapFeatureSessionReferenceDto_SerializesAndDeserializes()
    {
        var dto = new MapFeatureSessionReferenceDto
        {
            SessionNoteId = Guid.NewGuid(),
            SessionNoteTitle = "Player Notes",
            SessionId = Guid.NewGuid(),
            SessionName = "Session 8",
            SessionDate = new DateTime(2026, 3, 8, 0, 0, 0, DateTimeKind.Utc),
            CreatedAt = new DateTime(2026, 3, 9, 12, 30, 0, DateTimeKind.Utc),
        };

        var json = JsonSerializer.Serialize(dto, JsonOptions);
        var roundTrip = JsonSerializer.Deserialize<MapFeatureSessionReferenceDto>(json, JsonOptions);

        Assert.NotNull(roundTrip);
        Assert.Equal(dto.SessionNoteId, roundTrip!.SessionNoteId);
        Assert.Equal("Player Notes", roundTrip.SessionNoteTitle);
        Assert.Equal(dto.SessionId, roundTrip.SessionId);
        Assert.Equal("Session 8", roundTrip.SessionName);
        Assert.Equal(dto.SessionDate, roundTrip.SessionDate);
        Assert.Equal(dto.CreatedAt, roundTrip.CreatedAt);
    }
}
