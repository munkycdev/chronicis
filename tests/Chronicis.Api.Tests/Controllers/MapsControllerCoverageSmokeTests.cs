using System.Diagnostics.CodeAnalysis;
using Chronicis.Api.Controllers;
using Chronicis.Api.Services;
using Chronicis.Shared.DTOs.Maps;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace Chronicis.Api.Tests;

[ExcludeFromCodeCoverage]
public class MapsControllerCoverageSmokeTests
{
    private static MapsController CreateSut(IWorldMapService? service = null) =>
        new(
            service ?? Substitute.For<IWorldMapService>(),
            ControllerCoverageTestFixtures.CreateCurrentUserService(),
            NullLogger<MapsController>.Instance);

    // ── CreateMap ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateMap_NullDto_ReturnsBadRequest()
    {
        var result = await CreateSut().CreateMap(Guid.NewGuid(), null!);
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task CreateMap_EmptyName_ReturnsBadRequest()
    {
        var result = await CreateSut().CreateMap(Guid.NewGuid(), new MapCreateDto { Name = "  " });
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task CreateMap_Unauthorized_Returns403()
    {
        var service = Substitute.For<IWorldMapService>();
        service.CreateMapAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<MapCreateDto>())
            .ThrowsAsync(new UnauthorizedAccessException("denied"));

        var result = await CreateSut(service).CreateMap(Guid.NewGuid(), new MapCreateDto { Name = "Map" });

        var status = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(403, status.StatusCode);
    }

    [Fact]
    public async Task CreateMap_Success_ReturnsOk()
    {
        var service = Substitute.For<IWorldMapService>();
        service.CreateMapAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<MapCreateDto>())
            .Returns(new MapDto { Name = "Map" });

        var result = await CreateSut(service).CreateMap(Guid.NewGuid(), new MapCreateDto { Name = "Map" });

        Assert.IsType<OkObjectResult>(result.Result);
    }

    // ── GetMap ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetMap_NotFound_ReturnsNotFound()
    {
        var service = Substitute.For<IWorldMapService>();
        service.GetMapAsync(Arg.Any<Guid>(), Arg.Any<Guid>())
            .Returns((MapDto?)null);

        var result = await CreateSut(service).GetMap(Guid.NewGuid(), Guid.NewGuid());

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetMap_Found_ReturnsOk()
    {
        var mapId = Guid.NewGuid();
        var service = Substitute.For<IWorldMapService>();
        service.GetMapAsync(mapId, Arg.Any<Guid>())
            .Returns(new MapDto { WorldMapId = mapId });

        var result = await CreateSut(service).GetMap(Guid.NewGuid(), mapId);

        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task ListLayers_Unauthorized_Returns403()
    {
        var service = Substitute.For<IWorldMapService>();
        service.ListLayersForMapAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>())
            .ThrowsAsync(new UnauthorizedAccessException("denied"));

        var result = await CreateSut(service).ListLayers(Guid.NewGuid(), Guid.NewGuid());

        var status = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(403, status.StatusCode);
    }

    [Fact]
    public async Task ListLayers_NotFound_Returns404()
    {
        var service = Substitute.For<IWorldMapService>();
        service.ListLayersForMapAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>())
            .ThrowsAsync(new InvalidOperationException("Map not found"));

        var result = await CreateSut(service).ListLayers(Guid.NewGuid(), Guid.NewGuid());

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task ListLayers_Success_ReturnsOk()
    {
        var service = Substitute.For<IWorldMapService>();
        service.ListLayersForMapAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>())
            .Returns(new List<MapLayerDto>());

        var result = await CreateSut(service).ListLayers(Guid.NewGuid(), Guid.NewGuid());

        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task CreateLayer_NullDto_ReturnsBadRequest()
    {
        var result = await CreateSut().CreateLayer(Guid.NewGuid(), Guid.NewGuid(), null!);
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task CreateLayer_EmptyName_ReturnsBadRequest()
    {
        var result = await CreateSut().CreateLayer(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new CreateLayerRequest { Name = "  " });

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task CreateLayer_Unauthorized_ReturnsForbidden()
    {
        var service = Substitute.For<IWorldMapService>();
        service.CreateLayer(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<Guid?>())
            .ThrowsAsync(new UnauthorizedAccessException("denied"));

        var result = await CreateSut(service).CreateLayer(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new CreateLayerRequest { Name = "Cities" });

        Assert.IsType<ForbidResult>(result.Result);
    }

    [Fact]
    public async Task CreateLayer_ArgumentException_ReturnsBadRequest()
    {
        var service = Substitute.For<IWorldMapService>();
        service.CreateLayer(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<Guid?>())
            .ThrowsAsync(new ArgumentException("invalid"));

        var result = await CreateSut(service).CreateLayer(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new CreateLayerRequest { Name = "Cities" });

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal(400, badRequest.StatusCode);
    }

    [Fact]
    public async Task CreateLayer_NonexistentParent_ReturnsBadRequest()
    {
        var service = Substitute.For<IWorldMapService>();
        service.CreateLayer(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<Guid?>())
            .ThrowsAsync(new ArgumentException("Parent layer not found"));

        var result = await CreateSut(service).CreateLayer(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new CreateLayerRequest
            {
                Name = "Child",
                ParentLayerId = Guid.NewGuid(),
            });

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task CreateLayer_CrossMapParent_ReturnsBadRequest()
    {
        var service = Substitute.For<IWorldMapService>();
        service.CreateLayer(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<Guid?>())
            .ThrowsAsync(new ArgumentException("Parent layer does not belong to map"));

        var result = await CreateSut(service).CreateLayer(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new CreateLayerRequest
            {
                Name = "Child",
                ParentLayerId = Guid.NewGuid(),
            });

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task CreateLayer_Success_ReturnsOk()
    {
        var service = Substitute.For<IWorldMapService>();
        service.CreateLayer(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<Guid?>())
            .Returns(new MapLayerDto { MapLayerId = Guid.NewGuid(), Name = "Cities", SortOrder = 3, IsEnabled = true });

        var result = await CreateSut(service).CreateLayer(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new CreateLayerRequest { Name = "Cities" });

        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task CreateLayer_WithoutParentId_PassesNullParentToService()
    {
        var service = Substitute.For<IWorldMapService>();
        service.CreateLayer(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<Guid?>())
            .Returns(new MapLayerDto { MapLayerId = Guid.NewGuid(), Name = "Cities", SortOrder = 3, IsEnabled = true });

        var worldId = Guid.NewGuid();
        var mapId = Guid.NewGuid();
        var result = await CreateSut(service).CreateLayer(
            worldId,
            mapId,
            new CreateLayerRequest { Name = "Cities" });

        Assert.IsType<OkObjectResult>(result.Result);
        await service.Received(1).CreateLayer(worldId, mapId, Arg.Any<Guid>(), "Cities", null);
    }

    [Fact]
    public async Task CreateLayer_WithParentId_PassesParentToService()
    {
        var service = Substitute.For<IWorldMapService>();
        service.CreateLayer(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<Guid?>())
            .Returns(new MapLayerDto { MapLayerId = Guid.NewGuid(), Name = "Child", SortOrder = 0, IsEnabled = true });

        var worldId = Guid.NewGuid();
        var mapId = Guid.NewGuid();
        var parentId = Guid.NewGuid();

        var result = await CreateSut(service).CreateLayer(
            worldId,
            mapId,
            new CreateLayerRequest
            {
                Name = "Child",
                ParentLayerId = parentId,
            });

        Assert.IsType<OkObjectResult>(result.Result);
        await service.Received(1).CreateLayer(worldId, mapId, Arg.Any<Guid>(), "Child", parentId);
    }

    [Fact]
    public async Task RenameLayer_Success_ReturnsNoContent()
    {
        var service = Substitute.For<IWorldMapService>();
        service.RenameLayer(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<string>())
            .Returns(Task.CompletedTask);

        var result = await CreateSut(service).RenameLayer(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            new RenameLayerRequest { Name = "Settlements" });

        var noContent = Assert.IsType<NoContentResult>(result);
        Assert.Equal(204, noContent.StatusCode);
    }

    [Fact]
    public async Task RenameLayer_NullRequest_ReturnsBadRequest()
    {
        var result = await CreateSut().RenameLayer(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null!);
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task RenameLayer_EmptyName_ReturnsBadRequest()
    {
        var result = await CreateSut().RenameLayer(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            new RenameLayerRequest { Name = "   " });

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task RenameLayer_ArgumentException_ReturnsBadRequest()
    {
        var service = Substitute.For<IWorldMapService>();
        service.RenameLayer(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<string>())
            .ThrowsAsync(new ArgumentException("invalid"));

        var result = await CreateSut(service).RenameLayer(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            new RenameLayerRequest { Name = "Settlements" });

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, badRequest.StatusCode);
    }

    [Fact]
    public async Task RenameLayer_Unauthorized_ReturnsForbidden()
    {
        var service = Substitute.For<IWorldMapService>();
        service.RenameLayer(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<string>())
            .ThrowsAsync(new UnauthorizedAccessException("denied"));

        var result = await CreateSut(service).RenameLayer(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            new RenameLayerRequest { Name = "Settlements" });

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task SetLayerParent_NullRequest_ReturnsBadRequest()
    {
        var result = await CreateSut().SetLayerParent(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null!);
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task SetLayerParent_ArgumentException_ReturnsBadRequest()
    {
        var service = Substitute.For<IWorldMapService>();
        service.SetLayerParent(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid?>())
            .ThrowsAsync(new ArgumentException("invalid"));

        var result = await CreateSut(service).SetLayerParent(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            new SetLayerParentRequest { ParentLayerId = Guid.NewGuid() });

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, badRequest.StatusCode);
    }

    [Fact]
    public async Task SetLayerParent_Unauthorized_ReturnsForbidden()
    {
        var service = Substitute.For<IWorldMapService>();
        service.SetLayerParent(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid?>())
            .ThrowsAsync(new UnauthorizedAccessException("denied"));

        var result = await CreateSut(service).SetLayerParent(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            new SetLayerParentRequest { ParentLayerId = Guid.NewGuid() });

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task SetLayerParent_Success_WithParentId_ReturnsNoContent()
    {
        var service = Substitute.For<IWorldMapService>();
        service.SetLayerParent(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid?>())
            .Returns(Task.CompletedTask);

        var worldId = Guid.NewGuid();
        var mapId = Guid.NewGuid();
        var layerId = Guid.NewGuid();
        var parentId = Guid.NewGuid();

        var result = await CreateSut(service).SetLayerParent(
            worldId,
            mapId,
            layerId,
            new SetLayerParentRequest { ParentLayerId = parentId });

        Assert.IsType<NoContentResult>(result);
        await service.Received(1)
            .SetLayerParent(worldId, mapId, Arg.Any<Guid>(), layerId, parentId);
    }

    [Fact]
    public async Task SetLayerParent_Success_WithNullParent_ReturnsNoContent()
    {
        var service = Substitute.For<IWorldMapService>();
        service.SetLayerParent(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid?>())
            .Returns(Task.CompletedTask);

        var worldId = Guid.NewGuid();
        var mapId = Guid.NewGuid();
        var layerId = Guid.NewGuid();

        var result = await CreateSut(service).SetLayerParent(
            worldId,
            mapId,
            layerId,
            new SetLayerParentRequest { ParentLayerId = null });

        Assert.IsType<NoContentResult>(result);
        await service.Received(1)
            .SetLayerParent(worldId, mapId, Arg.Any<Guid>(), layerId, null);
    }

    [Fact]
    public async Task DeleteLayer_Success_ReturnsNoContent()
    {
        var service = Substitute.For<IWorldMapService>();
        service.DeleteLayer(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>())
            .Returns(Task.CompletedTask);

        var result = await CreateSut(service).DeleteLayer(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        var noContent = Assert.IsType<NoContentResult>(result);
        Assert.Equal(204, noContent.StatusCode);
    }

    [Fact]
    public async Task DeleteLayer_ArgumentException_ReturnsBadRequest()
    {
        var service = Substitute.For<IWorldMapService>();
        service.DeleteLayer(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>())
            .ThrowsAsync(new ArgumentException("invalid"));

        var result = await CreateSut(service).DeleteLayer(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, badRequest.StatusCode);
    }

    [Fact]
    public async Task DeleteLayer_Unauthorized_ReturnsForbidden()
    {
        var service = Substitute.For<IWorldMapService>();
        service.DeleteLayer(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>())
            .ThrowsAsync(new UnauthorizedAccessException("denied"));

        var result = await CreateSut(service).DeleteLayer(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        Assert.IsType<ForbidResult>(result);
    }

    // ── UpdateMap ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateMap_NullDto_ReturnsBadRequest()
    {
        var result = await CreateSut().UpdateMap(Guid.NewGuid(), Guid.NewGuid(), null!);
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task UpdateMap_EmptyName_ReturnsBadRequest()
    {
        var result = await CreateSut().UpdateMap(Guid.NewGuid(), Guid.NewGuid(), new MapUpdateDto { Name = "  " });
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task UpdateMap_Unauthorized_Returns403()
    {
        var service = Substitute.For<IWorldMapService>();
        service.UpdateMapAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<MapUpdateDto>())
            .ThrowsAsync(new UnauthorizedAccessException("denied"));

        var result = await CreateSut(service).UpdateMap(Guid.NewGuid(), Guid.NewGuid(), new MapUpdateDto { Name = "Renamed" });

        var status = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(403, status.StatusCode);
    }

    [Fact]
    public async Task UpdateMap_NotFound_Returns404()
    {
        var service = Substitute.For<IWorldMapService>();
        service.UpdateMapAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<MapUpdateDto>())
            .ThrowsAsync(new InvalidOperationException("Map not found"));

        var result = await CreateSut(service).UpdateMap(Guid.NewGuid(), Guid.NewGuid(), new MapUpdateDto { Name = "Renamed" });

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task UpdateMap_Success_ReturnsOk()
    {
        var mapId = Guid.NewGuid();
        var service = Substitute.For<IWorldMapService>();
        service.UpdateMapAsync(Arg.Any<Guid>(), mapId, Arg.Any<Guid>(), Arg.Any<MapUpdateDto>())
            .Returns(new MapDto { WorldMapId = mapId, Name = "Renamed" });

        var result = await CreateSut(service).UpdateMap(Guid.NewGuid(), mapId, new MapUpdateDto { Name = "Renamed" });

        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task UpdateLayerVisibility_Success_ReturnsNoContent()
    {
        var service = Substitute.For<IWorldMapService>();
        service.UpdateLayerVisibility(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<bool>())
            .Returns(Task.CompletedTask);

        var result = await CreateSut(service).UpdateLayerVisibility(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            new UpdateLayerVisibilityRequest { IsEnabled = true });

        var noContent = Assert.IsType<NoContentResult>(result);
        Assert.Equal(204, noContent.StatusCode);
    }

    [Fact]
    public async Task UpdateLayerVisibility_Unauthorized_ReturnsForbidden()
    {
        var service = Substitute.For<IWorldMapService>();
        service.UpdateLayerVisibility(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<bool>())
            .ThrowsAsync(new UnauthorizedAccessException("denied"));

        var result = await CreateSut(service).UpdateLayerVisibility(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            new UpdateLayerVisibilityRequest { IsEnabled = false });

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task UpdateLayerVisibility_ArgumentException_ReturnsBadRequest()
    {
        var service = Substitute.For<IWorldMapService>();
        service.UpdateLayerVisibility(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<bool>())
            .ThrowsAsync(new ArgumentException("invalid"));

        var result = await CreateSut(service).UpdateLayerVisibility(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            new UpdateLayerVisibilityRequest { IsEnabled = false });

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, badRequest.StatusCode);
    }

    [Fact]
    public async Task ReorderLayers_Success_ReturnsNoContent()
    {
        var service = Substitute.For<IWorldMapService>();
        service.ReorderLayers(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<IList<Guid>>())
            .Returns(Task.CompletedTask);

        var result = await CreateSut(service).ReorderLayers(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new ReorderLayersRequest { LayerIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() } });

        var noContent = Assert.IsType<NoContentResult>(result);
        Assert.Equal(204, noContent.StatusCode);
    }

    [Fact]
    public async Task ReorderLayers_Unauthorized_ReturnsForbidden()
    {
        var service = Substitute.For<IWorldMapService>();
        service.ReorderLayers(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<IList<Guid>>())
            .ThrowsAsync(new UnauthorizedAccessException("denied"));

        var result = await CreateSut(service).ReorderLayers(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new ReorderLayersRequest { LayerIds = new List<Guid> { Guid.NewGuid() } });

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task ReorderLayers_ArgumentException_ReturnsBadRequest()
    {
        var service = Substitute.For<IWorldMapService>();
        service.ReorderLayers(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<IList<Guid>>())
            .ThrowsAsync(new ArgumentException("invalid"));

        var result = await CreateSut(service).ReorderLayers(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new ReorderLayersRequest { LayerIds = new List<Guid> { Guid.NewGuid() } });

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, badRequest.StatusCode);
    }

    // ── ListMapsForWorld ──────────────────────────────────────────────────────

    [Fact]
    public async Task ListMapsForWorld_Unauthorized_Returns403()
    {
        var service = Substitute.For<IWorldMapService>();
        service.ListMapsForWorldAsync(Arg.Any<Guid>(), Arg.Any<Guid>())
            .ThrowsAsync(new UnauthorizedAccessException("denied"));

        var result = await CreateSut(service).ListMapsForWorld(Guid.NewGuid());

        var status = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(403, status.StatusCode);
    }

    [Fact]
    public async Task ListMapsForWorld_Success_ReturnsOk()
    {
        var service = Substitute.For<IWorldMapService>();
        service.ListMapsForWorldAsync(Arg.Any<Guid>(), Arg.Any<Guid>())
            .Returns(new List<MapSummaryDto>());

        var result = await CreateSut(service).ListMapsForWorld(Guid.NewGuid());

        Assert.IsType<OkObjectResult>(result.Result);
    }

    // ── AutocompleteMaps ──────────────────────────────────────────────────────

    [Fact]
    public async Task AutocompleteMaps_Unauthorized_Returns403()
    {
        var service = Substitute.For<IWorldMapService>();
        service.SearchMapsForWorldAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<string?>())
            .ThrowsAsync(new UnauthorizedAccessException("denied"));

        var result = await CreateSut(service).AutocompleteMaps(Guid.NewGuid(), "map");

        var status = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(403, status.StatusCode);
    }

    [Fact]
    public async Task AutocompleteMaps_Success_ReturnsOk()
    {
        var service = Substitute.For<IWorldMapService>();
        service.SearchMapsForWorldAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<string?>())
            .Returns(new List<MapAutocompleteDto>());

        var result = await CreateSut(service).AutocompleteMaps(Guid.NewGuid(), "map");

        Assert.IsType<OkObjectResult>(result.Result);
    }

    // ── Pins ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreatePin_NullDto_ReturnsBadRequest()
    {
        var result = await CreateSut().CreatePin(Guid.NewGuid(), Guid.NewGuid(), null!);
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task CreatePin_Unauthorized_Returns403()
    {
        var service = Substitute.For<IWorldMapService>();
        service.CreatePinAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<MapPinCreateDto>())
            .ThrowsAsync(new UnauthorizedAccessException("denied"));

        var result = await CreateSut(service)
            .CreatePin(Guid.NewGuid(), Guid.NewGuid(), new MapPinCreateDto { X = 0.2f, Y = 0.3f });

        var status = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(403, status.StatusCode);
    }

    [Fact]
    public async Task CreatePin_InvalidInput_ReturnsBadRequest()
    {
        var service = Substitute.For<IWorldMapService>();
        service.CreatePinAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<MapPinCreateDto>())
            .ThrowsAsync(new ArgumentException("invalid"));

        var result = await CreateSut(service)
            .CreatePin(Guid.NewGuid(), Guid.NewGuid(), new MapPinCreateDto { X = -1f, Y = 0.3f });

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task CreatePin_MapNotFound_Returns404()
    {
        var service = Substitute.For<IWorldMapService>();
        service.CreatePinAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<MapPinCreateDto>())
            .ThrowsAsync(new InvalidOperationException("Map not found"));

        var result = await CreateSut(service)
            .CreatePin(Guid.NewGuid(), Guid.NewGuid(), new MapPinCreateDto { X = 0.2f, Y = 0.3f });

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task CreatePin_Success_ReturnsOk()
    {
        var service = Substitute.For<IWorldMapService>();
        service.CreatePinAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<MapPinCreateDto>())
            .Returns(new MapPinResponseDto { PinId = Guid.NewGuid() });

        var result = await CreateSut(service)
            .CreatePin(Guid.NewGuid(), Guid.NewGuid(), new MapPinCreateDto { X = 0.2f, Y = 0.3f });

        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task ListPins_Unauthorized_Returns403()
    {
        var service = Substitute.For<IWorldMapService>();
        service.ListPinsForMapAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>())
            .ThrowsAsync(new UnauthorizedAccessException("denied"));

        var result = await CreateSut(service).ListPins(Guid.NewGuid(), Guid.NewGuid());

        var status = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(403, status.StatusCode);
    }

    [Fact]
    public async Task ListPins_NotFound_Returns404()
    {
        var service = Substitute.For<IWorldMapService>();
        service.ListPinsForMapAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>())
            .ThrowsAsync(new InvalidOperationException("Map not found"));

        var result = await CreateSut(service).ListPins(Guid.NewGuid(), Guid.NewGuid());

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task ListPins_Success_ReturnsOk()
    {
        var service = Substitute.For<IWorldMapService>();
        service.ListPinsForMapAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>())
            .Returns(new List<MapPinResponseDto>());

        var result = await CreateSut(service).ListPins(Guid.NewGuid(), Guid.NewGuid());

        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task UpdatePinPosition_NullDto_ReturnsBadRequest()
    {
        var result = await CreateSut().UpdatePinPosition(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null!);
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task UpdatePinPosition_Unauthorized_Returns403()
    {
        var service = Substitute.For<IWorldMapService>();
        service.UpdatePinPositionAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<MapPinPositionUpdateDto>())
            .ThrowsAsync(new UnauthorizedAccessException("denied"));

        var result = await CreateSut(service)
            .UpdatePinPosition(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), new MapPinPositionUpdateDto { X = 0.1f, Y = 0.2f });

        var status = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(403, status.StatusCode);
    }

    [Fact]
    public async Task UpdatePinPosition_InvalidInput_ReturnsBadRequest()
    {
        var service = Substitute.For<IWorldMapService>();
        service.UpdatePinPositionAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<MapPinPositionUpdateDto>())
            .ThrowsAsync(new ArgumentException("invalid"));

        var result = await CreateSut(service)
            .UpdatePinPosition(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), new MapPinPositionUpdateDto { X = 2f, Y = 0.2f });

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task UpdatePinPosition_NotFound_Returns404()
    {
        var service = Substitute.For<IWorldMapService>();
        service.UpdatePinPositionAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<MapPinPositionUpdateDto>())
            .ThrowsAsync(new InvalidOperationException("Pin not found"));

        var result = await CreateSut(service)
            .UpdatePinPosition(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), new MapPinPositionUpdateDto { X = 0.1f, Y = 0.2f });

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task UpdatePinPosition_Success_ReturnsOk()
    {
        var service = Substitute.For<IWorldMapService>();
        service.UpdatePinPositionAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<MapPinPositionUpdateDto>())
            .Returns(new MapPinResponseDto { PinId = Guid.NewGuid(), X = 0.7f, Y = 0.8f });

        var result = await CreateSut(service)
            .UpdatePinPosition(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), new MapPinPositionUpdateDto { X = 0.7f, Y = 0.8f });

        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task DeletePin_Unauthorized_Returns403()
    {
        var service = Substitute.For<IWorldMapService>();
        service.DeletePinAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>())
            .ThrowsAsync(new UnauthorizedAccessException("denied"));

        var result = await CreateSut(service).DeletePin(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        var status = Assert.IsType<ObjectResult>(result);
        Assert.Equal(403, status.StatusCode);
    }

    [Fact]
    public async Task DeletePin_NotFound_Returns404()
    {
        var service = Substitute.For<IWorldMapService>();
        service.DeletePinAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>())
            .ThrowsAsync(new InvalidOperationException("Pin not found"));

        var result = await CreateSut(service).DeletePin(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task DeletePin_Success_ReturnsNoContent()
    {
        var service = Substitute.For<IWorldMapService>();
        service.DeletePinAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>())
            .Returns(Task.CompletedTask);

        var result = await CreateSut(service).DeletePin(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        Assert.IsType<NoContentResult>(result);
    }

    // ── DeleteMap ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteMap_Unauthorized_Returns403()
    {
        var service = Substitute.For<IWorldMapService>();
        service.DeleteMapAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>())
            .ThrowsAsync(new UnauthorizedAccessException("denied"));

        var result = await CreateSut(service).DeleteMap(Guid.NewGuid(), Guid.NewGuid());

        var status = Assert.IsType<ObjectResult>(result);
        Assert.Equal(403, status.StatusCode);
    }

    [Fact]
    public async Task DeleteMap_NotFound_Returns404()
    {
        var service = Substitute.For<IWorldMapService>();
        service.DeleteMapAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>())
            .ThrowsAsync(new InvalidOperationException("Map not found"));

        var result = await CreateSut(service).DeleteMap(Guid.NewGuid(), Guid.NewGuid());

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task DeleteMap_Success_ReturnsNoContent()
    {
        var service = Substitute.For<IWorldMapService>();
        service.DeleteMapAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>())
            .Returns(Task.CompletedTask);

        var result = await CreateSut(service).DeleteMap(Guid.NewGuid(), Guid.NewGuid());

        Assert.IsType<NoContentResult>(result);
    }

    // ── RequestBasemapUpload ──────────────────────────────────────────────────

    [Fact]
    public async Task RequestBasemapUpload_NullDto_ReturnsBadRequest()
    {
        var result = await CreateSut().RequestBasemapUpload(Guid.NewGuid(), Guid.NewGuid(), null!);
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task RequestBasemapUpload_Unauthorized_Returns403()
    {
        var service = Substitute.For<IWorldMapService>();
        service.RequestBasemapUploadAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<RequestBasemapUploadDto>())
            .ThrowsAsync(new UnauthorizedAccessException("denied"));

        var result = await CreateSut(service)
            .RequestBasemapUpload(Guid.NewGuid(), Guid.NewGuid(), new RequestBasemapUploadDto { FileName = "f.png", ContentType = "image/png" });

        var status = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(403, status.StatusCode);
    }

    [Fact]
    public async Task RequestBasemapUpload_InvalidContentType_ReturnsBadRequest()
    {
        var service = Substitute.For<IWorldMapService>();
        service.RequestBasemapUploadAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<RequestBasemapUploadDto>())
            .ThrowsAsync(new ArgumentException("unsupported"));

        var result = await CreateSut(service)
            .RequestBasemapUpload(Guid.NewGuid(), Guid.NewGuid(), new RequestBasemapUploadDto { FileName = "f.bmp", ContentType = "image/bmp" });

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task RequestBasemapUpload_MapNotFound_ReturnsNotFound()
    {
        var service = Substitute.For<IWorldMapService>();
        service.RequestBasemapUploadAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<RequestBasemapUploadDto>())
            .ThrowsAsync(new InvalidOperationException("Map not found"));

        var result = await CreateSut(service)
            .RequestBasemapUpload(Guid.NewGuid(), Guid.NewGuid(), new RequestBasemapUploadDto { FileName = "f.png", ContentType = "image/png" });

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task RequestBasemapUpload_Success_ReturnsOk()
    {
        var service = Substitute.For<IWorldMapService>();
        service.RequestBasemapUploadAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<RequestBasemapUploadDto>())
            .Returns(new RequestBasemapUploadResponseDto { UploadUrl = "https://sas" });

        var result = await CreateSut(service)
            .RequestBasemapUpload(Guid.NewGuid(), Guid.NewGuid(), new RequestBasemapUploadDto { FileName = "f.png", ContentType = "image/png" });

        Assert.IsType<OkObjectResult>(result.Result);
    }

    // ── ConfirmBasemapUpload ──────────────────────────────────────────────────

    [Fact]
    public async Task ConfirmBasemapUpload_Unauthorized_Returns403()
    {
        var service = Substitute.For<IWorldMapService>();
        service.ConfirmBasemapUploadAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>())
            .ThrowsAsync(new UnauthorizedAccessException("denied"));

        var result = await CreateSut(service).ConfirmBasemapUpload(Guid.NewGuid(), Guid.NewGuid());

        var status = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(403, status.StatusCode);
    }

    [Fact]
    public async Task ConfirmBasemapUpload_MapNotFound_ReturnsNotFound()
    {
        var service = Substitute.For<IWorldMapService>();
        service.ConfirmBasemapUploadAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>())
            .ThrowsAsync(new InvalidOperationException("Map not found"));

        var result = await CreateSut(service).ConfirmBasemapUpload(Guid.NewGuid(), Guid.NewGuid());

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task ConfirmBasemapUpload_Success_ReturnsOk()
    {
        var mapId = Guid.NewGuid();
        var service = Substitute.For<IWorldMapService>();
        service.ConfirmBasemapUploadAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>())
            .Returns(new MapDto { WorldMapId = mapId, HasBasemap = true });

        var result = await CreateSut(service).ConfirmBasemapUpload(Guid.NewGuid(), mapId);

        Assert.IsType<OkObjectResult>(result.Result);
    }

    // ── GetBasemapReadUrl ────────────────────────────────────────────────────

    [Fact]
    public async Task GetBasemapReadUrl_Unauthorized_Returns403()
    {
        var service = Substitute.For<IWorldMapService>();
        service.GetBasemapReadUrlAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>())
            .ThrowsAsync(new UnauthorizedAccessException("denied"));

        var result = await CreateSut(service).GetBasemapReadUrl(Guid.NewGuid(), Guid.NewGuid());

        var status = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(403, status.StatusCode);
    }

    [Fact]
    public async Task GetBasemapReadUrl_NotFound_Returns404()
    {
        var service = Substitute.For<IWorldMapService>();
        service.GetBasemapReadUrlAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>())
            .ThrowsAsync(new InvalidOperationException("Map not found"));

        var result = await CreateSut(service).GetBasemapReadUrl(Guid.NewGuid(), Guid.NewGuid());

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetBasemapReadUrl_Success_ReturnsOk()
    {
        var service = Substitute.For<IWorldMapService>();
        service.GetBasemapReadUrlAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>())
            .Returns(new GetBasemapReadUrlResponseDto { ReadUrl = "https://blob.example.com/read" });

        var result = await CreateSut(service).GetBasemapReadUrl(Guid.NewGuid(), Guid.NewGuid());

        Assert.IsType<OkObjectResult>(result.Result);
    }
}
