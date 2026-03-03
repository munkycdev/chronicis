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
}
