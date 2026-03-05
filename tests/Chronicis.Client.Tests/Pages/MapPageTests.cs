using Bunit;
using Chronicis.Client.Components.Shared;
using Chronicis.Client.Pages.Maps;
using Chronicis.Client.Services;
using Chronicis.Client.Tests.Components;
using Chronicis.Shared.DTOs;
using Chronicis.Shared.DTOs.Maps;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Xunit;

namespace Chronicis.Client.Tests.Pages;

public class MapPageTests : MudBlazorTestContext
{
    private readonly IMapApiService _mapApi = Substitute.For<IMapApiService>();
    private readonly IWorldApiService _worldApi = Substitute.For<IWorldApiService>();
    private readonly IUserApiService _userApi = Substitute.For<IUserApiService>();
    private readonly IArticleApiService _articleApi = Substitute.For<IArticleApiService>();
    private readonly ITreeStateService _treeState = Substitute.For<ITreeStateService>();
    private readonly Guid _worldOwnerId = Guid.Parse("33000000-0000-0000-0000-000000000001");
    private readonly Guid _nonOwnerId = Guid.Parse("33000000-0000-0000-0000-000000000002");

    public MapPageTests()
    {
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
                Name = "Test World",
                OwnerId = _worldOwnerId,
                Campaigns = []
            };
        });

        _userApi.GetUserProfileAsync().Returns(new UserProfileDto
        {
            Id = _nonOwnerId,
            Email = "player@test.com",
            DisplayName = "Player"
        });

        _mapApi.ListPinsForMapAsync(Arg.Any<Guid>(), Arg.Any<Guid>())
            .Returns(new List<MapPinResponseDto>());
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
            Assert.DoesNotContain("_editMapName", cut.Markup, StringComparison.Ordinal);
            Assert.Contains("Dashboard", cut.Markup, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Test World", cut.Markup, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Sword Coast", cut.Markup, StringComparison.OrdinalIgnoreCase);
            Assert.Contains($"/world/{worldId}/maps", cut.Markup, StringComparison.OrdinalIgnoreCase);
        });

        _treeState.Received(1).ExpandPathToAndSelect(mapId);
    }

    [Fact]
    public void MapPage_WhenWorldNameUnavailable_UsesFallbackBreadcrumbLabel()
    {
        var worldId = Guid.NewGuid();
        var mapId = Guid.NewGuid();
        const string readUrl = "https://blob.example.com/read";

        _worldApi.GetWorldAsync(worldId).Returns((WorldDetailDto?)null);
        _mapApi.GetMapAsync(worldId, mapId).Returns((new MapDto { Name = "Fallback Map" }, 200, null));
        _mapApi.GetBasemapReadUrlAsync(worldId, mapId).Returns((new GetBasemapReadUrlResponseDto { ReadUrl = readUrl }, 200, null));

        var cut = RenderPage(worldId, mapId);

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Dashboard", cut.Markup, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Test World", cut.Markup, StringComparison.OrdinalIgnoreCase);
            Assert.Contains($"/world/{worldId}/maps", cut.Markup, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Fallback Map", cut.Markup, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public void MapPage_WhenUserIsOwner_RendersSaveControls()
    {
        var worldId = Guid.NewGuid();
        var mapId = Guid.NewGuid();

        _userApi.GetUserProfileAsync().Returns(new UserProfileDto
        {
            Id = _worldOwnerId,
            Email = "owner@test.com",
            DisplayName = "Owner"
        });

        _mapApi.GetMapAsync(worldId, mapId).Returns((new MapDto { Name = "Owner Map" }, 200, null));
        _mapApi.GetBasemapReadUrlAsync(worldId, mapId).Returns((new GetBasemapReadUrlResponseDto { ReadUrl = "https://blob/read" }, 200, null));

        var cut = RenderPage(worldId, mapId);

        cut.WaitForAssertion(() =>
            Assert.Contains(">Save<", cut.Markup, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task SaveMapNameAsync_WhenNameChanged_UpdatesMapAndBreadcrumbs()
    {
        var worldId = Guid.NewGuid();
        var mapId = Guid.NewGuid();

        _userApi.GetUserProfileAsync().Returns(new UserProfileDto { Id = _worldOwnerId });
        _mapApi.GetMapAsync(worldId, mapId).Returns((new MapDto { Name = "Old Name" }, 200, null));
        _mapApi.GetBasemapReadUrlAsync(worldId, mapId).Returns((new GetBasemapReadUrlResponseDto { ReadUrl = "https://blob/read" }, 200, null));
        _mapApi.UpdateMapAsync(worldId, mapId, Arg.Any<MapUpdateDto>()).Returns(new MapDto { Name = "Renamed Map" });

        var cut = RenderPage(worldId, mapId);
        cut.WaitForAssertion(() => Assert.Contains("Old Name", cut.Markup, StringComparison.OrdinalIgnoreCase));

        await InvokePrivateOnRendererAsync(cut, "OnMapTitleChanged", "Renamed Map");
        await InvokePrivateOnRendererAsync(cut, "OnMapTitleEdited");
        await InvokePrivateOnRendererAsync(cut, "SaveMapNameAsync");

        Assert.Equal("Renamed Map", GetField<string>(cut.Instance, "_mapName"));
        Assert.False(GetField<bool>(cut.Instance, "_hasUnsavedMapChanges"));
        _treeState.Received(1).UpdateNodeDisplay(mapId, "Renamed Map", null);
        await _mapApi.Received(1).UpdateMapAsync(
            worldId,
            mapId,
            Arg.Is<MapUpdateDto>(dto => dto.Name == "Renamed Map"));
    }

    [Fact]
    public async Task SaveMapNameAsync_WhenNameBlank_SetsValidationError()
    {
        var worldId = Guid.NewGuid();
        var mapId = Guid.NewGuid();

        _userApi.GetUserProfileAsync().Returns(new UserProfileDto { Id = _worldOwnerId });
        _mapApi.GetMapAsync(worldId, mapId).Returns((new MapDto { Name = "Old Name" }, 200, null));
        _mapApi.GetBasemapReadUrlAsync(worldId, mapId).Returns((new GetBasemapReadUrlResponseDto { ReadUrl = "https://blob/read" }, 200, null));

        var cut = RenderPage(worldId, mapId);
        cut.WaitForAssertion(() => Assert.Contains("Old Name", cut.Markup, StringComparison.OrdinalIgnoreCase));

        SetField(cut.Instance, "_editMapName", "   ");
        SetField(cut.Instance, "_hasUnsavedMapChanges", true);
        await InvokePrivateOnRendererAsync(cut, "SaveMapNameAsync");

        Assert.Equal("Map name is required.", GetField<string>(cut.Instance, "_mapSaveError"));
        await _mapApi.DidNotReceive().UpdateMapAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<MapUpdateDto>());
    }

    [Fact]
    public async Task SaveMapNameAsync_WhenEnterPressed_UpdatesMap()
    {
        var worldId = Guid.NewGuid();
        var mapId = Guid.NewGuid();

        _userApi.GetUserProfileAsync().Returns(new UserProfileDto { Id = _worldOwnerId });
        _mapApi.GetMapAsync(worldId, mapId).Returns((new MapDto { Name = "Old Name" }, 200, null));
        _mapApi.GetBasemapReadUrlAsync(worldId, mapId).Returns((new GetBasemapReadUrlResponseDto { ReadUrl = "https://blob/read" }, 200, null));
        _mapApi.UpdateMapAsync(worldId, mapId, Arg.Any<MapUpdateDto>()).Returns(new MapDto { Name = "By Enter" });

        var cut = RenderPage(worldId, mapId);
        cut.WaitForAssertion(() => Assert.Contains("Old Name", cut.Markup, StringComparison.OrdinalIgnoreCase));

        await InvokePrivateOnRendererAsync(cut, "OnMapTitleChanged", "By Enter");
        await InvokePrivateOnRendererAsync(cut, "OnMapTitleEdited");

        var header = cut.FindComponent<DetailPageHeader>();
        await header.InvokeAsync(() =>
            header.Instance.OnEnterPressed.InvokeAsync(new KeyboardEventArgs { Key = "Enter" }));

        Assert.Equal("By Enter", GetField<string>(cut.Instance, "_mapName"));
        await _mapApi.Received(1).UpdateMapAsync(
            worldId,
            mapId,
            Arg.Is<MapUpdateDto>(dto => dto.Name == "By Enter"));
    }

    [Fact]
    public void UpdateMapDirtyState_CoversPermissionAndNullBranches()
    {
        var worldId = Guid.NewGuid();
        var mapId = Guid.NewGuid();

        _userApi.GetUserProfileAsync().Returns(new UserProfileDto
        {
            Id = _worldOwnerId,
            Email = "owner@test.com",
            DisplayName = "Owner"
        });
        _mapApi.GetMapAsync(worldId, mapId).Returns((new MapDto { Name = "Base" }, 200, null));
        _mapApi.GetBasemapReadUrlAsync(worldId, mapId).Returns((new GetBasemapReadUrlResponseDto { ReadUrl = "https://blob/read" }, 200, null));

        var cut = RenderPage(worldId, mapId);
        cut.WaitForAssertion(() => Assert.Contains("Base", cut.Markup, StringComparison.OrdinalIgnoreCase));

        var updateDirtyState = cut.Instance.GetType().GetMethod("UpdateMapDirtyState", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        Assert.NotNull(updateDirtyState);

        SetField(cut.Instance, "_canManageMap", false);
        SetField(cut.Instance, "_mapName", "Base");
        SetField(cut.Instance, "_editMapName", "Changed");
        updateDirtyState!.Invoke(cut.Instance, null);
        Assert.False(GetField<bool>(cut.Instance, "_hasUnsavedMapChanges"));

        SetField(cut.Instance, "_canManageMap", true);
        SetField(cut.Instance, "_mapName", null);
        SetField(cut.Instance, "_editMapName", null);
        updateDirtyState.Invoke(cut.Instance, null);
        Assert.False(GetField<bool>(cut.Instance, "_hasUnsavedMapChanges"));

        SetField(cut.Instance, "_mapName", null);
        SetField(cut.Instance, "_editMapName", "Changed");
        updateDirtyState.Invoke(cut.Instance, null);
        Assert.True(GetField<bool>(cut.Instance, "_hasUnsavedMapChanges"));

        SetField(cut.Instance, "_mapName", "Changed");
        SetField(cut.Instance, "_editMapName", null);
        updateDirtyState.Invoke(cut.Instance, null);
        Assert.True(GetField<bool>(cut.Instance, "_hasUnsavedMapChanges"));
    }

    [Fact]
    public async Task ToggleCreatePinMode_ClearsErrorAndSelection_AndToggles()
    {
        var cut = RenderLoadedPage();
        SetField(cut.Instance, "_createPinError", "bad");
        SetField(cut.Instance, "_selectedPinId", Guid.NewGuid());
        SetField(cut.Instance, "_isCreatePinMode", false);
        SetField(cut.Instance, "_isCreatePinDialogOpen", false);
        SetField(cut.Instance, "_pendingCreatePinX", null);
        SetField(cut.Instance, "_pendingCreatePinY", null);

        await InvokePrivateOnRendererAsync(cut, "ToggleCreatePinMode");
        Assert.Equal(string.Empty, GetField<string>(cut.Instance, "_createPinError"));
        Assert.Null(GetField<Guid?>(cut.Instance, "_selectedPinId"));
        Assert.True(GetField<bool>(cut.Instance, "_isCreatePinMode"));

        SetField(cut.Instance, "_isCreatePinDialogOpen", true);
        SetField(cut.Instance, "_pendingCreatePinX", 0.25f);
        SetField(cut.Instance, "_pendingCreatePinY", 0.75f);
        await InvokePrivateOnRendererAsync(cut, "ToggleCreatePinMode");
        Assert.False(GetField<bool>(cut.Instance, "_isCreatePinMode"));
        Assert.False(GetField<bool>(cut.Instance, "_isCreatePinDialogOpen"));
        Assert.Null(GetField<float?>(cut.Instance, "_pendingCreatePinX"));
        Assert.Null(GetField<float?>(cut.Instance, "_pendingCreatePinY"));
    }

    [Fact]
    public void GetMapViewportClass_CoversCreateDragPanAndDefaultBranches()
    {
        var cut = RenderLoadedPage();
        var method = typeof(MapPage).GetMethod("GetMapViewportClass", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        Assert.NotNull(method);

        SetField(cut.Instance, "_isCreatePinMode", true);
        var createClass = (string?)method!.Invoke(cut.Instance, null);
        Assert.Equal("map-page__viewport map-page__viewport--create", createClass);

        SetField(cut.Instance, "_isCreatePinMode", false);
        SetField(cut.Instance, "_isMapDragging", true);
        var dragClass = (string?)method.Invoke(cut.Instance, null);
        Assert.Equal("map-page__viewport map-page__viewport--dragging", dragClass);

        SetField(cut.Instance, "_isMapDragging", false);
        SetField(cut.Instance, "_hasMapViewportLayout", true);
        SetField(cut.Instance, "_mapMinZoom", 1d);
        SetField(cut.Instance, "_mapZoom", 2d);
        var panClass = (string?)method.Invoke(cut.Instance, null);
        Assert.Equal("map-page__viewport map-page__viewport--pan", panClass);

        SetField(cut.Instance, "_mapZoom", 1d);
        var defaultClass = (string?)method.Invoke(cut.Instance, null);
        Assert.Equal("map-page__viewport", defaultClass);
    }

    [Fact]
    public void GetMapStageStyle_CoversFallbackAndFormattedTransform()
    {
        var cut = RenderLoadedPage();
        var method = typeof(MapPage).GetMethod("GetMapStageStyle", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        Assert.NotNull(method);

        SetField(cut.Instance, "_hasMapViewportLayout", false);
        var fallback = (string?)method!.Invoke(cut.Instance, null);
        Assert.Equal("width:100%;height:auto;transform:translate(0px,0px) scale(1);", fallback);

        SetField(cut.Instance, "_hasMapViewportLayout", true);
        SetField(cut.Instance, "_mapBaseWidth", 500d);
        SetField(cut.Instance, "_mapBaseHeight", 400d);
        SetField(cut.Instance, "_mapPanX", 12.3d);
        SetField(cut.Instance, "_mapPanY", -9.8d);
        SetField(cut.Instance, "_mapZoom", 1.25d);
        var transform = (string?)method.Invoke(cut.Instance, null);
        Assert.Equal("width:500.00px;height:400.00px;transform:translate(12.30px,-9.80px) scale(1.2500);", transform);
    }

    [Fact]
    public void GetMapImageClass_CoversPreAndPostLayoutBranches()
    {
        var cut = RenderLoadedPage();
        var method = typeof(MapPage).GetMethod("GetMapImageClass", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        Assert.NotNull(method);

        SetField(cut.Instance, "_hasMapViewportLayout", false);
        var beforeLayout = (string?)method!.Invoke(cut.Instance, null);
        Assert.Equal("map-page__image", beforeLayout);

        SetField(cut.Instance, "_hasMapViewportLayout", true);
        var afterLayout = (string?)method.Invoke(cut.Instance, null);
        Assert.Equal("map-page__image map-page__image--stage", afterLayout);
    }

    [Fact]
    public void GetMapShellStyle_CoversFallbackAndComputedHeightBranches()
    {
        var cut = RenderLoadedPage();
        var method = typeof(MapPage).GetMethod("GetMapShellStyle", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        Assert.NotNull(method);

        SetField(cut.Instance, "_mapPreferredViewportHeightPx", null);
        var fallback = (string?)method!.Invoke(cut.Instance, null);
        Assert.Equal("height:clamp(24rem, calc(100vh - 16rem), 78vh);", fallback);

        SetField(cut.Instance, "_mapPreferredViewportHeightPx", 640d);
        var computed = (string?)method.Invoke(cut.Instance, null);
        Assert.Equal("height:min(78vh, max(24rem, 640.00px));", computed);
    }

    [Fact]
    public void ZoomSliderHelpers_CoverClampsAndDegenerateRanges()
    {
        var sliderMethod = typeof(MapPage).GetMethod("GetSliderValueForZoom", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
        var zoomMethod = typeof(MapPage).GetMethod("GetZoomForSliderValue", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
        Assert.NotNull(sliderMethod);
        Assert.NotNull(zoomMethod);

        var midSlider = (int)sliderMethod!.Invoke(null, new object?[] { 3d, 1d, 5d })!;
        Assert.Equal(50, midSlider);

        var lowSlider = (int)sliderMethod.Invoke(null, new object?[] { 0.5d, 1d, 5d })!;
        Assert.Equal(0, lowSlider);

        var degenerateSlider = (int)sliderMethod.Invoke(null, new object?[] { 2d, 2d, 2d })!;
        Assert.Equal(0, degenerateSlider);

        var midZoom = (double)zoomMethod!.Invoke(null, new object?[] { 50d, 1d, 5d })!;
        Assert.Equal(3d, midZoom, 10);

        var clampedZoom = (double)zoomMethod.Invoke(null, new object?[] { 300d, 1d, 5d })!;
        Assert.Equal(5d, clampedZoom, 10);

        var degenerateZoom = (double)zoomMethod.Invoke(null, new object?[] { 50d, 2d, 2d })!;
        Assert.Equal(2d, degenerateZoom, 10);
    }

    [Fact]
    public void ClampPanAxis_CoversAllBranches()
    {
        var method = typeof(MapPage).GetMethod("ClampPanAxis", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
        Assert.NotNull(method);

        var noViewport = (double)method!.Invoke(null, new object?[] { 10d, 0d, 100d })!;
        Assert.Equal(0d, noViewport);

        var noScaledSize = (double)method.Invoke(null, new object?[] { 10d, 100d, 0d })!;
        Assert.Equal(0d, noScaledSize);

        var centered = (double)method.Invoke(null, new object?[] { 10d, 120d, 100d })!;
        Assert.Equal(10d, centered, 10);

        var clampedLow = (double)method.Invoke(null, new object?[] { -200d, 100d, 180d })!;
        Assert.Equal(-80d, clampedLow, 10);

        var clampedHigh = (double)method.Invoke(null, new object?[] { 25d, 100d, 180d })!;
        Assert.Equal(0d, clampedHigh, 10);
    }

    [Fact]
    public void TryReadDouble_CoversValidAndInvalidInput()
    {
        var method = typeof(MapPage).GetMethod("TryReadDouble", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
        Assert.NotNull(method);

        var validArgs = new object?[] { "12.5", 0d };
        var valid = (bool)method!.Invoke(null, validArgs)!;
        Assert.True(valid);
        Assert.Equal(12.5d, (double)validArgs[1]!, 10);

        var invalidArgs = new object?[] { "x", 0d };
        var invalid = (bool)method.Invoke(null, invalidArgs)!;
        Assert.False(invalid);
        Assert.Equal(0d, (double)invalidArgs[1]!, 10);

        var nullArgs = new object?[] { null, 0d };
        var nullResult = (bool)method.Invoke(null, nullArgs)!;
        Assert.False(nullResult);
        Assert.Equal(0d, (double)nullArgs[1]!, 10);
    }

    [Fact]
    public void TryMapClientPointToPinCoordinates_CoversValidationAndCoordinateMath()
    {
        var cut = RenderLoadedPage();
        var method = typeof(MapPage).GetMethod("TryMapClientPointToPinCoordinates", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        Assert.NotNull(method);

        SetField(cut.Instance, "_hasMapViewportLayout", false);
        var missingLayoutArgs = new object?[] { 300d, 250d, 0f, 0f, 0f, 0f };
        var missingLayout = (bool)method!.Invoke(cut.Instance, missingLayoutArgs)!;
        Assert.False(missingLayout);

        SetField(cut.Instance, "_hasMapViewportLayout", true);
        SetField(cut.Instance, "_mapBaseWidth", 1000d);
        SetField(cut.Instance, "_mapBaseHeight", 500d);
        SetField(cut.Instance, "_mapZoom", 2d);
        SetField(cut.Instance, "_mapPanX", -100d);
        SetField(cut.Instance, "_mapPanY", -50d);
        SetField(cut.Instance, "_mapViewportWidth", 800d);
        SetField(cut.Instance, "_mapViewportHeight", 600d);

        var negativeArgs = new object?[] { -1d, 100d, 0f, 0f, 0f, 0f };
        var negative = (bool)method.Invoke(cut.Instance, negativeArgs)!;
        Assert.False(negative);

        var validArgs = new object?[] { 300d, 250d, 0f, 0f, 0f, 0f };
        var valid = (bool)method.Invoke(cut.Instance, validArgs)!;
        Assert.True(valid);
        Assert.Equal(0.2f, (float)validArgs[2]!, 3);
        Assert.Equal(0.3f, (float)validArgs[3]!, 3);
        Assert.Equal(0.375f, (float)validArgs[4]!, 3);
        Assert.Equal(0.4167f, (float)validArgs[5]!, 3);
    }

    [Fact]
    public async Task SetZoomLevelAndPanHandlers_CoverClampAndDragFlow()
    {
        var cut = RenderLoadedPage();

        SetField(cut.Instance, "_hasMapViewportLayout", false);
        SetField(cut.Instance, "_mapZoom", 1d);
        await InvokePrivateOnRendererAsync(cut, "SetZoomLevel", 3d);
        Assert.Equal(1d, GetField<double>(cut.Instance, "_mapZoom"), 10);

        SetField(cut.Instance, "_hasMapViewportLayout", true);
        SetField(cut.Instance, "_mapBaseWidth", 1000d);
        SetField(cut.Instance, "_mapBaseHeight", 600d);
        SetField(cut.Instance, "_mapViewportWidth", 800d);
        SetField(cut.Instance, "_mapViewportHeight", 500d);
        SetField(cut.Instance, "_mapMinZoom", 1d);
        SetField(cut.Instance, "_mapMaxZoom", 4d);
        SetField(cut.Instance, "_mapZoom", 1d);
        SetField(cut.Instance, "_mapPanX", -100d);
        SetField(cut.Instance, "_mapPanY", -50d);

        await InvokePrivateOnRendererAsync(cut, "SetZoomLevel", 1d);
        Assert.Equal(1d, GetField<double>(cut.Instance, "_mapZoom"), 10);

        await InvokePrivateOnRendererAsync(cut, "SetZoomLevel", 2d);
        Assert.Equal(2d, GetField<double>(cut.Instance, "_mapZoom"), 10);
        Assert.Equal(-600d, GetField<double>(cut.Instance, "_mapPanX"), 10);
        Assert.Equal(-350d, GetField<double>(cut.Instance, "_mapPanY"), 10);

        await InvokePrivateOnRendererAsync(cut, "SetZoomLevel", 10d);
        Assert.Equal(4d, GetField<double>(cut.Instance, "_mapZoom"), 10);

        SetField(cut.Instance, "_mapZoom", 2d);
        SetField(cut.Instance, "_mapPanX", -600d);
        SetField(cut.Instance, "_mapPanY", -350d);
        SetField(cut.Instance, "_isCreatePinMode", false);

        await InvokePrivateOnRendererAsync(cut, "OnMapViewportMouseDown", new MouseEventArgs { Button = 1, ClientX = 100, ClientY = 100 });
        Assert.False(GetField<bool>(cut.Instance, "_isMapPointerDown"));

        SetField(cut.Instance, "_mapZoom", 1d);
        await InvokePrivateOnRendererAsync(cut, "OnMapViewportMouseDown", new MouseEventArgs { Button = 0, ClientX = 100, ClientY = 100 });
        Assert.False(GetField<bool>(cut.Instance, "_isMapPointerDown"));

        await InvokePrivateOnRendererAsync(cut, "OnMapViewportMouseMove", new MouseEventArgs { ClientX = 120, ClientY = 120 });
        Assert.False(GetField<bool>(cut.Instance, "_isMapDragging"));

        SetField(cut.Instance, "_mapZoom", 2d);
        await InvokePrivateOnRendererAsync(cut, "OnMapViewportMouseDown", new MouseEventArgs { Button = 0, ClientX = 100, ClientY = 100 });
        await InvokePrivateOnRendererAsync(cut, "OnMapViewportMouseMove", new MouseEventArgs { ClientX = 101, ClientY = 101 });
        Assert.False(GetField<bool>(cut.Instance, "_isMapDragging"));
        Assert.Equal(-600d, GetField<double>(cut.Instance, "_mapPanX"), 10);

        await InvokePrivateOnRendererAsync(cut, "OnMapViewportMouseMove", new MouseEventArgs { ClientX = 130, ClientY = 125 });
        Assert.True(GetField<bool>(cut.Instance, "_isMapDragging"));
        Assert.Equal(-570d, GetField<double>(cut.Instance, "_mapPanX"), 10);
        Assert.Equal(-325d, GetField<double>(cut.Instance, "_mapPanY"), 10);

        await InvokePrivateOnRendererAsync(cut, "OnMapViewportMouseMove", new MouseEventArgs { ClientX = 140, ClientY = 130 });
        Assert.Equal(-560d, GetField<double>(cut.Instance, "_mapPanX"), 10);
        Assert.Equal(-320d, GetField<double>(cut.Instance, "_mapPanY"), 10);

        await InvokePrivateOnRendererAsync(cut, "OnMapViewportMouseUp", new MouseEventArgs());
        Assert.False(GetField<bool>(cut.Instance, "_isMapPointerDown"));
        Assert.False(GetField<bool>(cut.Instance, "_isMapDragging"));
        Assert.True(GetField<bool>(cut.Instance, "_suppressCreatePinClick"));
    }

    [Fact]
    public async Task RecenterMapPan_CentersAndClampsStage()
    {
        var cut = RenderLoadedPage();
        SetField(cut.Instance, "_mapBaseWidth", 1000d);
        SetField(cut.Instance, "_mapBaseHeight", 600d);
        SetField(cut.Instance, "_mapViewportWidth", 800d);
        SetField(cut.Instance, "_mapViewportHeight", 500d);
        SetField(cut.Instance, "_mapZoom", 1.5d);
        SetField(cut.Instance, "_mapPanX", 999d);
        SetField(cut.Instance, "_mapPanY", -999d);

        await InvokePrivateOnRendererAsync(cut, "RecenterMapPan");

        Assert.Equal(-350d, GetField<double>(cut.Instance, "_mapPanX"), 10);
        Assert.Equal(-200d, GetField<double>(cut.Instance, "_mapPanY"), 10);
    }

    [Fact]
    public async Task OnZoomSliderInput_CoversNoLayoutInvalidAndValidPaths()
    {
        var cut = RenderLoadedPage();

        SetField(cut.Instance, "_hasMapViewportLayout", false);
        SetField(cut.Instance, "_mapZoom", 1d);
        await InvokePrivateOnRendererAsync(cut, "OnZoomSliderInput", new ChangeEventArgs { Value = "50" });
        Assert.Equal(1d, GetField<double>(cut.Instance, "_mapZoom"), 10);

        SetField(cut.Instance, "_hasMapViewportLayout", true);
        SetField(cut.Instance, "_mapBaseWidth", 1000d);
        SetField(cut.Instance, "_mapBaseHeight", 600d);
        SetField(cut.Instance, "_mapViewportWidth", 800d);
        SetField(cut.Instance, "_mapViewportHeight", 500d);
        SetField(cut.Instance, "_mapMinZoom", 1d);
        SetField(cut.Instance, "_mapMaxZoom", 5d);
        SetField(cut.Instance, "_mapZoom", 1d);
        SetField(cut.Instance, "_mapPanX", 0d);
        SetField(cut.Instance, "_mapPanY", 0d);

        await InvokePrivateOnRendererAsync(cut, "OnZoomSliderInput", new ChangeEventArgs { Value = "invalid" });
        Assert.Equal(1d, GetField<double>(cut.Instance, "_mapZoom"), 10);

        await InvokePrivateOnRendererAsync(cut, "OnZoomSliderInput", new ChangeEventArgs { Value = "50" });
        Assert.Equal(3d, GetField<double>(cut.Instance, "_mapZoom"), 10);
    }

    [Fact]
    public async Task ZoomButtons_CoverAvailabilityAndClickHandlers()
    {
        var cut = RenderLoadedPage();
        var canZoomInMethod = typeof(MapPage).GetMethod("CanZoomIn", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        var canZoomOutMethod = typeof(MapPage).GetMethod("CanZoomOut", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        Assert.NotNull(canZoomInMethod);
        Assert.NotNull(canZoomOutMethod);

        SetField(cut.Instance, "_hasMapViewportLayout", false);
        SetField(cut.Instance, "_mapZoom", 1d);
        Assert.False((bool)canZoomInMethod!.Invoke(cut.Instance, null)!);
        Assert.False((bool)canZoomOutMethod!.Invoke(cut.Instance, null)!);

        await InvokePrivateOnRendererAsync(cut, "ZoomInFromButton");
        await InvokePrivateOnRendererAsync(cut, "ZoomOutFromButton");
        Assert.Equal(1d, GetField<double>(cut.Instance, "_mapZoom"), 10);

        SetField(cut.Instance, "_hasMapViewportLayout", true);
        SetField(cut.Instance, "_mapBaseWidth", 1000d);
        SetField(cut.Instance, "_mapBaseHeight", 600d);
        SetField(cut.Instance, "_mapViewportWidth", 800d);
        SetField(cut.Instance, "_mapViewportHeight", 500d);
        SetField(cut.Instance, "_mapMinZoom", 1d);
        SetField(cut.Instance, "_mapMaxZoom", 5d);
        SetField(cut.Instance, "_mapPanX", 0d);
        SetField(cut.Instance, "_mapPanY", 0d);

        SetField(cut.Instance, "_mapZoom", 1d);
        Assert.True((bool)canZoomInMethod.Invoke(cut.Instance, null)!);
        Assert.False((bool)canZoomOutMethod.Invoke(cut.Instance, null)!);

        await InvokePrivateOnRendererAsync(cut, "ZoomOutFromButton");
        Assert.Equal(1d, GetField<double>(cut.Instance, "_mapZoom"), 10);

        await InvokePrivateOnRendererAsync(cut, "ZoomInFromButton");
        Assert.Equal(1.4d, GetField<double>(cut.Instance, "_mapZoom"), 10);

        SetField(cut.Instance, "_mapZoom", 5d);
        Assert.False((bool)canZoomInMethod.Invoke(cut.Instance, null)!);
        Assert.True((bool)canZoomOutMethod.Invoke(cut.Instance, null)!);

        await InvokePrivateOnRendererAsync(cut, "ZoomInFromButton");
        Assert.Equal(5d, GetField<double>(cut.Instance, "_mapZoom"), 10);

        await InvokePrivateOnRendererAsync(cut, "ZoomOutFromButton");
        Assert.Equal(4.6d, GetField<double>(cut.Instance, "_mapZoom"), 10);
    }

    [Fact]
    public async Task CreatePinInputMethods_UpdateAndClearInput()
    {
        var cut = RenderLoadedPage();
        var articleIdText = Guid.NewGuid().ToString();
        const string pinName = "Tavern";

        await InvokePrivateOnRendererAsync(cut, "OnCreatePinArticleIdChanged", articleIdText);
        Assert.Equal(articleIdText, GetField<string>(cut.Instance, "_createPinArticleIdInput"));

        await InvokePrivateOnRendererAsync(cut, "OnCreatePinNameChanged", pinName);
        Assert.Equal(pinName, GetField<string>(cut.Instance, "_createPinNameInput"));

        await InvokePrivateOnRendererAsync(cut, "ClearCreatePinArticleId");
        Assert.Equal(string.Empty, GetField<string>(cut.Instance, "_createPinArticleIdInput"));

        await InvokePrivateOnRendererAsync(cut, "OnCreatePinNameInput", new ChangeEventArgs { Value = "Harbor" });
        Assert.Equal("Harbor", GetField<string>(cut.Instance, "_createPinNameInput"));

        await InvokePrivateOnRendererAsync(cut, "OnCreatePinArticleIdInput", new ChangeEventArgs { Value = articleIdText });
        Assert.Equal(articleIdText, GetField<string>(cut.Instance, "_createPinArticleIdInput"));

        await InvokePrivateOnRendererAsync(cut, "OnCreatePinNameInput", new ChangeEventArgs { Value = null });
        Assert.Equal(string.Empty, GetField<string>(cut.Instance, "_createPinNameInput"));

        await InvokePrivateOnRendererAsync(cut, "OnCreatePinArticleIdInput", new ChangeEventArgs { Value = null });
        Assert.Equal(string.Empty, GetField<string>(cut.Instance, "_createPinArticleIdInput"));
    }

    [Fact]
    public async Task CancelCreatePinDialog_ClosesDialogAndClearsPendingCoordinates()
    {
        var cut = RenderLoadedPage();
        SetField(cut.Instance, "_isCreatePinDialogOpen", true);
        SetField(cut.Instance, "_pendingCreatePinX", 0.15f);
        SetField(cut.Instance, "_pendingCreatePinY", 0.85f);
        SetField(cut.Instance, "_pendingCreatePinViewportX", 0.2f);
        SetField(cut.Instance, "_pendingCreatePinViewportY", 0.8f);

        await InvokePrivateOnRendererAsync(cut, "CancelCreatePinDialog");

        Assert.False(GetField<bool>(cut.Instance, "_isCreatePinDialogOpen"));
        Assert.Null(GetField<float?>(cut.Instance, "_pendingCreatePinX"));
        Assert.Null(GetField<float?>(cut.Instance, "_pendingCreatePinY"));
        Assert.Null(GetField<float?>(cut.Instance, "_pendingCreatePinViewportX"));
        Assert.Null(GetField<float?>(cut.Instance, "_pendingCreatePinViewportY"));
    }

    [Fact]
    public async Task OnMapImageShellClick_WhenDialogAlreadyOpen_ReturnsEarly()
    {
        var cut = RenderLoadedPage();
        SetField(cut.Instance, "_isCreatePinMode", true);
        SetField(cut.Instance, "_isCreatePinDialogOpen", true);

        await InvokePrivateOnRendererAsync(cut, "OnMapImageShellClick", new MouseEventArgs { ClientX = 10, ClientY = 10 });

        await _mapApi.DidNotReceive().CreatePinAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<MapPinCreateDto>());
    }

    [Fact]
    public async Task ConfirmCreatePinFromDialogAsync_WhenLinkedArticleIdInvalid_SetsValidationError()
    {
        var cut = RenderLoadedPage();
        SetField(cut.Instance, "_isCreatePinDialogOpen", true);
        SetField(cut.Instance, "_pendingCreatePinX", 0.2f);
        SetField(cut.Instance, "_pendingCreatePinY", 0.3f);
        SetField(cut.Instance, "_createPinArticleIdInput", "not-a-guid");

        await InvokePrivateOnRendererAsync(cut, "ConfirmCreatePinFromDialogAsync");

        Assert.Equal("Linked Article Id must be a valid GUID.", GetField<string>(cut.Instance, "_createPinError"));
        await _mapApi.DidNotReceive().CreatePinAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<MapPinCreateDto>());
    }

    [Fact]
    public async Task ConfirmCreatePinFromDialogAsync_WhenPendingCoordinatesMissing_SetsBoundsError()
    {
        var cut = RenderLoadedPage();
        SetField(cut.Instance, "_isCreatePinDialogOpen", true);
        SetField(cut.Instance, "_createPinArticleIdInput", string.Empty);
        SetField(cut.Instance, "_pendingCreatePinX", null);
        SetField(cut.Instance, "_pendingCreatePinY", null);

        await InvokePrivateOnRendererAsync(cut, "ConfirmCreatePinFromDialogAsync");

        Assert.Equal("Could not determine map bounds for pin placement.", GetField<string>(cut.Instance, "_createPinError"));
        await _mapApi.DidNotReceive().CreatePinAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<MapPinCreateDto>());
    }

    [Fact]
    public async Task ConfirmCreatePinFromDialogAsync_WhenApiReturnsNull_SetsCreateErrorAndKeepsDialogOpen()
    {
        var cut = RenderLoadedPage();
        _mapApi.CreatePinAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<MapPinCreateDto>())
            .Returns((MapPinResponseDto?)null);
        SetField(cut.Instance, "_isCreatePinDialogOpen", true);
        SetField(cut.Instance, "_pendingCreatePinX", 0.2f);
        SetField(cut.Instance, "_pendingCreatePinY", 0.3f);
        SetField(cut.Instance, "_createPinArticleIdInput", string.Empty);
        SetField(cut.Instance, "_createPinNameInput", "Harbor");

        await InvokePrivateOnRendererAsync(cut, "ConfirmCreatePinFromDialogAsync");

        Assert.Equal("Failed to create pin.", GetField<string>(cut.Instance, "_createPinError"));
        Assert.True(GetField<bool>(cut.Instance, "_isCreatePinDialogOpen"));
    }

    [Fact]
    public async Task ConfirmCreatePinFromDialogAsync_WhenSuccessful_CreatesPinAndClosesDialog()
    {
        var cut = RenderLoadedPage();
        var linkedArticleId = Guid.NewGuid();
        var createdPin = new MapPinResponseDto
        {
            PinId = Guid.NewGuid(),
            MapId = Guid.NewGuid(),
            LayerId = Guid.NewGuid(),
            Name = "Harbor",
            X = 0.2f,
            Y = 0.3f
        };

        _mapApi.CreatePinAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<MapPinCreateDto>())
            .Returns(createdPin);

        SetField(cut.Instance, "_isCreatePinDialogOpen", true);
        SetField(cut.Instance, "_pendingCreatePinX", 0.2f);
        SetField(cut.Instance, "_pendingCreatePinY", 0.3f);
        SetField(cut.Instance, "_createPinArticleIdInput", linkedArticleId.ToString());
        SetField(cut.Instance, "_createPinNameInput", "Harbor");
        SetField(cut.Instance, "_pins", new List<MapPinResponseDto>());

        await InvokePrivateOnRendererAsync(cut, "ConfirmCreatePinFromDialogAsync");

        var pins = GetField<List<MapPinResponseDto>>(cut.Instance, "_pins");
        Assert.Single(pins);
        Assert.Equal(createdPin.PinId, pins[0].PinId);
        Assert.False(GetField<bool>(cut.Instance, "_isCreatePinDialogOpen"));
        Assert.Null(GetField<float?>(cut.Instance, "_pendingCreatePinX"));
        Assert.Null(GetField<float?>(cut.Instance, "_pendingCreatePinY"));
        Assert.Equal(string.Empty, GetField<string>(cut.Instance, "_createPinArticleIdInput"));
        Assert.Equal(string.Empty, GetField<string>(cut.Instance, "_createPinNameInput"));

        await _mapApi.Received(1).CreatePinAsync(
            Arg.Any<Guid>(),
            Arg.Any<Guid>(),
            Arg.Is<MapPinCreateDto>(dto =>
                dto.Name == "Harbor"
                && dto.X == 0.2f
                && dto.Y == 0.3f
                && dto.LinkedArticleId == linkedArticleId));
    }

    [Fact]
    public async Task OnCreatePinPopupKeyDown_WhenEnter_CreatesPin()
    {
        var cut = RenderLoadedPage();
        var createdPin = new MapPinResponseDto
        {
            PinId = Guid.NewGuid(),
            MapId = Guid.NewGuid(),
            LayerId = Guid.NewGuid(),
            X = 0.22f,
            Y = 0.33f
        };

        _mapApi.CreatePinAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<MapPinCreateDto>())
            .Returns(createdPin);

        SetField(cut.Instance, "_isCreatePinDialogOpen", true);
        SetField(cut.Instance, "_pendingCreatePinX", 0.22f);
        SetField(cut.Instance, "_pendingCreatePinY", 0.33f);
        SetField(cut.Instance, "_pins", new List<MapPinResponseDto>());

        await InvokePrivateOnRendererAsync(cut, "OnCreatePinPopupKeyDown", new KeyboardEventArgs { Key = "Enter" });

        Assert.Single(GetField<List<MapPinResponseDto>>(cut.Instance, "_pins"));
        await _mapApi.Received(1).CreatePinAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<MapPinCreateDto>());
    }

    [Fact]
    public async Task OnCreatePinPopupKeyDown_WhenNotEnter_DoesNotCreatePin()
    {
        var cut = RenderLoadedPage();
        SetField(cut.Instance, "_isCreatePinDialogOpen", true);
        SetField(cut.Instance, "_pendingCreatePinX", 0.22f);
        SetField(cut.Instance, "_pendingCreatePinY", 0.33f);

        await InvokePrivateOnRendererAsync(cut, "OnCreatePinPopupKeyDown", new KeyboardEventArgs { Key = "Escape" });

        await _mapApi.DidNotReceive().CreatePinAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<MapPinCreateDto>());
    }

    [Fact]
    public async Task DeleteSelectionMethods_ToggleAndClearSelection()
    {
        var cut = RenderLoadedPage();
        var pinId = Guid.NewGuid();
        SetField(cut.Instance, "_pinDeleteError", "bad");
        SetField(cut.Instance, "_selectedPinId", null);

        await InvokePrivateOnRendererAsync(cut, "SelectPinForDelete", pinId);
        Assert.Equal(string.Empty, GetField<string>(cut.Instance, "_pinDeleteError"));
        Assert.Equal(pinId, GetField<Guid?>(cut.Instance, "_selectedPinId"));

        await InvokePrivateOnRendererAsync(cut, "SelectPinForDelete", pinId);
        Assert.Null(GetField<Guid?>(cut.Instance, "_selectedPinId"));

        SetField(cut.Instance, "_selectedPinId", pinId);
        await InvokePrivateOnRendererAsync(cut, "CancelDeletePinSelection");
        Assert.Null(GetField<Guid?>(cut.Instance, "_selectedPinId"));
    }

    [Fact]
    public void GetPinStyle_FormatsNormalizedCoordinatesAsPercentages()
    {
        var method = typeof(MapPage).GetMethod("GetPinStyle", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
        Assert.NotNull(method);

        var style = (string?)method!.Invoke(null, new object?[] { new MapPinResponseDto { X = 0.1234f, Y = 0.5678f } });

        Assert.Equal("left:12.3400%;top:56.7800%;", style);
    }

    [Fact]
    public void GetCreatePinPopupStyle_UsesPendingCoordinatesOrCenterFallback()
    {
        var cut = RenderLoadedPage();
        var method = typeof(MapPage).GetMethod("GetCreatePinPopupStyle", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        Assert.NotNull(method);

        SetField(cut.Instance, "_pendingCreatePinViewportX", null);
        SetField(cut.Instance, "_pendingCreatePinViewportY", null);
        var fallback = (string?)method!.Invoke(cut.Instance, null);
        Assert.Equal("left:50.0000%;top:50.0000%;", fallback);

        SetField(cut.Instance, "_pendingCreatePinViewportX", 0.12f);
        SetField(cut.Instance, "_pendingCreatePinViewportY", 0.34f);
        var exact = (string?)method.Invoke(cut.Instance, null);
        Assert.Equal("left:12.0000%;top:34.0000%;", exact);
    }

    [Fact]
    public void PinLabelAndTooltipHelpers_CoverNameAndArticleFallbackBranches()
    {
        var hasPinLabelMethod = typeof(MapPage).GetMethod("HasPinLabel", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
        var getPinLabelStyleMethod = typeof(MapPage).GetMethod("GetPinLabelStyle", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
        var getPinTooltipMethod = typeof(MapPage).GetMethod("GetPinTooltip", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
        var getPinAriaLabelMethod = typeof(MapPage).GetMethod("GetPinAriaLabel", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
        Assert.NotNull(hasPinLabelMethod);
        Assert.NotNull(getPinLabelStyleMethod);
        Assert.NotNull(getPinTooltipMethod);
        Assert.NotNull(getPinAriaLabelMethod);

        var namedPin = new MapPinResponseDto { Name = "Harbor", X = 0.2f, Y = 0.4f };
        var articlePin = new MapPinResponseDto
        {
            LinkedArticle = new LinkedArticleSummaryDto { ArticleId = Guid.NewGuid(), Title = "Waterdeep" },
            X = 0.3f,
            Y = 0.6f
        };
        var barePin = new MapPinResponseDto { X = 0.5f, Y = 0.7f };

        Assert.True((bool)hasPinLabelMethod!.Invoke(null, new object?[] { namedPin })!);
        Assert.False((bool)hasPinLabelMethod.Invoke(null, new object?[] { articlePin })!);
        Assert.False((bool)hasPinLabelMethod.Invoke(null, new object?[] { barePin })!);

        var labelStyle = (string?)getPinLabelStyleMethod!.Invoke(null, new object?[] { namedPin });
        Assert.Equal("left:20.0000%;top:40.0000%;", labelStyle);

        Assert.Equal("Harbor", (string?)getPinTooltipMethod!.Invoke(null, new object?[] { namedPin }));
        Assert.Equal("Waterdeep", (string?)getPinTooltipMethod.Invoke(null, new object?[] { articlePin }));
        Assert.Equal("Map pin", (string?)getPinTooltipMethod.Invoke(null, new object?[] { barePin }));

        Assert.Equal("Open article: Waterdeep", (string?)getPinAriaLabelMethod!.Invoke(null, new object?[] { articlePin }));
        Assert.Equal("Pin: Map pin", (string?)getPinAriaLabelMethod.Invoke(null, new object?[] { barePin }));
    }

    [Fact]
    public void TryParseLinkedArticleId_CoversBlankValidAndInvalidBranches()
    {
        var method = typeof(MapPage).GetMethod("TryParseLinkedArticleId", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
        Assert.NotNull(method);

        var blankArgs = new object?[] { "   ", null };
        var blankResult = (bool)method!.Invoke(null, blankArgs)!;
        Assert.True(blankResult);
        Assert.Null((Guid?)blankArgs[1]);

        var validId = Guid.NewGuid();
        var validArgs = new object?[] { validId.ToString(), null };
        var validResult = (bool)method.Invoke(null, validArgs)!;
        Assert.True(validResult);
        Assert.Equal(validId, (Guid?)validArgs[1]);

        var invalidArgs = new object?[] { "not-a-guid", null };
        var invalidResult = (bool)method.Invoke(null, invalidArgs)!;
        Assert.False(invalidResult);
        Assert.Null((Guid?)invalidArgs[1]);
    }

    [Fact]
    public void Clamp01_ClampsLowAndHighAndReturnsMiddle()
    {
        var method = typeof(MapPage).GetMethod("Clamp01", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
        Assert.NotNull(method);

        var low = (double)method!.Invoke(null, new object?[] { -0.5d })!;
        var middle = (double)method.Invoke(null, new object?[] { 0.25d })!;
        var high = (double)method.Invoke(null, new object?[] { 1.5d })!;

        Assert.Equal(0d, low);
        Assert.Equal(0.25d, middle);
        Assert.Equal(1d, high);
    }

    [Fact]
    public void HasUsableBounds_CoversNullWidthHeightAndValidBranches()
    {
        var method = typeof(MapPage).GetMethod("HasUsableBounds", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
        var rectType = typeof(MapPage).GetNestedType("MapElementRect", System.Reflection.BindingFlags.NonPublic);
        Assert.NotNull(method);
        Assert.NotNull(rectType);

        Assert.False((bool)method!.Invoke(null, new object?[] { null })!);

        var noWidth = Activator.CreateInstance(rectType!);
        rectType!.GetProperty("Width")!.SetValue(noWidth, 0d);
        rectType.GetProperty("Height")!.SetValue(noWidth, 10d);
        Assert.False((bool)method.Invoke(null, new[] { noWidth })!);

        var noHeight = Activator.CreateInstance(rectType);
        rectType.GetProperty("Width")!.SetValue(noHeight, 10d);
        rectType.GetProperty("Height")!.SetValue(noHeight, 0d);
        Assert.False((bool)method.Invoke(null, new[] { noHeight })!);

        var valid = Activator.CreateInstance(rectType);
        rectType.GetProperty("Width")!.SetValue(valid, 10d);
        rectType.GetProperty("Height")!.SetValue(valid, 20d);
        Assert.True((bool)method.Invoke(null, new[] { valid })!);
    }

    [Fact]
    public void SelectedPin_ReturnsPinWhenSelectedIdExists_ElseNull()
    {
        var cut = RenderLoadedPage();
        var pin = new MapPinResponseDto { PinId = Guid.NewGuid(), MapId = Guid.NewGuid(), LayerId = Guid.NewGuid(), X = 0.3f, Y = 0.4f };
        SetField(cut.Instance, "_pins", new List<MapPinResponseDto> { pin });
        SetField(cut.Instance, "_selectedPinId", pin.PinId);

        var selectedPinProperty = cut.Instance.GetType()
            .GetProperty("SelectedPin", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        Assert.NotNull(selectedPinProperty);

        var selected = (MapPinResponseDto?)selectedPinProperty!.GetValue(cut.Instance);
        Assert.NotNull(selected);
        Assert.Equal(pin.PinId, selected!.PinId);

        SetField(cut.Instance, "_selectedPinId", null);
        var none = (MapPinResponseDto?)selectedPinProperty.GetValue(cut.Instance);
        Assert.Null(none);
    }

    private IRenderedComponent<MapPage> RenderPage(Guid worldId, Guid mapId)
    {
        return RenderComponent<MapPage>(parameters => parameters
            .Add(p => p.WorldId, worldId)
            .Add(p => p.MapId, mapId));
    }

    private IRenderedComponent<MapPage> RenderLoadedPage()
    {
        var worldId = Guid.NewGuid();
        var mapId = Guid.NewGuid();
        _mapApi.GetMapAsync(worldId, mapId).Returns((new MapDto { Name = "Loaded Map" }, 200, null));
        _mapApi.GetBasemapReadUrlAsync(worldId, mapId).Returns((new GetBasemapReadUrlResponseDto { ReadUrl = "https://blob/read" }, 200, null));
        _mapApi.ListPinsForMapAsync(worldId, mapId).Returns(new List<MapPinResponseDto>());

        var cut = RenderPage(worldId, mapId);
        cut.WaitForAssertion(() => Assert.Contains("<img", cut.Markup, StringComparison.OrdinalIgnoreCase));
        return cut;
    }

    private static T GetField<T>(object instance, string fieldName)
    {
        var field = instance.GetType().GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        Assert.NotNull(field);
        return (T)field!.GetValue(instance)!;
    }

    private static void SetField(object instance, string fieldName, object? value)
    {
        var field = instance.GetType().GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        Assert.NotNull(field);
        field!.SetValue(instance, value);
    }

    private static Task InvokePrivateOnRendererAsync(IRenderedComponent<MapPage> cut, string methodName, params object?[] args)
    {
        return cut.InvokeAsync(async () =>
        {
            var method = cut.Instance.GetType().GetMethod(methodName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Assert.NotNull(method);
            var result = method!.Invoke(cut.Instance, args);
            if (result is Task task)
            {
                await task;
            }
        });
    }
}
