using Bunit;
using Chronicis.Client.Components.Shared;
using Chronicis.Client.Pages.Maps;
using Chronicis.Client.Services;
using Chronicis.Client.Tests.Components;
using Chronicis.Shared.DTOs;
using Chronicis.Shared.DTOs.Maps;
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
    private readonly ITreeStateService _treeState = Substitute.For<ITreeStateService>();
    private readonly Guid _worldOwnerId = Guid.Parse("33000000-0000-0000-0000-000000000001");
    private readonly Guid _nonOwnerId = Guid.Parse("33000000-0000-0000-0000-000000000002");

    public MapPageTests()
    {
        Services.AddSingleton(_mapApi);
        Services.AddSingleton(_worldApi);
        Services.AddSingleton(_userApi);
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

    private IRenderedComponent<MapPage> RenderPage(Guid worldId, Guid mapId)
    {
        return RenderComponent<MapPage>(parameters => parameters
            .Add(p => p.WorldId, worldId)
            .Add(p => p.MapId, mapId));
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
