using System.Diagnostics.CodeAnalysis;
using Chronicis.Api.Data;
using Chronicis.Api.Services;
using Chronicis.Shared.DTOs.Maps;
using Chronicis.Shared.Enums;
using Chronicis.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Chronicis.Api.Tests.Services;

[ExcludeFromCodeCoverage]
public class WorldMapServiceTests : IDisposable
{
    private readonly ChronicisDbContext _db;
    private readonly IMapBlobStore _blobStore;
    private readonly WorldMapService _sut;

    // Fixed IDs for readability
    private readonly Guid _worldId = Guid.Parse("11000000-0000-0000-0000-000000000001");
    private readonly Guid _ownerId = Guid.Parse("22000000-0000-0000-0000-000000000001");
    private readonly Guid _memberId = Guid.Parse("22000000-0000-0000-0000-000000000002");
    private readonly Guid _outsiderId = Guid.Parse("22000000-0000-0000-0000-000000000003");

    public WorldMapServiceTests()
    {
        var options = new DbContextOptionsBuilder<ChronicisDbContext>()
            .UseInMemoryDatabase($"worldmap-{Guid.NewGuid()}")
            .Options;

        _db = new ChronicisDbContext(options);
        _blobStore = Substitute.For<IMapBlobStore>();
        _blobStore.DeleteMapFolderAsync(Arg.Any<Guid>()).Returns(Task.CompletedTask);
        _sut = new WorldMapService(_db, _blobStore, NullLogger<WorldMapService>.Instance);

        SeedWorld();
    }

    private void SeedWorld()
    {
        var owner = new User { Id = _ownerId, Auth0UserId = "auth0|owner", Email = "owner@test.com", DisplayName = "Owner" };
        var member = new User { Id = _memberId, Auth0UserId = "auth0|member", Email = "member@test.com", DisplayName = "Member" };

        var world = new World
        {
            Id = _worldId,
            OwnerId = _ownerId,
            Name = "Test World",
            Slug = "test-world",
            Owner = owner,
        };

        var worldMember = new WorldMember
        {
            Id = Guid.NewGuid(),
            WorldId = _worldId,
            UserId = _memberId,
            Role = WorldRole.Player,
        };

        _db.Users.AddRange(owner, member);
        _db.Worlds.Add(world);
        _db.WorldMembers.Add(worldMember);
        _db.SaveChanges();
    }

    public void Dispose()
    {
        _db.Dispose();
        GC.SuppressFinalize(this);
    }

    // ── CreateMapAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateMap_Success_CreatesMapAndThreeDefaultLayers()
    {
        var dto = new MapCreateDto { Name = "Faerûn" };

        var result = await _sut.CreateMapAsync(_worldId, _ownerId, dto);

        Assert.Equal("Faerûn", result.Name);
        Assert.Equal(_worldId, result.WorldId);
        Assert.False(result.HasBasemap);

        var layers = await _db.MapLayers
            .Where(l => l.WorldMapId == result.WorldMapId)
            .OrderBy(l => l.SortOrder)
            .ToListAsync();

        Assert.Equal(3, layers.Count);
        Assert.Equal("World", layers[0].Name);
        Assert.Equal("Campaign", layers[1].Name);
        Assert.Equal("Arc", layers[2].Name);
        Assert.All(layers, l => Assert.False(l.IsEnabled));
    }

    [Fact]
    public async Task CreateMap_Unauthorized_WhenUserIsNotOwner()
    {
        var dto = new MapCreateDto { Name = "Forbidden" };

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _sut.CreateMapAsync(_worldId, _outsiderId, dto));
    }

    // ── GetMapAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task GetMap_ReturnsNull_WhenMapDoesNotExist()
    {
        var result = await _sut.GetMapAsync(Guid.NewGuid(), _ownerId);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetMap_ReturnsNull_WhenUserHasNoAccess()
    {
        var map = await CreateTestMap("Hidden Map");

        var result = await _sut.GetMapAsync(map.WorldMapId, _outsiderId);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetMap_ReturnsDto_WhenUserIsOwner()
    {
        var map = await CreateTestMap("Owner Map");

        var result = await _sut.GetMapAsync(map.WorldMapId, _ownerId);

        Assert.NotNull(result);
        Assert.Equal("Owner Map", result.Name);
    }

    [Fact]
    public async Task GetMap_ReturnsDto_WhenUserIsMember()
    {
        var map = await CreateTestMap("Member Map");

        var result = await _sut.GetMapAsync(map.WorldMapId, _memberId);

        Assert.NotNull(result);
        Assert.Equal("Member Map", result.Name);
    }

    // ── UpdateMapAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateMap_Throws_WhenMapNotFound()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.UpdateMapAsync(_worldId, Guid.NewGuid(), _ownerId, new MapUpdateDto { Name = "Renamed" }));
    }

    [Fact]
    public async Task UpdateMap_Throws_WhenUserIsNotOwner()
    {
        var map = await CreateTestMap("Protected Name");

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _sut.UpdateMapAsync(_worldId, map.WorldMapId, _memberId, new MapUpdateDto { Name = "Renamed" }));
    }

    [Fact]
    public async Task UpdateMap_Success_UpdatesNameAndTimestamp()
    {
        var map = await CreateTestMap("Old Name");
        var originalUpdatedUtc = map.UpdatedUtc;

        await Task.Delay(5);
        var result = await _sut.UpdateMapAsync(_worldId, map.WorldMapId, _ownerId, new MapUpdateDto { Name = "  New Name  " });

        Assert.Equal("New Name", result.Name);

        var updated = await _db.WorldMaps.FindAsync(map.WorldMapId);
        Assert.NotNull(updated);
        Assert.Equal("New Name", updated!.Name);
        Assert.True(updated.UpdatedUtc > originalUpdatedUtc);
    }

    // ── ListMapsForWorldAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task ListMaps_Throws_WhenUserHasNoAccess()
    {
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _sut.ListMapsForWorldAsync(_worldId, _outsiderId));
    }

    [Fact]
    public async Task ListMaps_ReturnsSortedMapsWithCorrectScopes()
    {
        // World-scoped: no associations
        var mapA = new WorldMap { WorldMapId = Guid.NewGuid(), WorldId = _worldId, Name = "Zebra Map", CreatedUtc = DateTime.UtcNow, UpdatedUtc = DateTime.UtcNow };

        // Campaign-scoped
        var campaignId = Guid.NewGuid();
        var mapB = new WorldMap { WorldMapId = Guid.NewGuid(), WorldId = _worldId, Name = "Alpha Map", CreatedUtc = DateTime.UtcNow, UpdatedUtc = DateTime.UtcNow };
        var wmCampaign = new WorldMapCampaign { WorldMapId = mapB.WorldMapId, CampaignId = campaignId };

        // Arc-scoped
        var arcId = Guid.NewGuid();
        var mapC = new WorldMap { WorldMapId = Guid.NewGuid(), WorldId = _worldId, Name = "Middle Map", CreatedUtc = DateTime.UtcNow, UpdatedUtc = DateTime.UtcNow };
        var wmArc = new WorldMapArc { WorldMapId = mapC.WorldMapId, ArcId = arcId };

        _db.WorldMaps.AddRange(mapA, mapB, mapC);
        _db.WorldMapCampaigns.Add(wmCampaign);
        _db.WorldMapArcs.Add(wmArc);
        await _db.SaveChangesAsync();

        var result = await _sut.ListMapsForWorldAsync(_worldId, _ownerId);

        // Sorted by name: Alpha, Middle, Zebra
        Assert.Equal(3, result.Count);
        Assert.Equal("Alpha Map", result[0].Name);
        Assert.Equal("Middle Map", result[1].Name);
        Assert.Equal("Zebra Map", result[2].Name);

        Assert.Equal(MapScope.CampaignScoped, result[0].Scope);
        Assert.Contains(campaignId, result[0].CampaignIds);

        Assert.Equal(MapScope.ArcScoped, result[1].Scope);
        Assert.Contains(arcId, result[1].ArcIds);

        Assert.Equal(MapScope.WorldScoped, result[2].Scope);
    }

    // ── Pins (P1) ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreatePin_SelectsArcLayer_WhenMapHasArcAssociation()
    {
        var map = await CreateMapWithDefaultLayersAsync("Arc Scoped");
        var layers = await GetMapLayersByNameAsync(map.WorldMapId);

        var campaignId = await CreateCampaignAsync(_worldId);
        var arcId = await CreateArcAsync(campaignId);
        _db.WorldMapCampaigns.Add(new WorldMapCampaign { WorldMapId = map.WorldMapId, CampaignId = campaignId });
        _db.WorldMapArcs.Add(new WorldMapArc { WorldMapId = map.WorldMapId, ArcId = arcId });
        await _db.SaveChangesAsync();

        var created = await _sut.CreatePinAsync(
            _worldId,
            map.WorldMapId,
            _memberId,
            new MapPinCreateDto { X = 0.25f, Y = 0.75f });

        Assert.Equal(layers["Arc"], created.LayerId);
    }

    [Fact]
    public async Task CreatePin_SelectsCampaignLayer_WhenMapHasCampaignAssociationButNoArc()
    {
        var map = await CreateMapWithDefaultLayersAsync("Campaign Scoped");
        var layers = await GetMapLayersByNameAsync(map.WorldMapId);

        var campaignId = await CreateCampaignAsync(_worldId);
        _db.WorldMapCampaigns.Add(new WorldMapCampaign { WorldMapId = map.WorldMapId, CampaignId = campaignId });
        await _db.SaveChangesAsync();

        var created = await _sut.CreatePinAsync(
            _worldId,
            map.WorldMapId,
            _memberId,
            new MapPinCreateDto { X = 0.10f, Y = 0.20f });

        Assert.Equal(layers["Campaign"], created.LayerId);
    }

    [Fact]
    public async Task CreatePin_SelectsWorldLayer_WhenMapHasNoCampaignOrArcAssociations()
    {
        var map = await CreateMapWithDefaultLayersAsync("World Scoped");
        var layers = await GetMapLayersByNameAsync(map.WorldMapId);

        var created = await _sut.CreatePinAsync(
            _worldId,
            map.WorldMapId,
            _memberId,
            new MapPinCreateDto { X = 0.60f, Y = 0.40f });

        Assert.Equal(layers["World"], created.LayerId);
    }

    [Fact]
    public async Task CreatePin_Throws_WhenUserIsNotWorldMember()
    {
        var map = await CreateMapWithDefaultLayersAsync("No Access");

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _sut.CreatePinAsync(
                _worldId,
                map.WorldMapId,
                _outsiderId,
                new MapPinCreateDto { X = 0.5f, Y = 0.5f }));
    }

    [Fact]
    public async Task CreatePin_Throws_WhenMapNotFound()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.CreatePinAsync(
                _worldId,
                Guid.NewGuid(),
                _memberId,
                new MapPinCreateDto { X = 0.5f, Y = 0.5f }));
    }

    [Fact]
    public async Task CreatePin_Throws_WhenDefaultLayerMissing()
    {
        var map = await CreateMapWithDefaultLayersAsync("Missing Layer");
        _db.MapLayers.RemoveRange(_db.MapLayers.Where(l => l.WorldMapId == map.WorldMapId && l.Name == "World"));
        await _db.SaveChangesAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.CreatePinAsync(
                _worldId,
                map.WorldMapId,
                _memberId,
                new MapPinCreateDto { X = 0.5f, Y = 0.5f }));
    }

    public static IEnumerable<object[]> InvalidCoordinates =>
    [
        [ -0.001f, 0.5f ],
        [ 1.001f, 0.5f ],
        [ 0.5f, -0.001f ],
        [ 0.5f, 1.001f ],
        [ float.NaN, 0.5f ],
        [ 0.5f, float.NaN ],
        [ float.PositiveInfinity, 0.5f ],
        [ 0.5f, float.NegativeInfinity ],
    ];

    [Theory]
    [MemberData(nameof(InvalidCoordinates))]
    public async Task CreatePin_Throws_WhenCoordinatesInvalid_AndDoesNotWriteToDb(float x, float y)
    {
        var map = await CreateMapWithDefaultLayersAsync("Validate Create");
        var beforeCount = await _db.MapFeatures.CountAsync();

        await Assert.ThrowsAsync<ArgumentException>(
            () => _sut.CreatePinAsync(
                _worldId,
                map.WorldMapId,
                _memberId,
                new MapPinCreateDto { X = x, Y = y }));

        var afterCount = await _db.MapFeatures.CountAsync();
        Assert.Equal(beforeCount, afterCount);
    }

    [Fact]
    public async Task CreatePin_WithLinkedArticleIdButMissingArticle_ReturnsNullLinkedArticle()
    {
        var map = await CreateMapWithDefaultLayersAsync("Linked Missing");
        var missingArticleId = Guid.NewGuid();

        var created = await _sut.CreatePinAsync(
            _worldId,
            map.WorldMapId,
            _memberId,
            new MapPinCreateDto { X = 0.2f, Y = 0.8f, LinkedArticleId = missingArticleId });

        Assert.Null(created.LinkedArticle);
    }

    [Fact]
    public async Task ListPinsForMap_Throws_WhenUserIsNotWorldMember()
    {
        var map = await CreateMapWithDefaultLayersAsync("List Access");

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _sut.ListPinsForMapAsync(_worldId, map.WorldMapId, _outsiderId));
    }

    [Fact]
    public async Task ListPinsForMap_Throws_WhenMapNotInWorld()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.ListPinsForMapAsync(_worldId, Guid.NewGuid(), _memberId));
    }

    [Fact]
    public async Task UpdatePinPosition_Throws_WhenUserIsNotWorldMember()
    {
        var map = await CreateMapWithDefaultLayersAsync("Update Access");
        var pin = await _sut.CreatePinAsync(
            _worldId,
            map.WorldMapId,
            _memberId,
            new MapPinCreateDto { X = 0.2f, Y = 0.3f });

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _sut.UpdatePinPositionAsync(
                _worldId,
                map.WorldMapId,
                pin.PinId,
                _outsiderId,
                new MapPinPositionUpdateDto { X = 0.4f, Y = 0.5f }));
    }

    [Fact]
    public async Task UpdatePinPosition_Throws_WhenPinNotFound()
    {
        var map = await CreateMapWithDefaultLayersAsync("Update Missing Pin");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.UpdatePinPositionAsync(
                _worldId,
                map.WorldMapId,
                Guid.NewGuid(),
                _memberId,
                new MapPinPositionUpdateDto { X = 0.4f, Y = 0.5f }));
    }

    [Theory]
    [MemberData(nameof(InvalidCoordinates))]
    public async Task UpdatePinPosition_Throws_WhenCoordinatesInvalid_AndDoesNotMutateDb(float x, float y)
    {
        var map = await CreateMapWithDefaultLayersAsync("Validate Update");
        var pin = await _sut.CreatePinAsync(
            _worldId,
            map.WorldMapId,
            _memberId,
            new MapPinCreateDto { X = 0.3f, Y = 0.6f });
        var before = await _db.MapFeatures.AsNoTracking().FirstAsync(mf => mf.MapFeatureId == pin.PinId);

        await Assert.ThrowsAsync<ArgumentException>(
            () => _sut.UpdatePinPositionAsync(
                _worldId,
                map.WorldMapId,
                pin.PinId,
                _memberId,
                new MapPinPositionUpdateDto { X = x, Y = y }));

        var after = await _db.MapFeatures.AsNoTracking().FirstAsync(mf => mf.MapFeatureId == pin.PinId);
        Assert.Equal(before.X, after.X);
        Assert.Equal(before.Y, after.Y);
    }

    [Fact]
    public async Task DeletePin_Throws_WhenUserIsNotWorldMember()
    {
        var map = await CreateMapWithDefaultLayersAsync("Delete Access");
        var pin = await _sut.CreatePinAsync(
            _worldId,
            map.WorldMapId,
            _memberId,
            new MapPinCreateDto { X = 0.2f, Y = 0.3f });

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _sut.DeletePinAsync(_worldId, map.WorldMapId, pin.PinId, _outsiderId));
    }

    [Fact]
    public async Task DeletePin_Throws_WhenPinNotFound()
    {
        var map = await CreateMapWithDefaultLayersAsync("Delete Missing Pin");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.DeletePinAsync(_worldId, map.WorldMapId, Guid.NewGuid(), _memberId));
    }

    [Fact]
    public async Task PinCrud_HappyPath_IsDeterministicAndPersistsExpectedState()
    {
        var map = await CreateMapWithDefaultLayersAsync("CRUD");
        var article = await CreateArticleAsync(_worldId, "Waterdeep");

        var createdA = await _sut.CreatePinAsync(
            _worldId,
            map.WorldMapId,
            _memberId,
            new MapPinCreateDto { X = 0.10f, Y = 0.20f, LinkedArticleId = article.Id });
        var createdB = await _sut.CreatePinAsync(
            _worldId,
            map.WorldMapId,
            _memberId,
            new MapPinCreateDto { X = 0.80f, Y = 0.70f });
        var createdC = await _sut.CreatePinAsync(
            _worldId,
            map.WorldMapId,
            _memberId,
            new MapPinCreateDto { X = 0.40f, Y = 0.50f });

        var listed = await _sut.ListPinsForMapAsync(_worldId, map.WorldMapId, _memberId);
        var actualOrder = listed.Select(p => p.PinId).ToList();
        var expectedOrder = actualOrder.OrderBy(id => id).ToList();
        Assert.Equal(expectedOrder, actualOrder);

        var linkedPin = listed.Single(p => p.PinId == createdA.PinId);
        Assert.NotNull(linkedPin.LinkedArticle);
        Assert.Equal(article.Id, linkedPin.LinkedArticle!.ArticleId);
        Assert.Equal("Waterdeep", linkedPin.LinkedArticle.Title);

        var updated = await _sut.UpdatePinPositionAsync(
            _worldId,
            map.WorldMapId,
            createdB.PinId,
            _memberId,
            new MapPinPositionUpdateDto { X = 0.33f, Y = 0.66f });

        Assert.Equal(0.33f, updated.X);
        Assert.Equal(0.66f, updated.Y);

        var updatedEntity = await _db.MapFeatures.AsNoTracking().FirstAsync(mf => mf.MapFeatureId == createdB.PinId);
        Assert.Equal(0.33f, updatedEntity.X);
        Assert.Equal(0.66f, updatedEntity.Y);

        await _sut.DeletePinAsync(_worldId, map.WorldMapId, createdC.PinId, _memberId);

        Assert.False(await _db.MapFeatures.AnyAsync(mf => mf.MapFeatureId == createdC.PinId));
        var remainingPins = await _sut.ListPinsForMapAsync(_worldId, map.WorldMapId, _memberId);
        Assert.Equal(2, remainingPins.Count);
    }

    // ── RequestBasemapUploadAsync ─────────────────────────────────────────────

    [Fact]
    public async Task RequestBasemapUpload_Throws_WhenContentTypeInvalid()
    {
        var map = await CreateTestMap("Upload Map");
        var dto = new RequestBasemapUploadDto { FileName = "map.bmp", ContentType = "image/bmp" };

        await Assert.ThrowsAsync<ArgumentException>(
            () => _sut.RequestBasemapUploadAsync(_worldId, map.WorldMapId, _ownerId, dto));
    }

    [Fact]
    public async Task RequestBasemapUpload_Throws_WhenMapNotFound()
    {
        var dto = new RequestBasemapUploadDto { FileName = "map.png", ContentType = "image/png" };

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.RequestBasemapUploadAsync(_worldId, Guid.NewGuid(), _ownerId, dto));
    }

    [Fact]
    public async Task RequestBasemapUpload_Throws_WhenUserIsNotOwner()
    {
        var map = await CreateTestMap("Upload Map");
        var dto = new RequestBasemapUploadDto { FileName = "map.png", ContentType = "image/png" };

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _sut.RequestBasemapUploadAsync(_worldId, map.WorldMapId, _memberId, dto));
    }

    [Fact]
    public async Task RequestBasemapUpload_Success_StoresBlobKeyAndReturnsSasUrl()
    {
        var map = await CreateTestMap("Upload Map");
        var dto = new RequestBasemapUploadDto { FileName = "faerûn.png", ContentType = "image/png" };
        var expectedSas = "https://blob.example.com/sas-url";
        var expectedKey = $"maps/{map.WorldMapId}/basemap/faerûn.png";

        _blobStore.GenerateUploadSasUrlAsync(map.WorldMapId, dto.FileName, dto.ContentType)
            .Returns(Task.FromResult(expectedSas));
        _blobStore.BuildBasemapBlobKey(map.WorldMapId, dto.FileName)
            .Returns(expectedKey);

        var result = await _sut.RequestBasemapUploadAsync(_worldId, map.WorldMapId, _ownerId, dto);

        Assert.Equal(expectedSas, result.UploadUrl);

        var updated = await _db.WorldMaps.FindAsync(map.WorldMapId);
        Assert.Equal(expectedKey, updated!.BasemapBlobKey);
        Assert.Equal("faerûn.png", updated.BasemapOriginalFilename);
        Assert.Equal("image/png", updated.BasemapContentType);
    }

    // ── ConfirmBasemapUploadAsync ─────────────────────────────────────────────

    [Fact]
    public async Task ConfirmBasemapUpload_Throws_WhenMapNotFound()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.ConfirmBasemapUploadAsync(_worldId, Guid.NewGuid(), _ownerId));
    }

    [Fact]
    public async Task ConfirmBasemapUpload_Throws_WhenUserIsNotOwner()
    {
        var map = await CreateTestMap("Confirm Map");

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _sut.ConfirmBasemapUploadAsync(_worldId, map.WorldMapId, _memberId));
    }

    [Fact]
    public async Task ConfirmBasemapUpload_Throws_WhenNoBlobKeyStored()
    {
        var map = await CreateTestMap("Confirm Map");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.ConfirmBasemapUploadAsync(_worldId, map.WorldMapId, _ownerId));
    }

    [Fact]
    public async Task ConfirmBasemapUpload_Success_ReturnsMapDto()
    {
        var map = await CreateTestMap("Confirm Map");
        map.BasemapBlobKey = "maps/some-key/basemap/img.png";
        map.BasemapContentType = "image/png";
        map.BasemapOriginalFilename = "img.png";
        await _db.SaveChangesAsync();

        var result = await _sut.ConfirmBasemapUploadAsync(_worldId, map.WorldMapId, _ownerId);

        Assert.Equal("Confirm Map", result.Name);
        Assert.True(result.HasBasemap);
    }

    // ── GetBasemapReadUrlAsync ───────────────────────────────────────────────

    [Fact]
    public async Task GetBasemapReadUrl_Throws_WhenMapNotFound()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.GetBasemapReadUrlAsync(_worldId, Guid.NewGuid(), _ownerId));
    }

    [Fact]
    public async Task GetBasemapReadUrl_Throws_WhenUserHasNoAccess()
    {
        var map = await CreateTestMap("No Access Map");
        map.BasemapBlobKey = "maps/key/basemap/map.png";
        await _db.SaveChangesAsync();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _sut.GetBasemapReadUrlAsync(_worldId, map.WorldMapId, _outsiderId));
    }

    [Fact]
    public async Task GetBasemapReadUrl_Throws_WhenBasemapMissing()
    {
        var map = await CreateTestMap("Missing Basemap");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.GetBasemapReadUrlAsync(_worldId, map.WorldMapId, _ownerId));
    }

    [Fact]
    public async Task GetBasemapReadUrl_Success_ReturnsReadSasUrl()
    {
        var map = await CreateTestMap("With Basemap");
        map.BasemapBlobKey = $"maps/{map.WorldMapId}/basemap/world.png";
        await _db.SaveChangesAsync();

        const string expectedReadUrl = "https://blob.example.com/read-sas-url";
        _blobStore.GenerateReadSasUrlAsync(map.BasemapBlobKey)
            .Returns(Task.FromResult(expectedReadUrl));

        var result = await _sut.GetBasemapReadUrlAsync(_worldId, map.WorldMapId, _ownerId);

        Assert.Equal(expectedReadUrl, result.ReadUrl);
    }

    // ── DeleteMapAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteMap_Throws_WhenMapNotFound()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.DeleteMapAsync(_worldId, Guid.NewGuid(), _ownerId));
    }

    [Fact]
    public async Task DeleteMap_Throws_WhenUserIsNotOwner()
    {
        var map = await CreateTestMap("Protected");

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _sut.DeleteMapAsync(_worldId, map.WorldMapId, _memberId));
    }

    [Fact]
    public async Task DeleteMap_Success_DeletesMapAndRelatedMetadataAndBlobFolder()
    {
        var campaignId = Guid.NewGuid();
        var arcId = Guid.NewGuid();
        var mapId = Guid.NewGuid();

        var campaign = new Campaign
        {
            Id = campaignId,
            WorldId = _worldId,
            OwnerId = _ownerId,
            Name = "Campaign",
            CreatedAt = DateTime.UtcNow,
        };

        var arc = new Arc
        {
            Id = arcId,
            CampaignId = campaignId,
            Name = "Arc",
            SortOrder = 0,
            CreatedBy = _ownerId,
            CreatedAt = DateTime.UtcNow,
        };

        var map = new WorldMap
        {
            WorldMapId = mapId,
            WorldId = _worldId,
            Name = "Delete Me",
            CreatedUtc = DateTime.UtcNow,
            UpdatedUtc = DateTime.UtcNow,
            BasemapBlobKey = $"maps/{mapId}/basemap/delete-me.png",
        };

        _db.Campaigns.Add(campaign);
        _db.Arcs.Add(arc);
        _db.WorldMaps.Add(map);
        _db.MapLayers.Add(new MapLayer
        {
            MapLayerId = Guid.NewGuid(),
            WorldMapId = mapId,
            Name = "World",
            SortOrder = 0,
        });
        _db.WorldMapCampaigns.Add(new WorldMapCampaign { WorldMapId = mapId, CampaignId = campaignId });
        _db.WorldMapArcs.Add(new WorldMapArc { WorldMapId = mapId, ArcId = arcId });
        await _db.SaveChangesAsync();

        await _sut.DeleteMapAsync(_worldId, mapId, _ownerId);

        await _blobStore.Received(1).DeleteMapFolderAsync(mapId);
        Assert.False(await _db.WorldMaps.AnyAsync(m => m.WorldMapId == mapId));
        Assert.False(await _db.MapLayers.AnyAsync(l => l.WorldMapId == mapId));
        Assert.False(await _db.WorldMapCampaigns.AnyAsync(c => c.WorldMapId == mapId));
        Assert.False(await _db.WorldMapArcs.AnyAsync(a => a.WorldMapId == mapId));
    }

    // ── ComputeScope (internal static) ────────────────────────────────────────

    [Fact]
    public void ComputeScope_ReturnsWorldScoped_WhenNoAssociations()
    {
        var map = new WorldMap
        {
            WorldMapId = Guid.NewGuid(),
            WorldMapCampaigns = new List<WorldMapCampaign>(),
            WorldMapArcs = new List<WorldMapArc>(),
        };

        Assert.Equal(MapScope.WorldScoped, WorldMapService.ComputeScope(map));
    }

    [Fact]
    public void ComputeScope_ReturnsCampaignScoped_WhenOnlyCampaignAssociation()
    {
        var mapId = Guid.NewGuid();
        var map = new WorldMap
        {
            WorldMapId = mapId,
            WorldMapCampaigns = new List<WorldMapCampaign> { new() { WorldMapId = mapId, CampaignId = Guid.NewGuid() } },
            WorldMapArcs = new List<WorldMapArc>(),
        };

        Assert.Equal(MapScope.CampaignScoped, WorldMapService.ComputeScope(map));
    }

    [Fact]
    public void ComputeScope_ReturnsArcScoped_WhenArcAssociationPresent()
    {
        var mapId = Guid.NewGuid();
        var map = new WorldMap
        {
            WorldMapId = mapId,
            WorldMapCampaigns = new List<WorldMapCampaign>(),
            WorldMapArcs = new List<WorldMapArc> { new() { WorldMapId = mapId, ArcId = Guid.NewGuid() } },
        };

        Assert.Equal(MapScope.ArcScoped, WorldMapService.ComputeScope(map));
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<WorldMap> CreateTestMap(string name)
    {
        var map = new WorldMap
        {
            WorldMapId = Guid.NewGuid(),
            WorldId = _worldId,
            Name = name,
            CreatedUtc = DateTime.UtcNow,
            UpdatedUtc = DateTime.UtcNow,
        };
        _db.WorldMaps.Add(map);
        await _db.SaveChangesAsync();
        return map;
    }

    private async Task<WorldMap> CreateMapWithDefaultLayersAsync(string name)
    {
        var created = await _sut.CreateMapAsync(_worldId, _ownerId, new MapCreateDto { Name = name });
        var map = await _db.WorldMaps.FirstAsync(m => m.WorldMapId == created.WorldMapId);
        return map;
    }

    private async Task<Dictionary<string, Guid>> GetMapLayersByNameAsync(Guid mapId)
    {
        return await _db.MapLayers
            .Where(l => l.WorldMapId == mapId)
            .ToDictionaryAsync(l => l.Name, l => l.MapLayerId);
    }

    private async Task<Guid> CreateCampaignAsync(Guid worldId)
    {
        var campaign = new Campaign
        {
            Id = Guid.NewGuid(),
            WorldId = worldId,
            OwnerId = _ownerId,
            Name = "Campaign Scope",
            CreatedAt = DateTime.UtcNow,
        };
        _db.Campaigns.Add(campaign);
        await _db.SaveChangesAsync();
        return campaign.Id;
    }

    private async Task<Guid> CreateArcAsync(Guid campaignId)
    {
        var arc = new Arc
        {
            Id = Guid.NewGuid(),
            CampaignId = campaignId,
            Name = "Arc Scope",
            SortOrder = 0,
            CreatedBy = _ownerId,
            CreatedAt = DateTime.UtcNow,
        };
        _db.Arcs.Add(arc);
        await _db.SaveChangesAsync();
        return arc.Id;
    }

    private async Task<Article> CreateArticleAsync(Guid worldId, string title)
    {
        var article = new Article
        {
            Id = Guid.NewGuid(),
            WorldId = worldId,
            Title = title,
            Slug = title.ToLowerInvariant().Replace(' ', '-'),
            Type = ArticleType.WikiArticle,
            Visibility = ArticleVisibility.Public,
            CreatedBy = _ownerId,
            CreatedAt = DateTime.UtcNow,
            EffectiveDate = DateTime.UtcNow,
        };
        _db.Articles.Add(article);
        await _db.SaveChangesAsync();
        return article;
    }
}
