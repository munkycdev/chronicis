using Chronicis.Api.Controllers;
using Chronicis.Api.Services;
using Chronicis.Shared.DTOs.Maps;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Chronicis.Api.Tests;

public class PublicControllerCoverageSmokeTests
{
    [Fact]
    public async Task PublicController_GetPublicWorld_EmptySlug_ReturnsBadRequest()
    {
        var service = Substitute.For<IPublicWorldService>();
        var sut = new PublicController(service, NullLogger<PublicController>.Instance);

        var result = await sut.GetPublicWorld("");

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task PublicController_GetPublicDocumentContent_NotFound_ReturnsNotFound()
    {
        var service = Substitute.For<IPublicWorldService>();
        service.GetPublicDocumentDownloadUrlAsync(Arg.Any<Guid>()).Returns((string?)null);
        var sut = new PublicController(service, NullLogger<PublicController>.Instance);

        var result = await sut.GetPublicDocumentContent(Guid.NewGuid());

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task PublicController_GetPublicDocumentContent_Found_ReturnsRedirect()
    {
        var service = Substitute.For<IPublicWorldService>();
        service.GetPublicDocumentDownloadUrlAsync(Arg.Any<Guid>()).Returns("https://cdn.test/image.png");
        var sut = new PublicController(service, NullLogger<PublicController>.Instance);

        var result = await sut.GetPublicDocumentContent(Guid.NewGuid());

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal("https://cdn.test/image.png", redirect.Url);
    }

    [Fact]
    public async Task PublicController_GetPublicMapBasemap_NotFound_ReturnsNotFound()
    {
        var service = Substitute.For<IPublicWorldService>();
        service.GetPublicMapBasemapReadUrlAsync("slug", Arg.Any<Guid>())
            .Returns((null, "Basemap is missing for this map."));
        var sut = new PublicController(service, NullLogger<PublicController>.Instance);

        var result = await sut.GetPublicMapBasemap("slug", Guid.NewGuid());

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task PublicController_GetPublicMapLayers_Found_ReturnsOk()
    {
        var service = Substitute.For<IPublicWorldService>();
        service.GetPublicMapLayersAsync("slug", Arg.Any<Guid>())
            .Returns(new List<MapLayerDto> { new() { MapLayerId = Guid.NewGuid(), Name = "World", SortOrder = 0, IsEnabled = true } });
        var sut = new PublicController(service, NullLogger<PublicController>.Instance);

        var result = await sut.GetPublicMapLayers("slug", Guid.NewGuid());

        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task PublicController_GetPublicMapPins_NotFound_ReturnsNotFound()
    {
        var service = Substitute.For<IPublicWorldService>();
        service.GetPublicMapPinsAsync("slug", Arg.Any<Guid>()).Returns((List<MapPinResponseDto>?)null);
        var sut = new PublicController(service, NullLogger<PublicController>.Instance);

        var result = await sut.GetPublicMapPins("slug", Guid.NewGuid());

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task PublicController_GetPublicMapFeatures_Found_ReturnsOk()
    {
        var service = Substitute.For<IPublicWorldService>();
        service.GetPublicMapFeaturesAsync("slug", Arg.Any<Guid>())
            .Returns(new List<MapFeatureDto>());
        var sut = new PublicController(service, NullLogger<PublicController>.Instance);

        var result = await sut.GetPublicMapFeatures("slug", Guid.NewGuid());

        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task PublicController_PublicMapEndpoints_EmptySlug_ReturnBadRequest()
    {
        var service = Substitute.For<IPublicWorldService>();
        var sut = new PublicController(service, NullLogger<PublicController>.Instance);

        Assert.IsType<BadRequestObjectResult>((await sut.GetPublicMapBasemap("", Guid.NewGuid())).Result);
        Assert.IsType<BadRequestObjectResult>((await sut.GetPublicMapLayers("", Guid.NewGuid())).Result);
        Assert.IsType<BadRequestObjectResult>((await sut.GetPublicMapPins("", Guid.NewGuid())).Result);
        Assert.IsType<BadRequestObjectResult>((await sut.GetPublicMapFeatures("", Guid.NewGuid())).Result);
    }
}
