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
        _blobStore.BuildFeatureGeometryBlobKey(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>())
            .Returns(callInfo =>
            {
                var mapId = callInfo.ArgAt<Guid>(0);
                var layerId = callInfo.ArgAt<Guid>(1);
                var featureId = callInfo.ArgAt<Guid>(2);
                return $"maps/{mapId}/layers/{layerId}/features/{featureId}.geojson.gz";
            });
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

    // ── ListLayersForMapAsync ────────────────────────────────────────────────

    [Fact]
    public async Task ListLayersForMap_ReturnsOrderedLayersWithVisibility()
    {
        var map = await CreateMapWithDefaultLayersAsync("Layered Map");
        var worldLayer = await _db.MapLayers.FirstAsync(l => l.WorldMapId == map.WorldMapId && l.Name == "World");
        worldLayer.IsEnabled = true;
        await _db.SaveChangesAsync();

        var result = await _sut.ListLayersForMapAsync(_worldId, map.WorldMapId, _memberId);

        Assert.Equal(3, result.Count);
        Assert.Equal("World", result[0].Name);
        Assert.Equal("Campaign", result[1].Name);
        Assert.Equal("Arc", result[2].Name);
        Assert.True(result[0].IsEnabled);
        Assert.False(result[1].IsEnabled);
        Assert.False(result[2].IsEnabled);
    }

    [Fact]
    public async Task ListLayersForMap_Throws_WhenUserIsNotWorldMember()
    {
        var map = await CreateMapWithDefaultLayersAsync("No Access Layers");

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _sut.ListLayersForMapAsync(_worldId, map.WorldMapId, _outsiderId));
    }

    [Fact]
    public async Task ListLayersForMap_Throws_WhenMapNotFound()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.ListLayersForMapAsync(_worldId, Guid.NewGuid(), _memberId));
    }

    [Fact]
    public async Task CreateLayer_Success_AddsEnabledLayerWithNextSortOrder()
    {
        var map = await CreateMapWithDefaultLayersAsync("Custom Layer Map");
        var service = (IWorldMapService)_sut;

        var created = await service.CreateLayer(
            _worldId,
            map.WorldMapId,
            _memberId,
            "  Cities  ");

        Assert.Equal("Cities", created.Name);
        Assert.Equal(3, created.SortOrder);
        Assert.True(created.IsEnabled);

        var layers = await _db.MapLayers
            .AsNoTracking()
            .Where(layer => layer.WorldMapId == map.WorldMapId)
            .OrderBy(layer => layer.SortOrder)
            .ToListAsync();

        Assert.Equal(4, layers.Count);
        Assert.Equal("Cities", layers[3].Name);
        Assert.Equal(3, layers[3].SortOrder);
        Assert.Null(layers[3].ParentLayerId);
        Assert.True(layers[3].IsEnabled);
    }

    [Fact]
    public async Task CreateLayer_WithParent_CreatesChild_WithSiblingLocalSortOrder()
    {
        var map = await CreateMapWithDefaultLayersAsync("Child Layer Map");
        var parent = await _sut.CreateLayerAsync(_worldId, map.WorldMapId, _memberId, "Parent");
        _ = await _sut.CreateLayerAsync(_worldId, map.WorldMapId, _memberId, "Child A", parent.MapLayerId);
        _ = await _sut.CreateLayerAsync(_worldId, map.WorldMapId, _memberId, "Child B", parent.MapLayerId);
        var otherParent = await _sut.CreateLayerAsync(_worldId, map.WorldMapId, _memberId, "Other Parent");
        _ = await _sut.CreateLayerAsync(_worldId, map.WorldMapId, _memberId, "Other Child", otherParent.MapLayerId);
        _ = await _sut.CreateLayerAsync(_worldId, map.WorldMapId, _memberId, "Root Peer");

        var createdChild = await _sut.CreateLayerAsync(_worldId, map.WorldMapId, _memberId, "Child C", parent.MapLayerId);

        Assert.Equal(parent.MapLayerId, createdChild.ParentLayerId);
        Assert.Equal(2, createdChild.SortOrder);
        Assert.True(createdChild.IsEnabled);
    }

    [Fact]
    public async Task CreateLayer_WithParent_WhenNoSiblings_UsesRootBaselineForFirstSortOrder()
    {
        var map = await CreateMapWithDefaultLayersAsync("First Child Sort Baseline");
        var parent = await _sut.CreateLayerAsync(_worldId, map.WorldMapId, _memberId, "Parent");

        var createdChild = await _sut.CreateLayerAsync(_worldId, map.WorldMapId, _memberId, "First Child", parent.MapLayerId);

        Assert.Equal(0, createdChild.SortOrder);
    }

    [Fact]
    public async Task CreateLayer_WithParent_RejectsWhenParentDoesNotExist()
    {
        var map = await CreateMapWithDefaultLayersAsync("Missing Parent");

        await Assert.ThrowsAsync<ArgumentException>(
            () => _sut.CreateLayerAsync(
                _worldId,
                map.WorldMapId,
                _memberId,
                "Child",
                Guid.NewGuid()));
    }

    [Fact]
    public async Task CreateLayer_WithParent_RejectsWhenParentIsInDifferentMap()
    {
        var firstMap = await CreateMapWithDefaultLayersAsync("Create Child A");
        var secondMap = await CreateMapWithDefaultLayersAsync("Create Child B");
        var parentInOtherMap = await _sut.CreateLayerAsync(_worldId, secondMap.WorldMapId, _memberId, "Foreign Parent");

        await Assert.ThrowsAsync<ArgumentException>(
            () => _sut.CreateLayerAsync(
                _worldId,
                firstMap.WorldMapId,
                _memberId,
                "Child",
                parentInOtherMap.MapLayerId));
    }

    [Fact]
    public async Task CreateLayer_WithDisabledParent_CreatesEnabledChild()
    {
        var map = await CreateMapWithDefaultLayersAsync("Disabled Parent Child Create");
        var parent = await _sut.CreateLayerAsync(_worldId, map.WorldMapId, _memberId, "Parent");
        await _sut.UpdateLayerVisibilityAsync(_worldId, map.WorldMapId, parent.MapLayerId, _memberId, false);

        var createdChild = await _sut.CreateLayerAsync(_worldId, map.WorldMapId, _memberId, "Child", parent.MapLayerId);

        Assert.True(createdChild.IsEnabled);
        var persistedChild = await _db.MapLayers.AsNoTracking().FirstAsync(layer => layer.MapLayerId == createdChild.MapLayerId);
        Assert.True(persistedChild.IsEnabled);
    }

    [Fact]
    public async Task CreateLayer_ThrowsArgumentException_WhenLayerNameAlreadyExistsInMap()
    {
        var map = await CreateMapWithDefaultLayersAsync("Duplicate Layer Name Map");

        _ = await _sut.CreateLayerAsync(
            _worldId,
            map.WorldMapId,
            _memberId,
            "Cities");

        await Assert.ThrowsAsync<ArgumentException>(
            () => _sut.CreateLayerAsync(
                _worldId,
                map.WorldMapId,
                _memberId,
                "  Cities  "));
    }

    [Fact]
    public async Task CreateLayer_ThrowsArgumentException_WhenLayerNameInvalid()
    {
        var map = await CreateMapWithDefaultLayersAsync("Invalid Layer Name Map");

        await Assert.ThrowsAsync<ArgumentException>(
            () => _sut.CreateLayerAsync(
                _worldId,
                map.WorldMapId,
                _memberId,
                "   "));
    }

    [Fact]
    public async Task CreateLayer_ThrowsArgumentException_WhenLayerNameExceedsMaxLength()
    {
        var map = await CreateMapWithDefaultLayersAsync("Too Long Layer Name Map");

        await Assert.ThrowsAsync<ArgumentException>(
            () => _sut.CreateLayerAsync(
                _worldId,
                map.WorldMapId,
                _memberId,
                new string('L', 201)));
    }

    [Fact]
    public async Task CreateLayer_ThrowsArgumentException_WhenMapDoesNotExist()
    {
        await Assert.ThrowsAsync<ArgumentException>(
            () => _sut.CreateLayerAsync(
                _worldId,
                Guid.NewGuid(),
                _memberId,
                "Cities"));
    }

    [Fact]
    public async Task CreateLayer_ThrowsUnauthorizedAccessException_WhenUserNotWorldMember()
    {
        var map = await CreateMapWithDefaultLayersAsync("Unauthorized Layer Create");

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _sut.CreateLayerAsync(
                _worldId,
                map.WorldMapId,
                _outsiderId,
                "Cities"));
    }

    [Fact]
    public async Task RenameLayer_Succeeds_ForCustomLayer()
    {
        var map = await CreateMapWithDefaultLayersAsync("Rename Custom Layer");
        var customLayer = await _sut.CreateLayerAsync(
            _worldId,
            map.WorldMapId,
            _memberId,
            "Cities");

        var service = (IWorldMapService)_sut;
        await service.RenameLayer(_worldId, map.WorldMapId, _memberId, customLayer.MapLayerId, "Settlements");

        var renamed = await _db.MapLayers
            .AsNoTracking()
            .FirstAsync(layer => layer.MapLayerId == customLayer.MapLayerId);

        Assert.Equal("Settlements", renamed.Name);
    }

    [Fact]
    public async Task RenameLayer_ThrowsArgumentException_ForDefaultLayer()
    {
        var map = await CreateMapWithDefaultLayersAsync("Rename Default Layer");
        var worldLayer = await _db.MapLayers
            .AsNoTracking()
            .FirstAsync(layer => layer.WorldMapId == map.WorldMapId && layer.Name == "World");

        var service = (IWorldMapService)_sut;
        await Assert.ThrowsAsync<ArgumentException>(
            () => service.RenameLayer(_worldId, map.WorldMapId, _memberId, worldLayer.MapLayerId, "Renamed World"));
    }

    [Fact]
    public async Task RenameLayer_ThrowsArgumentException_WhenMapDoesNotExist()
    {
        var service = (IWorldMapService)_sut;
        await Assert.ThrowsAsync<ArgumentException>(
            () => service.RenameLayer(_worldId, Guid.NewGuid(), _memberId, Guid.NewGuid(), "Settlements"));
    }

    [Fact]
    public async Task RenameLayer_ThrowsArgumentException_WhenLayerDoesNotExist()
    {
        var map = await CreateMapWithDefaultLayersAsync("Rename Missing Layer");
        var service = (IWorldMapService)_sut;

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.RenameLayer(_worldId, map.WorldMapId, _memberId, Guid.NewGuid(), "Settlements"));
    }

    [Fact]
    public async Task RenameLayer_ThrowsArgumentException_WhenLayerBelongsToDifferentMap()
    {
        var firstMap = await CreateMapWithDefaultLayersAsync("Rename Source Map");
        var secondMap = await CreateMapWithDefaultLayersAsync("Rename Target Map");
        var customLayer = await _sut.CreateLayerAsync(
            _worldId,
            firstMap.WorldMapId,
            _memberId,
            "Cities");
        var service = (IWorldMapService)_sut;

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.RenameLayer(_worldId, secondMap.WorldMapId, _memberId, customLayer.MapLayerId, "Settlements"));
    }

    [Fact]
    public async Task RenameLayer_ThrowsUnauthorizedAccessException_WhenUserNotWorldMember()
    {
        var map = await CreateMapWithDefaultLayersAsync("Rename Unauthorized");
        var customLayer = await _sut.CreateLayerAsync(
            _worldId,
            map.WorldMapId,
            _memberId,
            "Cities");
        var service = (IWorldMapService)_sut;

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => service.RenameLayer(_worldId, map.WorldMapId, _outsiderId, customLayer.MapLayerId, "Settlements"));
    }

    [Fact]
    public async Task SetLayerParent_Success_AssignsParent_WithoutMutatingOtherFields()
    {
        var map = await CreateMapWithDefaultLayersAsync("Set Parent Success");
        var parent = await _sut.CreateLayerAsync(_worldId, map.WorldMapId, _memberId, "Parent Layer");
        var child = await _sut.CreateLayerAsync(_worldId, map.WorldMapId, _memberId, "Child Layer");

        var before = await _db.MapLayers.AsNoTracking().FirstAsync(layer => layer.MapLayerId == child.MapLayerId);

        var service = (IWorldMapService)_sut;
        await service.SetLayerParentAsync(_worldId, map.WorldMapId, _memberId, child.MapLayerId, parent.MapLayerId);

        var updated = await _db.MapLayers.AsNoTracking().FirstAsync(layer => layer.MapLayerId == child.MapLayerId);
        Assert.Equal(parent.MapLayerId, updated.ParentLayerId);
        Assert.Equal(before.Name, updated.Name);
        Assert.Equal(before.SortOrder, updated.SortOrder);
        Assert.Equal(before.IsEnabled, updated.IsEnabled);
        Assert.Equal(before.WorldMapId, updated.WorldMapId);
    }

    [Fact]
    public async Task SetLayerParent_Success_ClearsParent()
    {
        var map = await CreateMapWithDefaultLayersAsync("Clear Parent Success");
        var parent = await _sut.CreateLayerAsync(_worldId, map.WorldMapId, _memberId, "Parent");
        var child = await _sut.CreateLayerAsync(_worldId, map.WorldMapId, _memberId, "Child");

        var service = (IWorldMapService)_sut;
        await service.SetLayerParentAsync(_worldId, map.WorldMapId, _memberId, child.MapLayerId, parent.MapLayerId);
        await service.SetLayerParentAsync(_worldId, map.WorldMapId, _memberId, child.MapLayerId, null);

        var updated = await _db.MapLayers.AsNoTracking().FirstAsync(layer => layer.MapLayerId == child.MapLayerId);
        Assert.Null(updated.ParentLayerId);
    }

    [Fact]
    public async Task SetLayerParent_Success_NoOp_WhenParentUnchanged()
    {
        var map = await CreateMapWithDefaultLayersAsync("Set Parent No-Op");
        var parent = await _sut.CreateLayerAsync(_worldId, map.WorldMapId, _memberId, "Parent");
        var child = await _sut.CreateLayerAsync(_worldId, map.WorldMapId, _memberId, "Child");

        var service = (IWorldMapService)_sut;
        await service.SetLayerParentAsync(_worldId, map.WorldMapId, _memberId, child.MapLayerId, parent.MapLayerId);
        var before = await _db.MapLayers.AsNoTracking().FirstAsync(layer => layer.MapLayerId == child.MapLayerId);

        await service.SetLayerParentAsync(_worldId, map.WorldMapId, _memberId, child.MapLayerId, parent.MapLayerId);

        var after = await _db.MapLayers.AsNoTracking().FirstAsync(layer => layer.MapLayerId == child.MapLayerId);
        Assert.Equal(before.ParentLayerId, after.ParentLayerId);
        Assert.Equal(before.Name, after.Name);
        Assert.Equal(before.SortOrder, after.SortOrder);
        Assert.Equal(before.IsEnabled, after.IsEnabled);
    }

    [Fact]
    public async Task SetLayerParent_Success_NoOp_WhenAlreadyRootAndCleared()
    {
        var map = await CreateMapWithDefaultLayersAsync("Set Root No-Op");
        var layer = await _sut.CreateLayerAsync(_worldId, map.WorldMapId, _memberId, "Standalone");
        var before = await _db.MapLayers.AsNoTracking().FirstAsync(existing => existing.MapLayerId == layer.MapLayerId);

        var service = (IWorldMapService)_sut;
        await service.SetLayerParentAsync(_worldId, map.WorldMapId, _memberId, layer.MapLayerId, null);

        var after = await _db.MapLayers.AsNoTracking().FirstAsync(existing => existing.MapLayerId == layer.MapLayerId);
        Assert.Null(after.ParentLayerId);
        Assert.Equal(before.SortOrder, after.SortOrder);
        Assert.Equal(before.Name, after.Name);
        Assert.Equal(before.IsEnabled, after.IsEnabled);
    }

    [Fact]
    public async Task SetLayerParent_ThrowsArgumentException_WhenTargetLayerNotFound()
    {
        var map = await CreateMapWithDefaultLayersAsync("Set Parent Missing Target");
        var parent = await _sut.CreateLayerAsync(_worldId, map.WorldMapId, _memberId, "Parent");

        var service = (IWorldMapService)_sut;
        await Assert.ThrowsAsync<ArgumentException>(
            () => service.SetLayerParentAsync(_worldId, map.WorldMapId, _memberId, Guid.NewGuid(), parent.MapLayerId));
    }

    [Fact]
    public async Task SetLayerParent_ThrowsArgumentException_WhenTargetLayerInDifferentMap()
    {
        var firstMap = await CreateMapWithDefaultLayersAsync("Set Parent Source");
        var secondMap = await CreateMapWithDefaultLayersAsync("Set Parent Target");
        var layerInFirstMap = await _sut.CreateLayerAsync(_worldId, firstMap.WorldMapId, _memberId, "Layer A");
        var parentInSecondMap = await _sut.CreateLayerAsync(_worldId, secondMap.WorldMapId, _memberId, "Layer B");

        var service = (IWorldMapService)_sut;
        await Assert.ThrowsAsync<ArgumentException>(
            () => service.SetLayerParentAsync(_worldId, secondMap.WorldMapId, _memberId, layerInFirstMap.MapLayerId, parentInSecondMap.MapLayerId));
    }

    [Fact]
    public async Task SetLayerParent_ThrowsArgumentException_WhenParentLayerNotFound()
    {
        var map = await CreateMapWithDefaultLayersAsync("Set Parent Missing Parent");
        var child = await _sut.CreateLayerAsync(_worldId, map.WorldMapId, _memberId, "Child");

        var service = (IWorldMapService)_sut;
        await Assert.ThrowsAsync<ArgumentException>(
            () => service.SetLayerParentAsync(_worldId, map.WorldMapId, _memberId, child.MapLayerId, Guid.NewGuid()));
    }

    [Fact]
    public async Task SetLayerParent_ThrowsArgumentException_WhenParentLayerInDifferentMap()
    {
        var firstMap = await CreateMapWithDefaultLayersAsync("Set Parent Cross Map A");
        var secondMap = await CreateMapWithDefaultLayersAsync("Set Parent Cross Map B");
        var child = await _sut.CreateLayerAsync(_worldId, firstMap.WorldMapId, _memberId, "Child");
        var parentInOtherMap = await _sut.CreateLayerAsync(_worldId, secondMap.WorldMapId, _memberId, "Foreign Parent");
        var original = await _db.MapLayers.AsNoTracking().FirstAsync(layer => layer.MapLayerId == child.MapLayerId);

        var service = (IWorldMapService)_sut;
        await Assert.ThrowsAsync<ArgumentException>(
            () => service.SetLayerParentAsync(_worldId, firstMap.WorldMapId, _memberId, child.MapLayerId, parentInOtherMap.MapLayerId));

        var unchanged = await _db.MapLayers.AsNoTracking().FirstAsync(layer => layer.MapLayerId == child.MapLayerId);
        Assert.Equal(original.ParentLayerId, unchanged.ParentLayerId);
    }

    [Fact]
    public async Task SetLayerParent_ThrowsArgumentException_WhenSelfParentRequested()
    {
        var map = await CreateMapWithDefaultLayersAsync("Set Parent Self");
        var layer = await _sut.CreateLayerAsync(_worldId, map.WorldMapId, _memberId, "Self Layer");
        var original = await _db.MapLayers.AsNoTracking().FirstAsync(existing => existing.MapLayerId == layer.MapLayerId);

        var service = (IWorldMapService)_sut;
        await Assert.ThrowsAsync<ArgumentException>(
            () => service.SetLayerParentAsync(_worldId, map.WorldMapId, _memberId, layer.MapLayerId, layer.MapLayerId));

        var unchanged = await _db.MapLayers.AsNoTracking().FirstAsync(existing => existing.MapLayerId == layer.MapLayerId);
        Assert.Equal(original.ParentLayerId, unchanged.ParentLayerId);
    }

    [Fact]
    public async Task SetLayerParent_ThrowsArgumentException_WhenDirectCycleWouldBeCreated()
    {
        var map = await CreateMapWithDefaultLayersAsync("Set Parent Direct Cycle");
        var parent = await _sut.CreateLayerAsync(_worldId, map.WorldMapId, _memberId, "Parent");
        var child = await _sut.CreateLayerAsync(_worldId, map.WorldMapId, _memberId, "Child");
        var service = (IWorldMapService)_sut;

        await service.SetLayerParentAsync(_worldId, map.WorldMapId, _memberId, child.MapLayerId, parent.MapLayerId);

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.SetLayerParentAsync(_worldId, map.WorldMapId, _memberId, parent.MapLayerId, child.MapLayerId));
    }

    [Fact]
    public async Task SetLayerParent_ThrowsArgumentException_WhenIndirectCycleWouldBeCreated()
    {
        var map = await CreateMapWithDefaultLayersAsync("Set Parent Indirect Cycle");
        var layerA = await _sut.CreateLayerAsync(_worldId, map.WorldMapId, _memberId, "Layer A");
        var layerB = await _sut.CreateLayerAsync(_worldId, map.WorldMapId, _memberId, "Layer B");
        var layerC = await _sut.CreateLayerAsync(_worldId, map.WorldMapId, _memberId, "Layer C");
        var service = (IWorldMapService)_sut;

        await service.SetLayerParentAsync(_worldId, map.WorldMapId, _memberId, layerB.MapLayerId, layerA.MapLayerId);
        await service.SetLayerParentAsync(_worldId, map.WorldMapId, _memberId, layerC.MapLayerId, layerB.MapLayerId);

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.SetLayerParentAsync(_worldId, map.WorldMapId, _memberId, layerA.MapLayerId, layerC.MapLayerId));
    }

    [Fact]
    public async Task SetLayerParent_ThrowsArgumentException_WhenParentChainIsCorruptedCycle()
    {
        var map = await CreateMapWithDefaultLayersAsync("Set Parent Corrupt Cycle");
        var target = await _sut.CreateLayerAsync(_worldId, map.WorldMapId, _memberId, "Target");
        var badA = await _sut.CreateLayerAsync(_worldId, map.WorldMapId, _memberId, "Corrupt A");
        var badB = await _sut.CreateLayerAsync(_worldId, map.WorldMapId, _memberId, "Corrupt B");

        var badALayer = await _db.MapLayers.FirstAsync(layer => layer.MapLayerId == badA.MapLayerId);
        var badBLayer = await _db.MapLayers.FirstAsync(layer => layer.MapLayerId == badB.MapLayerId);
        badALayer.ParentLayerId = badBLayer.MapLayerId;
        badBLayer.ParentLayerId = badALayer.MapLayerId;
        await _db.SaveChangesAsync();

        var service = (IWorldMapService)_sut;
        await Assert.ThrowsAsync<ArgumentException>(
            () => service.SetLayerParentAsync(_worldId, map.WorldMapId, _memberId, target.MapLayerId, badA.MapLayerId));
    }

    [Fact]
    public async Task SetLayerParent_ThrowsArgumentException_ForDefaultLayerMutation()
    {
        var map = await CreateMapWithDefaultLayersAsync("Set Parent Default Restriction");
        var worldLayer = await _db.MapLayers
            .AsNoTracking()
            .FirstAsync(layer => layer.WorldMapId == map.WorldMapId && layer.Name == "World");
        var customParent = await _sut.CreateLayerAsync(_worldId, map.WorldMapId, _memberId, "Parent");

        var service = (IWorldMapService)_sut;
        await Assert.ThrowsAsync<ArgumentException>(
            () => service.SetLayerParentAsync(_worldId, map.WorldMapId, _memberId, worldLayer.MapLayerId, customParent.MapLayerId));
    }

    [Fact]
    public async Task SetLayerParent_ThrowsUnauthorizedAccessException_WhenUserNotWorldMember()
    {
        var map = await CreateMapWithDefaultLayersAsync("Set Parent Unauthorized");
        var layer = await _sut.CreateLayerAsync(_worldId, map.WorldMapId, _memberId, "Layer");

        var service = (IWorldMapService)_sut;
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => service.SetLayerParentAsync(_worldId, map.WorldMapId, _outsiderId, layer.MapLayerId, null));
    }

    [Fact]
    public async Task DeleteLayer_Succeeds_ForCustomRootLayer_AndNormalizesRootSiblingSortOrder()
    {
        var map = await CreateMapWithDefaultLayersAsync("Delete Custom Layer");
        var customA = await _sut.CreateLayerAsync(
            _worldId,
            map.WorldMapId,
            _memberId,
            "Cities");
        _ = await _sut.CreateLayerAsync(
            _worldId,
            map.WorldMapId,
            _memberId,
            "Roads");

        var service = (IWorldMapService)_sut;
        await service.DeleteLayer(_worldId, map.WorldMapId, _memberId, customA.MapLayerId);

        Assert.False(await _db.MapLayers.AnyAsync(layer => layer.MapLayerId == customA.MapLayerId));

        var remaining = await _db.MapLayers
            .AsNoTracking()
            .Where(layer => layer.WorldMapId == map.WorldMapId)
            .OrderBy(layer => layer.SortOrder)
            .ToListAsync();

        Assert.Equal(new[] { 0, 1, 2, 3 }, remaining.Select(layer => layer.SortOrder).ToArray());
    }

    [Fact]
    public async Task DeleteLayer_Succeeds_ForNestedLeaf_NormalizesOnlySiblingGroup_AndLeavesOtherBranchesAndMapsUnchanged()
    {
        var map = await CreateMapWithDefaultLayersAsync("Delete Nested Layer");
        var parentA = await _sut.CreateLayerAsync(_worldId, map.WorldMapId, _memberId, "Parent A");
        var parentB = await _sut.CreateLayerAsync(_worldId, map.WorldMapId, _memberId, "Parent B");
        var childA1 = await _sut.CreateLayerAsync(_worldId, map.WorldMapId, _memberId, "Child A1", parentA.MapLayerId);
        var childA2 = await _sut.CreateLayerAsync(_worldId, map.WorldMapId, _memberId, "Child A2", parentA.MapLayerId);
        _ = await _sut.CreateLayerAsync(_worldId, map.WorldMapId, _memberId, "Child B1", parentB.MapLayerId);
        var secondMap = await CreateMapWithDefaultLayersAsync("Delete Nested Layer Second Map");
        _ = await _sut.CreateLayerAsync(_worldId, secondMap.WorldMapId, _memberId, "Other Map Custom Layer");

        var rootGroupBefore = await _db.MapLayers
            .AsNoTracking()
            .Where(layer => layer.WorldMapId == map.WorldMapId && layer.ParentLayerId == null)
            .OrderBy(layer => layer.SortOrder)
            .ThenBy(layer => layer.MapLayerId)
            .Select(layer => new { layer.MapLayerId, layer.SortOrder })
            .ToListAsync();
        var parentBChildrenBefore = await _db.MapLayers
            .AsNoTracking()
            .Where(layer => layer.WorldMapId == map.WorldMapId && layer.ParentLayerId == parentB.MapLayerId)
            .OrderBy(layer => layer.SortOrder)
            .ThenBy(layer => layer.MapLayerId)
            .Select(layer => new { layer.MapLayerId, layer.SortOrder })
            .ToListAsync();
        var secondMapRootBefore = await _db.MapLayers
            .AsNoTracking()
            .Where(layer => layer.WorldMapId == secondMap.WorldMapId && layer.ParentLayerId == null)
            .OrderBy(layer => layer.SortOrder)
            .ThenBy(layer => layer.MapLayerId)
            .Select(layer => new { layer.MapLayerId, layer.SortOrder })
            .ToListAsync();

        var service = (IWorldMapService)_sut;
        await service.DeleteLayer(_worldId, map.WorldMapId, _memberId, childA1.MapLayerId);

        Assert.False(await _db.MapLayers.AnyAsync(layer => layer.MapLayerId == childA1.MapLayerId));

        var parentAChildrenAfter = await _db.MapLayers
            .AsNoTracking()
            .Where(layer => layer.WorldMapId == map.WorldMapId && layer.ParentLayerId == parentA.MapLayerId)
            .OrderBy(layer => layer.SortOrder)
            .ThenBy(layer => layer.MapLayerId)
            .Select(layer => new { layer.MapLayerId, layer.SortOrder })
            .ToListAsync();
        Assert.Single(parentAChildrenAfter);
        Assert.Equal(childA2.MapLayerId, parentAChildrenAfter[0].MapLayerId);
        Assert.Equal(0, parentAChildrenAfter[0].SortOrder);

        var rootGroupAfter = await _db.MapLayers
            .AsNoTracking()
            .Where(layer => layer.WorldMapId == map.WorldMapId && layer.ParentLayerId == null)
            .OrderBy(layer => layer.SortOrder)
            .ThenBy(layer => layer.MapLayerId)
            .Select(layer => new { layer.MapLayerId, layer.SortOrder })
            .ToListAsync();
        var parentBChildrenAfter = await _db.MapLayers
            .AsNoTracking()
            .Where(layer => layer.WorldMapId == map.WorldMapId && layer.ParentLayerId == parentB.MapLayerId)
            .OrderBy(layer => layer.SortOrder)
            .ThenBy(layer => layer.MapLayerId)
            .Select(layer => new { layer.MapLayerId, layer.SortOrder })
            .ToListAsync();
        var secondMapRootAfter = await _db.MapLayers
            .AsNoTracking()
            .Where(layer => layer.WorldMapId == secondMap.WorldMapId && layer.ParentLayerId == null)
            .OrderBy(layer => layer.SortOrder)
            .ThenBy(layer => layer.MapLayerId)
            .Select(layer => new { layer.MapLayerId, layer.SortOrder })
            .ToListAsync();

        Assert.Equal(rootGroupBefore, rootGroupAfter);
        Assert.Equal(parentBChildrenBefore, parentBChildrenAfter);
        Assert.Equal(secondMapRootBefore, secondMapRootAfter);
    }

    [Fact]
    public async Task DeleteLayer_ThrowsArgumentException_WhenLayerHasChildren_AndDoesNotMutatePersistedState()
    {
        var map = await CreateMapWithDefaultLayersAsync("Delete Parent Layer");
        var parentLayer = await _sut.CreateLayerAsync(_worldId, map.WorldMapId, _memberId, "Parent");
        _ = await _sut.CreateLayerAsync(_worldId, map.WorldMapId, _memberId, "Child A", parentLayer.MapLayerId);
        _ = await _sut.CreateLayerAsync(_worldId, map.WorldMapId, _memberId, "Child B", parentLayer.MapLayerId);
        _ = await _sut.CreateLayerAsync(_worldId, map.WorldMapId, _memberId, "Root Peer");

        var rootSiblingSortBefore = await _db.MapLayers
            .AsNoTracking()
            .Where(layer => layer.WorldMapId == map.WorldMapId && layer.ParentLayerId == null)
            .OrderBy(layer => layer.SortOrder)
            .ThenBy(layer => layer.MapLayerId)
            .Select(layer => new { layer.MapLayerId, layer.SortOrder })
            .ToListAsync();
        var childCountBefore = await _db.MapLayers
            .AsNoTracking()
            .CountAsync(layer => layer.WorldMapId == map.WorldMapId && layer.ParentLayerId == parentLayer.MapLayerId);
        var layerCountBefore = await _db.MapLayers
            .AsNoTracking()
            .CountAsync(layer => layer.WorldMapId == map.WorldMapId);

        var service = (IWorldMapService)_sut;
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => service.DeleteLayer(_worldId, map.WorldMapId, _memberId, parentLayer.MapLayerId));

        Assert.Contains("child layers", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.True(await _db.MapLayers.AsNoTracking().AnyAsync(layer => layer.MapLayerId == parentLayer.MapLayerId));

        var rootSiblingSortAfter = await _db.MapLayers
            .AsNoTracking()
            .Where(layer => layer.WorldMapId == map.WorldMapId && layer.ParentLayerId == null)
            .OrderBy(layer => layer.SortOrder)
            .ThenBy(layer => layer.MapLayerId)
            .Select(layer => new { layer.MapLayerId, layer.SortOrder })
            .ToListAsync();
        var childCountAfter = await _db.MapLayers
            .AsNoTracking()
            .CountAsync(layer => layer.WorldMapId == map.WorldMapId && layer.ParentLayerId == parentLayer.MapLayerId);
        var layerCountAfter = await _db.MapLayers
            .AsNoTracking()
            .CountAsync(layer => layer.WorldMapId == map.WorldMapId);

        Assert.Equal(rootSiblingSortBefore, rootSiblingSortAfter);
        Assert.Equal(childCountBefore, childCountAfter);
        Assert.Equal(layerCountBefore, layerCountAfter);
    }

    [Fact]
    public async Task DeleteLayer_ThrowsArgumentException_WhenPinsReferenceLayer_AndDoesNotMutatePersistedState()
    {
        var map = await CreateMapWithDefaultLayersAsync("Delete Referenced Layer");
        var customLayer = await _sut.CreateLayerAsync(
            _worldId,
            map.WorldMapId,
            _memberId,
            "Cities");
        _ = await _sut.CreateLayerAsync(
            _worldId,
            map.WorldMapId,
            _memberId,
            "Roads");

        var firstPin = await _sut.CreatePinAsync(
            _worldId,
            map.WorldMapId,
            _memberId,
            new MapPinCreateDto
            {
                X = 0.4f,
                Y = 0.6f,
                LayerId = customLayer.MapLayerId,
            });
        var secondPin = await _sut.CreatePinAsync(
            _worldId,
            map.WorldMapId,
            _memberId,
            new MapPinCreateDto
            {
                X = 0.45f,
                Y = 0.65f,
                LayerId = customLayer.MapLayerId,
            });

        var rootSiblingSortBefore = await _db.MapLayers
            .AsNoTracking()
            .Where(layer => layer.WorldMapId == map.WorldMapId && layer.ParentLayerId == null)
            .OrderBy(layer => layer.SortOrder)
            .ThenBy(layer => layer.MapLayerId)
            .Select(layer => new { layer.MapLayerId, layer.SortOrder })
            .ToListAsync();
        var layerCountBefore = await _db.MapLayers
            .AsNoTracking()
            .CountAsync(layer => layer.WorldMapId == map.WorldMapId);
        var pinCountBefore = await _db.MapFeatures
            .AsNoTracking()
            .CountAsync(feature => feature.MapLayerId == customLayer.MapLayerId);

        var service = (IWorldMapService)_sut;
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => service.DeleteLayer(_worldId, map.WorldMapId, _memberId, customLayer.MapLayerId));

        Assert.Contains("features reference", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.True(await _db.MapLayers.AsNoTracking().AnyAsync(layer => layer.MapLayerId == customLayer.MapLayerId));
        Assert.True(await _db.MapFeatures.AsNoTracking().AnyAsync(feature => feature.MapFeatureId == firstPin.PinId));
        Assert.True(await _db.MapFeatures.AsNoTracking().AnyAsync(feature => feature.MapFeatureId == secondPin.PinId));

        var rootSiblingSortAfter = await _db.MapLayers
            .AsNoTracking()
            .Where(layer => layer.WorldMapId == map.WorldMapId && layer.ParentLayerId == null)
            .OrderBy(layer => layer.SortOrder)
            .ThenBy(layer => layer.MapLayerId)
            .Select(layer => new { layer.MapLayerId, layer.SortOrder })
            .ToListAsync();
        var layerCountAfter = await _db.MapLayers
            .AsNoTracking()
            .CountAsync(layer => layer.WorldMapId == map.WorldMapId);
        var pinCountAfter = await _db.MapFeatures
            .AsNoTracking()
            .CountAsync(feature => feature.MapLayerId == customLayer.MapLayerId);

        Assert.Equal(rootSiblingSortBefore, rootSiblingSortAfter);
        Assert.Equal(layerCountBefore, layerCountAfter);
        Assert.Equal(pinCountBefore, pinCountAfter);
    }

    [Theory]
    [InlineData("World")]
    [InlineData("Campaign")]
    [InlineData("Arc")]
    public async Task DeleteLayer_ThrowsArgumentException_ForProtectedDefaultLayer(string layerName)
    {
        var map = await CreateMapWithDefaultLayersAsync("Delete Default Layer");
        var layer = await _db.MapLayers
            .AsNoTracking()
            .FirstAsync(existingLayer => existingLayer.WorldMapId == map.WorldMapId && existingLayer.Name == layerName);
        Assert.False(await _db.MapLayers.AsNoTracking().AnyAsync(existingLayer => existingLayer.ParentLayerId == layer.MapLayerId));
        Assert.False(await _db.MapFeatures.AsNoTracking().AnyAsync(feature => feature.MapLayerId == layer.MapLayerId));

        var service = (IWorldMapService)_sut;
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => service.DeleteLayer(_worldId, map.WorldMapId, _memberId, layer.MapLayerId));

        Assert.Contains("cannot be deleted", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.True(await _db.MapLayers.AsNoTracking().AnyAsync(existingLayer => existingLayer.MapLayerId == layer.MapLayerId));
    }

    [Fact]
    public async Task DeleteLayer_ThrowsArgumentException_WhenMapDoesNotExist()
    {
        var service = (IWorldMapService)_sut;
        await Assert.ThrowsAsync<ArgumentException>(
            () => service.DeleteLayer(_worldId, Guid.NewGuid(), _memberId, Guid.NewGuid()));
    }

    [Fact]
    public async Task DeleteLayer_ThrowsArgumentException_WhenLayerDoesNotExist()
    {
        var map = await CreateMapWithDefaultLayersAsync("Delete Missing Layer");
        var service = (IWorldMapService)_sut;

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.DeleteLayer(_worldId, map.WorldMapId, _memberId, Guid.NewGuid()));
    }

    [Fact]
    public async Task DeleteLayer_ThrowsArgumentException_WhenLayerBelongsToDifferentMap()
    {
        var firstMap = await CreateMapWithDefaultLayersAsync("Delete Source Map");
        var secondMap = await CreateMapWithDefaultLayersAsync("Delete Target Map");
        var customLayer = await _sut.CreateLayerAsync(
            _worldId,
            firstMap.WorldMapId,
            _memberId,
            "Cities");
        var service = (IWorldMapService)_sut;

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.DeleteLayer(_worldId, secondMap.WorldMapId, _memberId, customLayer.MapLayerId));
    }

    [Fact]
    public async Task DeleteLayer_ThrowsUnauthorizedAccessException_WhenUserNotWorldMember()
    {
        var map = await CreateMapWithDefaultLayersAsync("Delete Unauthorized");
        var customLayer = await _sut.CreateLayerAsync(
            _worldId,
            map.WorldMapId,
            _memberId,
            "Cities");
        var service = (IWorldMapService)_sut;

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => service.DeleteLayer(_worldId, map.WorldMapId, _outsiderId, customLayer.MapLayerId));
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

    // ── SearchMapsForWorldAsync (P2) ─────────────────────────────────────────

    [Fact]
    public async Task SearchMaps_Throws_WhenUserHasNoAccess()
    {
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _sut.SearchMapsForWorldAsync(_worldId, _outsiderId, null));
    }

    [Fact]
    public async Task SearchMaps_ReturnsOnlyMatchingNames_WhenQueryProvided()
    {
        _db.WorldMaps.AddRange(
            new WorldMap { WorldMapId = Guid.NewGuid(), WorldId = _worldId, Name = "Waterdeep", CreatedUtc = DateTime.UtcNow, UpdatedUtc = DateTime.UtcNow },
            new WorldMap { WorldMapId = Guid.NewGuid(), WorldId = _worldId, Name = "Neverwinter", CreatedUtc = DateTime.UtcNow, UpdatedUtc = DateTime.UtcNow },
            new WorldMap { WorldMapId = Guid.NewGuid(), WorldId = _worldId, Name = "Waterfall", CreatedUtc = DateTime.UtcNow, UpdatedUtc = DateTime.UtcNow });
        await _db.SaveChangesAsync();

        var result = await _sut.SearchMapsForWorldAsync(_worldId, _ownerId, "Water");

        Assert.Equal(2, result.Count);
        Assert.All(result, item => Assert.Contains("Water", item.Name));
    }

    [Fact]
    public async Task SearchMaps_MatchesNames_CaseInsensitively()
    {
        _db.WorldMaps.AddRange(
            new WorldMap { WorldMapId = Guid.NewGuid(), WorldId = _worldId, Name = "Ambria", CreatedUtc = DateTime.UtcNow, UpdatedUtc = DateTime.UtcNow },
            new WorldMap { WorldMapId = Guid.NewGuid(), WorldId = _worldId, Name = "Zanbar", CreatedUtc = DateTime.UtcNow, UpdatedUtc = DateTime.UtcNow });
        await _db.SaveChangesAsync();

        var result = await _sut.SearchMapsForWorldAsync(_worldId, _ownerId, "amb");

        Assert.Single(result);
        Assert.Equal("Ambria", result[0].Name);
    }

    [Fact]
    public async Task SearchMaps_ReturnsSortedResults_AndLimitsToSpecifiedWorld()
    {
        var otherWorldId = Guid.NewGuid();
        _db.Worlds.Add(new World
        {
            Id = otherWorldId,
            OwnerId = _ownerId,
            Name = "Other World",
            Slug = "other-world",
        });

        _db.WorldMaps.AddRange(
            new WorldMap { WorldMapId = Guid.NewGuid(), WorldId = _worldId, Name = "Zebra", CreatedUtc = DateTime.UtcNow, UpdatedUtc = DateTime.UtcNow },
            new WorldMap { WorldMapId = Guid.NewGuid(), WorldId = _worldId, Name = "Alpha", CreatedUtc = DateTime.UtcNow, UpdatedUtc = DateTime.UtcNow },
            new WorldMap { WorldMapId = Guid.NewGuid(), WorldId = otherWorldId, Name = "Aardvark", CreatedUtc = DateTime.UtcNow, UpdatedUtc = DateTime.UtcNow });
        await _db.SaveChangesAsync();

        var result = await _sut.SearchMapsForWorldAsync(_worldId, _ownerId, null);

        Assert.Equal(2, result.Count);
        Assert.Equal("Alpha", result[0].Name);
        Assert.Equal("Zebra", result[1].Name);
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
            new MapPinCreateDto { X = 0.60f, Y = 0.40f, LayerId = null });

        Assert.Equal(layers["World"], created.LayerId);

        var persisted = await _db.MapFeatures
            .AsNoTracking()
            .FirstAsync(mf => mf.MapFeatureId == created.PinId);
        Assert.Equal(layers["World"], persisted.MapLayerId);
    }

    [Fact]
    public async Task CreatePin_UsesRequestedLayer_WhenLayerIdProvided()
    {
        var map = await CreateMapWithDefaultLayersAsync("Requested Layer");
        var layers = await GetMapLayersByNameAsync(map.WorldMapId);
        var requestedLayerId = layers["Campaign"];

        var created = await _sut.CreatePinAsync(
            _worldId,
            map.WorldMapId,
            _memberId,
            new MapPinCreateDto { X = 0.42f, Y = 0.24f, LayerId = requestedLayerId });

        Assert.Equal(requestedLayerId, created.LayerId);

        var persisted = await _db.MapFeatures
            .AsNoTracking()
            .FirstAsync(mf => mf.MapFeatureId == created.PinId);
        Assert.Equal(requestedLayerId, persisted.MapLayerId);
    }

    [Fact]
    public async Task CreatePin_Throws_WhenProvidedLayerDoesNotExist()
    {
        var map = await CreateMapWithDefaultLayersAsync("Invalid Requested Layer");

        await Assert.ThrowsAsync<ArgumentException>(
            () => _sut.CreatePinAsync(
                _worldId,
                map.WorldMapId,
                _memberId,
                new MapPinCreateDto { X = 0.33f, Y = 0.66f, LayerId = Guid.NewGuid() }));
    }

    [Fact]
    public async Task CreatePin_Throws_WhenProvidedLayerBelongsToDifferentMap()
    {
        var targetMap = await CreateMapWithDefaultLayersAsync("Target Map");
        var otherMap = await CreateMapWithDefaultLayersAsync("Other Map");
        var otherMapLayers = await GetMapLayersByNameAsync(otherMap.WorldMapId);
        var foreignLayerId = otherMapLayers["Arc"];

        await Assert.ThrowsAsync<ArgumentException>(
            () => _sut.CreatePinAsync(
                _worldId,
                targetMap.WorldMapId,
                _memberId,
                new MapPinCreateDto { X = 0.12f, Y = 0.21f, LayerId = foreignLayerId }));
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
            new MapPinCreateDto { Name = "  Dock Ward  ", X = 0.2f, Y = 0.8f, LinkedArticleId = missingArticleId });

        Assert.Equal("Dock Ward", created.Name);
        Assert.Null(created.LinkedArticle);
    }

    [Fact]
    public async Task CreatePin_Throws_WhenNameTooLong_AndDoesNotWriteToDb()
    {
        var map = await CreateMapWithDefaultLayersAsync("Validate Pin Name");
        var beforeCount = await _db.MapFeatures.CountAsync();
        var tooLongName = new string('x', 201);

        await Assert.ThrowsAsync<ArgumentException>(
            () => _sut.CreatePinAsync(
                _worldId,
                map.WorldMapId,
                _memberId,
                new MapPinCreateDto { Name = tooLongName, X = 0.2f, Y = 0.3f }));

        var afterCount = await _db.MapFeatures.CountAsync();
        Assert.Equal(beforeCount, afterCount);
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
    public async Task UpdateLayerVisibility_Success_UpdatesIsEnabled()
    {
        var map = await CreateMapWithDefaultLayersAsync("Visibility Update");
        var layer = await _db.MapLayers
            .AsNoTracking()
            .FirstAsync(l => l.WorldMapId == map.WorldMapId && l.Name == "World");

        Assert.False(layer.IsEnabled);

        var service = (IWorldMapService)_sut;
        await service.UpdateLayerVisibility(_worldId, map.WorldMapId, layer.MapLayerId, _memberId, true);

        var updated = await _db.MapLayers
            .AsNoTracking()
            .FirstAsync(l => l.MapLayerId == layer.MapLayerId);

        Assert.True(updated.IsEnabled);
    }

    [Fact]
    public async Task UpdateLayerVisibility_ThrowsArgumentException_WhenMapDoesNotExist()
    {
        await Assert.ThrowsAsync<ArgumentException>(
            () => _sut.UpdateLayerVisibilityAsync(
                _worldId,
                Guid.NewGuid(),
                Guid.NewGuid(),
                _memberId,
                true));
    }

    [Fact]
    public async Task UpdateLayerVisibility_ThrowsArgumentException_WhenLayerDoesNotExist()
    {
        var map = await CreateMapWithDefaultLayersAsync("Missing Layer");

        await Assert.ThrowsAsync<ArgumentException>(
            () => _sut.UpdateLayerVisibilityAsync(
                _worldId,
                map.WorldMapId,
                Guid.NewGuid(),
                _memberId,
                true));
    }

    [Fact]
    public async Task UpdateLayerVisibility_ThrowsArgumentException_WhenLayerBelongsToDifferentMap()
    {
        var firstMap = await CreateMapWithDefaultLayersAsync("First Map");
        var secondMap = await CreateMapWithDefaultLayersAsync("Second Map");
        var layerFromFirstMap = await _db.MapLayers
            .AsNoTracking()
            .FirstAsync(l => l.WorldMapId == firstMap.WorldMapId && l.Name == "World");

        await Assert.ThrowsAsync<ArgumentException>(
            () => _sut.UpdateLayerVisibilityAsync(
                _worldId,
                secondMap.WorldMapId,
                layerFromFirstMap.MapLayerId,
                _memberId,
                true));
    }

    [Fact]
    public async Task UpdateLayerVisibility_ThrowsUnauthorizedAccessException_WhenUserNotWorldMember()
    {
        var map = await CreateMapWithDefaultLayersAsync("Unauthorized Visibility Update");
        var layer = await _db.MapLayers
            .AsNoTracking()
            .FirstAsync(l => l.WorldMapId == map.WorldMapId && l.Name == "World");

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _sut.UpdateLayerVisibilityAsync(
                _worldId,
                map.WorldMapId,
                layer.MapLayerId,
                _outsiderId,
                true));
    }

    [Fact]
    public async Task ReorderLayers_Success_PersistsSequentialSortOrder_AndPreservesIsEnabled()
    {
        var map = await CreateMapWithDefaultLayersAsync("Reorder Success");
        var layers = await _db.MapLayers
            .Where(l => l.WorldMapId == map.WorldMapId)
            .ToListAsync();

        var worldLayer = layers.First(l => l.Name == "World");
        var campaignLayer = layers.First(l => l.Name == "Campaign");
        var arcLayer = layers.First(l => l.Name == "Arc");

        worldLayer.IsEnabled = true;
        campaignLayer.IsEnabled = false;
        arcLayer.IsEnabled = true;
        await _db.SaveChangesAsync();

        var submittedOrder = new List<Guid>
        {
            arcLayer.MapLayerId,
            worldLayer.MapLayerId,
            campaignLayer.MapLayerId,
        };

        var service = (IWorldMapService)_sut;
        await service.ReorderLayers(_worldId, map.WorldMapId, _memberId, submittedOrder);

        var reordered = await _db.MapLayers
            .AsNoTracking()
            .Where(l => l.WorldMapId == map.WorldMapId)
            .OrderBy(l => l.SortOrder)
            .ToListAsync();

        Assert.Equal(submittedOrder, reordered.Select(l => l.MapLayerId).ToList());
        Assert.Equal(new[] { 0, 1, 2 }, reordered.Select(l => l.SortOrder).ToArray());

        Assert.True(reordered.Single(l => l.MapLayerId == worldLayer.MapLayerId).IsEnabled);
        Assert.False(reordered.Single(l => l.MapLayerId == campaignLayer.MapLayerId).IsEnabled);
        Assert.True(reordered.Single(l => l.MapLayerId == arcLayer.MapLayerId).IsEnabled);
    }

    [Fact]
    public async Task ReorderLayers_Success_ReordersChildSiblingsOnly()
    {
        var map = await CreateMapWithDefaultLayersAsync("Reorder Child Siblings");
        var parent = await _sut.CreateLayerAsync(_worldId, map.WorldMapId, _memberId, "Parent");
        var childA = await _sut.CreateLayerAsync(_worldId, map.WorldMapId, _memberId, "Child A");
        var childB = await _sut.CreateLayerAsync(_worldId, map.WorldMapId, _memberId, "Child B");
        var rootLayer = await _sut.CreateLayerAsync(_worldId, map.WorldMapId, _memberId, "Root Peer");

        await _sut.SetLayerParentAsync(_worldId, map.WorldMapId, _memberId, childA.MapLayerId, parent.MapLayerId);
        await _sut.SetLayerParentAsync(_worldId, map.WorldMapId, _memberId, childB.MapLayerId, parent.MapLayerId);

        var originalRootSortOrder = await _db.MapLayers
            .AsNoTracking()
            .Where(layer => layer.MapLayerId == rootLayer.MapLayerId)
            .Select(layer => layer.SortOrder)
            .SingleAsync();

        var service = (IWorldMapService)_sut;
        await service.ReorderLayers(
            _worldId,
            map.WorldMapId,
            _memberId,
            new List<Guid> { childB.MapLayerId, childA.MapLayerId });

        var reorderedChildren = await _db.MapLayers
            .AsNoTracking()
            .Where(layer => layer.ParentLayerId == parent.MapLayerId)
            .OrderBy(layer => layer.SortOrder)
            .Select(layer => layer.MapLayerId)
            .ToListAsync();
        Assert.Equal(new[] { childB.MapLayerId, childA.MapLayerId }, reorderedChildren);

        var persistedRootSortOrder = await _db.MapLayers
            .AsNoTracking()
            .Where(layer => layer.MapLayerId == rootLayer.MapLayerId)
            .Select(layer => layer.SortOrder)
            .SingleAsync();
        Assert.Equal(originalRootSortOrder, persistedRootSortOrder);
    }

    [Fact]
    public async Task ReorderLayers_ThrowsArgumentException_WhenLayerIdDoesNotBelongToMap()
    {
        var map = await CreateMapWithDefaultLayersAsync("Reorder Invalid Layer");
        var otherMap = await CreateMapWithDefaultLayersAsync("Reorder Invalid Layer Other Map");
        var layerIds = await _db.MapLayers
            .AsNoTracking()
            .Where(l => l.WorldMapId == map.WorldMapId)
            .OrderBy(l => l.SortOrder)
            .Select(l => l.MapLayerId)
            .ToListAsync();
        var foreignLayerId = await _db.MapLayers
            .AsNoTracking()
            .Where(l => l.WorldMapId == otherMap.WorldMapId)
            .Select(l => l.MapLayerId)
            .FirstAsync();

        layerIds[0] = foreignLayerId;

        var service = (IWorldMapService)_sut;
        await Assert.ThrowsAsync<ArgumentException>(
            () => service.ReorderLayers(_worldId, map.WorldMapId, _memberId, layerIds));
    }

    [Fact]
    public async Task ReorderLayers_ThrowsArgumentException_WhenLayerIdIsUnknown()
    {
        var map = await CreateMapWithDefaultLayersAsync("Reorder Unknown Layer");
        var layerIds = await _db.MapLayers
            .AsNoTracking()
            .Where(l => l.WorldMapId == map.WorldMapId)
            .OrderBy(l => l.SortOrder)
            .Select(l => l.MapLayerId)
            .ToListAsync();

        layerIds[0] = Guid.NewGuid();

        var service = (IWorldMapService)_sut;
        await Assert.ThrowsAsync<ArgumentException>(
            () => service.ReorderLayers(_worldId, map.WorldMapId, _memberId, layerIds));
    }

    [Fact]
    public async Task ReorderLayers_ThrowsArgumentException_WhenLayerIdsContainDuplicates()
    {
        var map = await CreateMapWithDefaultLayersAsync("Reorder Duplicates");
        var layerIds = await _db.MapLayers
            .AsNoTracking()
            .Where(l => l.WorldMapId == map.WorldMapId)
            .OrderBy(l => l.SortOrder)
            .Select(l => l.MapLayerId)
            .ToListAsync();

        var duplicatePayload = new List<Guid> { layerIds[0], layerIds[0], layerIds[1] };

        var service = (IWorldMapService)_sut;
        await Assert.ThrowsAsync<ArgumentException>(
            () => service.ReorderLayers(_worldId, map.WorldMapId, _memberId, duplicatePayload));
    }

    [Fact]
    public async Task ReorderLayers_ThrowsArgumentException_WhenLayerIdsAreEmpty()
    {
        var map = await CreateMapWithDefaultLayersAsync("Reorder Empty Payload");
        var service = (IWorldMapService)_sut;

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.ReorderLayers(_worldId, map.WorldMapId, _memberId, []));
    }

    [Fact]
    public async Task ReorderLayers_ThrowsArgumentException_WhenLayerIdsSpanParentGroups_AndDoesNotMutate()
    {
        var map = await CreateMapWithDefaultLayersAsync("Reorder Mixed Parent Groups");
        var parent = await _sut.CreateLayerAsync(_worldId, map.WorldMapId, _memberId, "Parent");
        var child = await _sut.CreateLayerAsync(_worldId, map.WorldMapId, _memberId, "Child");
        await _sut.SetLayerParentAsync(_worldId, map.WorldMapId, _memberId, child.MapLayerId, parent.MapLayerId);

        var worldLayerId = await _db.MapLayers
            .AsNoTracking()
            .Where(layer => layer.WorldMapId == map.WorldMapId && layer.Name == "World")
            .Select(layer => layer.MapLayerId)
            .SingleAsync();

        var rootOrdersBefore = await _db.MapLayers
            .AsNoTracking()
            .Where(layer => layer.WorldMapId == map.WorldMapId && layer.ParentLayerId == null)
            .OrderBy(layer => layer.SortOrder)
            .Select(layer => new { layer.MapLayerId, layer.SortOrder })
            .ToListAsync();

        var service = (IWorldMapService)_sut;
        await Assert.ThrowsAsync<ArgumentException>(() => service.ReorderLayers(
            _worldId,
            map.WorldMapId,
            _memberId,
            [worldLayerId, child.MapLayerId]));

        var rootOrdersAfter = await _db.MapLayers
            .AsNoTracking()
            .Where(layer => layer.WorldMapId == map.WorldMapId && layer.ParentLayerId == null)
            .OrderBy(layer => layer.SortOrder)
            .Select(layer => new { layer.MapLayerId, layer.SortOrder })
            .ToListAsync();
        Assert.Equal(rootOrdersBefore, rootOrdersAfter);
    }

    [Fact]
    public async Task ReorderLayers_ThrowsArgumentException_WhenLayerIdsDoNotIncludeAllSiblings()
    {
        var map = await CreateMapWithDefaultLayersAsync("Reorder Missing Siblings");
        var layerIds = await _db.MapLayers
            .AsNoTracking()
            .Where(l => l.WorldMapId == map.WorldMapId)
            .OrderBy(l => l.SortOrder)
            .Select(l => l.MapLayerId)
            .ToListAsync();

        var partialSiblingPayload = layerIds.Take(2).ToList();
        var service = (IWorldMapService)_sut;
        await Assert.ThrowsAsync<ArgumentException>(
            () => service.ReorderLayers(_worldId, map.WorldMapId, _memberId, partialSiblingPayload));
    }

    [Fact]
    public async Task ReorderLayers_ThrowsArgumentException_WhenRootLayersSpanDifferentMaps()
    {
        var firstMap = await CreateMapWithDefaultLayersAsync("Reorder Root Map A");
        var secondMap = await CreateMapWithDefaultLayersAsync("Reorder Root Map B");

        var firstRootLayer = await _db.MapLayers
            .AsNoTracking()
            .Where(layer => layer.WorldMapId == firstMap.WorldMapId && layer.Name == "World")
            .Select(layer => layer.MapLayerId)
            .SingleAsync();
        var secondRootLayer = await _db.MapLayers
            .AsNoTracking()
            .Where(layer => layer.WorldMapId == secondMap.WorldMapId && layer.Name == "World")
            .Select(layer => layer.MapLayerId)
            .SingleAsync();

        var service = (IWorldMapService)_sut;
        await Assert.ThrowsAsync<ArgumentException>(
            () => service.ReorderLayers(_worldId, firstMap.WorldMapId, _memberId, [firstRootLayer, secondRootLayer]));
    }

    [Fact]
    public async Task ReorderLayers_ThrowsUnauthorizedAccessException_WhenUserNotWorldMember()
    {
        var map = await CreateMapWithDefaultLayersAsync("Reorder Unauthorized");
        var layerIds = await _db.MapLayers
            .AsNoTracking()
            .Where(l => l.WorldMapId == map.WorldMapId)
            .OrderBy(l => l.SortOrder)
            .Select(l => l.MapLayerId)
            .ToListAsync();

        var service = (IWorldMapService)_sut;
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => service.ReorderLayers(_worldId, map.WorldMapId, _outsiderId, layerIds));
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
            new MapPinCreateDto { Name = "Tavern", X = 0.80f, Y = 0.70f });
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
        Assert.Equal("Tavern", updated.Name);

        var updatedEntity = await _db.MapFeatures.AsNoTracking().FirstAsync(mf => mf.MapFeatureId == createdB.PinId);
        Assert.Equal(0.33f, updatedEntity.X);
        Assert.Equal(0.66f, updatedEntity.Y);
        Assert.Equal("Tavern", updatedEntity.Name);

        await _sut.DeletePinAsync(_worldId, map.WorldMapId, createdC.PinId, _memberId);

        Assert.False(await _db.MapFeatures.AnyAsync(mf => mf.MapFeatureId == createdC.PinId));
        var remainingPins = await _sut.ListPinsForMapAsync(_worldId, map.WorldMapId, _memberId);
        Assert.Equal(2, remainingPins.Count);
    }

    [Fact]
    public async Task CreateFeatureAsync_Polygon_PersistsBlobBackedFeatureAndAppearsInFeatureList()
    {
        var map = await CreateMapWithDefaultLayersAsync("Polygon Feature Map");
        var layerId = (await GetMapLayersByNameAsync(map.WorldMapId))["World"];
        var expectedBlobKey = $"maps/{map.WorldMapId}/layers/{layerId}/features/";

        _blobStore
            .SaveFeatureGeometryAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("\"polygon-etag\"");
        _blobStore
            .LoadFeatureGeometryAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.ArgAt<string>(0).Contains(".geojson.gz", StringComparison.Ordinal)
                ? """{"type":"Polygon","coordinates":[[[0.1,0.2],[0.6,0.2],[0.4,0.7],[0.1,0.2]]]}"""
                : null);

        var created = await _sut.CreateFeatureAsync(
            _worldId,
            map.WorldMapId,
            _memberId,
            new MapFeatureCreateDto
            {
                FeatureType = MapFeatureType.Polygon,
                LayerId = layerId,
                Name = "Dessarin Valley",
                Color = "teal",
                Polygon = new PolygonGeometryDto
                {
                    Type = "Polygon",
                    Coordinates =
                    [
                        [
                            [0.1f, 0.2f],
                            [0.6f, 0.2f],
                            [0.4f, 0.7f],
                        ],
                    ],
                },
            });

        Assert.Equal(MapFeatureType.Polygon, created.FeatureType);
        Assert.Equal("teal", created.Color);
        Assert.NotNull(created.Geometry);
        Assert.StartsWith(expectedBlobKey, created.Geometry!.BlobKey, StringComparison.Ordinal);
        Assert.Equal("\"polygon-etag\"", created.Geometry.ETag);
        Assert.NotNull(created.Polygon);
        Assert.Equal(4, created.Polygon!.Coordinates[0].Count);

        var persisted = await _db.MapFeatures.AsNoTracking().FirstAsync(feature => feature.MapFeatureId == created.FeatureId);
        Assert.Equal(MapFeatureType.Polygon, persisted.FeatureType);
        Assert.Equal("teal", persisted.Color);
        Assert.Equal(created.Geometry.BlobKey, persisted.GeometryBlobKey);

        var listed = await _sut.ListFeaturesForMapAsync(_worldId, map.WorldMapId, _memberId);
        Assert.Contains(
            listed,
            feature => feature.FeatureId == created.FeatureId
                && feature.FeatureType == MapFeatureType.Polygon
                && feature.Color == "teal");
    }

    [Fact]
    public async Task CreateFeatureAsync_Polygon_RejectsOutOfBoundsCoordinates()
    {
        var map = await CreateMapWithDefaultLayersAsync("Invalid Polygon");
        var layerId = (await GetMapLayersByNameAsync(map.WorldMapId))["World"];

        await Assert.ThrowsAsync<ArgumentException>(() => _sut.CreateFeatureAsync(
            _worldId,
            map.WorldMapId,
            _memberId,
            new MapFeatureCreateDto
            {
                FeatureType = MapFeatureType.Polygon,
                LayerId = layerId,
                Polygon = new PolygonGeometryDto
                {
                    Type = "Polygon",
                    Coordinates =
                    [
                        [
                            [-0.1f, 0.2f],
                            [0.6f, 0.2f],
                            [0.4f, 0.7f],
                        ],
                    ],
                },
            }));
    }

    [Fact]
    public async Task CreateFeatureAsync_Polygon_RejectsFewerThanThreeDistinctVertices()
    {
        var map = await CreateMapWithDefaultLayersAsync("Invalid Distinct Vertices");
        var layerId = (await GetMapLayersByNameAsync(map.WorldMapId))["World"];

        await Assert.ThrowsAsync<ArgumentException>(() => _sut.CreateFeatureAsync(
            _worldId,
            map.WorldMapId,
            _memberId,
            new MapFeatureCreateDto
            {
                FeatureType = MapFeatureType.Polygon,
                LayerId = layerId,
                Polygon = new PolygonGeometryDto
                {
                    Type = "Polygon",
                    Coordinates =
                    [
                        [
                            [0.2f, 0.2f],
                            [0.2f, 0.2f],
                            [0.7f, 0.7f],
                        ],
                    ],
                },
            }));
    }

    [Fact]
    public async Task CreateFeatureAsync_Polygon_RejectsMissingGeometry()
    {
        var map = await CreateMapWithDefaultLayersAsync("Missing Polygon Geometry");
        var layerId = (await GetMapLayersByNameAsync(map.WorldMapId))["World"];

        await Assert.ThrowsAsync<ArgumentException>(() => _sut.CreateFeatureAsync(
            _worldId,
            map.WorldMapId,
            _memberId,
            new MapFeatureCreateDto
            {
                FeatureType = MapFeatureType.Polygon,
                LayerId = layerId,
                Polygon = null,
            }));
    }

    [Fact]
    public async Task CreateFeatureAsync_Polygon_RejectsInvalidGeometryType()
    {
        var map = await CreateMapWithDefaultLayersAsync("Invalid Polygon Type");
        var layerId = (await GetMapLayersByNameAsync(map.WorldMapId))["World"];

        await Assert.ThrowsAsync<ArgumentException>(() => _sut.CreateFeatureAsync(
            _worldId,
            map.WorldMapId,
            _memberId,
            new MapFeatureCreateDto
            {
                FeatureType = MapFeatureType.Polygon,
                LayerId = layerId,
                Polygon = new PolygonGeometryDto
                {
                    Type = "LineString",
                    Coordinates =
                    [
                        [
                            [0.1f, 0.1f],
                            [0.6f, 0.1f],
                            [0.4f, 0.5f],
                        ],
                    ],
                },
            }));
    }

    [Fact]
    public async Task CreateFeatureAsync_Polygon_RejectsMultipleOuterRings()
    {
        var map = await CreateMapWithDefaultLayersAsync("Polygon Multiple Rings");
        var layerId = (await GetMapLayersByNameAsync(map.WorldMapId))["World"];

        await Assert.ThrowsAsync<ArgumentException>(() => _sut.CreateFeatureAsync(
            _worldId,
            map.WorldMapId,
            _memberId,
            new MapFeatureCreateDto
            {
                FeatureType = MapFeatureType.Polygon,
                LayerId = layerId,
                Polygon = new PolygonGeometryDto
                {
                    Type = "Polygon",
                    Coordinates =
                    [
                        [
                            [0.1f, 0.1f],
                            [0.3f, 0.1f],
                            [0.2f, 0.3f],
                        ],
                        [
                            [0.6f, 0.6f],
                            [0.8f, 0.6f],
                            [0.7f, 0.8f],
                        ],
                    ],
                },
            }));
    }

    [Fact]
    public async Task CreateFeatureAsync_Polygon_RejectsFewerThanThreeVertices()
    {
        var map = await CreateMapWithDefaultLayersAsync("Polygon Too Few Vertices");
        var layerId = (await GetMapLayersByNameAsync(map.WorldMapId))["World"];

        await Assert.ThrowsAsync<ArgumentException>(() => _sut.CreateFeatureAsync(
            _worldId,
            map.WorldMapId,
            _memberId,
            new MapFeatureCreateDto
            {
                FeatureType = MapFeatureType.Polygon,
                LayerId = layerId,
                Polygon = new PolygonGeometryDto
                {
                    Type = "Polygon",
                    Coordinates =
                    [
                        [
                            [0.1f, 0.1f],
                            [0.3f, 0.2f],
                        ],
                    ],
                },
            }));
    }

    [Fact]
    public async Task CreateFeatureAsync_Polygon_RejectsCoordinateTriples()
    {
        var map = await CreateMapWithDefaultLayersAsync("Polygon Invalid Coordinate Pair");
        var layerId = (await GetMapLayersByNameAsync(map.WorldMapId))["World"];

        await Assert.ThrowsAsync<ArgumentException>(() => _sut.CreateFeatureAsync(
            _worldId,
            map.WorldMapId,
            _memberId,
            new MapFeatureCreateDto
            {
                FeatureType = MapFeatureType.Polygon,
                LayerId = layerId,
                Polygon = new PolygonGeometryDto
                {
                    Type = "Polygon",
                    Coordinates =
                    [
                        [
                            [0.1f, 0.1f, 0.9f],
                            [0.6f, 0.1f],
                            [0.4f, 0.5f],
                        ],
                    ],
                },
            }));
    }

    [Fact]
    public async Task CreateFeatureAsync_Polygon_ClosesRingWhenOnlyYDiffersOnLastVertex()
    {
        var map = await CreateMapWithDefaultLayersAsync("Polygon Shared X Closure");
        var layerId = (await GetMapLayersByNameAsync(map.WorldMapId))["World"];
        string? savedGeometryJson = null;

        _blobStore.SaveFeatureGeometryAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                savedGeometryJson = callInfo.ArgAt<string>(1);
                return "\"etag-shared-x\"";
            });
        _blobStore.LoadFeatureGeometryAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(_ => savedGeometryJson);

        var created = await _sut.CreateFeatureAsync(
            _worldId,
            map.WorldMapId,
            _memberId,
            new MapFeatureCreateDto
            {
                FeatureType = MapFeatureType.Polygon,
                LayerId = layerId,
                Polygon = new PolygonGeometryDto
                {
                    Type = "Polygon",
                    Coordinates =
                    [
                        [
                            [0.2f, 0.2f],
                            [0.8f, 0.2f],
                            [0.2f, 0.7f],
                        ],
                    ],
                },
            });

        Assert.NotNull(created.Polygon);
        var normalizedRing = created.Polygon!.Coordinates[0];
        Assert.Equal(4, normalizedRing.Count);
        Assert.Equal(normalizedRing[0][0], normalizedRing[^1][0]);
        Assert.Equal(normalizedRing[0][1], normalizedRing[^1][1]);
    }

    [Fact]
    public async Task UpdateFeatureAsync_Polygon_RewritesGeometryBlobWhenLayerChanges()
    {
        var map = await CreateMapWithDefaultLayersAsync("Polygon Update");
        var worldLayerId = (await GetMapLayersByNameAsync(map.WorldMapId))["World"];
        var regionsLayer = await _sut.CreateLayerAsync(_worldId, map.WorldMapId, _memberId, "Regions");

        _blobStore.SaveFeatureGeometryAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("\"etag-1\"", "\"etag-2\"");
        _blobStore.LoadFeatureGeometryAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("""{"type":"Polygon","coordinates":[[[0.2,0.2],[0.5,0.2],[0.3,0.6],[0.2,0.2]]]}""");

        var created = await _sut.CreateFeatureAsync(
            _worldId,
            map.WorldMapId,
            _memberId,
            new MapFeatureCreateDto
            {
                FeatureType = MapFeatureType.Polygon,
                LayerId = worldLayerId,
                Polygon = new PolygonGeometryDto
                {
                    Type = "Polygon",
                    Coordinates =
                    [
                        [
                            [0.1f, 0.1f],
                            [0.4f, 0.1f],
                            [0.2f, 0.5f],
                        ],
                    ],
                },
            });

        var updated = await _sut.UpdateFeatureAsync(
            _worldId,
            map.WorldMapId,
            created.FeatureId,
            _memberId,
            new MapFeatureUpdateDto
            {
                LayerId = regionsLayer.MapLayerId,
                Name = "Updated",
                Color = "green",
                Polygon = new PolygonGeometryDto
                {
                    Type = "Polygon",
                    Coordinates =
                    [
                        [
                            [0.2f, 0.2f],
                            [0.5f, 0.2f],
                            [0.3f, 0.6f],
                        ],
                    ],
                },
            });

        Assert.Equal(regionsLayer.MapLayerId, updated.LayerId);
        Assert.Equal("Updated", updated.Name);
        Assert.Equal("green", updated.Color);
        Assert.Equal("\"etag-2\"", updated.Geometry!.ETag);
        await _blobStore.Received(1).DeleteFeatureGeometryAsync(created.Geometry!.BlobKey, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteFeatureAsync_Polygon_DeletesBlobAndEntity()
    {
        var map = await CreateMapWithDefaultLayersAsync("Polygon Delete");
        var layerId = (await GetMapLayersByNameAsync(map.WorldMapId))["World"];

        _blobStore.SaveFeatureGeometryAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("\"etag-delete\"");
        _blobStore.LoadFeatureGeometryAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("""{"type":"Polygon","coordinates":[[[0.2,0.2],[0.5,0.2],[0.3,0.6],[0.2,0.2]]]}""");

        var created = await _sut.CreateFeatureAsync(
            _worldId,
            map.WorldMapId,
            _memberId,
            new MapFeatureCreateDto
            {
                FeatureType = MapFeatureType.Polygon,
                LayerId = layerId,
                Polygon = new PolygonGeometryDto
                {
                    Type = "Polygon",
                    Coordinates =
                    [
                        [
                            [0.2f, 0.2f],
                            [0.5f, 0.2f],
                            [0.3f, 0.6f],
                        ],
                    ],
                },
            });

        await _sut.DeleteFeatureAsync(_worldId, map.WorldMapId, created.FeatureId, _memberId);

        Assert.False(await _db.MapFeatures.AnyAsync(feature => feature.MapFeatureId == created.FeatureId));
        await _blobStore.Received(1).DeleteFeatureGeometryAsync(created.Geometry!.BlobKey, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteLayer_ThrowsArgumentException_WhenPolygonReferencesLayer()
    {
        var map = await CreateMapWithDefaultLayersAsync("Polygon Layer Delete Guard");
        var customLayer = await _sut.CreateLayerAsync(_worldId, map.WorldMapId, _memberId, "Regions");

        _blobStore.SaveFeatureGeometryAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("\"etag-guard\"");
        _blobStore.LoadFeatureGeometryAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("""{"type":"Polygon","coordinates":[[[0.2,0.2],[0.5,0.2],[0.3,0.6],[0.2,0.2]]]}""");

        _ = await _sut.CreateFeatureAsync(
            _worldId,
            map.WorldMapId,
            _memberId,
            new MapFeatureCreateDto
            {
                FeatureType = MapFeatureType.Polygon,
                LayerId = customLayer.MapLayerId,
                Polygon = new PolygonGeometryDto
                {
                    Type = "Polygon",
                    Coordinates =
                    [
                        [
                            [0.2f, 0.2f],
                            [0.5f, 0.2f],
                            [0.3f, 0.6f],
                        ],
                    ],
                },
            });

        var service = (IWorldMapService)_sut;
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => service.DeleteLayer(_worldId, map.WorldMapId, _memberId, customLayer.MapLayerId));

        Assert.Contains("features reference", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    // ── SessionNoteMapFeature ────────────────────────────────────────────────

    [Fact]
    public async Task AddFeatureToSessionNoteAsync_AllowsMultipleFeaturesPerNote()
    {
        var map = await CreateMapWithDefaultLayersAsync("Session Note Feature Links");
        var firstFeature = await CreatePointFeatureAsync(map.WorldMapId, "Blackroot Ford", 0.2f, 0.3f);
        var secondFeature = await CreatePointFeatureAsync(map.WorldMapId, "South Gate", 0.5f, 0.6f);
        var sessionNote = await CreateSessionNoteArticleAsync("Session 3");

        await _sut.AddFeatureToSessionNoteAsync(_worldId, sessionNote.Id, firstFeature.FeatureId, _memberId);
        await _sut.AddFeatureToSessionNoteAsync(_worldId, sessionNote.Id, secondFeature.FeatureId, _memberId);

        var linked = await _sut.ListFeaturesForSessionNoteAsync(_worldId, sessionNote.Id, _memberId);

        Assert.Equal(2, linked.Count);
        Assert.Contains(linked, feature => feature.FeatureId == firstFeature.FeatureId);
        Assert.Contains(linked, feature => feature.FeatureId == secondFeature.FeatureId);
        Assert.Equal(2, await _db.SessionNoteMapFeatures.CountAsync(link => link.SessionNoteId == sessionNote.Id));
    }

    [Fact]
    public async Task AddFeatureToSessionNoteAsync_AllowsSameFeatureAcrossMultipleNotes()
    {
        var map = await CreateMapWithDefaultLayersAsync("Shared Session Feature");
        var feature = await CreatePointFeatureAsync(map.WorldMapId, "Ruined Watchtower", 0.4f, 0.4f);
        var firstNote = await CreateSessionNoteArticleAsync("Session 8");
        var secondNote = await CreateSessionNoteArticleAsync("Session 11");

        await _sut.AddFeatureToSessionNoteAsync(_worldId, firstNote.Id, feature.FeatureId, _memberId);
        await _sut.AddFeatureToSessionNoteAsync(_worldId, secondNote.Id, feature.FeatureId, _memberId);

        var firstLinked = await _sut.ListFeaturesForSessionNoteAsync(_worldId, firstNote.Id, _memberId);
        var secondLinked = await _sut.ListFeaturesForSessionNoteAsync(_worldId, secondNote.Id, _memberId);

        Assert.Single(firstLinked);
        Assert.Single(secondLinked);
        Assert.Equal(feature.FeatureId, firstLinked[0].FeatureId);
        Assert.Equal(feature.FeatureId, secondLinked[0].FeatureId);
        Assert.Equal(2, await _db.SessionNoteMapFeatures.CountAsync(link => link.MapFeatureId == feature.FeatureId));
    }

    [Fact]
    public async Task RemoveFeatureFromSessionNoteAsync_RemovesOnlyRequestedLink()
    {
        var map = await CreateMapWithDefaultLayersAsync("Remove Session Feature");
        var feature = await CreatePointFeatureAsync(map.WorldMapId, "Blackroot Ford", 0.1f, 0.1f);
        var firstNote = await CreateSessionNoteArticleAsync("Session 1");
        var secondNote = await CreateSessionNoteArticleAsync("Session 2");

        await _sut.AddFeatureToSessionNoteAsync(_worldId, firstNote.Id, feature.FeatureId, _memberId);
        await _sut.AddFeatureToSessionNoteAsync(_worldId, secondNote.Id, feature.FeatureId, _memberId);

        await _sut.RemoveFeatureFromSessionNoteAsync(_worldId, firstNote.Id, feature.FeatureId, _memberId);

        Assert.Empty(await _sut.ListFeaturesForSessionNoteAsync(_worldId, firstNote.Id, _memberId));
        var remaining = await _sut.ListFeaturesForSessionNoteAsync(_worldId, secondNote.Id, _memberId);
        Assert.Single(remaining);
        Assert.Equal(1, await _db.SessionNoteMapFeatures.CountAsync(link => link.MapFeatureId == feature.FeatureId));
    }

    [Fact]
    public async Task SessionNoteFeatureMethods_EnforceAuthorization()
    {
        var map = await CreateMapWithDefaultLayersAsync("Unauthorized Session Feature");
        var feature = await CreatePointFeatureAsync(map.WorldMapId, "South Gate", 0.7f, 0.2f);
        var sessionNote = await CreateSessionNoteArticleAsync("Session 9");

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _sut.AddFeatureToSessionNoteAsync(_worldId, sessionNote.Id, feature.FeatureId, _outsiderId));

        await _sut.AddFeatureToSessionNoteAsync(_worldId, sessionNote.Id, feature.FeatureId, _memberId);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _sut.ListFeaturesForSessionNoteAsync(_worldId, sessionNote.Id, _outsiderId));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _sut.RemoveFeatureFromSessionNoteAsync(_worldId, sessionNote.Id, feature.FeatureId, _outsiderId));
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

    private async Task<Article> CreateSessionNoteArticleAsync(string title)
    {
        var article = new Article
        {
            Id = Guid.NewGuid(),
            WorldId = _worldId,
            Title = title,
            Slug = title.ToLowerInvariant().Replace(' ', '-'),
            Type = ArticleType.SessionNote,
            Visibility = ArticleVisibility.Public,
            CreatedBy = _ownerId,
            CreatedAt = DateTime.UtcNow,
            EffectiveDate = DateTime.UtcNow,
        };

        _db.Articles.Add(article);
        await _db.SaveChangesAsync();
        return article;
    }

    private async Task<MapFeatureDto> CreatePointFeatureAsync(Guid mapId, string? name, float x, float y)
    {
        var layerId = (await GetMapLayersByNameAsync(mapId))["World"];

        return await _sut.CreateFeatureAsync(
            _worldId,
            mapId,
            _memberId,
            new MapFeatureCreateDto
            {
                FeatureType = MapFeatureType.Point,
                LayerId = layerId,
                Name = name,
                Point = new MapFeaturePointDto
                {
                    X = x,
                    Y = y,
                },
            });
    }
}

