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
