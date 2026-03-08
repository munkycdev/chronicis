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

public class MapDetailTests : MudBlazorTestContext
{
    private readonly IMapApiService _mapApi = Substitute.For<IMapApiService>();
    private readonly IWorldApiService _worldApi = Substitute.For<IWorldApiService>();
    private readonly IUserApiService _userApi = Substitute.For<IUserApiService>();
    private readonly IArticleApiService _articleApi = Substitute.For<IArticleApiService>();
    private readonly ITreeStateService _treeState = Substitute.For<ITreeStateService>();
    private readonly Guid _worldOwnerId = Guid.Parse("33000000-0000-0000-0000-000000000001");
    private readonly Guid _nonOwnerId = Guid.Parse("33000000-0000-0000-0000-000000000002");
    private readonly Guid _worldLayerId = Guid.Parse("44000000-0000-0000-0000-000000000001");
    private readonly Guid _campaignLayerId = Guid.Parse("44000000-0000-0000-0000-000000000002");
    private readonly Guid _arcLayerId = Guid.Parse("44000000-0000-0000-0000-000000000003");

    public MapDetailTests()
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
        _mapApi.GetLayersForMapAsync(Arg.Any<Guid>(), Arg.Any<Guid>())
            .Returns(new List<MapLayerDto>
            {
                new() { MapLayerId = _worldLayerId, Name = "World", SortOrder = 0, IsEnabled = true },
                new() { MapLayerId = _campaignLayerId, Name = "Campaign", SortOrder = 1, IsEnabled = true },
                new() { MapLayerId = _arcLayerId, Name = "Arc", SortOrder = 2, IsEnabled = true },
            });
        _mapApi.ReorderLayersAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<IList<Guid>>())
            .Returns(Task.CompletedTask);
        _mapApi.RenameLayerAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<string>())
            .Returns(Task.CompletedTask);
        _mapApi.SetLayerParentAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid?>())
            .Returns(Task.CompletedTask);
        _mapApi.DeleteLayerAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>())
            .Returns(Task.CompletedTask);
    }

    [Fact]
    public void MapDetail_WhenLoading_RendersLoadingSkeleton()
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
    public void MapDetail_WhenNotFound_RendersNotFoundState()
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
    public void MapDetail_WhenUnauthorized_RendersUnauthorizedState()
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
    public void MapDetail_WhenUnauthorized401_RendersUnauthorizedState()
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
    public void MapDetail_WhenBasemapMissingByError_RendersBasemapMissingState()
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
    public void MapDetail_WhenBasemapReadUrlEmpty_RendersBasemapMissingState()
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
    public void MapDetail_WhenSuccessful_RendersBasemapImage()
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
    public void MapDetail_WhenSuccessful_RendersLayerPanelRows()
    {
        var cut = RenderLoadedPage();

        cut.WaitForAssertion(() =>
        {
            var rows = cut.FindAll(".map-page__layer-row");
            Assert.Equal(3, rows.Count);
            Assert.Contains("World", rows[0].TextContent, StringComparison.Ordinal);
            Assert.Contains("Campaign", rows[1].TextContent, StringComparison.Ordinal);
            Assert.Contains("Arc", rows[2].TextContent, StringComparison.Ordinal);
        });
    }

    [Fact]
    public void MapDetail_WhenSuccessful_RendersRootAddButtonAndNoLegacyAddControls()
    {
        var cut = RenderLoadedPage();

        cut.WaitForAssertion(() =>
        {
            Assert.Single(cut.FindAll(".map-page__layer-add-root-button"));
            Assert.Empty(cut.FindAll(".map-page__layer-add-input"));
            Assert.Empty(cut.FindAll(".map-page__layer-add-button"));
        });
    }

    [Fact]
    public void MapDetail_WhenSuccessful_RendersAddChildLayerActionForEachLayer()
    {
        var cut = RenderLoadedPage();

        cut.WaitForAssertion(() =>
        {
            var addChildButtons = cut.FindAll(".map-page__layer-add-child-button");
            Assert.Equal(3, addChildButtons.Count);
            Assert.All(addChildButtons, button =>
                Assert.Contains("Add Child Layer", button.TextContent, StringComparison.Ordinal));
        });
    }

    [Fact]
    public void AddRootLevelLayer_WhenClicked_RendersInlineCreateRowAtRootDepth()
    {
        var cut = RenderLoadedPage();

        cut.Find(".map-page__layer-add-root-button").Click();

        cut.WaitForAssertion(() =>
        {
            var draftRows = cut.FindAll(".map-page__layer-row--inline-create");
            Assert.Single(draftRows);
            Assert.Equal("0", draftRows[0].GetAttribute("data-layer-depth"));
            Assert.Single(cut.FindAll(".map-page__layer-create-input"));
            Assert.Contains("Save", draftRows[0].TextContent, StringComparison.Ordinal);
            Assert.Contains("Cancel", draftRows[0].TextContent, StringComparison.Ordinal);
        });

        Assert.True(GetField<bool>(cut.Instance, "_isInlineCreateActive"));
        Assert.True(GetField<bool>(cut.Instance, "_shouldFocusCreateLayerInput"));
        Assert.Null(GetField<Guid?>(cut.Instance, "_createLayerParentLayerId"));
    }

    [Fact]
    public async Task InlineCreate_OnAfterRender_ClearsPendingCreateFocusFlag()
    {
        var cut = RenderLoadedPage();

        cut.Find(".map-page__layer-add-root-button").Click();
        Assert.True(GetField<bool>(cut.Instance, "_shouldFocusCreateLayerInput"));

        await InvokePrivateOnRendererAsync(cut, "OnAfterRenderAsync", false);

        Assert.False(GetField<bool>(cut.Instance, "_shouldFocusCreateLayerInput"));
    }

    [Fact]
    public void AddChildLayer_WhenClicked_InsertsDraftAtEndOfParentSubtree()
    {
        var worldId = Guid.NewGuid();
        var mapId = Guid.NewGuid();
        var locationsLayerId = Guid.NewGuid();
        var harborLayerId = Guid.NewGuid();

        _mapApi.GetMapAsync(worldId, mapId).Returns((new MapDto { Name = "Nested Layer Map" }, 200, null));
        _mapApi.GetBasemapReadUrlAsync(worldId, mapId).Returns((new GetBasemapReadUrlResponseDto { ReadUrl = "https://blob/read" }, 200, null));
        _mapApi.ListPinsForMapAsync(worldId, mapId).Returns(new List<MapPinResponseDto>());
        _mapApi.GetLayersForMapAsync(worldId, mapId).Returns(new List<MapLayerDto>
        {
            new() { MapLayerId = _worldLayerId, Name = "World", SortOrder = 0, IsEnabled = true },
            new() { MapLayerId = _campaignLayerId, Name = "Campaign", SortOrder = 1, IsEnabled = true },
            new() { MapLayerId = _arcLayerId, Name = "Arc", SortOrder = 2, IsEnabled = true },
            new() { MapLayerId = locationsLayerId, Name = "Locations", SortOrder = 0, IsEnabled = true, ParentLayerId = _worldLayerId },
            new() { MapLayerId = harborLayerId, Name = "Harbor", SortOrder = 0, IsEnabled = true, ParentLayerId = locationsLayerId },
        });

        var cut = RenderPage(worldId, mapId);
        cut.WaitForAssertion(() => Assert.Equal(5, cut.FindAll(".map-page__layer-row").Count));

        cut.Find($".map-page__layer-row[data-layer-id='{_worldLayerId}'] .map-page__layer-add-child-button").Click();

        cut.WaitForAssertion(() =>
        {
            var rows = cut.FindAll(".map-page__layer-row").ToList();
            var draftIndex = rows.FindIndex(row => row.ClassList.Contains("map-page__layer-row--inline-create"));
            var campaignIndex = rows.FindIndex(row => row.TextContent.Contains("Campaign", StringComparison.Ordinal));
            Assert.True(draftIndex >= 0);
            Assert.Equal(campaignIndex - 1, draftIndex);
            Assert.Equal("1", rows[draftIndex].GetAttribute("data-layer-depth"));
        });

        Assert.Equal(_worldLayerId, GetField<Guid?>(cut.Instance, "_createLayerParentLayerId"));
    }

    [Fact]
    public void LayerTree_ParentsRenderDisclosureAndLeavesRenderSpacer()
    {
        var locationsLayerId = Guid.NewGuid();
        var harborLayerId = Guid.NewGuid();
        var cut = RenderNestedLayerPage(locationsLayerId, harborLayerId);

        cut.WaitForAssertion(() =>
        {
            var disclosureButtons = cut.FindAll(".map-page__layer-disclosure");
            Assert.Equal(2, disclosureButtons.Count);
            Assert.NotNull(cut.Find($".map-page__layer-row[data-layer-id='{_worldLayerId}'] .map-page__layer-disclosure"));
            Assert.NotNull(cut.Find($".map-page__layer-row[data-layer-id='{locationsLayerId}'] .map-page__layer-disclosure"));

            var leafRow = cut.Find($".map-page__layer-row[data-layer-id='{harborLayerId}']");
            Assert.Null(leafRow.QuerySelector(".map-page__layer-disclosure"));
            Assert.NotNull(leafRow.QuerySelector(".map-page__layer-disclosure-spacer"));
        });
    }

    [Fact]
    public void LayerTree_CollapseAndExpand_HidesAndRestoresDescendants()
    {
        var locationsLayerId = Guid.NewGuid();
        var harborLayerId = Guid.NewGuid();
        var cut = RenderNestedLayerPage(locationsLayerId, harborLayerId);

        cut.Find($".map-page__layer-row[data-layer-id='{_worldLayerId}'] .map-page__layer-disclosure").Click();

        cut.WaitForAssertion(() =>
        {
            var visibleLayerIds = GetVisibleLayerRowIds(cut);
            Assert.DoesNotContain(locationsLayerId.ToString(), visibleLayerIds);
            Assert.DoesNotContain(harborLayerId.ToString(), visibleLayerIds);
            Assert.Contains(_campaignLayerId.ToString(), visibleLayerIds);
            Assert.Equal("false", cut.Find($".map-page__layer-row[data-layer-id='{_worldLayerId}'] .map-page__layer-disclosure").GetAttribute("aria-expanded"));
        });

        cut.Find($".map-page__layer-row[data-layer-id='{_worldLayerId}'] .map-page__layer-disclosure").Click();

        cut.WaitForAssertion(() =>
        {
            var visibleLayerIds = GetVisibleLayerRowIds(cut);
            Assert.Contains(locationsLayerId.ToString(), visibleLayerIds);
            Assert.Contains(harborLayerId.ToString(), visibleLayerIds);
            Assert.Equal("true", cut.Find($".map-page__layer-row[data-layer-id='{_worldLayerId}'] .map-page__layer-disclosure").GetAttribute("aria-expanded"));
        });
    }

    [Fact]
    public void AddChildLayer_FromCollapsedParent_AutoExpandsAndShowsDraft()
    {
        var locationsLayerId = Guid.NewGuid();
        var harborLayerId = Guid.NewGuid();
        var cut = RenderNestedLayerPage(locationsLayerId, harborLayerId);

        cut.Find($".map-page__layer-row[data-layer-id='{_worldLayerId}'] .map-page__layer-disclosure").Click();
        cut.WaitForAssertion(() => Assert.DoesNotContain(locationsLayerId.ToString(), GetVisibleLayerRowIds(cut)));

        cut.Find($".map-page__layer-row[data-layer-id='{_worldLayerId}'] .map-page__layer-add-child-button").Click();

        cut.WaitForAssertion(() =>
        {
            var rows = cut.FindAll(".map-page__layer-row").ToList();
            var draftIndex = rows.FindIndex(row => row.ClassList.Contains("map-page__layer-row--inline-create"));
            var campaignIndex = rows.FindIndex(row => row.GetAttribute("data-layer-id") == _campaignLayerId.ToString());
            Assert.True(draftIndex >= 0);
            Assert.Equal(campaignIndex - 1, draftIndex);
            Assert.Equal("1", rows[draftIndex].GetAttribute("data-layer-depth"));
            Assert.Equal("true", cut.Find($".map-page__layer-row[data-layer-id='{_worldLayerId}'] .map-page__layer-disclosure").GetAttribute("aria-expanded"));
            Assert.Contains(locationsLayerId.ToString(), GetVisibleLayerRowIds(cut));
        });
    }

    [Fact]
    public async Task BeginLayerRename_FromCollapsedBranch_ExpandsAncestorsAndKeepsEditorVisible()
    {
        var locationsLayerId = Guid.NewGuid();
        var harborLayerId = Guid.NewGuid();
        var cut = RenderNestedLayerPage(locationsLayerId, harborLayerId);

        cut.Find($".map-page__layer-row[data-layer-id='{_worldLayerId}'] .map-page__layer-disclosure").Click();
        cut.WaitForAssertion(() => Assert.DoesNotContain(harborLayerId.ToString(), GetVisibleLayerRowIds(cut)));

        await InvokePrivateOnRendererAsync(cut, "BeginLayerRename", harborLayerId);

        cut.WaitForAssertion(() =>
        {
            Assert.Contains(harborLayerId.ToString(), GetVisibleLayerRowIds(cut));
            Assert.Equal("true", cut.Find($".map-page__layer-row[data-layer-id='{_worldLayerId}'] .map-page__layer-disclosure").GetAttribute("aria-expanded"));
            Assert.Equal("true", cut.Find($".map-page__layer-row[data-layer-id='{locationsLayerId}'] .map-page__layer-disclosure").GetAttribute("aria-expanded"));
            Assert.NotNull(cut.Find($".map-page__layer-row[data-layer-id='{harborLayerId}'] .map-page__layer-rename-input"));
        });
    }

    [Fact]
    public async Task LayerTree_CollapseBlocked_WhenDescendantRenameOrCreateIsActive()
    {
        var locationsLayerId = Guid.NewGuid();
        var harborLayerId = Guid.NewGuid();
        var cut = RenderNestedLayerPage(locationsLayerId, harborLayerId);

        await InvokePrivateOnRendererAsync(cut, "BeginLayerRename", harborLayerId);

        cut.WaitForAssertion(() =>
        {
            Assert.True(cut.Find($".map-page__layer-row[data-layer-id='{_worldLayerId}'] .map-page__layer-disclosure").HasAttribute("disabled"));
            Assert.True(cut.Find($".map-page__layer-row[data-layer-id='{locationsLayerId}'] .map-page__layer-disclosure").HasAttribute("disabled"));
        });

        cut.Find($".map-page__layer-row[data-layer-id='{harborLayerId}'] .map-page__layer-action-button--cancel").Click();

        cut.Find($".map-page__layer-row[data-layer-id='{locationsLayerId}'] .map-page__layer-add-child-button").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.True(cut.Find($".map-page__layer-row[data-layer-id='{_worldLayerId}'] .map-page__layer-disclosure").HasAttribute("disabled"));
            Assert.True(cut.Find($".map-page__layer-row[data-layer-id='{locationsLayerId}'] .map-page__layer-disclosure").HasAttribute("disabled"));
            Assert.Single(cut.FindAll(".map-page__layer-row--inline-create"));
        });
    }

    [Fact]
    public async Task CollapsedBranch_RenameAndCreateSaveFlowsStillWork()
    {
        var worldId = Guid.NewGuid();
        var mapId = Guid.NewGuid();
        var locationsLayerId = Guid.NewGuid();
        var harborLayerId = Guid.NewGuid();
        var newChildLayerId = Guid.NewGuid();

        ConfigureNestedLayerPage(worldId, mapId, locationsLayerId, harborLayerId);
        _mapApi.CreateLayerAsync(worldId, mapId, Arg.Any<string>(), Arg.Any<Guid?>())
            .Returns(new MapLayerDto
            {
                MapLayerId = newChildLayerId,
                Name = "Docks",
                SortOrder = 1,
                IsEnabled = true,
                ParentLayerId = locationsLayerId,
            });

        var cut = RenderPage(worldId, mapId);
        cut.WaitForAssertion(() => Assert.Equal(5, cut.FindAll(".map-page__layer-row").Count));

        cut.Find($".map-page__layer-row[data-layer-id='{_worldLayerId}'] .map-page__layer-disclosure").Click();
        cut.WaitForAssertion(() => Assert.DoesNotContain(harborLayerId.ToString(), GetVisibleLayerRowIds(cut)));

        await InvokePrivateOnRendererAsync(cut, "BeginLayerRename", harborLayerId);
        cut.Find($".map-page__layer-row[data-layer-id='{harborLayerId}'] .map-page__layer-rename-input").Input("Port");
        cut.Find($".map-page__layer-row[data-layer-id='{harborLayerId}'] .map-page__layer-action-button").Click();

        await _mapApi.Received(1).RenameLayerAsync(worldId, mapId, harborLayerId, "Port");

        cut.WaitForAssertion(() =>
            Assert.Contains("Port", cut.Find($".map-page__layer-row[data-layer-id='{harborLayerId}']").TextContent, StringComparison.Ordinal));

        cut.Find($".map-page__layer-row[data-layer-id='{locationsLayerId}'] .map-page__layer-disclosure").Click();
        cut.WaitForAssertion(() => Assert.DoesNotContain(harborLayerId.ToString(), GetVisibleLayerRowIds(cut)));

        cut.Find($".map-page__layer-row[data-layer-id='{locationsLayerId}'] .map-page__layer-add-child-button").Click();
        cut.Find(".map-page__layer-create-input").Input("Docks");
        cut.Find(".map-page__layer-create-input").KeyDown("Enter");

        await _mapApi.Received(1).CreateLayerAsync(worldId, mapId, "Docks", locationsLayerId);

        cut.WaitForAssertion(() =>
        {
            Assert.Contains(newChildLayerId.ToString(), GetVisibleLayerRowIds(cut));
            Assert.Equal(newChildLayerId, GetField<Guid?>(cut.Instance, "SelectedLayerId"));
        });
    }

    [Fact]
    public async Task InlineCreate_WhenEnterOnRootDraft_SavesAndSelectsLayer()
    {
        var worldId = Guid.NewGuid();
        var mapId = Guid.NewGuid();
        var customLayerId = Guid.NewGuid();

        _mapApi.GetMapAsync(worldId, mapId).Returns((new MapDto { Name = "Add Layer Map" }, 200, null));
        _mapApi.GetBasemapReadUrlAsync(worldId, mapId).Returns((new GetBasemapReadUrlResponseDto { ReadUrl = "https://blob/read" }, 200, null));
        _mapApi.ListPinsForMapAsync(worldId, mapId).Returns(new List<MapPinResponseDto>());
        _mapApi.CreateLayerAsync(worldId, mapId, Arg.Any<string>(), Arg.Any<Guid?>())
            .Returns(new MapLayerDto
            {
                MapLayerId = customLayerId,
                Name = "Cities",
                SortOrder = 3,
                IsEnabled = true
            });

        var cut = RenderPage(worldId, mapId);
        cut.WaitForAssertion(() => Assert.Equal(3, cut.FindAll(".map-page__layer-row").Count));

        cut.Find(".map-page__layer-add-root-button").Click();
        cut.Find(".map-page__layer-create-input").Input("Cities");
        cut.Find(".map-page__layer-create-input").KeyDown("Enter");

        await _mapApi.Received(1).CreateLayerAsync(
            worldId,
            mapId,
            Arg.Is<string>(name => name == "Cities"),
            null);

        cut.WaitForAssertion(() =>
        {
            var rows = cut.FindAll(".map-page__layer-row");
            Assert.Equal(4, rows.Count);
            Assert.Contains("Cities", rows[3].TextContent, StringComparison.Ordinal);
        });

        Assert.Empty(cut.FindAll(".map-page__layer-row--inline-create"));
        Assert.Equal(customLayerId, GetField<Guid?>(cut.Instance, "SelectedLayerId"));
        Assert.Equal(string.Empty, GetField<string>(cut.Instance, "_createLayerNameInput"));
    }

    [Fact]
    public async Task InlineCreate_WhenEnterOnChildDraft_SendsParentAndPersistsChild()
    {
        var worldId = Guid.NewGuid();
        var mapId = Guid.NewGuid();
        var childLayerId = Guid.NewGuid();

        _mapApi.GetMapAsync(worldId, mapId).Returns((new MapDto { Name = "Child Layer Map" }, 200, null));
        _mapApi.GetBasemapReadUrlAsync(worldId, mapId).Returns((new GetBasemapReadUrlResponseDto { ReadUrl = "https://blob/read" }, 200, null));
        _mapApi.ListPinsForMapAsync(worldId, mapId).Returns(new List<MapPinResponseDto>());
        _mapApi.CreateLayerAsync(worldId, mapId, Arg.Any<string>(), Arg.Any<Guid?>())
            .Returns(new MapLayerDto
            {
                MapLayerId = childLayerId,
                Name = "Child",
                SortOrder = 0,
                IsEnabled = true,
                ParentLayerId = _worldLayerId,
            });

        var cut = RenderPage(worldId, mapId);
        cut.WaitForAssertion(() => Assert.Equal(3, cut.FindAll(".map-page__layer-row").Count));

        cut.Find($".map-page__layer-row[data-layer-id='{_worldLayerId}'] .map-page__layer-add-child-button").Click();
        cut.Find(".map-page__layer-create-input").Input("Child");
        cut.Find(".map-page__layer-create-input").KeyDown("Enter");

        await _mapApi.Received(1).CreateLayerAsync(worldId, mapId, "Child", _worldLayerId);

        cut.WaitForAssertion(() =>
        {
            var childRow = cut.FindAll(".map-page__layer-row")
                .Single(row => row.TextContent.Contains("Child", StringComparison.Ordinal));
            Assert.Equal("1", childRow.GetAttribute("data-layer-depth"));
        });

        Assert.False(GetField<bool>(cut.Instance, "_isInlineCreateActive"));
        Assert.Null(GetField<Guid?>(cut.Instance, "_createLayerParentLayerId"));
    }

    [Fact]
    public async Task InlineCreate_WhenEscape_CancelsDraftAndSkipsApi()
    {
        var cut = RenderLoadedPage();

        cut.Find(".map-page__layer-add-root-button").Click();
        cut.Find(".map-page__layer-create-input").Input("Cities");
        cut.Find(".map-page__layer-create-input").KeyDown("Escape");

        Assert.Empty(cut.FindAll(".map-page__layer-row--inline-create"));
        Assert.False(GetField<bool>(cut.Instance, "_isInlineCreateActive"));
        await _mapApi.DidNotReceive().CreateLayerAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<Guid?>());
    }

    [Fact]
    public void LayerInlineCreate_WhenActive_DisablesAdditionalAddAndRenameEntryPoints()
    {
        var worldId = Guid.NewGuid();
        var mapId = Guid.NewGuid();
        var customLayerId = Guid.NewGuid();

        _mapApi.GetMapAsync(worldId, mapId).Returns((new MapDto { Name = "Inline Guard Map" }, 200, null));
        _mapApi.GetBasemapReadUrlAsync(worldId, mapId).Returns((new GetBasemapReadUrlResponseDto { ReadUrl = "https://blob/read" }, 200, null));
        _mapApi.GetLayersForMapAsync(worldId, mapId).Returns(new List<MapLayerDto>
        {
            new() { MapLayerId = _worldLayerId, Name = "World", SortOrder = 0, IsEnabled = true },
            new() { MapLayerId = _campaignLayerId, Name = "Campaign", SortOrder = 1, IsEnabled = true },
            new() { MapLayerId = _arcLayerId, Name = "Arc", SortOrder = 2, IsEnabled = true },
            new() { MapLayerId = customLayerId, Name = "Cities", SortOrder = 3, IsEnabled = true },
        });
        _mapApi.ListPinsForMapAsync(worldId, mapId).Returns(new List<MapPinResponseDto>());

        var cut = RenderPage(worldId, mapId);
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".map-page__layer-name-button")));

        cut.Find(".map-page__layer-add-root-button").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.True(cut.Find(".map-page__layer-add-root-button").HasAttribute("disabled"));
            Assert.All(cut.FindAll(".map-page__layer-add-child-button"), button => Assert.True(button.HasAttribute("disabled")));
            Assert.True(cut.Find(".map-page__layer-name-button").HasAttribute("disabled"));
        });
    }

    [Fact]
    public void BeginInlineCreate_ClearsPendingDeleteAndNestTransientState()
    {
        var cut = RenderLoadedPage();

        SetField(cut.Instance, "_pendingDeleteLayerId", _campaignLayerId);
        SetField(cut.Instance, "_nestingLayerId", _campaignLayerId);
        SetField(cut.Instance, "_selectedNestParentLayerId", _worldLayerId);
        SetField(cut.Instance, "_isNestParentPickerOpen", true);

        cut.Find(".map-page__layer-add-root-button").Click();

        Assert.Null(GetField<Guid?>(cut.Instance, "_pendingDeleteLayerId"));
        Assert.Null(GetField<Guid?>(cut.Instance, "_nestingLayerId"));
        Assert.Null(GetField<Guid?>(cut.Instance, "_selectedNestParentLayerId"));
        Assert.False(GetField<bool>(cut.Instance, "_isNestParentPickerOpen"));
    }

    [Fact]
    public async Task SaveCreatedLayerAsync_WhenNameBlank_SetsValidationErrorAndSkipsApi()
    {
        var cut = RenderLoadedPage();
        SetField(cut.Instance, "_isInlineCreateActive", true);
        SetField(cut.Instance, "_createLayerNameInput", "   ");

        await InvokePrivateOnRendererAsync(cut, "SaveCreatedLayerAsync");

        Assert.Equal("Layer name is required.", GetField<string>(cut.Instance, "_addLayerError"));
        await _mapApi.DidNotReceive().CreateLayerAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<Guid?>());
    }

    [Fact]
    public async Task SaveCreatedLayerAsync_WhenNameTooLong_SetsValidationErrorAndSkipsApi()
    {
        var cut = RenderLoadedPage();
        SetField(cut.Instance, "_isInlineCreateActive", true);
        SetField(cut.Instance, "_createLayerNameInput", new string('L', 201));

        await InvokePrivateOnRendererAsync(cut, "SaveCreatedLayerAsync");

        Assert.Equal("Layer name must be 200 characters or fewer.", GetField<string>(cut.Instance, "_addLayerError"));
        await _mapApi.DidNotReceive().CreateLayerAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<Guid?>());
    }

    [Fact]
    public async Task SaveCreatedLayerAsync_WhenApiThrows_SetsErrorAndKeepsDraft()
    {
        var cut = RenderLoadedPage();
        SetField(cut.Instance, "_isInlineCreateActive", true);
        SetField(cut.Instance, "_createLayerNameInput", "Cities");
        _mapApi.CreateLayerAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<Guid?>())
            .Returns(Task.FromException<MapLayerDto>(new InvalidOperationException("boom")));

        await InvokePrivateOnRendererAsync(cut, "SaveCreatedLayerAsync");

        Assert.Equal("Failed to add layer: boom", GetField<string>(cut.Instance, "_addLayerError"));
        Assert.True(GetField<bool>(cut.Instance, "_isInlineCreateActive"));
    }

    [Fact]
    public async Task SaveCreatedLayerAsync_WhenAlreadyAdding_DoesNothing()
    {
        var cut = RenderLoadedPage();
        SetField(cut.Instance, "_isInlineCreateActive", true);
        SetField(cut.Instance, "_createLayerNameInput", "Cities");
        SetField(cut.Instance, "_isAddingLayer", true);

        await InvokePrivateOnRendererAsync(cut, "SaveCreatedLayerAsync");

        await _mapApi.DidNotReceive().CreateLayerAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<Guid?>());
    }

    [Fact]
    public async Task OnCreateLayerNameKeyDown_CoversEnterEscapeAndDefaultPaths()
    {
        var cut = RenderLoadedPage();
        SetField(cut.Instance, "_isInlineCreateActive", true);
        SetField(cut.Instance, "_createLayerNameInput", "Cities");
        _mapApi.CreateLayerAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<Guid?>())
            .Returns(new MapLayerDto
            {
                MapLayerId = Guid.NewGuid(),
                Name = "Cities",
                SortOrder = 3,
                IsEnabled = true
            });

        await InvokePrivateOnRendererAsync(cut, "OnCreateLayerNameKeyDown", new KeyboardEventArgs { Key = "Escape" });
        Assert.False(GetField<bool>(cut.Instance, "_isInlineCreateActive"));
        await _mapApi.DidNotReceive().CreateLayerAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<Guid?>());

        SetField(cut.Instance, "_isInlineCreateActive", true);
        SetField(cut.Instance, "_createLayerNameInput", "Cities");
        await InvokePrivateOnRendererAsync(cut, "OnCreateLayerNameKeyDown", new KeyboardEventArgs { Key = "Enter" });
        await _mapApi.Received(1).CreateLayerAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<Guid?>());

        SetField(cut.Instance, "_isInlineCreateActive", true);
        SetField(cut.Instance, "_createLayerNameInput", "Cities");
        await InvokePrivateOnRendererAsync(cut, "OnCreateLayerNameKeyDown", new KeyboardEventArgs { Key = "Tab" });
    }

    [Fact]
    public async Task OnCreateLayerNameInput_WhenValueNull_UsesEmptyStringAndClearsError()
    {
        var cut = RenderLoadedPage();
        SetField(cut.Instance, "_createLayerNameInput", "Existing");
        SetField(cut.Instance, "_addLayerError", "old error");

        await InvokePrivateOnRendererAsync(cut, "OnCreateLayerNameInput", new ChangeEventArgs { Value = null });

        Assert.Equal(string.Empty, GetField<string>(cut.Instance, "_createLayerNameInput"));
        Assert.Equal(string.Empty, GetField<string>(cut.Instance, "_addLayerError"));
    }

    [Fact]
    public void LayerRow_CustomLayer_ShowsInlineRenameEntryAndNoLegacyRenameButtons()
    {
        var worldId = Guid.NewGuid();
        var mapId = Guid.NewGuid();
        var customLayerId = Guid.NewGuid();

        _mapApi.GetMapAsync(worldId, mapId).Returns((new MapDto { Name = "Custom Controls Map" }, 200, null));
        _mapApi.GetBasemapReadUrlAsync(worldId, mapId).Returns((new GetBasemapReadUrlResponseDto { ReadUrl = "https://blob/read" }, 200, null));
        _mapApi.GetLayersForMapAsync(worldId, mapId).Returns(new List<MapLayerDto>
        {
            new() { MapLayerId = _worldLayerId, Name = "World", SortOrder = 0, IsEnabled = true },
            new() { MapLayerId = _campaignLayerId, Name = "Campaign", SortOrder = 1, IsEnabled = true },
            new() { MapLayerId = _arcLayerId, Name = "Arc", SortOrder = 2, IsEnabled = true },
            new() { MapLayerId = customLayerId, Name = "Cities", SortOrder = 3, IsEnabled = true },
        });
        _mapApi.ListPinsForMapAsync(worldId, mapId).Returns(new List<MapPinResponseDto>());

        var cut = RenderPage(worldId, mapId);
        cut.WaitForAssertion(() =>
        {
            Assert.Empty(cut.FindAll(".map-page__layer-rename-button"));
            Assert.Single(cut.FindAll(".map-page__layer-name-button"));
            Assert.Single(cut.FindAll(".map-page__layer-delete-button"));
        });
    }

    [Fact]
    public async Task LayerRename_UpdatesLayerNameInUi()
    {
        var worldId = Guid.NewGuid();
        var mapId = Guid.NewGuid();
        var customLayerId = Guid.NewGuid();

        _mapApi.GetMapAsync(worldId, mapId).Returns((new MapDto { Name = "Rename Layer Map" }, 200, null));
        _mapApi.GetBasemapReadUrlAsync(worldId, mapId).Returns((new GetBasemapReadUrlResponseDto { ReadUrl = "https://blob/read" }, 200, null));
        _mapApi.GetLayersForMapAsync(worldId, mapId).Returns(new List<MapLayerDto>
        {
            new() { MapLayerId = _worldLayerId, Name = "World", SortOrder = 0, IsEnabled = true },
            new() { MapLayerId = _campaignLayerId, Name = "Campaign", SortOrder = 1, IsEnabled = true },
            new() { MapLayerId = _arcLayerId, Name = "Arc", SortOrder = 2, IsEnabled = true },
            new() { MapLayerId = customLayerId, Name = "Cities", SortOrder = 3, IsEnabled = true },
        });
        _mapApi.ListPinsForMapAsync(worldId, mapId).Returns(new List<MapPinResponseDto>());

        var cut = RenderPage(worldId, mapId);
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".map-page__layer-name-button")));

        cut.Find(".map-page__layer-name-button").Click();
        cut.Find(".map-page__layer-rename-input").Input("Settlements");
        cut.FindAll(".map-page__layer-action-button")
            .First(button => button.TextContent.Contains("Save", StringComparison.Ordinal))
            .Click();

        await _mapApi.Received(1).RenameLayerAsync(worldId, mapId, customLayerId, "Settlements");

        cut.WaitForAssertion(() => Assert.Contains("Settlements", cut.Markup, StringComparison.Ordinal));
    }

    [Fact]
    public void LayerRename_BeginEdit_OnlyTargetLayerEntersEditMode()
    {
        var worldId = Guid.NewGuid();
        var mapId = Guid.NewGuid();
        var customLayerAId = Guid.NewGuid();
        var customLayerBId = Guid.NewGuid();

        _mapApi.GetMapAsync(worldId, mapId).Returns((new MapDto { Name = "Inline Edit Map" }, 200, null));
        _mapApi.GetBasemapReadUrlAsync(worldId, mapId).Returns((new GetBasemapReadUrlResponseDto { ReadUrl = "https://blob/read" }, 200, null));
        _mapApi.GetLayersForMapAsync(worldId, mapId).Returns(new List<MapLayerDto>
        {
            new() { MapLayerId = _worldLayerId, Name = "World", SortOrder = 0, IsEnabled = true },
            new() { MapLayerId = _campaignLayerId, Name = "Campaign", SortOrder = 1, IsEnabled = true },
            new() { MapLayerId = _arcLayerId, Name = "Arc", SortOrder = 2, IsEnabled = true },
            new() { MapLayerId = customLayerAId, Name = "Cities", SortOrder = 3, IsEnabled = true },
            new() { MapLayerId = customLayerBId, Name = "Terrain", SortOrder = 4, IsEnabled = true },
        });
        _mapApi.ListPinsForMapAsync(worldId, mapId).Returns(new List<MapPinResponseDto>());

        var cut = RenderPage(worldId, mapId);
        cut.WaitForAssertion(() => Assert.Equal(5, cut.FindAll(".map-page__layer-row").Count));

        var rowA = cut.Find($".map-page__layer-row[data-layer-id='{customLayerAId}']");
        var rowB = cut.Find($".map-page__layer-row[data-layer-id='{customLayerBId}']");

        rowA.Find(".map-page__layer-name-button").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Single(cut.FindAll(".map-page__layer-rename-input"));
            Assert.Single(rowA.QuerySelectorAll(".map-page__layer-rename-input"));
            Assert.Empty(rowB.QuerySelectorAll(".map-page__layer-rename-input"));
        });
    }

    [Fact]
    public void LayerRename_LegacyRenameActionButtons_AreRemovedForAllRows()
    {
        var worldId = Guid.NewGuid();
        var mapId = Guid.NewGuid();

        _mapApi.GetMapAsync(worldId, mapId).Returns((new MapDto { Name = "No Legacy Rename Buttons Map" }, 200, null));
        _mapApi.GetBasemapReadUrlAsync(worldId, mapId).Returns((new GetBasemapReadUrlResponseDto { ReadUrl = "https://blob/read" }, 200, null));
        _mapApi.GetLayersForMapAsync(worldId, mapId).Returns(new List<MapLayerDto>
        {
            new() { MapLayerId = _worldLayerId, Name = "World", SortOrder = 0, IsEnabled = true },
            new() { MapLayerId = _campaignLayerId, Name = "Campaign", SortOrder = 1, IsEnabled = true },
            new() { MapLayerId = _arcLayerId, Name = "Arc", SortOrder = 2, IsEnabled = true },
            new() { MapLayerId = Guid.NewGuid(), Name = "Cities", SortOrder = 3, IsEnabled = true },
            new() { MapLayerId = Guid.NewGuid(), Name = "Terrain", SortOrder = 4, IsEnabled = true },
        });
        _mapApi.ListPinsForMapAsync(worldId, mapId).Returns(new List<MapPinResponseDto>());

        var cut = RenderPage(worldId, mapId);

        cut.WaitForAssertion(() =>
        {
            Assert.Equal(5, cut.FindAll(".map-page__layer-row").Count);
            Assert.Empty(cut.FindAll(".map-page__layer-rename-button"));
        });
    }

    [Fact]
    public void LayerRename_WhenEditingLayer_ClickingAnotherLayerNameDoesNothing()
    {
        var worldId = Guid.NewGuid();
        var mapId = Guid.NewGuid();
        var customLayerAId = Guid.NewGuid();
        var customLayerBId = Guid.NewGuid();

        _mapApi.GetMapAsync(worldId, mapId).Returns((new MapDto { Name = "Single Editor Map" }, 200, null));
        _mapApi.GetBasemapReadUrlAsync(worldId, mapId).Returns((new GetBasemapReadUrlResponseDto { ReadUrl = "https://blob/read" }, 200, null));
        _mapApi.GetLayersForMapAsync(worldId, mapId).Returns(new List<MapLayerDto>
        {
            new() { MapLayerId = _worldLayerId, Name = "World", SortOrder = 0, IsEnabled = true },
            new() { MapLayerId = _campaignLayerId, Name = "Campaign", SortOrder = 1, IsEnabled = true },
            new() { MapLayerId = _arcLayerId, Name = "Arc", SortOrder = 2, IsEnabled = true },
            new() { MapLayerId = customLayerAId, Name = "Cities", SortOrder = 3, IsEnabled = true },
            new() { MapLayerId = customLayerBId, Name = "Terrain", SortOrder = 4, IsEnabled = true },
        });
        _mapApi.ListPinsForMapAsync(worldId, mapId).Returns(new List<MapPinResponseDto>());

        var cut = RenderPage(worldId, mapId);
        cut.WaitForAssertion(() => Assert.Equal(5, cut.FindAll(".map-page__layer-row").Count));

        var rowA = cut.Find($".map-page__layer-row[data-layer-id='{customLayerAId}']");
        var rowB = cut.Find($".map-page__layer-row[data-layer-id='{customLayerBId}']");
        rowA.Find(".map-page__layer-name-button").Click();

        cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".map-page__layer-rename-input")));
        Assert.True(rowB.Find(".map-page__layer-name-button").HasAttribute("disabled"));

        cut.WaitForAssertion(() =>
        {
            Assert.Single(rowA.QuerySelectorAll(".map-page__layer-rename-input"));
            Assert.Empty(rowB.QuerySelectorAll(".map-page__layer-rename-input"));
            Assert.Equal(customLayerAId, GetField<Guid?>(cut.Instance, "_editingLayerId"));
        });
    }

    [Fact]
    public void LayerRename_NestedLayer_ClickTargetsOnlyNestedLayer()
    {
        var worldId = Guid.NewGuid();
        var mapId = Guid.NewGuid();
        var parentLayerId = Guid.NewGuid();
        var childLayerId = Guid.NewGuid();

        _mapApi.GetMapAsync(worldId, mapId).Returns((new MapDto { Name = "Nested Inline Rename Map" }, 200, null));
        _mapApi.GetBasemapReadUrlAsync(worldId, mapId).Returns((new GetBasemapReadUrlResponseDto { ReadUrl = "https://blob/read" }, 200, null));
        _mapApi.GetLayersForMapAsync(worldId, mapId).Returns(new List<MapLayerDto>
        {
            new() { MapLayerId = _worldLayerId, Name = "World", SortOrder = 0, IsEnabled = true },
            new() { MapLayerId = _campaignLayerId, Name = "Campaign", SortOrder = 1, IsEnabled = true },
            new() { MapLayerId = _arcLayerId, Name = "Arc", SortOrder = 2, IsEnabled = true },
            new() { MapLayerId = parentLayerId, Name = "Parent", SortOrder = 3, IsEnabled = true },
            new() { MapLayerId = childLayerId, Name = "Child", SortOrder = 4, IsEnabled = true, ParentLayerId = parentLayerId },
        });
        _mapApi.ListPinsForMapAsync(worldId, mapId).Returns(new List<MapPinResponseDto>());

        var cut = RenderPage(worldId, mapId);
        cut.WaitForAssertion(() => Assert.Equal(5, cut.FindAll(".map-page__layer-row").Count));

        var parentRow = cut.Find($".map-page__layer-row[data-layer-id='{parentLayerId}']");
        var childRow = cut.Find($".map-page__layer-row[data-layer-id='{childLayerId}']");

        childRow.Find(".map-page__layer-name-button").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Single(cut.FindAll(".map-page__layer-rename-input"));
            Assert.Empty(parentRow.QuerySelectorAll(".map-page__layer-rename-input"));
            Assert.Single(childRow.QuerySelectorAll(".map-page__layer-rename-input"));
        });
    }

    [Fact]
    public async Task LayerDelete_RemovesCustomLayerRow()
    {
        var worldId = Guid.NewGuid();
        var mapId = Guid.NewGuid();
        var customLayerId = Guid.NewGuid();

        _mapApi.GetMapAsync(worldId, mapId).Returns((new MapDto { Name = "Delete Layer Map" }, 200, null));
        _mapApi.GetBasemapReadUrlAsync(worldId, mapId).Returns((new GetBasemapReadUrlResponseDto { ReadUrl = "https://blob/read" }, 200, null));
        _mapApi.GetLayersForMapAsync(worldId, mapId).Returns(new List<MapLayerDto>
        {
            new() { MapLayerId = _worldLayerId, Name = "World", SortOrder = 0, IsEnabled = true },
            new() { MapLayerId = _campaignLayerId, Name = "Campaign", SortOrder = 1, IsEnabled = true },
            new() { MapLayerId = _arcLayerId, Name = "Arc", SortOrder = 2, IsEnabled = true },
            new() { MapLayerId = customLayerId, Name = "Cities", SortOrder = 3, IsEnabled = true },
        });
        _mapApi.ListPinsForMapAsync(worldId, mapId).Returns(new List<MapPinResponseDto>());

        var cut = RenderPage(worldId, mapId);
        cut.WaitForAssertion(() => Assert.Equal(4, cut.FindAll(".map-page__layer-row").Count));

        cut.Find(".map-page__layer-delete-button").Click();
        cut.FindAll(".map-page__layer-action-button")
            .First(button => button.TextContent.Contains("Confirm", StringComparison.Ordinal))
            .Click();

        await _mapApi.Received(1).DeleteLayerAsync(worldId, mapId, customLayerId);

        cut.WaitForAssertion(() =>
        {
            Assert.Equal(3, cut.FindAll(".map-page__layer-row").Count);
            Assert.DoesNotContain("Cities", cut.Markup, StringComparison.Ordinal);
        });
    }

    [Fact]
    public void LayerDelete_PreservesSelection_WhenDeletedLayerIsNotSelected()
    {
        var worldId = Guid.NewGuid();
        var mapId = Guid.NewGuid();
        var customLayerId = Guid.NewGuid();

        _mapApi.GetMapAsync(worldId, mapId).Returns((new MapDto { Name = "Selection Preserve Map" }, 200, null));
        _mapApi.GetBasemapReadUrlAsync(worldId, mapId).Returns((new GetBasemapReadUrlResponseDto { ReadUrl = "https://blob/read" }, 200, null));
        _mapApi.GetLayersForMapAsync(worldId, mapId).Returns(new List<MapLayerDto>
        {
            new() { MapLayerId = _worldLayerId, Name = "World", SortOrder = 0, IsEnabled = true },
            new() { MapLayerId = _campaignLayerId, Name = "Campaign", SortOrder = 1, IsEnabled = true },
            new() { MapLayerId = _arcLayerId, Name = "Arc", SortOrder = 2, IsEnabled = true },
            new() { MapLayerId = customLayerId, Name = "Cities", SortOrder = 3, IsEnabled = true },
        });
        _mapApi.ListPinsForMapAsync(worldId, mapId).Returns(new List<MapPinResponseDto>());

        var cut = RenderPage(worldId, mapId);
        cut.WaitForAssertion(() => Assert.Equal(_arcLayerId, GetField<Guid?>(cut.Instance, "SelectedLayerId")));

        cut.Find(".map-page__layer-delete-button").Click();
        cut.FindAll(".map-page__layer-action-button")
            .First(button => button.TextContent.Contains("Confirm", StringComparison.Ordinal))
            .Click();

        Assert.Equal(_arcLayerId, GetField<Guid?>(cut.Instance, "SelectedLayerId"));
    }

    [Fact]
    public void LayerDelete_WhenSelectedLayerDeleted_SelectsFirstAvailableLayer()
    {
        var worldId = Guid.NewGuid();
        var mapId = Guid.NewGuid();
        var customLayerId = Guid.NewGuid();

        _mapApi.GetMapAsync(worldId, mapId).Returns((new MapDto { Name = "Selection Fallback Map" }, 200, null));
        _mapApi.GetBasemapReadUrlAsync(worldId, mapId).Returns((new GetBasemapReadUrlResponseDto { ReadUrl = "https://blob/read" }, 200, null));
        _mapApi.GetLayersForMapAsync(worldId, mapId).Returns(new List<MapLayerDto>
        {
            new() { MapLayerId = _worldLayerId, Name = "World", SortOrder = 0, IsEnabled = true },
            new() { MapLayerId = _campaignLayerId, Name = "Campaign", SortOrder = 1, IsEnabled = true },
            new() { MapLayerId = _arcLayerId, Name = "Arc", SortOrder = 2, IsEnabled = true },
            new() { MapLayerId = customLayerId, Name = "Cities", SortOrder = 3, IsEnabled = true },
        });
        _mapApi.ListPinsForMapAsync(worldId, mapId).Returns(new List<MapPinResponseDto>());

        var cut = RenderPage(worldId, mapId);
        cut.WaitForAssertion(() => Assert.Equal(4, cut.FindAll(".map-page__layer-row").Count));

        cut.FindAll(".map-page__layer-row")[3].Click();
        Assert.Equal(customLayerId, GetField<Guid?>(cut.Instance, "SelectedLayerId"));

        cut.Find(".map-page__layer-delete-button").Click();
        cut.FindAll(".map-page__layer-action-button")
            .First(button => button.TextContent.Contains("Confirm", StringComparison.Ordinal))
            .Click();

        Assert.Equal(_worldLayerId, GetField<Guid?>(cut.Instance, "SelectedLayerId"));
    }

    [Fact]
    public async Task LayerNest_Confirm_CallsApiAndRendersChildUnderParent()
    {
        var worldId = Guid.NewGuid();
        var mapId = Guid.NewGuid();
        var parentLayerId = Guid.NewGuid();
        var childLayerId = Guid.NewGuid();

        _mapApi.GetMapAsync(worldId, mapId).Returns((new MapDto { Name = "Nest Map" }, 200, null));
        _mapApi.GetBasemapReadUrlAsync(worldId, mapId).Returns((new GetBasemapReadUrlResponseDto { ReadUrl = "https://blob/read" }, 200, null));
        _mapApi.GetLayersForMapAsync(worldId, mapId).Returns(new List<MapLayerDto>
        {
            new() { MapLayerId = _worldLayerId, Name = "World", SortOrder = 0, IsEnabled = true },
            new() { MapLayerId = _campaignLayerId, Name = "Campaign", SortOrder = 1, IsEnabled = true },
            new() { MapLayerId = _arcLayerId, Name = "Arc", SortOrder = 2, IsEnabled = true },
            new() { MapLayerId = parentLayerId, Name = "Parent", SortOrder = 3, IsEnabled = true },
            new() { MapLayerId = childLayerId, Name = "Child", SortOrder = 4, IsEnabled = true },
        });
        _mapApi.ListPinsForMapAsync(worldId, mapId).Returns(new List<MapPinResponseDto>());

        var cut = RenderPage(worldId, mapId);
        cut.WaitForAssertion(() => Assert.Equal(5, cut.FindAll(".map-page__layer-row").Count));

        var childRow = cut.Find($".map-page__layer-row[data-layer-id='{childLayerId}']");
        childRow.Find(".map-page__layer-nest-button").Click();
        cut.Find(".map-page__layer-parent-select").Change(parentLayerId.ToString());
        cut.Find(".map-page__layer-parent-picker .map-page__layer-action-button:not(.map-page__layer-action-button--cancel)").Click();

        await _mapApi.Received(1).SetLayerParentAsync(worldId, mapId, childLayerId, parentLayerId);

        cut.WaitForAssertion(() =>
        {
            var updatedChildRow = cut.Find($".map-page__layer-row[data-layer-id='{childLayerId}']");
            Assert.Equal("1", updatedChildRow.GetAttribute("data-layer-depth"));
        });
    }

    [Fact]
    public async Task LayerMoveToRoot_CallsApiWithNull_AndRemovesDepth()
    {
        var worldId = Guid.NewGuid();
        var mapId = Guid.NewGuid();
        var parentLayerId = Guid.NewGuid();
        var childLayerId = Guid.NewGuid();

        _mapApi.GetMapAsync(worldId, mapId).Returns((new MapDto { Name = "Root Move Map" }, 200, null));
        _mapApi.GetBasemapReadUrlAsync(worldId, mapId).Returns((new GetBasemapReadUrlResponseDto { ReadUrl = "https://blob/read" }, 200, null));
        _mapApi.GetLayersForMapAsync(worldId, mapId).Returns(new List<MapLayerDto>
        {
            new() { MapLayerId = _worldLayerId, Name = "World", SortOrder = 0, IsEnabled = true },
            new() { MapLayerId = _campaignLayerId, Name = "Campaign", SortOrder = 1, IsEnabled = true },
            new() { MapLayerId = _arcLayerId, Name = "Arc", SortOrder = 2, IsEnabled = true },
            new() { MapLayerId = parentLayerId, Name = "Parent", SortOrder = 3, IsEnabled = true },
            new() { MapLayerId = childLayerId, Name = "Child", SortOrder = 4, IsEnabled = true, ParentLayerId = parentLayerId },
        });
        _mapApi.ListPinsForMapAsync(worldId, mapId).Returns(new List<MapPinResponseDto>());

        var cut = RenderPage(worldId, mapId);
        cut.WaitForAssertion(() =>
        {
            var initialChildRow = cut.Find($".map-page__layer-row[data-layer-id='{childLayerId}']");
            Assert.Equal("1", initialChildRow.GetAttribute("data-layer-depth"));
        });

        var childRow = cut.Find($".map-page__layer-row[data-layer-id='{childLayerId}']");
        childRow.Find(".map-page__layer-root-button").Click();

        await _mapApi.Received(1).SetLayerParentAsync(worldId, mapId, childLayerId, null);

        cut.WaitForAssertion(() =>
        {
            var updatedChildRow = cut.Find($".map-page__layer-row[data-layer-id='{childLayerId}']");
            Assert.Equal("0", updatedChildRow.GetAttribute("data-layer-depth"));
        });
    }

    [Fact]
    public async Task LayerMoveToRoot_WhenAlreadyRoot_DoesNotCallApi()
    {
        var cut = RenderLoadedPage();

        await InvokePrivateOnRendererAsync(cut, "MoveLayerToRootAsync", _arcLayerId);

        await _mapApi.DidNotReceive().SetLayerParentAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid?>());
    }

    [Fact]
    public async Task LayerNest_Cancel_DoesNotCallApi()
    {
        var worldId = Guid.NewGuid();
        var mapId = Guid.NewGuid();
        var parentLayerId = Guid.NewGuid();
        var childLayerId = Guid.NewGuid();

        _mapApi.GetMapAsync(worldId, mapId).Returns((new MapDto { Name = "Cancel Nest Map" }, 200, null));
        _mapApi.GetBasemapReadUrlAsync(worldId, mapId).Returns((new GetBasemapReadUrlResponseDto { ReadUrl = "https://blob/read" }, 200, null));
        _mapApi.GetLayersForMapAsync(worldId, mapId).Returns(new List<MapLayerDto>
        {
            new() { MapLayerId = _worldLayerId, Name = "World", SortOrder = 0, IsEnabled = true },
            new() { MapLayerId = _campaignLayerId, Name = "Campaign", SortOrder = 1, IsEnabled = true },
            new() { MapLayerId = _arcLayerId, Name = "Arc", SortOrder = 2, IsEnabled = true },
            new() { MapLayerId = parentLayerId, Name = "Parent", SortOrder = 3, IsEnabled = true },
            new() { MapLayerId = childLayerId, Name = "Child", SortOrder = 4, IsEnabled = true },
        });
        _mapApi.ListPinsForMapAsync(worldId, mapId).Returns(new List<MapPinResponseDto>());

        var cut = RenderPage(worldId, mapId);
        cut.WaitForAssertion(() => Assert.NotNull(cut.Find($".map-page__layer-row[data-layer-id='{childLayerId}']")));

        cut.Find($".map-page__layer-row[data-layer-id='{childLayerId}'] .map-page__layer-nest-button").Click();
        cut.Find(".map-page__layer-parent-picker .map-page__layer-action-button--cancel").Click();

        await _mapApi.DidNotReceive().SetLayerParentAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid?>());
    }

    [Fact]
    public async Task LayerNest_WhenApiFails_PreservesParentSelectionAndDepth()
    {
        var worldId = Guid.NewGuid();
        var mapId = Guid.NewGuid();
        var parentLayerId = Guid.NewGuid();
        var childLayerId = Guid.NewGuid();

        _mapApi.GetMapAsync(worldId, mapId).Returns((new MapDto { Name = "Nest Failure Map" }, 200, null));
        _mapApi.GetBasemapReadUrlAsync(worldId, mapId).Returns((new GetBasemapReadUrlResponseDto { ReadUrl = "https://blob/read" }, 200, null));
        _mapApi.GetLayersForMapAsync(worldId, mapId).Returns(new List<MapLayerDto>
        {
            new() { MapLayerId = _worldLayerId, Name = "World", SortOrder = 0, IsEnabled = true },
            new() { MapLayerId = _campaignLayerId, Name = "Campaign", SortOrder = 1, IsEnabled = true },
            new() { MapLayerId = _arcLayerId, Name = "Arc", SortOrder = 2, IsEnabled = true },
            new() { MapLayerId = parentLayerId, Name = "Parent", SortOrder = 3, IsEnabled = true },
            new() { MapLayerId = childLayerId, Name = "Child", SortOrder = 4, IsEnabled = true },
        });
        _mapApi.ListPinsForMapAsync(worldId, mapId).Returns(new List<MapPinResponseDto>());
        _mapApi.SetLayerParentAsync(worldId, mapId, childLayerId, parentLayerId)
            .Returns(Task.FromException(new InvalidOperationException("parent failed")));

        var cut = RenderPage(worldId, mapId);
        cut.WaitForAssertion(() => Assert.Equal(_arcLayerId, GetField<Guid?>(cut.Instance, "SelectedLayerId")));

        cut.Find($".map-page__layer-row[data-layer-id='{childLayerId}']").Click();
        Assert.Equal(childLayerId, GetField<Guid?>(cut.Instance, "SelectedLayerId"));

        cut.Find($".map-page__layer-row[data-layer-id='{childLayerId}'] .map-page__layer-nest-button").Click();
        cut.Find(".map-page__layer-parent-select").Change(parentLayerId.ToString());
        cut.Find(".map-page__layer-parent-picker .map-page__layer-action-button:not(.map-page__layer-action-button--cancel)").Click();

        Assert.Equal(childLayerId, GetField<Guid?>(cut.Instance, "SelectedLayerId"));
        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Failed to move layer: parent failed", cut.Markup, StringComparison.Ordinal);
            var childRow = cut.Find($".map-page__layer-row[data-layer-id='{childLayerId}']");
            Assert.Equal("0", childRow.GetAttribute("data-layer-depth"));
        });
    }

    [Fact]
    public void LayerNest_ParentCandidates_ExcludeSelfAndDescendants_IncludeValidNonDescendants()
    {
        var worldId = Guid.NewGuid();
        var mapId = Guid.NewGuid();
        var layerA = Guid.NewGuid();
        var layerB = Guid.NewGuid();
        var layerC = Guid.NewGuid();
        var layerD = Guid.NewGuid();

        _mapApi.GetMapAsync(worldId, mapId).Returns((new MapDto { Name = "Candidate Map" }, 200, null));
        _mapApi.GetBasemapReadUrlAsync(worldId, mapId).Returns((new GetBasemapReadUrlResponseDto { ReadUrl = "https://blob/read" }, 200, null));
        _mapApi.GetLayersForMapAsync(worldId, mapId).Returns(new List<MapLayerDto>
        {
            new() { MapLayerId = _worldLayerId, Name = "World", SortOrder = 0, IsEnabled = true },
            new() { MapLayerId = _campaignLayerId, Name = "Campaign", SortOrder = 1, IsEnabled = true },
            new() { MapLayerId = _arcLayerId, Name = "Arc", SortOrder = 2, IsEnabled = true },
            new() { MapLayerId = layerA, Name = "A", SortOrder = 3, IsEnabled = true },
            new() { MapLayerId = layerB, Name = "B", SortOrder = 4, IsEnabled = true, ParentLayerId = layerA },
            new() { MapLayerId = layerC, Name = "C", SortOrder = 5, IsEnabled = true, ParentLayerId = layerB },
            new() { MapLayerId = layerD, Name = "D", SortOrder = 6, IsEnabled = true },
        });
        _mapApi.ListPinsForMapAsync(worldId, mapId).Returns(new List<MapPinResponseDto>());

        var cut = RenderPage(worldId, mapId);
        cut.WaitForAssertion(() => Assert.NotNull(cut.Find($".map-page__layer-row[data-layer-id='{layerA}']")));

        cut.Find($".map-page__layer-row[data-layer-id='{layerA}'] .map-page__layer-nest-button").Click();

        var options = cut.FindAll(".map-page__layer-parent-select option")
            .Select(option => option.GetAttribute("value"))
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToList();

        Assert.DoesNotContain(layerA.ToString(), options);
        Assert.DoesNotContain(layerB.ToString(), options);
        Assert.DoesNotContain(layerC.ToString(), options);
        Assert.Contains(layerD.ToString(), options);
    }

    [Fact]
    public async Task LayerParentChange_PreservesSelection_WhenMovingNonSelectedLayer()
    {
        var worldId = Guid.NewGuid();
        var mapId = Guid.NewGuid();
        var parentLayerId = Guid.NewGuid();
        var movedLayerId = Guid.NewGuid();

        _mapApi.GetMapAsync(worldId, mapId).Returns((new MapDto { Name = "Selection NonSelected Map" }, 200, null));
        _mapApi.GetBasemapReadUrlAsync(worldId, mapId).Returns((new GetBasemapReadUrlResponseDto { ReadUrl = "https://blob/read" }, 200, null));
        _mapApi.GetLayersForMapAsync(worldId, mapId).Returns(new List<MapLayerDto>
        {
            new() { MapLayerId = _worldLayerId, Name = "World", SortOrder = 0, IsEnabled = true },
            new() { MapLayerId = _campaignLayerId, Name = "Campaign", SortOrder = 1, IsEnabled = true },
            new() { MapLayerId = _arcLayerId, Name = "Arc", SortOrder = 2, IsEnabled = true },
            new() { MapLayerId = parentLayerId, Name = "Parent", SortOrder = 3, IsEnabled = true },
            new() { MapLayerId = movedLayerId, Name = "Moved", SortOrder = 4, IsEnabled = true },
        });
        _mapApi.ListPinsForMapAsync(worldId, mapId).Returns(new List<MapPinResponseDto>());

        var cut = RenderPage(worldId, mapId);
        cut.WaitForAssertion(() => Assert.Equal(_arcLayerId, GetField<Guid?>(cut.Instance, "SelectedLayerId")));

        cut.Find($".map-page__layer-row[data-layer-id='{movedLayerId}'] .map-page__layer-nest-button").Click();
        cut.Find(".map-page__layer-parent-select").Change(parentLayerId.ToString());
        cut.Find(".map-page__layer-parent-picker .map-page__layer-action-button:not(.map-page__layer-action-button--cancel)").Click();

        Assert.Equal(_arcLayerId, GetField<Guid?>(cut.Instance, "SelectedLayerId"));
    }

    [Fact]
    public async Task LayerParentChange_PreservesSelection_WhenMovingSelectedLayer()
    {
        var worldId = Guid.NewGuid();
        var mapId = Guid.NewGuid();
        var parentLayerId = Guid.NewGuid();
        var movedLayerId = Guid.NewGuid();

        _mapApi.GetMapAsync(worldId, mapId).Returns((new MapDto { Name = "Selection Selected Map" }, 200, null));
        _mapApi.GetBasemapReadUrlAsync(worldId, mapId).Returns((new GetBasemapReadUrlResponseDto { ReadUrl = "https://blob/read" }, 200, null));
        _mapApi.GetLayersForMapAsync(worldId, mapId).Returns(new List<MapLayerDto>
        {
            new() { MapLayerId = _worldLayerId, Name = "World", SortOrder = 0, IsEnabled = true },
            new() { MapLayerId = _campaignLayerId, Name = "Campaign", SortOrder = 1, IsEnabled = true },
            new() { MapLayerId = _arcLayerId, Name = "Arc", SortOrder = 2, IsEnabled = true },
            new() { MapLayerId = parentLayerId, Name = "Parent", SortOrder = 3, IsEnabled = true },
            new() { MapLayerId = movedLayerId, Name = "Moved", SortOrder = 4, IsEnabled = true },
        });
        _mapApi.ListPinsForMapAsync(worldId, mapId).Returns(new List<MapPinResponseDto>());

        var cut = RenderPage(worldId, mapId);
        cut.WaitForAssertion(() => Assert.NotNull(cut.Find($".map-page__layer-row[data-layer-id='{movedLayerId}']")));

        cut.Find($".map-page__layer-row[data-layer-id='{movedLayerId}']").Click();
        Assert.Equal(movedLayerId, GetField<Guid?>(cut.Instance, "SelectedLayerId"));

        cut.Find($".map-page__layer-row[data-layer-id='{movedLayerId}'] .map-page__layer-nest-button").Click();
        cut.Find(".map-page__layer-parent-select").Change(parentLayerId.ToString());
        cut.Find(".map-page__layer-parent-picker .map-page__layer-action-button:not(.map-page__layer-action-button--cancel)").Click();

        Assert.Equal(movedLayerId, GetField<Guid?>(cut.Instance, "SelectedLayerId"));
    }

    [Fact]
    public async Task LayerParentChange_DeterministicOrder_UsesSortOrderWithinSiblingGroups()
    {
        var worldId = Guid.NewGuid();
        var mapId = Guid.NewGuid();
        var layerA = Guid.NewGuid();
        var layerB = Guid.NewGuid();
        var layerC = Guid.NewGuid();
        var layerChildLowSort = Guid.NewGuid();
        var layerChildHighSort = Guid.NewGuid();

        _mapApi.GetMapAsync(worldId, mapId).Returns((new MapDto { Name = "Order Map" }, 200, null));
        _mapApi.GetBasemapReadUrlAsync(worldId, mapId).Returns((new GetBasemapReadUrlResponseDto { ReadUrl = "https://blob/read" }, 200, null));
        _mapApi.GetLayersForMapAsync(worldId, mapId).Returns(new List<MapLayerDto>
        {
            new() { MapLayerId = _worldLayerId, Name = "World", SortOrder = 0, IsEnabled = true },
            new() { MapLayerId = _campaignLayerId, Name = "Campaign", SortOrder = 1, IsEnabled = true },
            new() { MapLayerId = _arcLayerId, Name = "Arc", SortOrder = 2, IsEnabled = true },
            new() { MapLayerId = layerA, Name = "A", SortOrder = 6, IsEnabled = true },
            new() { MapLayerId = layerB, Name = "B", SortOrder = 5, IsEnabled = true },
            new() { MapLayerId = layerC, Name = "C", SortOrder = 7, IsEnabled = true },
            new() { MapLayerId = layerChildLowSort, Name = "ChildLow", SortOrder = 1, IsEnabled = true, ParentLayerId = layerA },
            new() { MapLayerId = layerChildHighSort, Name = "ChildHigh", SortOrder = 2, IsEnabled = true, ParentLayerId = layerA },
        });
        _mapApi.ListPinsForMapAsync(worldId, mapId).Returns(new List<MapPinResponseDto>());

        var cut = RenderPage(worldId, mapId);

        cut.WaitForAssertion(() =>
        {
            var rootRows = cut.FindAll(".map-page__layer-row")
                .Where(row => row.GetAttribute("data-layer-depth") == "0")
                .Select(row => row.GetAttribute("data-layer-id"))
                .ToList();

            Assert.True(rootRows.IndexOf(layerB.ToString()) < rootRows.IndexOf(layerA.ToString()));

            var childRows = cut.FindAll(".map-page__layer-row")
                .Where(row => row.GetAttribute("data-layer-depth") == "1")
                .Select(row => row.GetAttribute("data-layer-id"))
                .ToList();

            Assert.True(childRows.IndexOf(layerChildLowSort.ToString()) < childRows.IndexOf(layerChildHighSort.ToString()));
        });
    }

    [Fact]
    public async Task LayerParentChange_WhenParentUnchanged_DoesNotCallApi()
    {
        var cut = RenderLoadedPage();
        var customLayerId = Guid.NewGuid();
        var existingParentLayerId = Guid.NewGuid();
        var layers = GetField<List<MapLayerDto>>(cut.Instance, "_layers");
        layers.Add(new MapLayerDto
        {
            MapLayerId = existingParentLayerId,
            Name = "Existing Parent",
            SortOrder = 3,
            IsEnabled = true,
        });
        layers.Add(new MapLayerDto
        {
            MapLayerId = customLayerId,
            Name = "Custom Child",
            SortOrder = 4,
            IsEnabled = true,
            ParentLayerId = existingParentLayerId,
        });

        await InvokePrivateOnRendererAsync(cut, "ApplyLayerParentChangeAsync", customLayerId, existingParentLayerId, false);

        await _mapApi.DidNotReceive().SetLayerParentAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid?>());
    }

    [Fact]
    public async Task DescendantTraversal_OnMalformedCycle_IsSafe()
    {
        var cut = RenderLoadedPage();
        var layerA = Guid.NewGuid();
        var layerB = Guid.NewGuid();
        var layerC = Guid.NewGuid();

        SetField(cut.Instance, "_layers", new List<MapLayerDto>
        {
            new() { MapLayerId = layerA, Name = "A", SortOrder = 0, IsEnabled = true, ParentLayerId = layerC },
            new() { MapLayerId = layerB, Name = "B", SortOrder = 1, IsEnabled = true, ParentLayerId = layerA },
            new() { MapLayerId = layerC, Name = "C", SortOrder = 2, IsEnabled = true, ParentLayerId = layerB },
        });

        await cut.InvokeAsync(() =>
        {
            var method = cut.Instance.GetType().GetMethod("GetDescendantLayerIds", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Assert.NotNull(method);
            var descendants = (HashSet<Guid>)method!.Invoke(cut.Instance, new object?[] { layerA })!;
            Assert.Contains(layerB, descendants);
            Assert.Contains(layerC, descendants);
            Assert.DoesNotContain(layerA, descendants);
        });
    }

    [Fact]
    public async Task BeginLayerRename_WhenProtectedMissingOrAnotherLayerAlreadyEditing_DoesNothing()
    {
        var cut = RenderLoadedPage();
        var customLayerId = Guid.NewGuid();
        GetField<List<MapLayerDto>>(cut.Instance, "_layers").Add(new MapLayerDto
        {
            MapLayerId = customLayerId,
            Name = "Custom",
            SortOrder = 3,
            IsEnabled = true
        });

        await InvokePrivateOnRendererAsync(cut, "BeginLayerRename", _worldLayerId);
        Assert.Null(GetField<Guid?>(cut.Instance, "_editingLayerId"));

        await InvokePrivateOnRendererAsync(cut, "BeginLayerRename", Guid.NewGuid());
        Assert.Null(GetField<Guid?>(cut.Instance, "_editingLayerId"));

        SetField(cut.Instance, "_editingLayerId", _campaignLayerId);
        SetField(cut.Instance, "_renameLayerNameInput", "Campaign");
        await InvokePrivateOnRendererAsync(cut, "BeginLayerRename", customLayerId);

        Assert.Equal(_campaignLayerId, GetField<Guid?>(cut.Instance, "_editingLayerId"));
        Assert.Equal("Campaign", GetField<string>(cut.Instance, "_renameLayerNameInput"));

        SetField(cut.Instance, "_isRenamingLayer", true);
        await InvokePrivateOnRendererAsync(cut, "BeginLayerRename", customLayerId);
        Assert.Equal(_campaignLayerId, GetField<Guid?>(cut.Instance, "_editingLayerId"));
    }

    [Fact]
    public async Task OnRenameLayerNameInput_WhenValueNull_UsesEmptyStringAndClearsError()
    {
        var cut = RenderLoadedPage();
        SetField(cut.Instance, "_renameLayerNameInput", "Existing");
        SetField(cut.Instance, "_manageLayerError", "old error");

        await InvokePrivateOnRendererAsync(cut, "OnRenameLayerNameInput", new ChangeEventArgs { Value = null });

        Assert.Equal(string.Empty, GetField<string>(cut.Instance, "_renameLayerNameInput"));
        Assert.Equal(string.Empty, GetField<string>(cut.Instance, "_manageLayerError"));

        SetField(cut.Instance, "_renameLayerNameInput", "Keep");
        SetField(cut.Instance, "_isRenamingLayer", true);
        await InvokePrivateOnRendererAsync(cut, "OnRenameLayerNameInput", new ChangeEventArgs { Value = "Ignored" });
        Assert.Equal("Keep", GetField<string>(cut.Instance, "_renameLayerNameInput"));
    }

    [Fact]
    public async Task OnRenameLayerNameKeyDown_CoversEscapeAndDefaultPaths()
    {
        var cut = RenderLoadedPage();
        SetField(cut.Instance, "_editingLayerId", _campaignLayerId);
        SetField(cut.Instance, "_renameLayerNameInput", "Campaign Layer");
        SetField(cut.Instance, "_renameLayerOriginalName", "Campaign");

        await InvokePrivateOnRendererAsync(cut, "OnRenameLayerNameKeyDown", _campaignLayerId, new KeyboardEventArgs { Key = "Escape" });
        Assert.Null(GetField<Guid?>(cut.Instance, "_editingLayerId"));
        Assert.Equal(string.Empty, GetField<string>(cut.Instance, "_renameLayerNameInput"));
        Assert.Equal(string.Empty, GetField<string>(cut.Instance, "_renameLayerOriginalName"));

        await InvokePrivateOnRendererAsync(cut, "OnRenameLayerNameKeyDown", _campaignLayerId, new KeyboardEventArgs { Key = "Tab" });
        await _mapApi.DidNotReceive().RenameLayerAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<string>());
    }

    [Fact]
    public async Task OnRenameLayerNameKeyDown_WhenEnter_InvokesRenamePath()
    {
        var cut = RenderLoadedPage();
        var customLayerId = Guid.NewGuid();
        var layers = GetField<List<MapLayerDto>>(cut.Instance, "_layers");
        layers.Add(new MapLayerDto
        {
            MapLayerId = customLayerId,
            Name = "Custom",
            SortOrder = 3,
            IsEnabled = true
        });
        SetField(cut.Instance, "_editingLayerId", customLayerId);
        SetField(cut.Instance, "_renameLayerNameInput", "Renamed Custom");
        SetField(cut.Instance, "_renameLayerOriginalName", "Custom");

        await InvokePrivateOnRendererAsync(cut, "OnRenameLayerNameKeyDown", customLayerId, new KeyboardEventArgs { Key = "Enter" });

        await _mapApi.Received(1).RenameLayerAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), customLayerId, "Renamed Custom");
    }

    [Fact]
    public async Task OnRenameLayerNameKeyDown_WhenEnterAndNameUnchanged_SkipsApiAndExitsEdit()
    {
        var cut = RenderLoadedPage();
        var customLayerId = Guid.NewGuid();
        var layers = GetField<List<MapLayerDto>>(cut.Instance, "_layers");
        layers.Add(new MapLayerDto
        {
            MapLayerId = customLayerId,
            Name = "Custom",
            SortOrder = 3,
            IsEnabled = true
        });
        SetField(cut.Instance, "_editingLayerId", customLayerId);
        SetField(cut.Instance, "_renameLayerNameInput", "Custom");
        SetField(cut.Instance, "_renameLayerOriginalName", "Custom");

        await InvokePrivateOnRendererAsync(cut, "OnRenameLayerNameKeyDown", customLayerId, new KeyboardEventArgs { Key = "Enter" });

        await _mapApi.DidNotReceive().RenameLayerAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), customLayerId, Arg.Any<string>());
        Assert.Null(GetField<Guid?>(cut.Instance, "_editingLayerId"));
    }

    [Fact]
    public async Task ConfirmLayerRenameAsync_CoversValidationAndFailurePaths()
    {
        var cut = RenderLoadedPage();
        var customLayerId = Guid.NewGuid();
        GetField<List<MapLayerDto>>(cut.Instance, "_layers").Add(new MapLayerDto
        {
            MapLayerId = customLayerId,
            Name = "Custom",
            SortOrder = 3,
            IsEnabled = true
        });

        SetField(cut.Instance, "_isRenamingLayer", true);
        SetField(cut.Instance, "_editingLayerId", customLayerId);
        await InvokePrivateOnRendererAsync(cut, "ConfirmLayerRenameAsync", customLayerId);
        await _mapApi.DidNotReceive().RenameLayerAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<string>());

        SetField(cut.Instance, "_isRenamingLayer", false);
        SetField(cut.Instance, "_editingLayerId", null);
        await InvokePrivateOnRendererAsync(cut, "ConfirmLayerRenameAsync", customLayerId);
        await _mapApi.DidNotReceive().RenameLayerAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<string>());

        SetField(cut.Instance, "_editingLayerId", customLayerId);
        SetField(cut.Instance, "_renameLayerNameInput", "   ");
        SetField(cut.Instance, "_renameLayerOriginalName", "Custom");
        await InvokePrivateOnRendererAsync(cut, "ConfirmLayerRenameAsync", customLayerId);
        Assert.Equal("Layer name is required.", GetField<string>(cut.Instance, "_manageLayerError"));
        Assert.Equal(customLayerId, GetField<Guid?>(cut.Instance, "_editingLayerId"));
        Assert.Equal("   ", GetField<string>(cut.Instance, "_renameLayerNameInput"));

        SetField(cut.Instance, "_renameLayerNameInput", new string('L', 201));
        await InvokePrivateOnRendererAsync(cut, "ConfirmLayerRenameAsync", customLayerId);
        Assert.Equal("Layer name must be 200 characters or fewer.", GetField<string>(cut.Instance, "_manageLayerError"));
        Assert.Equal(customLayerId, GetField<Guid?>(cut.Instance, "_editingLayerId"));
    }

    [Fact]
    public async Task ConfirmLayerRenameAsync_WhenNameUnchanged_ExitsEditModeWithoutApiCall()
    {
        var cut = RenderLoadedPage();
        var customLayerId = Guid.NewGuid();
        GetField<List<MapLayerDto>>(cut.Instance, "_layers").Add(new MapLayerDto
        {
            MapLayerId = customLayerId,
            Name = "Custom",
            SortOrder = 3,
            IsEnabled = true
        });
        SetField(cut.Instance, "_editingLayerId", customLayerId);
        SetField(cut.Instance, "_renameLayerNameInput", "Custom");
        SetField(cut.Instance, "_renameLayerOriginalName", "Custom");

        await InvokePrivateOnRendererAsync(cut, "ConfirmLayerRenameAsync", customLayerId);

        await _mapApi.DidNotReceive().RenameLayerAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<string>());
        Assert.Null(GetField<Guid?>(cut.Instance, "_editingLayerId"));
        Assert.Equal(string.Empty, GetField<string>(cut.Instance, "_renameLayerNameInput"));
    }

    [Fact]
    public async Task ConfirmLayerRenameAsync_WhenApiThrows_KeepsEditSessionAndDraft()
    {
        var cut = RenderLoadedPage();
        var customLayerId = Guid.NewGuid();
        GetField<List<MapLayerDto>>(cut.Instance, "_layers").Add(new MapLayerDto
        {
            MapLayerId = customLayerId,
            Name = "Custom",
            SortOrder = 3,
            IsEnabled = true
        });
        SetField(cut.Instance, "_editingLayerId", customLayerId);
        SetField(cut.Instance, "_renameLayerNameInput", "Renamed Layer");
        SetField(cut.Instance, "_renameLayerOriginalName", "Custom");
        _mapApi.RenameLayerAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<string>())
            .Returns(Task.FromException(new InvalidOperationException("rename failed")));

        await InvokePrivateOnRendererAsync(cut, "ConfirmLayerRenameAsync", customLayerId);

        Assert.Equal("Failed to rename layer: rename failed", GetField<string>(cut.Instance, "_manageLayerError"));
        Assert.Equal(customLayerId, GetField<Guid?>(cut.Instance, "_editingLayerId"));
        Assert.Equal("Renamed Layer", GetField<string>(cut.Instance, "_renameLayerNameInput"));
    }

    [Fact]
    public async Task ConfirmLayerRenameAsync_WhenSaveAlreadyInFlight_SendsSingleRequest()
    {
        var cut = RenderLoadedPage();
        var customLayerId = Guid.NewGuid();
        GetField<List<MapLayerDto>>(cut.Instance, "_layers").Add(new MapLayerDto
        {
            MapLayerId = customLayerId,
            Name = "Custom",
            SortOrder = 3,
            IsEnabled = true
        });

        var renameTcs = new TaskCompletionSource();
        _mapApi.RenameLayerAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<string>())
            .Returns(renameTcs.Task);

        SetField(cut.Instance, "_editingLayerId", customLayerId);
        SetField(cut.Instance, "_renameLayerNameInput", "Renamed Layer");
        SetField(cut.Instance, "_renameLayerOriginalName", "Custom");

        var firstSave = InvokePrivateOnRendererAsync(cut, "ConfirmLayerRenameAsync", customLayerId);
        var secondSave = InvokePrivateOnRendererAsync(cut, "ConfirmLayerRenameAsync", customLayerId);

        await _mapApi.Received(1).RenameLayerAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), customLayerId, "Renamed Layer");

        renameTcs.SetResult();
        await Task.WhenAll(firstSave, secondSave);
    }

    [Fact]
    public async Task BeginAndCancelLayerDelete_CoversProtectedAndCancelPaths()
    {
        var cut = RenderLoadedPage();
        var customLayerId = Guid.NewGuid();
        GetField<List<MapLayerDto>>(cut.Instance, "_layers").Add(new MapLayerDto
        {
            MapLayerId = customLayerId,
            Name = "Custom",
            SortOrder = 3,
            IsEnabled = true
        });

        await InvokePrivateOnRendererAsync(cut, "BeginLayerDelete", _worldLayerId);
        Assert.Null(GetField<Guid?>(cut.Instance, "_pendingDeleteLayerId"));

        await InvokePrivateOnRendererAsync(cut, "BeginLayerDelete", customLayerId);
        Assert.Equal(customLayerId, GetField<Guid?>(cut.Instance, "_pendingDeleteLayerId"));

        await InvokePrivateOnRendererAsync(cut, "CancelLayerDelete");
        Assert.Null(GetField<Guid?>(cut.Instance, "_pendingDeleteLayerId"));
    }

    [Fact]
    public async Task ConfirmLayerDeleteAsync_CoversEarlyReturnAndFailurePaths()
    {
        var cut = RenderLoadedPage();
        var customLayerId = Guid.NewGuid();
        GetField<List<MapLayerDto>>(cut.Instance, "_layers").Add(new MapLayerDto
        {
            MapLayerId = customLayerId,
            Name = "Custom",
            SortOrder = 3,
            IsEnabled = true
        });

        SetField(cut.Instance, "_isDeletingLayer", true);
        SetField(cut.Instance, "_pendingDeleteLayerId", customLayerId);
        await InvokePrivateOnRendererAsync(cut, "ConfirmLayerDeleteAsync", customLayerId);
        await _mapApi.DidNotReceive().DeleteLayerAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>());

        SetField(cut.Instance, "_isDeletingLayer", false);
        SetField(cut.Instance, "_pendingDeleteLayerId", null);
        await InvokePrivateOnRendererAsync(cut, "ConfirmLayerDeleteAsync", customLayerId);
        await _mapApi.DidNotReceive().DeleteLayerAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>());

        SetField(cut.Instance, "_pendingDeleteLayerId", customLayerId);
        _mapApi.DeleteLayerAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>())
            .Returns(Task.FromException(new InvalidOperationException("delete failed")));

        await InvokePrivateOnRendererAsync(cut, "ConfirmLayerDeleteAsync", customLayerId);

        Assert.Equal("Failed to delete layer: delete failed", GetField<string>(cut.Instance, "_manageLayerError"));
        Assert.Null(GetField<Guid?>(cut.Instance, "_pendingDeleteLayerId"));
    }

    [Fact]
    public void MapDetail_WhenSuccessful_RendersLayerDragHandles()
    {
        var cut = RenderLoadedPage();

        cut.WaitForAssertion(() =>
        {
            var handles = cut.FindAll(".map-page__layer-drag-handle");
            Assert.Equal(3, handles.Count);
        });
    }

    [Fact]
    public void MapDetail_LayerRows_IncludeHoverableClass()
    {
        var cut = RenderLoadedPage();

        cut.WaitForAssertion(() =>
        {
            var rows = cut.FindAll(".map-page__layer-row");
            Assert.Equal(3, rows.Count);
            Assert.All(rows, row =>
                Assert.Contains("map-page__layer-row--hoverable", row.GetAttribute("class"), StringComparison.Ordinal));
        });
    }

    [Fact]
    public void MapDetail_DefaultSelection_ChoosesArc_WhenAvailable()
    {
        var cut = RenderLoadedPage();

        cut.WaitForAssertion(() =>
            Assert.Equal(_arcLayerId, GetField<Guid?>(cut.Instance, "SelectedLayerId")));
    }

    [Fact]
    public void MapDetail_DefaultSelection_FallsBackToCampaignThenWorld()
    {
        var worldIdCampaign = Guid.NewGuid();
        var mapIdCampaign = Guid.NewGuid();
        _mapApi.GetMapAsync(worldIdCampaign, mapIdCampaign).Returns((new MapDto { Name = "Campaign Fallback" }, 200, null));
        _mapApi.GetBasemapReadUrlAsync(worldIdCampaign, mapIdCampaign).Returns((new GetBasemapReadUrlResponseDto { ReadUrl = "https://blob/read" }, 200, null));
        _mapApi.GetLayersForMapAsync(worldIdCampaign, mapIdCampaign).Returns(new List<MapLayerDto>
        {
            new() { MapLayerId = _worldLayerId, Name = "World", SortOrder = 0, IsEnabled = true },
            new() { MapLayerId = _campaignLayerId, Name = "Campaign", SortOrder = 1, IsEnabled = true },
        });
        _mapApi.ListPinsForMapAsync(worldIdCampaign, mapIdCampaign).Returns(new List<MapPinResponseDto>());

        var cutCampaign = RenderPage(worldIdCampaign, mapIdCampaign);
        cutCampaign.WaitForAssertion(() =>
            Assert.Equal(_campaignLayerId, GetField<Guid?>(cutCampaign.Instance, "SelectedLayerId")));

        var worldIdWorld = Guid.NewGuid();
        var mapIdWorld = Guid.NewGuid();
        _mapApi.GetMapAsync(worldIdWorld, mapIdWorld).Returns((new MapDto { Name = "World Fallback" }, 200, null));
        _mapApi.GetBasemapReadUrlAsync(worldIdWorld, mapIdWorld).Returns((new GetBasemapReadUrlResponseDto { ReadUrl = "https://blob/read" }, 200, null));
        _mapApi.GetLayersForMapAsync(worldIdWorld, mapIdWorld).Returns(new List<MapLayerDto>
        {
            new() { MapLayerId = _worldLayerId, Name = "World", SortOrder = 0, IsEnabled = true },
        });
        _mapApi.ListPinsForMapAsync(worldIdWorld, mapIdWorld).Returns(new List<MapPinResponseDto>());

        var cutWorld = RenderPage(worldIdWorld, mapIdWorld);
        cutWorld.WaitForAssertion(() =>
            Assert.Equal(_worldLayerId, GetField<Guid?>(cutWorld.Instance, "SelectedLayerId")));
    }

    [Fact]
    public void MapDetail_DefaultSelection_UsesFirstAvailable_WhenNoArcCampaignWorld_AndNullWhenNoLayers()
    {
        var worldId = Guid.NewGuid();
        var mapId = Guid.NewGuid();
        var customLayerId = Guid.NewGuid();

        _mapApi.GetMapAsync(worldId, mapId).Returns((new MapDto { Name = "Custom Fallback" }, 200, null));
        _mapApi.GetBasemapReadUrlAsync(worldId, mapId).Returns((new GetBasemapReadUrlResponseDto { ReadUrl = "https://blob/read" }, 200, null));
        _mapApi.GetLayersForMapAsync(worldId, mapId).Returns(new List<MapLayerDto>
        {
            new() { MapLayerId = customLayerId, Name = "Custom", SortOrder = 0, IsEnabled = true },
        });
        _mapApi.ListPinsForMapAsync(worldId, mapId).Returns(new List<MapPinResponseDto>());

        var cut = RenderPage(worldId, mapId);
        cut.WaitForAssertion(() =>
            Assert.Equal(customLayerId, GetField<Guid?>(cut.Instance, "SelectedLayerId")));

        var resolveDefaultSelectedLayerIdMethod = cut.Instance.GetType()
            .GetMethod("ResolveDefaultSelectedLayerId", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
        Assert.NotNull(resolveDefaultSelectedLayerIdMethod);

        var noLayerSelection = (Guid?)resolveDefaultSelectedLayerIdMethod!.Invoke(null, new object?[] { new List<MapLayerDto>() });
        Assert.Null(noLayerSelection);
    }

    [Fact]
    public void LayerToggle_CallsUpdateLayerVisibilityAsync_WithExpectedValues()
    {
        var worldId = Guid.NewGuid();
        var mapId = Guid.NewGuid();
        _mapApi.GetMapAsync(worldId, mapId).Returns((new MapDto { Name = "Layer Toggle Map" }, 200, null));
        _mapApi.GetBasemapReadUrlAsync(worldId, mapId).Returns((new GetBasemapReadUrlResponseDto { ReadUrl = "https://blob/read" }, 200, null));
        _mapApi.ListPinsForMapAsync(worldId, mapId).Returns(new List<MapPinResponseDto>());

        var cut = RenderPage(worldId, mapId);
        cut.WaitForAssertion(() => Assert.Equal(3, cut.FindAll(".map-page__layer-toggle").Count));

        cut.FindAll(".map-page__layer-toggle")[0].Change(false);

        _mapApi.Received(1).UpdateLayerVisibilityAsync(worldId, mapId, _worldLayerId, false);
    }

    [Fact]
    public void LayerRowClick_UpdatesSelectedLayerId()
    {
        var cut = RenderLoadedPage();
        cut.WaitForAssertion(() => Assert.Equal(3, cut.FindAll(".map-page__layer-row").Count));

        cut.FindAll(".map-page__layer-row")[0].Click();

        Assert.Equal(_worldLayerId, GetField<Guid?>(cut.Instance, "SelectedLayerId"));
    }

    [Fact]
    public void VisibilityToggle_DoesNotChangeSelectedLayerId()
    {
        var cut = RenderLoadedPage();
        cut.WaitForAssertion(() => Assert.Equal(3, cut.FindAll(".map-page__layer-row").Count));

        cut.FindAll(".map-page__layer-row")[1].Click();
        Assert.Equal(_campaignLayerId, GetField<Guid?>(cut.Instance, "SelectedLayerId"));

        cut.FindAll(".map-page__layer-toggle")[0].Change(false);

        Assert.Equal(_campaignLayerId, GetField<Guid?>(cut.Instance, "SelectedLayerId"));
    }

    [Fact]
    public async Task LayerDrop_ReordersRowsInUiImmediately()
    {
        var cut = RenderLoadedPage();
        cut.WaitForAssertion(() => Assert.Equal(3, cut.FindAll(".map-page__layer-row").Count));

        var reorderTcs = new TaskCompletionSource<bool>();
        _mapApi.ReorderLayersAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<IList<Guid>>())
            .Returns(reorderTcs.Task);

        await InvokePrivateOnRendererAsync(cut, "OnLayerDragStart", _worldLayerId, new DragEventArgs());

        var onLayerDropMethod = typeof(MapDetail).GetMethod(
            "OnLayerDropAsync",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        Assert.NotNull(onLayerDropMethod);

        Task? pendingDropTask = null;
        await cut.InvokeAsync(() =>
        {
            pendingDropTask = (Task)onLayerDropMethod!.Invoke(cut.Instance, new object?[] { 2 })!;
        });
        Assert.NotNull(pendingDropTask);
        Assert.False(pendingDropTask!.IsCompleted);

        var pendingLayerOrder = GetField<List<MapLayerDto>>(cut.Instance, "_layers")
            .Select(layer => layer.Name)
            .ToList();
        Assert.Equal(new[] { "Campaign", "Arc", "World" }, pendingLayerOrder);

        reorderTcs.SetResult(true);
        await pendingDropTask;
    }

    [Fact]
    public async Task LayerDrop_CallsReorderLayersAsync_WithLayerIdsInUiOrder()
    {
        var cut = RenderLoadedPage();
        cut.WaitForAssertion(() => Assert.Equal(3, cut.FindAll(".map-page__layer-row").Count));

        await InvokePrivateOnRendererAsync(cut, "OnLayerDragStart", _worldLayerId, new DragEventArgs());
        await InvokePrivateOnRendererAsync(cut, "OnLayerDropAsync", 2);

        await _mapApi.Received(1).ReorderLayersAsync(
            cut.Instance.WorldId,
            cut.Instance.MapId,
            Arg.Is<IList<Guid>>(layerIds =>
                layerIds.Count == 3
                && layerIds[0] == _campaignLayerId
                && layerIds[1] == _arcLayerId
                && layerIds[2] == _worldLayerId));
    }

    [Fact]
    public async Task LayerDrop_FromDomDropEvent_UsesDroppedRowIndex()
    {
        var cut = RenderLoadedPage();
        cut.WaitForAssertion(() => Assert.Equal(3, cut.FindAll(".map-page__layer-row").Count));

        await InvokePrivateOnRendererAsync(cut, "OnLayerDragStart", _worldLayerId, new DragEventArgs());
        cut.FindAll(".map-page__layer-row")[2].TriggerEvent("ondrop", new DragEventArgs());

        await _mapApi.Received(1).ReorderLayersAsync(
            cut.Instance.WorldId,
            cut.Instance.MapId,
            Arg.Is<IList<Guid>>(layerIds =>
                layerIds.Count == 3
                && layerIds[0] == _campaignLayerId
                && layerIds[1] == _arcLayerId
                && layerIds[2] == _worldLayerId));
    }

    [Fact]
    public async Task LayerDrop_ForChildSiblings_CallsReorderLayersAsync_WithSiblingGroupOnly()
    {
        var cut = RenderLoadedPage();
        var parentId = Guid.NewGuid();
        var childAId = Guid.NewGuid();
        var childBId = Guid.NewGuid();
        var rootPeerId = Guid.NewGuid();
        SetField(cut.Instance, "_layers", new List<MapLayerDto>
        {
            new() { MapLayerId = _worldLayerId, Name = "World", SortOrder = 0, IsEnabled = true },
            new() { MapLayerId = parentId, Name = "Parent", SortOrder = 1, IsEnabled = true },
            new() { MapLayerId = childAId, Name = "Child A", SortOrder = 2, IsEnabled = true, ParentLayerId = parentId },
            new() { MapLayerId = childBId, Name = "Child B", SortOrder = 3, IsEnabled = true, ParentLayerId = parentId },
            new() { MapLayerId = rootPeerId, Name = "Root Peer", SortOrder = 4, IsEnabled = true },
        });

        await InvokePrivateOnRendererAsync(cut, "OnLayerDragStart", childAId, new DragEventArgs());
        await InvokePrivateOnRendererAsync(cut, "OnLayerDropAsync", 3);

        await _mapApi.Received(1).ReorderLayersAsync(
            cut.Instance.WorldId,
            cut.Instance.MapId,
            Arg.Is<IList<Guid>>(layerIds =>
                layerIds.Count == 2
                && layerIds[0] == childBId
                && layerIds[1] == childAId));
    }

    [Fact]
    public async Task LayerDrop_CrossParentDrop_DoesNotSubmitReorder_AndKeepsLayerOrder()
    {
        var cut = RenderLoadedPage();
        var parentId = Guid.NewGuid();
        var childId = Guid.NewGuid();
        var rootPeerId = Guid.NewGuid();
        var originalOrder = new List<MapLayerDto>
        {
            new() { MapLayerId = _worldLayerId, Name = "World", SortOrder = 0, IsEnabled = true },
            new() { MapLayerId = parentId, Name = "Parent", SortOrder = 1, IsEnabled = true },
            new() { MapLayerId = childId, Name = "Child", SortOrder = 2, IsEnabled = true, ParentLayerId = parentId },
            new() { MapLayerId = rootPeerId, Name = "Root Peer", SortOrder = 3, IsEnabled = true },
        };
        var originalOrderIds = originalOrder.Select(layer => layer.MapLayerId).ToList();
        SetField(cut.Instance, "_layers", originalOrder);

        await InvokePrivateOnRendererAsync(cut, "OnLayerDragStart", childId, new DragEventArgs());
        await InvokePrivateOnRendererAsync(cut, "OnLayerDropAsync", 3);

        await _mapApi.DidNotReceive().ReorderLayersAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<IList<Guid>>());

        var resultingOrder = GetField<List<MapLayerDto>>(cut.Instance, "_layers")
            .Select(layer => layer.MapLayerId)
            .ToList();
        Assert.Equal(originalOrderIds, resultingOrder);
    }

    [Fact]
    public async Task LayerDrop_WhenApiFails_RestoresPreviousLayerOrder()
    {
        var cut = RenderLoadedPage();
        cut.WaitForAssertion(() => Assert.Equal(3, cut.FindAll(".map-page__layer-row").Count));

        _mapApi.ReorderLayersAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<IList<Guid>>())
            .Returns(Task.FromException(new InvalidOperationException("reorder failed")));

        await InvokePrivateOnRendererAsync(cut, "OnLayerDragStart", _worldLayerId, new DragEventArgs());
        await InvokePrivateOnRendererAsync(cut, "OnLayerDropAsync", 2);

        cut.Render();
        var rows = cut.FindAll(".map-page__layer-row");
        Assert.Contains("World", rows[0].TextContent, StringComparison.Ordinal);
        Assert.Contains("Campaign", rows[1].TextContent, StringComparison.Ordinal);
        Assert.Contains("Arc", rows[2].TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public async Task LayerDrop_DoesNotChangeSelectedLayerIdentity()
    {
        var cut = RenderLoadedPage();
        cut.WaitForAssertion(() => Assert.Equal(3, cut.FindAll(".map-page__layer-row").Count));

        cut.FindAll(".map-page__layer-row")[1].Click();
        Assert.Equal(_campaignLayerId, GetField<Guid?>(cut.Instance, "SelectedLayerId"));

        await InvokePrivateOnRendererAsync(cut, "OnLayerDragStart", _worldLayerId, new DragEventArgs());
        await InvokePrivateOnRendererAsync(cut, "OnLayerDropAsync", 2);

        Assert.Equal(_campaignLayerId, GetField<Guid?>(cut.Instance, "SelectedLayerId"));
    }

    [Fact]
    public async Task LayerDrop_PreservesVisibilityByLayerIdentity()
    {
        var cut = RenderLoadedPage();
        var layers = GetField<List<MapLayerDto>>(cut.Instance, "_layers");

        layers.Single(layer => layer.MapLayerId == _worldLayerId).IsEnabled = false;
        layers.Single(layer => layer.MapLayerId == _campaignLayerId).IsEnabled = true;
        layers.Single(layer => layer.MapLayerId == _arcLayerId).IsEnabled = false;

        await InvokePrivateOnRendererAsync(cut, "OnLayerDragStart", _worldLayerId, new DragEventArgs());
        await InvokePrivateOnRendererAsync(cut, "OnLayerDropAsync", 2);

        var reorderedLayers = GetField<List<MapLayerDto>>(cut.Instance, "_layers");
        Assert.False(reorderedLayers.Single(layer => layer.MapLayerId == _worldLayerId).IsEnabled);
        Assert.True(reorderedLayers.Single(layer => layer.MapLayerId == _campaignLayerId).IsEnabled);
        Assert.False(reorderedLayers.Single(layer => layer.MapLayerId == _arcLayerId).IsEnabled);
    }

    [Fact]
    public async Task LayerDragStart_WithDataTransfer_SetsDragStateAndTransferOptions()
    {
        var cut = RenderLoadedPage();
        var dragEventArgs = new DragEventArgs { DataTransfer = new DataTransfer() };

        await InvokePrivateOnRendererAsync(cut, "OnLayerDragStart", _campaignLayerId, dragEventArgs);

        Assert.Equal(_campaignLayerId, GetField<Guid?>(cut.Instance, "_draggedLayerId"));
        Assert.Equal(1, GetField<int?>(cut.Instance, "_dropTargetIndex"));
        Assert.Equal("move", dragEventArgs.DataTransfer!.DropEffect);
        Assert.Equal("move", dragEventArgs.DataTransfer.EffectAllowed);
    }

    [Fact]
    public async Task LayerDragEnter_WithoutDraggedLayer_DoesNotUpdateDropTarget()
    {
        var cut = RenderLoadedPage();
        SetField(cut.Instance, "_draggedLayerId", null);
        SetField(cut.Instance, "_dropTargetIndex", null);

        await InvokePrivateOnRendererAsync(cut, "OnLayerDragEnter", 2);

        Assert.Null(GetField<int?>(cut.Instance, "_dropTargetIndex"));
    }

    [Fact]
    public async Task LayerDragOver_WithoutDraggedLayer_DoesNotUpdateDropTarget()
    {
        var cut = RenderLoadedPage();
        SetField(cut.Instance, "_draggedLayerId", null);
        SetField(cut.Instance, "_dropTargetIndex", null);

        await InvokePrivateOnRendererAsync(cut, "OnLayerDragOver", 2);

        Assert.Null(GetField<int?>(cut.Instance, "_dropTargetIndex"));
    }

    [Fact]
    public async Task LayerDragEnterAndOver_WithDraggedLayer_UpdatesDropTarget()
    {
        var cut = RenderLoadedPage();
        SetField(cut.Instance, "_draggedLayerId", _worldLayerId);

        await InvokePrivateOnRendererAsync(cut, "OnLayerDragEnter", 1);
        Assert.Equal(1, GetField<int?>(cut.Instance, "_dropTargetIndex"));

        await InvokePrivateOnRendererAsync(cut, "OnLayerDragOver", 2);
        Assert.Equal(2, GetField<int?>(cut.Instance, "_dropTargetIndex"));
    }

    [Fact]
    public async Task LayerDrop_WithoutDraggedLayer_ClearsTransientStateAndSkipsApi()
    {
        var cut = RenderLoadedPage();
        SetField(cut.Instance, "_draggedLayerId", null);
        SetField(cut.Instance, "_dropTargetIndex", 1);

        await InvokePrivateOnRendererAsync(cut, "OnLayerDropAsync", 1);

        Assert.Null(GetField<Guid?>(cut.Instance, "_draggedLayerId"));
        Assert.Null(GetField<int?>(cut.Instance, "_dropTargetIndex"));
        await _mapApi.DidNotReceive().ReorderLayersAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<IList<Guid>>());
    }

    [Fact]
    public async Task LayerDrop_WithMissingDraggedLayer_ClearsTransientStateAndSkipsApi()
    {
        var cut = RenderLoadedPage();
        var missingLayerId = Guid.NewGuid();
        SetField(cut.Instance, "_draggedLayerId", missingLayerId);
        SetField(cut.Instance, "_dropTargetIndex", 1);

        await InvokePrivateOnRendererAsync(cut, "OnLayerDropAsync", 1);

        Assert.Null(GetField<Guid?>(cut.Instance, "_draggedLayerId"));
        Assert.Null(GetField<int?>(cut.Instance, "_dropTargetIndex"));
        await _mapApi.DidNotReceive().ReorderLayersAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<IList<Guid>>());
    }

    [Fact]
    public async Task LayerDrop_WithInvalidTargetIndex_ClearsTransientStateAndSkipsApi()
    {
        var cut = RenderLoadedPage();
        SetField(cut.Instance, "_draggedLayerId", _worldLayerId);
        SetField(cut.Instance, "_dropTargetIndex", 0);

        await InvokePrivateOnRendererAsync(cut, "OnLayerDropAsync", -1);

        Assert.Null(GetField<Guid?>(cut.Instance, "_draggedLayerId"));
        Assert.Null(GetField<int?>(cut.Instance, "_dropTargetIndex"));
        await _mapApi.DidNotReceive().ReorderLayersAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<IList<Guid>>());
    }

    [Fact]
    public async Task LayerDrop_WithSameIndex_ClearsTransientStateAndSkipsApi()
    {
        var cut = RenderLoadedPage();
        SetField(cut.Instance, "_draggedLayerId", _worldLayerId);
        SetField(cut.Instance, "_dropTargetIndex", 0);

        await InvokePrivateOnRendererAsync(cut, "OnLayerDropAsync", 0);

        Assert.Null(GetField<Guid?>(cut.Instance, "_draggedLayerId"));
        Assert.Null(GetField<int?>(cut.Instance, "_dropTargetIndex"));
        await _mapApi.DidNotReceive().ReorderLayersAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<IList<Guid>>());
    }

    [Fact]
    public async Task LayerDragEnd_ClearsTransientDragState()
    {
        var cut = RenderLoadedPage();
        SetField(cut.Instance, "_draggedLayerId", _arcLayerId);
        SetField(cut.Instance, "_dropTargetIndex", 2);

        await InvokePrivateOnRendererAsync(cut, "OnLayerDragEnd", new DragEventArgs());

        Assert.Null(GetField<Guid?>(cut.Instance, "_draggedLayerId"));
        Assert.Null(GetField<int?>(cut.Instance, "_dropTargetIndex"));
    }

    [Fact]
    public void RestoreLayerOrder_AppendsUnspecifiedLayers_WhenOrderListIsPartial()
    {
        var restoreLayerOrderMethod = typeof(MapDetail).GetMethod(
            "RestoreLayerOrder",
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
        Assert.NotNull(restoreLayerOrderMethod);

        var world = new MapLayerDto { MapLayerId = _worldLayerId, Name = "World", IsEnabled = true };
        var campaign = new MapLayerDto { MapLayerId = _campaignLayerId, Name = "Campaign", IsEnabled = false };
        var arc = new MapLayerDto { MapLayerId = _arcLayerId, Name = "Arc", IsEnabled = true };

        var currentLayers = new List<MapLayerDto> { world, campaign, arc };
        var partialOrder = new List<Guid> { _campaignLayerId };

        var restored = (List<MapLayerDto>)restoreLayerOrderMethod!.Invoke(null, new object?[] { currentLayers, partialOrder })!;

        Assert.Equal(3, restored.Count);
        Assert.Equal(_campaignLayerId, restored[0].MapLayerId);
        Assert.Equal(_worldLayerId, restored[1].MapLayerId);
        Assert.Equal(_arcLayerId, restored[2].MapLayerId);
    }

    [Fact]
    public void SelectedLayerRow_RendersHighlightClass()
    {
        var cut = RenderLoadedPage();

        cut.WaitForAssertion(() =>
        {
            var selectedRows = cut.FindAll(".layer-row-selected");
            Assert.Single(selectedRows);
            Assert.Contains("Arc", selectedRows[0].TextContent, StringComparison.Ordinal);
        });
    }

    [Fact]
    public async Task MapDetail_InheritedVisibility_HiddenParentHidesAndReenableRestoresChildPin()
    {
        var cut = RenderLoadedPage();
        var parentLayerId = Guid.NewGuid();
        var childLayerId = Guid.NewGuid();
        var siblingLayerId = Guid.NewGuid();
        SetField(cut.Instance, "_layers", new List<MapLayerDto>
        {
            new() { MapLayerId = parentLayerId, Name = "Parent", SortOrder = 0, IsEnabled = false },
            new() { MapLayerId = childLayerId, Name = "Child", SortOrder = 1, IsEnabled = true, ParentLayerId = parentLayerId },
            new() { MapLayerId = siblingLayerId, Name = "Sibling", SortOrder = 2, IsEnabled = true },
        });
        SetField(cut.Instance, "_pins", new List<MapPinResponseDto>
        {
            new() { PinId = Guid.NewGuid(), MapId = cut.Instance.MapId, LayerId = childLayerId, Name = "Child Pin", X = 0.2f, Y = 0.3f },
            new() { PinId = Guid.NewGuid(), MapId = cut.Instance.MapId, LayerId = siblingLayerId, Name = "Sibling Pin", X = 0.5f, Y = 0.6f },
        });

        cut.Render();
        cut.WaitForAssertion(() =>
        {
            var pinNames = cut.FindAll(".map-page__pin-name")
                .Select(pinName => pinName.TextContent.Trim())
                .ToList();
            Assert.Equal(["Sibling Pin"], pinNames);
        });

        await InvokePrivateOnRendererAsync(
            cut,
            "OnLayerVisibilityChangedAsync",
            parentLayerId,
            new ChangeEventArgs { Value = true });

        cut.WaitForAssertion(() =>
        {
            var pinNames = cut.FindAll(".map-page__pin-name")
                .Select(pinName => pinName.TextContent.Trim())
                .ToList();
            Assert.Equal(["Child Pin", "Sibling Pin"], pinNames.OrderBy(name => name).ToList());
        });

        var layers = GetField<List<MapLayerDto>>(cut.Instance, "_layers");
        Assert.True(layers.Single(layer => layer.MapLayerId == childLayerId).IsEnabled);
    }

    [Fact]
    public async Task MapDetail_InheritedVisibility_ExplicitlyDisabledChildRemainsHidden()
    {
        var cut = RenderLoadedPage();
        var parentLayerId = Guid.NewGuid();
        var childLayerId = Guid.NewGuid();
        SetField(cut.Instance, "_layers", new List<MapLayerDto>
        {
            new() { MapLayerId = parentLayerId, Name = "Parent", SortOrder = 0, IsEnabled = true },
            new() { MapLayerId = childLayerId, Name = "Child", SortOrder = 1, IsEnabled = false, ParentLayerId = parentLayerId },
        });
        SetField(cut.Instance, "_pins", new List<MapPinResponseDto>
        {
            new() { PinId = Guid.NewGuid(), MapId = cut.Instance.MapId, LayerId = parentLayerId, Name = "Parent Pin", X = 0.1f, Y = 0.2f },
            new() { PinId = Guid.NewGuid(), MapId = cut.Instance.MapId, LayerId = childLayerId, Name = "Child Pin", X = 0.3f, Y = 0.4f },
        });

        cut.Render();
        cut.WaitForAssertion(() =>
        {
            var pinNames = cut.FindAll(".map-page__pin-name")
                .Select(pinName => pinName.TextContent.Trim())
                .ToList();
            Assert.Equal(["Parent Pin"], pinNames);
        });

        await InvokePrivateOnRendererAsync(
            cut,
            "OnLayerVisibilityChangedAsync",
            parentLayerId,
            new ChangeEventArgs { Value = false });
        await InvokePrivateOnRendererAsync(
            cut,
            "OnLayerVisibilityChangedAsync",
            parentLayerId,
            new ChangeEventArgs { Value = true });

        cut.WaitForAssertion(() =>
        {
            var pinNames = cut.FindAll(".map-page__pin-name")
                .Select(pinName => pinName.TextContent.Trim())
                .ToList();
            Assert.Equal(["Parent Pin"], pinNames);
        });

        var layers = GetField<List<MapLayerDto>>(cut.Instance, "_layers");
        Assert.False(layers.Single(layer => layer.MapLayerId == childLayerId).IsEnabled);
    }

    [Fact]
    public void MapDetail_InheritedVisibility_DeepAncestorAndSiblingIsolation()
    {
        var cut = RenderLoadedPage();
        var rootId = Guid.NewGuid();
        var parentAId = Guid.NewGuid();
        var childAId = Guid.NewGuid();
        var parentBId = Guid.NewGuid();
        var childBId = Guid.NewGuid();
        SetField(cut.Instance, "_layers", new List<MapLayerDto>
        {
            new() { MapLayerId = rootId, Name = "Root", SortOrder = 0, IsEnabled = true },
            new() { MapLayerId = parentAId, Name = "Parent A", SortOrder = 1, IsEnabled = false, ParentLayerId = rootId },
            new() { MapLayerId = childAId, Name = "Child A", SortOrder = 2, IsEnabled = true, ParentLayerId = parentAId },
            new() { MapLayerId = parentBId, Name = "Parent B", SortOrder = 3, IsEnabled = true, ParentLayerId = rootId },
            new() { MapLayerId = childBId, Name = "Child B", SortOrder = 4, IsEnabled = true, ParentLayerId = parentBId },
        });
        SetField(cut.Instance, "_pins", new List<MapPinResponseDto>
        {
            new() { PinId = Guid.NewGuid(), MapId = cut.Instance.MapId, LayerId = childAId, Name = "Child A Pin", X = 0.2f, Y = 0.3f },
            new() { PinId = Guid.NewGuid(), MapId = cut.Instance.MapId, LayerId = childBId, Name = "Child B Pin", X = 0.4f, Y = 0.5f },
        });

        cut.Render();
        cut.WaitForAssertion(() =>
        {
            var pinNames = cut.FindAll(".map-page__pin-name")
                .Select(pinName => pinName.TextContent.Trim())
                .ToList();
            Assert.Equal(["Child B Pin"], pinNames);
        });
    }

    [Fact]
    public void MapDetail_InheritedVisibility_MissingParentAndSelfCycleAreHiddenWithoutThrowing()
    {
        var cut = RenderLoadedPage();
        var missingParentChildId = Guid.NewGuid();
        var selfCycleId = Guid.NewGuid();
        SetField(cut.Instance, "_layers", new List<MapLayerDto>
        {
            new() { MapLayerId = missingParentChildId, Name = "Orphan", SortOrder = 0, IsEnabled = true, ParentLayerId = Guid.NewGuid() },
            new() { MapLayerId = selfCycleId, Name = "Cycle", SortOrder = 1, IsEnabled = true, ParentLayerId = selfCycleId },
        });
        SetField(cut.Instance, "_pins", new List<MapPinResponseDto>
        {
            new() { PinId = Guid.NewGuid(), MapId = cut.Instance.MapId, LayerId = missingParentChildId, Name = "Orphan Pin", X = 0.2f, Y = 0.3f },
            new() { PinId = Guid.NewGuid(), MapId = cut.Instance.MapId, LayerId = selfCycleId, Name = "Cycle Pin", X = 0.4f, Y = 0.5f },
        });

        cut.Render();
        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll(".map-page__pin-name")));
    }

    [Fact]
    public void MapDetail_VisiblePins_RenderInLayerSortOrder()
    {
        var worldId = Guid.NewGuid();
        var mapId = Guid.NewGuid();

        _mapApi.GetMapAsync(worldId, mapId).Returns((new MapDto { Name = "Pin Order Map" }, 200, null));
        _mapApi.GetBasemapReadUrlAsync(worldId, mapId).Returns((new GetBasemapReadUrlResponseDto { ReadUrl = "https://blob/read" }, 200, null));
        _mapApi.GetLayersForMapAsync(worldId, mapId).Returns(new List<MapLayerDto>
        {
            new() { MapLayerId = _worldLayerId, Name = "World", SortOrder = 0, IsEnabled = true },
            new() { MapLayerId = _campaignLayerId, Name = "Campaign", SortOrder = 1, IsEnabled = true },
            new() { MapLayerId = _arcLayerId, Name = "Arc", SortOrder = 2, IsEnabled = true },
        });
        _mapApi.ListPinsForMapAsync(worldId, mapId).Returns(new List<MapPinResponseDto>
        {
            new() { PinId = Guid.NewGuid(), MapId = mapId, LayerId = _arcLayerId, Name = "Arc Pin 1", X = 0.1f, Y = 0.1f },
            new() { PinId = Guid.NewGuid(), MapId = mapId, LayerId = _worldLayerId, Name = "World Pin 1", X = 0.2f, Y = 0.2f },
            new() { PinId = Guid.NewGuid(), MapId = mapId, LayerId = _campaignLayerId, Name = "Campaign Pin", X = 0.3f, Y = 0.3f },
            new() { PinId = Guid.NewGuid(), MapId = mapId, LayerId = _worldLayerId, Name = "World Pin 2", X = 0.4f, Y = 0.4f },
            new() { PinId = Guid.NewGuid(), MapId = mapId, LayerId = _arcLayerId, Name = "Arc Pin 2", X = 0.5f, Y = 0.5f },
        });

        var cut = RenderPage(worldId, mapId);

        cut.WaitForAssertion(() =>
        {
            var pinNames = cut.FindAll(".map-page__pin-name")
                .Select(pinName => pinName.TextContent.Trim())
                .ToList();

            Assert.Equal(["World Pin 1", "World Pin 2", "Campaign Pin", "Arc Pin 1", "Arc Pin 2"], pinNames);
        });
    }

    [Fact]
    public void MapDetail_LaterLayerPins_RenderAfterEarlierLayerPins()
    {
        var worldId = Guid.NewGuid();
        var mapId = Guid.NewGuid();

        _mapApi.GetMapAsync(worldId, mapId).Returns((new MapDto { Name = "Layer Stack Map" }, 200, null));
        _mapApi.GetBasemapReadUrlAsync(worldId, mapId).Returns((new GetBasemapReadUrlResponseDto { ReadUrl = "https://blob/read" }, 200, null));
        _mapApi.GetLayersForMapAsync(worldId, mapId).Returns(new List<MapLayerDto>
        {
            new() { MapLayerId = _worldLayerId, Name = "World", SortOrder = 0, IsEnabled = true },
            new() { MapLayerId = _arcLayerId, Name = "Arc", SortOrder = 1, IsEnabled = true },
        });
        _mapApi.ListPinsForMapAsync(worldId, mapId).Returns(new List<MapPinResponseDto>
        {
            new() { PinId = Guid.NewGuid(), MapId = mapId, LayerId = _arcLayerId, Name = "Arc Layer Pin", X = 0.8f, Y = 0.2f },
            new() { PinId = Guid.NewGuid(), MapId = mapId, LayerId = _worldLayerId, Name = "World Layer Pin", X = 0.2f, Y = 0.8f },
        });

        var cut = RenderPage(worldId, mapId);

        cut.WaitForAssertion(() =>
        {
            var pinNames = cut.FindAll(".map-page__pin-name")
                .Select(pinName => pinName.TextContent.Trim())
                .ToList();

            Assert.Equal(["World Layer Pin", "Arc Layer Pin"], pinNames);
        });
    }

    [Fact]
    public void GetVisiblePins_WhenLayersUnavailable_ReturnsAllPins()
    {
        var cut = RenderLoadedPage();
        var firstPinId = Guid.NewGuid();
        var secondPinId = Guid.NewGuid();

        SetField(cut.Instance, "_layers", new List<MapLayerDto>());
        SetField(cut.Instance, "_pins", new List<MapPinResponseDto>
        {
            new() { PinId = firstPinId, MapId = Guid.NewGuid(), LayerId = Guid.NewGuid(), X = 0.1f, Y = 0.2f },
            new() { PinId = secondPinId, MapId = Guid.NewGuid(), LayerId = Guid.NewGuid(), X = 0.3f, Y = 0.4f },
        });

        var getVisiblePinsMethod = cut.Instance.GetType()
            .GetMethod("GetVisiblePins", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        Assert.NotNull(getVisiblePinsMethod);

        var visiblePins = ((IEnumerable<MapPinResponseDto>)getVisiblePinsMethod!.Invoke(cut.Instance, null)!).ToList();

        Assert.Equal(2, visiblePins.Count);
        Assert.Equal([firstPinId, secondPinId], visiblePins.Select(pin => pin.PinId).ToList());
    }

    [Fact]
    public void MapDetail_WhenWorldNameUnavailable_UsesFallbackBreadcrumbLabel()
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
    public void MapDetail_WhenUserIsOwner_RendersSaveControls()
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
        var method = typeof(MapDetail).GetMethod("GetMapViewportClass", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
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
        var method = typeof(MapDetail).GetMethod("GetMapStageStyle", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
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
        var method = typeof(MapDetail).GetMethod("GetMapImageClass", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
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
        var method = typeof(MapDetail).GetMethod("GetMapShellStyle", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
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
        var sliderMethod = typeof(MapDetail).GetMethod("GetSliderValueForZoom", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
        var zoomMethod = typeof(MapDetail).GetMethod("GetZoomForSliderValue", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
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
        var method = typeof(MapDetail).GetMethod("ClampPanAxis", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
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
        var method = typeof(MapDetail).GetMethod("TryReadDouble", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
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
        var method = typeof(MapDetail).GetMethod("TryMapClientPointToPinCoordinates", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
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
        var canZoomInMethod = typeof(MapDetail).GetMethod("CanZoomIn", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        var canZoomOutMethod = typeof(MapDetail).GetMethod("CanZoomOut", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
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
    public async Task OnWheelZoom_WhenLayoutUnavailable_DoesNothing()
    {
        var cut = RenderLoadedPage();
        SetField(cut.Instance, "_hasMapViewportLayout", false);
        SetField(cut.Instance, "_mapZoom", 2d);

        await cut.InvokeAsync(() => cut.Instance.OnWheelZoom(-120d));

        Assert.Equal(2d, GetField<double>(cut.Instance, "_mapZoom"), 10);
    }

    [Fact]
    public async Task OnWheelZoom_WhenLayoutAvailable_AdjustsZoomForBothDirections()
    {
        var cut = RenderLoadedPage();
        SetField(cut.Instance, "_hasMapViewportLayout", true);
        SetField(cut.Instance, "_mapMinZoom", 1d);
        SetField(cut.Instance, "_mapMaxZoom", 5d);
        SetField(cut.Instance, "_mapZoom", 3d);
        SetField(cut.Instance, "_mapViewportWidth", 100d);
        SetField(cut.Instance, "_mapViewportHeight", 100d);
        SetField(cut.Instance, "_mapBaseWidth", 100d);
        SetField(cut.Instance, "_mapBaseHeight", 100d);

        await cut.InvokeAsync(() => cut.Instance.OnWheelZoom(-1d));
        var afterZoomIn = GetField<double>(cut.Instance, "_mapZoom");
        Assert.True(afterZoomIn > 3d);

        await cut.InvokeAsync(() => cut.Instance.OnWheelZoom(1d));
        var afterZoomOut = GetField<double>(cut.Instance, "_mapZoom");
        Assert.True(afterZoomOut < afterZoomIn);
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
                && dto.LinkedArticleId == linkedArticleId
                && dto.LayerId == _arcLayerId));
    }

    [Fact]
    public async Task ConfirmCreatePinFromDialogAsync_UsesCurrentPageSelectionForLayerId()
    {
        var cut = RenderLoadedPage();
        var createdPin = new MapPinResponseDto
        {
            PinId = Guid.NewGuid(),
            MapId = Guid.NewGuid(),
            LayerId = _worldLayerId,
            X = 0.2f,
            Y = 0.3f
        };

        _mapApi.CreatePinAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<MapPinCreateDto>())
            .Returns(createdPin);

        cut.WaitForAssertion(() => Assert.Equal(3, cut.FindAll(".map-page__layer-row").Count));
        cut.FindAll(".map-page__layer-row")[0].Click();

        SetField(cut.Instance, "_isCreatePinDialogOpen", true);
        SetField(cut.Instance, "_pendingCreatePinX", 0.2f);
        SetField(cut.Instance, "_pendingCreatePinY", 0.3f);
        SetField(cut.Instance, "_pins", new List<MapPinResponseDto>());

        await InvokePrivateOnRendererAsync(cut, "ConfirmCreatePinFromDialogAsync");

        await _mapApi.Received(1).CreatePinAsync(
            Arg.Any<Guid>(),
            Arg.Any<Guid>(),
            Arg.Is<MapPinCreateDto>(dto => dto.LayerId == _worldLayerId));
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
        var method = typeof(MapDetail).GetMethod("GetPinStyle", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
        Assert.NotNull(method);

        var style = (string?)method!.Invoke(null, new object?[] { new MapPinResponseDto { X = 0.1234f, Y = 0.5678f } });

        Assert.Equal("left:12.3400%;top:56.7800%;", style);
    }

    [Fact]
    public void GetCreatePinPopupStyle_UsesPendingCoordinatesOrCenterFallback()
    {
        var cut = RenderLoadedPage();
        var method = typeof(MapDetail).GetMethod("GetCreatePinPopupStyle", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
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
        var hasPinLabelMethod = typeof(MapDetail).GetMethod("HasPinLabel", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
        var getPinLabelStyleMethod = typeof(MapDetail).GetMethod("GetPinLabelStyle", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
        var getPinTooltipMethod = typeof(MapDetail).GetMethod("GetPinTooltip", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
        var getPinAriaLabelMethod = typeof(MapDetail).GetMethod("GetPinAriaLabel", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
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
        var method = typeof(MapDetail).GetMethod("TryParseLinkedArticleId", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
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
        var method = typeof(MapDetail).GetMethod("Clamp01", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
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
        var method = typeof(MapDetail).GetMethod("HasUsableBounds", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
        var rectType = typeof(MapDetail).GetNestedType("MapElementRect", System.Reflection.BindingFlags.NonPublic);
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

    private IRenderedComponent<MapDetail> RenderPage(Guid worldId, Guid mapId)
    {
        return RenderComponent<MapDetail>(parameters => parameters
            .Add(p => p.WorldId, worldId)
            .Add(p => p.MapId, mapId));
    }

    private IRenderedComponent<MapDetail> RenderLoadedPage()
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

    private IRenderedComponent<MapDetail> RenderNestedLayerPage(Guid locationsLayerId, Guid harborLayerId)
    {
        var worldId = Guid.NewGuid();
        var mapId = Guid.NewGuid();
        ConfigureNestedLayerPage(worldId, mapId, locationsLayerId, harborLayerId);
        var cut = RenderPage(worldId, mapId);
        cut.WaitForAssertion(() => Assert.Equal(5, cut.FindAll(".map-page__layer-row").Count));
        return cut;
    }

    private void ConfigureNestedLayerPage(Guid worldId, Guid mapId, Guid locationsLayerId, Guid harborLayerId)
    {
        _mapApi.GetMapAsync(worldId, mapId).Returns((new MapDto { Name = "Nested Layer Map" }, 200, null));
        _mapApi.GetBasemapReadUrlAsync(worldId, mapId).Returns((new GetBasemapReadUrlResponseDto { ReadUrl = "https://blob/read" }, 200, null));
        _mapApi.ListPinsForMapAsync(worldId, mapId).Returns(new List<MapPinResponseDto>());
        _mapApi.GetLayersForMapAsync(worldId, mapId).Returns(new List<MapLayerDto>
        {
            new() { MapLayerId = _worldLayerId, Name = "World", SortOrder = 0, IsEnabled = true },
            new() { MapLayerId = _campaignLayerId, Name = "Campaign", SortOrder = 1, IsEnabled = true },
            new() { MapLayerId = _arcLayerId, Name = "Arc", SortOrder = 2, IsEnabled = true },
            new() { MapLayerId = locationsLayerId, Name = "Locations", SortOrder = 0, IsEnabled = true, ParentLayerId = _worldLayerId },
            new() { MapLayerId = harborLayerId, Name = "Harbor", SortOrder = 0, IsEnabled = true, ParentLayerId = locationsLayerId },
        });
    }

    private static List<string?> GetVisibleLayerRowIds(IRenderedComponent<MapDetail> cut) =>
        cut.FindAll(".map-page__layer-row[data-layer-id]")
            .Select(row => row.GetAttribute("data-layer-id"))
            .ToList();

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

    private static Task InvokePrivateOnRendererAsync(IRenderedComponent<MapDetail> cut, string methodName, params object?[] args)
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
