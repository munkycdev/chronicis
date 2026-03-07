using Bunit;
using Chronicis.Client.Components.Maps;
using Chronicis.Client.Services;
using Chronicis.Shared.DTOs.Maps;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Xunit;

namespace Chronicis.Client.Tests.Components.Maps;

public class SessionMapViewerModalTests : MudBlazorTestContext
{
    private readonly IMapApiService _mapApi = Substitute.For<IMapApiService>();
    private readonly Guid _worldId = Guid.Parse("a00f3f5a-6a8e-4f2a-a16d-4cad56429061");
    private readonly Guid _mapId = Guid.Parse("8bbca211-54d4-4b7e-bf3b-2034b91ff17f");

    public SessionMapViewerModalTests()
    {
        Services.AddSingleton(_mapApi);
        _mapApi.ListPinsForMapAsync(Arg.Any<Guid>(), Arg.Any<Guid>())
            .Returns(new List<MapPinResponseDto>());
    }

    [Fact]
    public void Modal_FetchesOnOpen_AndWhenMapChangesWhileOpen()
    {
        var secondMapId = Guid.Parse("f35c6eb5-954a-4f58-a198-f11f489a447a");
        _mapApi.GetBasemapReadUrlAsync(_worldId, _mapId)
            .Returns((new GetBasemapReadUrlResponseDto { ReadUrl = "https://blob.example.com/one" }, 200, null));
        _mapApi.GetBasemapReadUrlAsync(_worldId, secondMapId)
            .Returns((new GetBasemapReadUrlResponseDto { ReadUrl = "https://blob.example.com/two" }, 200, null));

        var cut = RenderComponent<SessionMapViewerModal>(parameters => parameters
            .Add(x => x.IsOpen, false)
            .Add(x => x.WorldId, _worldId)
            .Add(x => x.MapId, _mapId));

        _mapApi.DidNotReceive().GetBasemapReadUrlAsync(Arg.Any<Guid>(), Arg.Any<Guid>());

        cut.SetParametersAndRender(parameters => parameters
            .Add(x => x.IsOpen, true)
            .Add(x => x.WorldId, _worldId)
            .Add(x => x.MapId, _mapId));

        cut.WaitForAssertion(() =>
            _mapApi.Received(1).GetBasemapReadUrlAsync(_worldId, _mapId));
        cut.WaitForAssertion(() =>
            _mapApi.Received(1).ListPinsForMapAsync(_worldId, _mapId));

        cut.SetParametersAndRender(parameters => parameters
            .Add(x => x.IsOpen, true)
            .Add(x => x.WorldId, _worldId)
            .Add(x => x.MapId, secondMapId));

        cut.WaitForAssertion(() =>
            _mapApi.Received(1).GetBasemapReadUrlAsync(_worldId, secondMapId));
        cut.WaitForAssertion(() =>
            _mapApi.Received(1).ListPinsForMapAsync(_worldId, secondMapId));
    }

    [Fact]
    public void Modal_WhenBasemapLoads_RendersImageOnly()
    {
        const string readUrl = "https://blob.example.com/read";
        _mapApi.GetBasemapReadUrlAsync(_worldId, _mapId)
            .Returns((new GetBasemapReadUrlResponseDto { ReadUrl = readUrl }, 200, null));

        var cut = RenderComponent<SessionMapViewerModal>(parameters => parameters
            .Add(x => x.IsOpen, true)
            .Add(x => x.WorldId, _worldId)
            .Add(x => x.MapId, _mapId)
            .Add(x => x.MapName, "Sword Coast"));

        cut.WaitForAssertion(() =>
        {
            var image = cut.Find(".session-map-viewer-modal__image");
            Assert.Equal(readUrl, image.GetAttribute("src"));
            Assert.Equal("Sword Coast", image.GetAttribute("alt"));
        });
    }

    [Fact]
    public void Modal_WhenPinsExist_RendersPinsOverlay()
    {
        const string readUrl = "https://blob.example.com/read";
        _mapApi.GetBasemapReadUrlAsync(_worldId, _mapId)
            .Returns((new GetBasemapReadUrlResponseDto { ReadUrl = readUrl }, 200, null));
        _mapApi.ListPinsForMapAsync(_worldId, _mapId)
            .Returns(new List<MapPinResponseDto>
            {
                new()
                {
                    PinId = Guid.Parse("2e2dbf4b-8de8-4684-82b9-10188b3d44a0"),
                    MapId = _mapId,
                    LayerId = Guid.Parse("44b3db3d-ae17-48ee-b31d-b2f93782595b"),
                    Name = "Castle",
                    X = 0.25f,
                    Y = 0.50f
                }
            });

        var cut = RenderComponent<SessionMapViewerModal>(parameters => parameters
            .Add(x => x.IsOpen, true)
            .Add(x => x.WorldId, _worldId)
            .Add(x => x.MapId, _mapId));

        cut.WaitForAssertion(() =>
        {
            Assert.Single(cut.FindAll(".session-map-viewer-modal__pin"));
            Assert.Single(cut.FindAll(".session-map-viewer-modal__pin-name"));
        });
    }

    [Fact]
    public void Modal_WhileFetching_RendersLoadingState()
    {
        var tcs = new TaskCompletionSource<(GetBasemapReadUrlResponseDto? Basemap, int? StatusCode, string? Error)>();
        _mapApi.GetBasemapReadUrlAsync(_worldId, _mapId).Returns(tcs.Task);

        var cut = RenderComponent<SessionMapViewerModal>(parameters => parameters
            .Add(x => x.IsOpen, true)
            .Add(x => x.WorldId, _worldId)
            .Add(x => x.MapId, _mapId));

        cut.WaitForAssertion(() =>
            Assert.Contains("Loading map...", cut.Markup, StringComparison.OrdinalIgnoreCase));

        tcs.SetResult((new GetBasemapReadUrlResponseDto { ReadUrl = "https://blob.example.com/read" }, 200, null));
    }

    [Theory]
    [InlineData(401, "Unauthorized", "You do not have access to this map.")]
    [InlineData(403, "World not found or access denied", "You do not have access to this map.")]
    [InlineData(404, "Map not found", "Map not found.")]
    [InlineData(404, "Basemap not found for this map", "Basemap is missing for this map.")]
    public void Modal_WhenLoadFails_RendersExpectedErrorMessage(int statusCode, string error, string expectedMessage)
    {
        _mapApi.GetBasemapReadUrlAsync(_worldId, _mapId)
            .Returns((null, statusCode, error));

        var cut = RenderComponent<SessionMapViewerModal>(parameters => parameters
            .Add(x => x.IsOpen, true)
            .Add(x => x.WorldId, _worldId)
            .Add(x => x.MapId, _mapId));

        cut.WaitForAssertion(() =>
            Assert.Contains(expectedMessage, cut.Markup, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Modal_CloseButton_InvokesOnClose()
    {
        _mapApi.GetBasemapReadUrlAsync(_worldId, _mapId)
            .Returns((new GetBasemapReadUrlResponseDto { ReadUrl = "https://blob.example.com/read" }, 200, null));
        var closeCount = 0;

        var cut = RenderComponent<SessionMapViewerModal>(parameters => parameters
            .Add(x => x.IsOpen, true)
            .Add(x => x.WorldId, _worldId)
            .Add(x => x.MapId, _mapId)
            .Add(x => x.OnClose, EventCallback.Factory.Create(this, () => closeCount++)));

        cut.WaitForAssertion(() => Assert.Contains("session-map-viewer-modal__close", cut.Markup, StringComparison.Ordinal));
        cut.Find(".session-map-viewer-modal__close").Click();

        Assert.Equal(1, closeCount);
    }

    [Fact]
    public void Modal_EscapeKey_InvokesOnClose()
    {
        _mapApi.GetBasemapReadUrlAsync(_worldId, _mapId)
            .Returns((new GetBasemapReadUrlResponseDto { ReadUrl = "https://blob.example.com/read" }, 200, null));
        var closeCount = 0;

        var cut = RenderComponent<SessionMapViewerModal>(parameters => parameters
            .Add(x => x.IsOpen, true)
            .Add(x => x.WorldId, _worldId)
            .Add(x => x.MapId, _mapId)
            .Add(x => x.OnClose, EventCallback.Factory.Create(this, () => closeCount++)));

        cut.WaitForAssertion(() => Assert.Contains("session-map-viewer-modal", cut.Markup, StringComparison.Ordinal));
        cut.Find(".session-map-viewer-modal").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.Equal(1, closeCount);
    }

    [Fact]
    public void Modal_NonEscapeKey_DoesNotInvokeOnClose()
    {
        _mapApi.GetBasemapReadUrlAsync(_worldId, _mapId)
            .Returns((new GetBasemapReadUrlResponseDto { ReadUrl = "https://blob.example.com/read" }, 200, null));
        var closeCount = 0;

        var cut = RenderComponent<SessionMapViewerModal>(parameters => parameters
            .Add(x => x.IsOpen, true)
            .Add(x => x.WorldId, _worldId)
            .Add(x => x.MapId, _mapId)
            .Add(x => x.OnClose, EventCallback.Factory.Create(this, () => closeCount++)));

        cut.WaitForAssertion(() => Assert.Contains("session-map-viewer-modal", cut.Markup, StringComparison.Ordinal));
        cut.Find(".session-map-viewer-modal").KeyDown(new KeyboardEventArgs { Key = "Enter" });

        Assert.Equal(0, closeCount);
    }
}
