using Bunit;
using Chronicis.Client.Pages.Maps;
using Chronicis.Client.Services;
using Chronicis.Client.Tests.Components;
using Chronicis.Shared.DTOs.Maps;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Xunit;

namespace Chronicis.Client.Tests.Pages;

public class MapPageTests : MudBlazorTestContext
{
    private readonly IMapApiService _mapApi = Substitute.For<IMapApiService>();

    public MapPageTests()
    {
        Services.AddSingleton(_mapApi);
    }

    [Fact]
    public void MapPage_WhenLoading_RendersLoadingSkeleton()
    {
        var worldId = Guid.NewGuid();
        var mapId = Guid.NewGuid();
        var mapTcs = new TaskCompletionSource<(MapDto? Map, int? StatusCode, string? Error)>();

        _mapApi.GetMapAsync(worldId, mapId).Returns(mapTcs.Task);

        var cut = RenderPage(worldId, mapId);

        Assert.Contains("chronicis-loading-skeleton", cut.Markup, StringComparison.OrdinalIgnoreCase);

        mapTcs.SetResult((new MapDto { Name = "Map" }, 200, null));
        _mapApi.GetBasemapReadUrlAsync(worldId, mapId)
            .Returns((new GetBasemapReadUrlResponseDto { ReadUrl = "https://blob/read" }, 200, null));
    }

    [Fact]
    public void MapPage_WhenNotFound_RendersNotFoundState()
    {
        var worldId = Guid.NewGuid();
        var mapId = Guid.NewGuid();
        _mapApi.GetMapAsync(worldId, mapId).Returns((null, 404, "Map not found"));
        _mapApi.GetBasemapReadUrlAsync(worldId, mapId).Returns((null, 404, "Map not found"));

        var cut = RenderPage(worldId, mapId);

        cut.WaitForAssertion(() =>
            Assert.Contains("Map not found.", cut.Markup, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void MapPage_WhenUnauthorized_RendersUnauthorizedState()
    {
        var worldId = Guid.NewGuid();
        var mapId = Guid.NewGuid();
        _mapApi.GetMapAsync(worldId, mapId).Returns((null, 404, "Map not found"));
        _mapApi.GetBasemapReadUrlAsync(worldId, mapId).Returns((null, 403, "World not found or access denied"));

        var cut = RenderPage(worldId, mapId);

        cut.WaitForAssertion(() =>
            Assert.Contains("You do not have access to this map.", cut.Markup, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void MapPage_WhenUnauthorized401_RendersUnauthorizedState()
    {
        var worldId = Guid.NewGuid();
        var mapId = Guid.NewGuid();
        _mapApi.GetMapAsync(worldId, mapId).Returns((null, 401, "Unauthorized"));
        _mapApi.GetBasemapReadUrlAsync(worldId, mapId).Returns((null, 404, "Map not found"));

        var cut = RenderPage(worldId, mapId);

        cut.WaitForAssertion(() =>
            Assert.Contains("You do not have access to this map.", cut.Markup, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void MapPage_WhenBasemapMissingByError_RendersBasemapMissingState()
    {
        var worldId = Guid.NewGuid();
        var mapId = Guid.NewGuid();
        _mapApi.GetMapAsync(worldId, mapId).Returns((new MapDto { Name = "Map" }, 200, null));
        _mapApi.GetBasemapReadUrlAsync(worldId, mapId).Returns((null, 404, "Basemap not found for this map"));

        var cut = RenderPage(worldId, mapId);

        cut.WaitForAssertion(() =>
            Assert.Contains("Basemap is missing for this map.", cut.Markup, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void MapPage_WhenBasemapReadUrlEmpty_RendersBasemapMissingState()
    {
        var worldId = Guid.NewGuid();
        var mapId = Guid.NewGuid();
        _mapApi.GetMapAsync(worldId, mapId).Returns((new MapDto { Name = "Map" }, 200, null));
        _mapApi.GetBasemapReadUrlAsync(worldId, mapId).Returns((new GetBasemapReadUrlResponseDto { ReadUrl = " " }, 200, null));

        var cut = RenderPage(worldId, mapId);

        cut.WaitForAssertion(() =>
            Assert.Contains("Basemap is missing for this map.", cut.Markup, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void MapPage_WhenSuccessful_RendersBasemapImage()
    {
        var worldId = Guid.NewGuid();
        var mapId = Guid.NewGuid();
        const string readUrl = "https://blob.example.com/read";
        _mapApi.GetMapAsync(worldId, mapId).Returns((new MapDto { Name = "Sword Coast" }, 200, null));
        _mapApi.GetBasemapReadUrlAsync(worldId, mapId).Returns((new GetBasemapReadUrlResponseDto { ReadUrl = readUrl }, 200, null));

        var cut = RenderPage(worldId, mapId);

        cut.WaitForAssertion(() =>
        {
            var img = cut.Find("img");
            Assert.Equal(readUrl, img.GetAttribute("src"));
            Assert.Equal("Sword Coast", img.GetAttribute("alt"));
        });
    }

    private IRenderedComponent<MapPage> RenderPage(Guid worldId, Guid mapId)
    {
        return RenderComponent<MapPage>(parameters => parameters
            .Add(p => p.WorldId, worldId)
            .Add(p => p.MapId, mapId));
    }
}
