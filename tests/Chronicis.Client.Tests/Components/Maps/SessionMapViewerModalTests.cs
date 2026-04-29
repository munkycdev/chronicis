using System.Reflection;
using Bunit;
using Chronicis.Client.Components.Maps;
using Chronicis.Client.Services;
using Chronicis.Shared.DTOs.Maps;
using Chronicis.Shared.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Chronicis.Client.Tests.Components.Maps;

public class SessionMapViewerModalTests : MudBlazorTestContext
{
    [Fact]
    public void FocusTargetFeatureIfNeeded_PointTarget_CentersAndHighlights()
    {
        var mapApi = Substitute.For<IMapApiService>();
        Services.AddSingleton(Substitute.For<IPublicApiService>());
        Services.AddSingleton(mapApi);
        Services.AddSingleton(Substitute.For<ILogger<SessionMapViewerModal>>());

        var targetId = Guid.NewGuid();
        var cut = RenderComponent<SessionMapViewerModal>(parameters => parameters
            .Add(component => component.IsOpen, false)
            .Add(component => component.TargetFeatureId, targetId));

        SetField(cut.Instance, "_features", new List<MapFeatureDto>
        {
            new()
            {
                FeatureId = targetId,
                LayerId = Guid.NewGuid(),
                FeatureType = MapFeatureType.Point,
                Point = new MapFeaturePointDto { X = 0.25f, Y = 0.75f }
            }
        });
        SetField(cut.Instance, "_hasMapViewportLayout", true);
        SetField(cut.Instance, "_targetFocusPending", true);
        SetField(cut.Instance, "_mapBaseWidth", 1000d);
        SetField(cut.Instance, "_mapBaseHeight", 800d);
        SetField(cut.Instance, "_mapViewportWidth", 500d);
        SetField(cut.Instance, "_mapViewportHeight", 400d);
        SetField(cut.Instance, "_mapMinZoom", 1d);
        SetField(cut.Instance, "_mapMaxZoom", 4d);
        SetField(cut.Instance, "_mapZoom", 1d);

        InvokePrivate(cut.Instance, "FocusTargetFeatureIfNeeded");

        Assert.Equal(targetId, (Guid?)GetField(cut.Instance, "_highlightedFeatureId"));
        Assert.False((bool)GetField(cut.Instance, "_targetFocusPending")!);
        Assert.Equal(2d, (double)GetField(cut.Instance, "_mapZoom")!);
    }

    [Fact]
    public void FocusTargetFeatureIfNeeded_MissingTarget_LeavesHighlightUnset()
    {
        var mapApi = Substitute.For<IMapApiService>();
        Services.AddSingleton(Substitute.For<IPublicApiService>());
        Services.AddSingleton(mapApi);
        Services.AddSingleton(Substitute.For<ILogger<SessionMapViewerModal>>());

        var cut = RenderComponent<SessionMapViewerModal>(parameters => parameters
            .Add(component => component.IsOpen, false)
            .Add(component => component.TargetFeatureId, Guid.NewGuid()));

        SetField(cut.Instance, "_features", new List<MapFeatureDto>());
        SetField(cut.Instance, "_hasMapViewportLayout", true);
        SetField(cut.Instance, "_targetFocusPending", true);

        InvokePrivate(cut.Instance, "FocusTargetFeatureIfNeeded");

        Assert.Null(GetField(cut.Instance, "_highlightedFeatureId"));
        Assert.False((bool)GetField(cut.Instance, "_targetFocusPending")!);
    }

    [Fact]
    public void PolygonTarget_HighlightClass_IsApplied()
    {
        var mapApi = Substitute.For<IMapApiService>();
        Services.AddSingleton(Substitute.For<IPublicApiService>());
        Services.AddSingleton(mapApi);
        Services.AddSingleton(Substitute.For<ILogger<SessionMapViewerModal>>());

        var targetId = Guid.NewGuid();
        var feature = new MapFeatureDto
        {
            FeatureId = targetId,
            LayerId = Guid.NewGuid(),
            FeatureType = MapFeatureType.Polygon,
            Polygon = new PolygonGeometryDto
            {
                Coordinates =
                [
                    [
                        [0.1f, 0.1f],
                        [0.6f, 0.1f],
                        [0.3f, 0.5f],
                        [0.1f, 0.1f]
                    ]
                ]
            }
        };

        var cut = RenderComponent<SessionMapViewerModal>(parameters => parameters
            .Add(component => component.IsOpen, false)
            .Add(component => component.TargetFeatureId, targetId));

        SetField(cut.Instance, "_features", new List<MapFeatureDto> { feature });
        SetField(cut.Instance, "_hasMapViewportLayout", true);
        SetField(cut.Instance, "_targetFocusPending", true);
        SetField(cut.Instance, "_mapBaseWidth", 1000d);
        SetField(cut.Instance, "_mapBaseHeight", 800d);
        SetField(cut.Instance, "_mapViewportWidth", 500d);
        SetField(cut.Instance, "_mapViewportHeight", 400d);
        SetField(cut.Instance, "_mapMinZoom", 1d);
        SetField(cut.Instance, "_mapMaxZoom", 4d);
        SetField(cut.Instance, "_mapZoom", 1d);

        InvokePrivate(cut.Instance, "FocusTargetFeatureIfNeeded");

        var cssClass = (string)InvokePrivateWithResult(cut.Instance, "GetFeaturePolygonClass", feature)!;
        Assert.Contains("highlighted", cssClass, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PublicMode_LoadsViaPublicApi_AndTogglesLayersLocally()
    {
        var mapId = Guid.NewGuid();
        var layerId = Guid.NewGuid();
        var mapApi = Substitute.For<IMapApiService>();
        var publicApi = Substitute.For<IPublicApiService>();
        publicApi.GetPublicMapBasemapReadUrlAsync("public-world", mapId)
            .Returns((new GetBasemapReadUrlResponseDto { ReadUrl = "https://cdn.test/map.png" }, 200, null));

        var layers = new List<MapLayerDto>
        {
            new() { MapLayerId = layerId, Name = "World", SortOrder = 0, IsEnabled = true }
        };

        publicApi.GetPublicMapLayersAsync("public-world", mapId).Returns(layers);
        publicApi.GetPublicMapPinsAsync("public-world", mapId).Returns(new List<MapPinResponseDto>());
        publicApi.GetPublicMapFeaturesAsync("public-world", mapId).Returns(new List<MapFeatureDto>());

        Services.AddSingleton(mapApi);
        Services.AddSingleton(publicApi);
        Services.AddSingleton(Substitute.For<ILogger<SessionMapViewerModal>>());

        var cut = RenderComponent<SessionMapViewerModal>(parameters => parameters
            .Add(component => component.IsOpen, true)
            .Add(component => component.MapId, mapId)
            .Add(component => component.WorldId, Guid.Empty)
            .Add(component => component.MapName, "Roshar")
            .Add(component => component.PublicSlug, "public-world"));

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Roshar", cut.Markup, StringComparison.Ordinal);
            Assert.Contains("session-map-viewer-modal__layer-toggle", cut.Markup, StringComparison.Ordinal);
        });

        await mapApi.DidNotReceive().GetBasemapReadUrlAsync(Arg.Any<Guid>(), Arg.Any<Guid>());
        await publicApi.Received(1).GetPublicMapBasemapReadUrlAsync("public-world", mapId);

        cut.Find("input.session-map-viewer-modal__layer-toggle").Change(false);

        Assert.False(layers[0].IsEnabled);
        await mapApi.DidNotReceive().UpdateLayerVisibilityAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<bool>());
    }

    private static object? GetField(object instance, string name)
        => instance.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(instance);

    private static void SetField(object instance, string name, object? value)
        => instance.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(instance, value);

    private static void InvokePrivate(object instance, string methodName, params object?[]? args)
        => instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(instance, args);

    private static object? InvokePrivateWithResult(object instance, string methodName, params object?[]? args)
        => instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(instance, args);
}
