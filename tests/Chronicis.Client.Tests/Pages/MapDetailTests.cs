using System.Reflection;
using Bunit;
using Bunit.TestDoubles;
using Chronicis.Client.Engine.Interaction;
using Chronicis.Client.Pages.Maps;
using Chronicis.Client.Services;
using Chronicis.Client.Tests.Components;
using Chronicis.Shared.DTOs;
using Chronicis.Shared.DTOs.Maps;
using Chronicis.Shared.Enums;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Xunit;

namespace Chronicis.Client.Tests.Pages;

public class MapDetailTests : MudBlazorTestContext
{
    private readonly IMapApiService _mapApi = Substitute.For<IMapApiService>();
    private readonly IWorldApiService _worldApi = Substitute.For<IWorldApiService>();
    private readonly IUserApiService _userApi = Substitute.For<IUserApiService>();
    private readonly IArticleApiService _articleApi = Substitute.For<IArticleApiService>();
    private readonly ITreeStateService _treeState = Substitute.For<ITreeStateService>();

    private static readonly Guid UserId = Guid.Parse("10000000-0000-0000-0000-000000000001");
    private static readonly Guid RootLayerId = Guid.Parse("20000000-0000-0000-0000-000000000001");
    private static readonly Guid VisibleChildLayerId = Guid.Parse("20000000-0000-0000-0000-000000000002");
    private static readonly Guid HiddenParentLayerId = Guid.Parse("20000000-0000-0000-0000-000000000003");
    private static readonly Guid HiddenChildLayerId = Guid.Parse("20000000-0000-0000-0000-000000000004");
    private static readonly Guid WorldId = Guid.Parse("40000000-0000-0000-0000-000000000001");
    private static readonly Guid MapId = Guid.Parse("50000000-0000-0000-0000-000000000001");

    public MapDetailTests()
    {
        var auth = this.AddTestAuthorization();
        auth.SetAuthorized("test-user");

        Services.AddSingleton(_mapApi);
        Services.AddSingleton(_worldApi);
        Services.AddSingleton(_userApi);
        Services.AddSingleton(_articleApi);
        Services.AddSingleton(_treeState);

        _worldApi.GetWorldAsync(Arg.Any<Guid>()).Returns(call =>
        {
            var worldId = call.Arg<Guid>();
            return new WorldDetailDto
            {
                Id = worldId,
                OwnerId = UserId,
                Name = "World",
                Campaigns = []
            };
        });

        _userApi.GetUserProfileAsync().Returns(new UserProfileDto { Id = UserId, DisplayName = "Owner", Email = "owner@test.dev" });
        _mapApi.GetMapAsync(Arg.Any<Guid>(), Arg.Any<Guid>()).Returns(call =>
        {
            var worldId = call.ArgAt<Guid>(0);
            var mapId = call.ArgAt<Guid>(1);
            return (new MapDto
            {
                WorldId = worldId,
                WorldMapId = mapId,
                Name = "Map",
                HasBasemap = true
            }, 200, (string?)null);
        });
        _mapApi.GetBasemapReadUrlAsync(Arg.Any<Guid>(), Arg.Any<Guid>()).Returns((new GetBasemapReadUrlResponseDto
        {
            ReadUrl = "https://example.test/map.png"
        }, 200, (string?)null));
        _mapApi.GetLayersForMapAsync(Arg.Any<Guid>(), Arg.Any<Guid>()).Returns(_ => CreateLayers());
        _mapApi.ListPinsForMapAsync(Arg.Any<Guid>(), Arg.Any<Guid>()).Returns(new List<MapPinResponseDto>
        {
            new()
            {
                PinId = Guid.NewGuid(),
                MapId = MapId,
                LayerId = VisibleChildLayerId,
                Name = "Pin",
                X = 0.6f,
                Y = 0.6f
            }
        });
        _mapApi.ListFeaturesForMapAsync(Arg.Any<Guid>(), Arg.Any<Guid>()).Returns(_ => []);
        _mapApi.UpdateLayerVisibilityAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<bool>())
            .Returns(Task.CompletedTask);
    }

    [Fact]
    public async Task MapDetail_EnteringPolygonMode_DisablesCreatePinMode()
    {
        var cut = RenderMapDetail();

        await InvokePrivateOnRendererAsync(cut, "ToggleCreatePinMode");
        await InvokePrivateOnRendererAsync(cut, "ToggleCreatePolygonMode");

        Assert.True(GetField<bool>(cut.Instance, "_isCreatePolygonMode"));
        Assert.False(GetField<bool>(cut.Instance, "_isCreatePinMode"));
    }

    [Fact]
    public async Task MapDetail_MissingSelectedLayer_BlocksPolygonCompletion()
    {
        var cut = RenderMapDetail();
        SetMapViewportLayout(cut.Instance);
        await InvokePrivateOnRendererAsync(cut, "ToggleCreatePolygonMode");
        SetField(cut.Instance, "SelectedLayerId", null);

        var draft = GetField<PolygonDraftState>(cut.Instance, "_polygonDraft");
        draft.AddVertex(new(0.1f, 0.1f));
        draft.AddVertex(new(0.5f, 0.1f));

        await InvokePrivateOnRendererAsync(
            cut,
            "OnMapViewportDoubleClickAsync",
            new MouseEventArgs { OffsetX = 500, OffsetY = 250 });

        Assert.DoesNotContain(
            _mapApi.ReceivedCalls(),
            call => string.Equals(call.GetMethodInfo().Name, nameof(IMapApiService.CreateFeatureAsync), StringComparison.Ordinal));
        Assert.Equal("Select a layer before saving a polygon.", GetField<string>(cut.Instance, "_createPolygonError"));
    }

    [Fact]
    public async Task MapDetail_Escape_CancelsPolygonDraft()
    {
        var cut = RenderMapDetail();
        await InvokePrivateOnRendererAsync(cut, "ToggleCreatePolygonMode");

        var draft = GetField<PolygonDraftState>(cut.Instance, "_polygonDraft");
        draft.AddVertex(new(0.1f, 0.1f));
        draft.AddVertex(new(0.5f, 0.1f));
        await InvokePrivateOnRendererAsync(cut, "UpdatePolygonDraftPreview");

        cut.Find(".map-page__viewport").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        cut.WaitForAssertion(() =>
        {
            Assert.Empty(draft.Vertices);
            Assert.Empty(cut.FindAll(".map-page__polygon-draft"));
        });
    }

    [Fact]
    public async Task MapDetail_Backspace_RemovesLastDraftVertex()
    {
        var cut = RenderMapDetail();
        await InvokePrivateOnRendererAsync(cut, "ToggleCreatePolygonMode");

        var draft = GetField<PolygonDraftState>(cut.Instance, "_polygonDraft");
        draft.AddVertex(new(0.1f, 0.1f));
        draft.AddVertex(new(0.5f, 0.1f));
        draft.AddVertex(new(0.5f, 0.5f));
        await InvokePrivateOnRendererAsync(cut, "UpdatePolygonDraftPreview");

        cut.Find(".map-page__viewport").KeyDown(new KeyboardEventArgs { Key = "Backspace" });

        cut.WaitForAssertion(() =>
        {
            Assert.Equal(2, draft.Vertices.Count);
            Assert.Equal(new Chronicis.Client.Engine.Geometry.NormalizedMapPoint(0.5f, 0.1f), draft.Vertices[^1]);
        });
    }

    [Fact]
    public async Task MapDetail_DoubleClickCompletion_PersistsSingleTerminalVertex()
    {
        MapFeatureCreateDto? capturedRequest = null;
        _mapApi.CreateFeatureAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<MapFeatureCreateDto>())
            .Returns(call =>
            {
                capturedRequest = call.ArgAt<MapFeatureCreateDto>(2);
                return CreatePolygonFeature(
                    Guid.Parse("30000000-0000-0000-0000-000000000010"),
                    capturedRequest.LayerId,
                    capturedRequest.Polygon!);
            });

        var cut = RenderMapDetail();
        SetMapViewportLayout(cut.Instance);
        SetField(cut.Instance, "SelectedLayerId", VisibleChildLayerId);
        await InvokePrivateOnRendererAsync(cut, "ToggleCreatePolygonMode");

        await AddPolygonVertexAsync(cut, 100, 100);
        await AddPolygonVertexAsync(cut, 400, 100);

        await InvokePrivateOnRendererAsync(cut, "OnMapImageShellClick", new MouseEventArgs { OffsetX = 450, OffsetY = 250, Detail = 1 });
        await InvokePrivateOnRendererAsync(cut, "OnMapImageShellClick", new MouseEventArgs { OffsetX = 450, OffsetY = 250, Detail = 2 });
        await InvokePrivateOnRendererAsync(cut, "OnMapViewportDoubleClickAsync", new MouseEventArgs { OffsetX = 450, OffsetY = 250 });

        Assert.Single(
            _mapApi.ReceivedCalls().Where(call =>
                string.Equals(call.GetMethodInfo().Name, nameof(IMapApiService.CreateFeatureAsync), StringComparison.Ordinal)));
        Assert.NotNull(capturedRequest);
        Assert.Equal(4, capturedRequest!.Polygon!.Coordinates[0].Count);
        Assert.Equal(new[] { 0.45f, 0.5f }, capturedRequest.Polygon.Coordinates[0][2]);
        Assert.Equal(capturedRequest.Polygon.Coordinates[0][0], capturedRequest.Polygon.Coordinates[0][^1]);
        Assert.NotEqual(capturedRequest.Polygon.Coordinates[0][1], capturedRequest.Polygon.Coordinates[0][2]);

        var visiblePolygons = GetVisiblePolygonModels(cut.Instance);
        Assert.Single(visiblePolygons);
        Assert.Equal("M 0.1 0.2 L 0.4 0.2 L 0.45 0.5 L 0.1 0.2 Z", GetProperty<string>(visiblePolygons[0], "PathData"));
    }

    [Fact]
    public async Task MapDetail_SaveReloadAndVisibilityInheritance_WorkForCreatedPolygon()
    {
        var createdFeatureId = Guid.Parse("30000000-0000-0000-0000-000000000011");
        var persistedFeatures = new List<MapFeatureDto>();
        _mapApi.ListFeaturesForMapAsync(Arg.Any<Guid>(), Arg.Any<Guid>()).Returns(_ => persistedFeatures);
        _mapApi.CreateFeatureAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<MapFeatureCreateDto>())
            .Returns(call =>
            {
                var request = call.ArgAt<MapFeatureCreateDto>(2);
                var created = CreatePolygonFeature(createdFeatureId, request.LayerId, request.Polygon!);
                persistedFeatures = [created];
                return created;
            });

        var cut = RenderMapDetail();
        SetMapViewportLayout(cut.Instance);
        SetField(cut.Instance, "SelectedLayerId", VisibleChildLayerId);
        await InvokePrivateOnRendererAsync(cut, "ToggleCreatePolygonMode");

        await AddPolygonVertexAsync(cut, 100, 100);
        await AddPolygonVertexAsync(cut, 400, 100);
        await InvokePrivateOnRendererAsync(cut, "OnMapViewportDoubleClickAsync", new MouseEventArgs { OffsetX = 450, OffsetY = 250 });
        Assert.Single(GetVisiblePolygonModels(cut.Instance));

        await InvokePrivateOnRendererAsync(cut, "LoadAsync");
        Assert.Single(GetVisiblePolygonModels(cut.Instance));

        await InvokePrivateOnRendererAsync(
            cut,
            "OnLayerVisibilityChangedAsync",
            RootLayerId,
            new ChangeEventArgs { Value = false });
        Assert.Empty(GetVisiblePolygonModels(cut.Instance));
    }

    [Fact]
    public void MapDetail_PersistedPolygon_RendersSvgOverlay()
    {
        var featureId = Guid.Parse("30000000-0000-0000-0000-000000000001");
        _mapApi.ListFeaturesForMapAsync(Arg.Any<Guid>(), Arg.Any<Guid>()).Returns(
        [
            CreatePolygonFeature(featureId, VisibleChildLayerId)
        ]);

        var cut = RenderMapDetail();

        cut.WaitForAssertion(() =>
        {
            var path = cut.Find($"path[data-feature-id='{featureId}']");
            Assert.Equal("M 0.1 0.2 L 0.8 0.2 L 0.4 0.7 L 0.1 0.2 Z", path.GetAttribute("d"));
        });
    }

    [Fact]
    public void MapDetail_DisabledAncestor_HidesPolygonAndLeavesPinsIntact()
    {
        var hiddenFeatureId = Guid.Parse("30000000-0000-0000-0000-000000000002");
        var visibleFeatureId = Guid.Parse("30000000-0000-0000-0000-000000000003");
        _mapApi.ListFeaturesForMapAsync(Arg.Any<Guid>(), Arg.Any<Guid>()).Returns(
        [
            CreatePolygonFeature(hiddenFeatureId, HiddenChildLayerId),
            CreatePolygonFeature(visibleFeatureId, VisibleChildLayerId)
        ]);

        var cut = RenderMapDetail();

        cut.WaitForAssertion(() =>
        {
            Assert.Single(cut.FindAll("path[data-feature-id]"));
            Assert.NotNull(cut.Find($"path[data-feature-id='{visibleFeatureId}']"));
            Assert.DoesNotContain(hiddenFeatureId.ToString(), cut.Markup, StringComparison.Ordinal);
            Assert.Single(cut.FindAll(".map-page__pin"));
        });
    }

    [Fact]
    public void MapDetail_InvalidPersistedPolygon_IsSkipped()
    {
        _mapApi.ListFeaturesForMapAsync(Arg.Any<Guid>(), Arg.Any<Guid>()).Returns(
        [
            CreatePolygonFeature(Guid.Parse("30000000-0000-0000-0000-000000000004"), VisibleChildLayerId, closed: false)
        ]);

        var cut = RenderMapDetail();

        cut.WaitForAssertion(() =>
        {
            Assert.Empty(cut.FindAll("path[data-feature-id]"));
            Assert.Single(cut.FindAll(".map-page__pin"));
        });
    }

    [Fact]
    public void MapDetail_RenderedPolygon_UsesFeatureIdForIdentity()
    {
        var featureA = Guid.Parse("30000000-0000-0000-0000-000000000005");
        var featureB = Guid.Parse("30000000-0000-0000-0000-000000000006");
        _mapApi.ListFeaturesForMapAsync(Arg.Any<Guid>(), Arg.Any<Guid>()).Returns(
        [
            CreatePolygonFeature(featureB, VisibleChildLayerId, name: "B"),
            CreatePolygonFeature(featureA, VisibleChildLayerId, name: "A")
        ]);

        var cut = RenderMapDetail();

        cut.WaitForAssertion(() =>
        {
            var ids = cut.FindAll("path[data-feature-id]").Select(path => path.GetAttribute("data-feature-id")).ToList();
            Assert.Contains(featureA.ToString(), ids);
            Assert.Contains(featureB.ToString(), ids);
        });
    }

    private IRenderedComponent<MapDetail> RenderMapDetail() =>
        RenderComponent<MapDetail>(parameters => parameters
            .Add(p => p.WorldId, WorldId)
            .Add(p => p.MapId, MapId));

    private static List<MapLayerDto> CreateLayers() =>
    [
        new() { MapLayerId = RootLayerId, Name = "World", SortOrder = 0, IsEnabled = true },
        new() { MapLayerId = HiddenParentLayerId, Name = "Campaign", SortOrder = 1, IsEnabled = false },
        new() { MapLayerId = VisibleChildLayerId, ParentLayerId = RootLayerId, Name = "Visible", SortOrder = 0, IsEnabled = true },
        new() { MapLayerId = HiddenChildLayerId, ParentLayerId = HiddenParentLayerId, Name = "Hidden Child", SortOrder = 0, IsEnabled = true }
    ];

    private static MapFeatureDto CreatePolygonFeature(Guid featureId, Guid layerId, PolygonGeometryDto? polygon = null, string? name = null, bool closed = true) =>
        new()
        {
            FeatureId = featureId,
            MapId = MapId,
            LayerId = layerId,
            FeatureType = MapFeatureType.Polygon,
            Name = name ?? "Polygon",
            Polygon = polygon ?? new PolygonGeometryDto
            {
                Type = "Polygon",
                Coordinates =
                [
                    closed
                        ? [[0.1f, 0.2f], [0.8f, 0.2f], [0.4f, 0.7f], [0.1f, 0.2f]]
                        : [[0.1f, 0.2f], [0.8f, 0.2f], [0.4f, 0.7f], [0.2f, 0.3f]]
                ]
            }
        };

    private static void SetMapViewportLayout(object instance)
    {
        SetField(instance, "_hasMapViewportLayout", true);
        SetField(instance, "_mapBaseWidth", 1000d);
        SetField(instance, "_mapBaseHeight", 500d);
        SetField(instance, "_mapViewportWidth", 1000d);
        SetField(instance, "_mapViewportHeight", 500d);
        SetField(instance, "_mapMinZoom", 1d);
        SetField(instance, "_mapMaxZoom", 5d);
        SetField(instance, "_mapZoom", 1d);
        SetField(instance, "_mapPanX", 0d);
        SetField(instance, "_mapPanY", 0d);
    }

    private async Task AddPolygonVertexAsync(IRenderedComponent<MapDetail> cut, double offsetX, double offsetY)
    {
        await InvokePrivateOnRendererAsync(
            cut,
            "OnMapImageShellClick",
            new MouseEventArgs { OffsetX = offsetX, OffsetY = offsetY, Detail = 1 });

        await Task.Delay(250);
    }

    private static List<object> GetVisiblePolygonModels(object instance) =>
        ((System.Collections.IEnumerable)(InvokePrivate(instance, "GetVisiblePolygons") ?? Array.Empty<object>()))
        .Cast<object>()
        .ToList();

    private static T GetProperty<T>(object instance, string propertyName)
    {
        var property = instance.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"Missing property {propertyName}.");
        return (T)property.GetValue(instance)!;
    }

    private static T GetField<T>(object instance, string fieldName)
    {
        var field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"Missing field {fieldName}.");
        return (T)field.GetValue(instance)!;
    }

    private static void SetField(object instance, string fieldName, object? value)
    {
        var field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"Missing field {fieldName}.");
        field.SetValue(instance, value);
    }

    private static object? InvokePrivate(object instance, string methodName, params object[] args)
    {
        var method = instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"Missing method {methodName}.");
        return method.Invoke(instance, args);
    }

    private static async Task InvokePrivateAsync(object instance, string methodName, params object[] args)
    {
        var result = InvokePrivate(instance, methodName, args);
        switch (result)
        {
            case Task task:
                await task;
                break;
            case ValueTask valueTask:
                await valueTask;
                break;
        }
    }

    private static Task InvokePrivateOnRendererAsync(IRenderedComponent<MapDetail> cut, string methodName, params object[] args) =>
        cut.InvokeAsync(() => InvokePrivateAsync(cut.Instance, methodName, args));
}
