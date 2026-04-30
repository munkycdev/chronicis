using System.Diagnostics.CodeAnalysis;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using Bunit;
using Chronicis.Client.Abstractions;
using Chronicis.Client.Models;
using Chronicis.Client.Pages.Maps;
using Chronicis.Client.Services;
using Chronicis.Client.Services.Routing;
using Chronicis.Client.Tests.Components;
using Chronicis.Shared.DTOs;
using Chronicis.Shared.DTOs.Maps;
using Chronicis.Shared.Enums;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Xunit;

namespace Chronicis.Client.Tests.Pages;

public class MapListingTests : MudBlazorTestContext
{
    private readonly IMapApiService _mapApi = Substitute.For<IMapApiService>();
    private readonly IWorldApiService _worldApi = Substitute.For<IWorldApiService>();
    private readonly IArcApiService _arcApi = Substitute.For<IArcApiService>();
    private readonly ITreeStateService _treeState = Substitute.For<ITreeStateService>();

    public MapListingTests()
    {
        Services.AddSingleton(_mapApi);
        Services.AddSingleton(_worldApi);
        Services.AddSingleton(_arcApi);
        Services.AddSingleton(_treeState);
        Services.AddSingleton(Substitute.For<IAppNavigator>());
        Services.AddSingleton<IAppUrlBuilder>(new AppUrlBuilder());

        _worldApi.GetWorldAsync(Arg.Any<Guid>()).Returns(call =>
        {
            var worldId = call.Arg<Guid>();
            return new WorldDetailDto
            {
                Id = worldId,
                Slug = "test-world",
                Name = "Test World",
                Campaigns = []
            };
        });
        _mapApi.ListMapsForWorldAsync(Arg.Any<Guid>()).Returns(new List<MapSummaryDto>());
        _arcApi.GetArcsByCampaignAsync(Arg.Any<Guid>()).Returns(new List<ArcDto>());
        _treeState.RootNodes.Returns(new List<TreeNode>());
        _treeState.IsLoading.Returns(false);
        _treeState.InitializeAsync().Returns(Task.CompletedTask);
    }

    [Fact]
    public void MapListing_WhenLoading_RendersLoadingSkeleton()
    {
        var worldId = Guid.NewGuid();
        var tcs = new TaskCompletionSource<WorldDetailDto?>();
        _worldApi.GetWorldAsync(worldId).Returns(tcs.Task);

        var cut = RenderMapListing(worldId);

        Assert.Contains("chronicis-loading-skeleton", cut.Markup, StringComparison.OrdinalIgnoreCase);

        tcs.SetResult(new WorldDetailDto { Id = worldId, Name = "Loaded World", Campaigns = [] });
    }

    [Fact]
    public void MapListing_WhenWorldMissing_RendersLoadFailureAlert()
    {
        var worldId = Guid.NewGuid();
        _worldApi.GetWorldAsync(worldId).Returns((WorldDetailDto?)null);

        var cut = RenderMapListing(worldId);

        cut.WaitForAssertion(() =>
            Assert.Contains("World not found or access denied.", cut.Markup, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void MapListing_WhenWorldApiThrows_RendersLoadFailureAlert()
    {
        var worldId = Guid.NewGuid();
        _worldApi.GetWorldAsync(worldId)
            .Returns(Task.FromException<WorldDetailDto?>(new InvalidOperationException("boom")));

        var cut = RenderMapListing(worldId);

        cut.WaitForAssertion(() =>
            Assert.Contains("World not found or access denied.", cut.Markup, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void MapListing_WhenMapsLoaded_BuildsAllScopeGroupingsAndFallbackNames()
    {
        var worldId = Guid.NewGuid();
        var campaignKnownId = Guid.NewGuid();
        var campaignUnnamedId = Guid.NewGuid();
        var arcKnownId = Guid.NewGuid();
        var arcUnknownId = Guid.NewGuid();
        var unknownCampaignId = Guid.NewGuid();
        var knownWorldMapId = Guid.NewGuid();
        var unknownWorldMapId = Guid.NewGuid();
        var campaignMapId = Guid.NewGuid();
        var campaignUnknownMapId = Guid.NewGuid();
        var arcKnownMapId = Guid.NewGuid();
        var arcUnknownMapId = Guid.NewGuid();
        var mismatchedScopeMapId = Guid.NewGuid();

        _worldApi.GetWorldAsync(worldId).Returns(new WorldDetailDto
        {
            Id = worldId,
            Slug = "test-world-maps",
            Name = "",
            Campaigns =
            [
                new CampaignDto { Id = campaignKnownId, Name = "Campaign A" },
                new CampaignDto { Id = campaignUnnamedId, Name = "" },
            ]
        });

        _arcApi.GetArcsByCampaignAsync(campaignKnownId).Returns([
            new ArcDto { Id = arcKnownId, CampaignId = campaignKnownId, Name = "Arc Prime" },
            new ArcDto { Id = arcKnownId, CampaignId = campaignKnownId, Name = "Arc Prime Duplicate" },
        ]);
        _arcApi.GetArcsByCampaignAsync(campaignUnnamedId).Returns([
            new ArcDto { Id = Guid.NewGuid(), CampaignId = campaignUnnamedId, Name = "" }
        ]);

        _mapApi.ListMapsForWorldAsync(worldId).Returns(
        [
            new MapSummaryDto
            {
                WorldMapId = knownWorldMapId,
                Slug = "world-map",
                Name = "World Map",
                Scope = MapScope.WorldScoped
            },
            new MapSummaryDto
            {
                WorldMapId = unknownWorldMapId,
                Slug = "unknown-world-map",
                Name = "",
                Scope = MapScope.WorldScoped
            },
            new MapSummaryDto
            {
                WorldMapId = campaignMapId,
                Slug = "campaign-map",
                Name = "Campaign Map",
                Scope = MapScope.CampaignScoped,
                CampaignIds = [campaignKnownId]
            },
            new MapSummaryDto
            {
                WorldMapId = campaignUnknownMapId,
                Slug = "unknown-campaign-map",
                Name = "Unknown Campaign Map",
                Scope = MapScope.CampaignScoped,
                CampaignIds = [unknownCampaignId]
            },
            new MapSummaryDto
            {
                WorldMapId = arcKnownMapId,
                Slug = "arc-map",
                Name = "Arc Map",
                Scope = MapScope.ArcScoped,
                ArcIds = [arcKnownId]
            },
            new MapSummaryDto
            {
                WorldMapId = arcUnknownMapId,
                Slug = "arc-unknown-map",
                Name = "Arc Unknown",
                Scope = MapScope.ArcScoped,
                ArcIds = [arcUnknownId]
            },
            new MapSummaryDto
            {
                WorldMapId = mismatchedScopeMapId,
                Slug = "mismatched-scope-map",
                Name = "Mismatched Scope",
                Scope = MapScope.WorldScoped,
                CampaignIds = [campaignKnownId]
            }
        ]);

        var cut = RenderMapListing(worldId);

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("World Maps", cut.Markup, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("World-scoped", cut.Markup, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Campaign-scoped", cut.Markup, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Arc-scoped", cut.Markup, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Untitled Map", cut.Markup, StringComparison.OrdinalIgnoreCase);
            Assert.Contains($"Unknown Campaign ({unknownCampaignId})", cut.Markup, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Unknown Campaign", cut.Markup, StringComparison.OrdinalIgnoreCase);
            Assert.Contains($"Unknown Arc ({arcUnknownId})", cut.Markup, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("/test-world-maps/maps/world-map", cut.Markup, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public void MapListing_OnLoad_ExpandsMapsGroupInTree()
    {
        var worldId = Guid.NewGuid();
        var mapsGroupId = Guid.NewGuid();

        _treeState.RootNodes.Returns(new List<TreeNode>
        {
            new()
            {
                Id = worldId,
                NodeType = TreeNodeType.World,
                Children =
                [
                    new TreeNode
                    {
                        Id = mapsGroupId,
                        NodeType = TreeNodeType.VirtualGroup,
                        VirtualGroupType = VirtualGroupType.Maps,
                        Title = "Maps"
                    }
                ]
            }
        });

        var cut = RenderMapListing(worldId);

        cut.WaitForAssertion(() => Assert.False(GetField<bool>(cut.Instance, "_isLoading")));
        _treeState.Received(1).ExpandPathToAndSelect(mapsGroupId);
    }

    [Fact]
    public async Task OnBasemapFileSelected_WhenUnsupportedType_SetsErrorAndClearsSelection()
    {
        var cut = RenderLoadedComponent();
        var file = CreateBrowserFile("map.gif", "image/gif", 3);

        await InvokePrivateOnRendererAsync(cut, "OnBasemapFileSelected", CreateInputFileChangeArgs(file));

        Assert.Contains("Unsupported file type", GetField<string>(cut.Instance, "_createError"), StringComparison.OrdinalIgnoreCase);
        Assert.Null(GetField<IBrowserFile?>(cut.Instance, "_selectedBasemapFile"));
        Assert.Null(GetField<byte[]?>(cut.Instance, "_selectedBasemapBytes"));
    }

    [Fact]
    public async Task OnBasemapFileSelected_WhenReadFails_SetsErrorAndClearsSelection()
    {
        var cut = RenderLoadedComponent();
        var file = CreateBrowserFile("map.png", "image/png", 3, throwOnRead: new InvalidOperationException("stream failed"));

        await InvokePrivateOnRendererAsync(cut, "OnBasemapFileSelected", CreateInputFileChangeArgs(file));

        Assert.Contains("Failed to read selected file", GetField<string>(cut.Instance, "_createError"), StringComparison.OrdinalIgnoreCase);
        Assert.Null(GetField<IBrowserFile?>(cut.Instance, "_selectedBasemapFile"));
        Assert.Null(GetField<byte[]?>(cut.Instance, "_selectedBasemapBytes"));
    }

    [Fact]
    public async Task OnBasemapFileSelected_WhenValidType_ReadsBytes()
    {
        var cut = RenderLoadedComponent();
        var file = CreateBrowserFile("map.webp", "image/webp", 4, bytes: [1, 2, 3, 4]);

        await InvokePrivateOnRendererAsync(cut, "OnBasemapFileSelected", CreateInputFileChangeArgs(file));

        Assert.Equal(string.Empty, GetField<string>(cut.Instance, "_createError"));
        Assert.Same(file, GetField<IBrowserFile?>(cut.Instance, "_selectedBasemapFile"));
        Assert.Equal([1, 2, 3, 4], GetField<byte[]?>(cut.Instance, "_selectedBasemapBytes"));
    }

    [Fact]
    public async Task CreateMapWithBasemap_WhenNameMissing_SetsValidationError()
    {
        var cut = RenderLoadedComponent();
        SetField(cut.Instance, "_newMapName", " ");
        SetField(cut.Instance, "_selectedBasemapFile", CreateBrowserFile("map.png", "image/png", 3));
        SetField(cut.Instance, "_selectedBasemapBytes", new byte[] { 1, 2, 3 });

        await InvokePrivateOnRendererAsync(cut, "CreateMapWithBasemapAsync");

        Assert.Equal("Map name is required.", GetField<string>(cut.Instance, "_createError"));
        await _mapApi.DidNotReceive().CreateMapAsync(Arg.Any<Guid>(), Arg.Any<MapCreateDto>());
    }

    [Fact]
    public async Task CreateMapWithBasemap_WhenFileMissing_SetsValidationError()
    {
        var cut = RenderLoadedComponent();
        SetField(cut.Instance, "_newMapName", "My Map");
        SetField(cut.Instance, "_selectedBasemapFile", null);
        SetField(cut.Instance, "_selectedBasemapBytes", null);

        await InvokePrivateOnRendererAsync(cut, "CreateMapWithBasemapAsync");

        Assert.Equal("Basemap file is required.", GetField<string>(cut.Instance, "_createError"));
    }

    [Fact]
    public async Task CreateMapWithBasemap_WhenTypeUnsupported_SetsValidationError()
    {
        var cut = RenderLoadedComponent();
        SetField(cut.Instance, "_newMapName", "My Map");
        SetField(cut.Instance, "_selectedBasemapFile", CreateBrowserFile("map.gif", "image/gif", 3));
        SetField(cut.Instance, "_selectedBasemapBytes", new byte[] { 1, 2, 3 });

        await InvokePrivateOnRendererAsync(cut, "CreateMapWithBasemapAsync");

        Assert.Contains("Unsupported file type", GetField<string>(cut.Instance, "_createError"), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateMapWithBasemap_WhenCreateMapReturnsNull_SetsError()
    {
        var cut = RenderLoadedComponent();
        var worldId = cut.Instance.WorldId;
        var file = CreateBrowserFile("map.png", "image/png", 3);

        SetField(cut.Instance, "_newMapName", "My Map");
        SetField(cut.Instance, "_selectedBasemapFile", file);
        SetField(cut.Instance, "_selectedBasemapBytes", new byte[] { 1, 2, 3 });
        _mapApi.CreateMapAsync(worldId, Arg.Any<MapCreateDto>()).Returns((MapDto?)null);

        await InvokePrivateOnRendererAsync(cut, "CreateMapWithBasemapAsync");

        Assert.Equal("Failed to create map record.", GetField<string>(cut.Instance, "_createError"));
    }

    [Fact]
    public async Task CreateMapWithBasemap_WhenRequestUploadReturnsNull_SetsError()
    {
        var cut = RenderLoadedComponent();
        var worldId = cut.Instance.WorldId;
        var mapId = Guid.NewGuid();
        var file = CreateBrowserFile("map.png", "image/png", 3);

        SetField(cut.Instance, "_newMapName", "My Map");
        SetField(cut.Instance, "_selectedBasemapFile", file);
        SetField(cut.Instance, "_selectedBasemapBytes", new byte[] { 1, 2, 3 });

        _mapApi.CreateMapAsync(worldId, Arg.Any<MapCreateDto>()).Returns(new MapDto { WorldMapId = mapId, Name = "My Map" });
        _mapApi.RequestBasemapUploadAsync(worldId, mapId, Arg.Any<RequestBasemapUploadDto>())
            .Returns((RequestBasemapUploadResponseDto?)null);

        await InvokePrivateOnRendererAsync(cut, "CreateMapWithBasemapAsync");

        Assert.Equal("Failed to request basemap upload URL.", GetField<string>(cut.Instance, "_createError"));
    }

    [Fact]
    public async Task CreateMapWithBasemap_WhenUploadFails_SetsError()
    {
        var cut = RenderLoadedComponent();
        var worldId = cut.Instance.WorldId;
        var mapId = Guid.NewGuid();
        var file = CreateBrowserFile("map.jpeg", "image/jpeg", 3);

        using var endpoint = StartUploadEndpoint(500, "Internal Server Error");

        SetField(cut.Instance, "_newMapName", "My Map");
        SetField(cut.Instance, "_selectedBasemapFile", file);
        SetField(cut.Instance, "_selectedBasemapBytes", new byte[] { 1, 2, 3 });

        _mapApi.CreateMapAsync(worldId, Arg.Any<MapCreateDto>()).Returns(new MapDto { WorldMapId = mapId, Name = "My Map" });
        _mapApi.RequestBasemapUploadAsync(worldId, mapId, Arg.Any<RequestBasemapUploadDto>())
            .Returns(new RequestBasemapUploadResponseDto { UploadUrl = endpoint.Url });

        await InvokePrivateOnRendererAsync(cut, "CreateMapWithBasemapAsync");
        await endpoint.RequestHandled;

        Assert.Contains("Basemap upload failed", GetField<string>(cut.Instance, "_createError"), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateMapWithBasemap_WhenConfirmReturnsNull_SetsError()
    {
        var cut = RenderLoadedComponent();
        var worldId = cut.Instance.WorldId;
        var mapId = Guid.NewGuid();
        var file = CreateBrowserFile("map.png", "image/png", 3);

        using var endpoint = StartUploadEndpoint(201, "Created");

        SetField(cut.Instance, "_newMapName", "My Map");
        SetField(cut.Instance, "_selectedBasemapFile", file);
        SetField(cut.Instance, "_selectedBasemapBytes", new byte[] { 1, 2, 3 });

        _mapApi.CreateMapAsync(worldId, Arg.Any<MapCreateDto>()).Returns(new MapDto { WorldMapId = mapId, Name = "My Map" });
        _mapApi.RequestBasemapUploadAsync(worldId, mapId, Arg.Any<RequestBasemapUploadDto>())
            .Returns(new RequestBasemapUploadResponseDto { UploadUrl = endpoint.Url });
        _mapApi.ConfirmBasemapUploadAsync(worldId, mapId, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns((MapDto?)null);

        await InvokePrivateOnRendererAsync(cut, "CreateMapWithBasemapAsync");
        await endpoint.RequestHandled;

        Assert.Equal("Failed to confirm basemap upload.", GetField<string>(cut.Instance, "_createError"));
    }

    [Fact]
    public async Task CreateMapWithBasemap_WhenCreateThrows_SetsErrorAndResetsState()
    {
        var cut = RenderLoadedComponent();
        var worldId = cut.Instance.WorldId;
        var file = CreateBrowserFile("map.png", "image/png", 3);

        SetField(cut.Instance, "_newMapName", "My Map");
        SetField(cut.Instance, "_selectedBasemapFile", file);
        SetField(cut.Instance, "_selectedBasemapBytes", new byte[] { 1, 2, 3 });

        _mapApi.CreateMapAsync(worldId, Arg.Any<MapCreateDto>())
            .Returns(Task.FromException<MapDto?>(new InvalidOperationException("explode")));

        await InvokePrivateOnRendererAsync(cut, "CreateMapWithBasemapAsync");

        Assert.Contains("Failed to create map: explode", GetField<string>(cut.Instance, "_createError"), StringComparison.OrdinalIgnoreCase);
        Assert.False(GetField<bool>(cut.Instance, "_isCreatingMap"));
    }

    [Fact]
    public async Task CreateMapWithBasemap_WhenSuccessful_SetsSuccessAndReloadsMaps()
    {
        var worldId = Guid.NewGuid();
        var mapId = Guid.NewGuid();
        var file = CreateBrowserFile("success-map.png", "image/png", 3);

        _worldApi.GetWorldAsync(worldId).Returns(new WorldDetailDto { Id = worldId, Slug = "success-world", Name = "World", Campaigns = [] });
        _mapApi.ListMapsForWorldAsync(worldId).Returns(
            new List<MapSummaryDto>(),
            [new MapSummaryDto { WorldMapId = mapId, Name = "Success Map", Scope = MapScope.WorldScoped }]);

        using var endpoint = StartUploadEndpoint(201, "Created");
        _mapApi.CreateMapAsync(worldId, Arg.Any<MapCreateDto>())
            .Returns(new MapDto { WorldMapId = mapId, Name = "Success Map" });
        _mapApi.RequestBasemapUploadAsync(worldId, mapId, Arg.Any<RequestBasemapUploadDto>())
            .Returns(new RequestBasemapUploadResponseDto { UploadUrl = endpoint.Url });
        _mapApi.ConfirmBasemapUploadAsync(worldId, mapId, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(new MapDto { WorldMapId = mapId, Name = "Success Map" });

        var cut = RenderMapListing(worldId);
        cut.WaitForAssertion(() => Assert.False(GetField<bool>(cut.Instance, "_isLoading")));

        SetField(cut.Instance, "_newMapName", "Success Map");
        SetField(cut.Instance, "_selectedBasemapFile", file);
        SetField(cut.Instance, "_selectedBasemapBytes", new byte[] { 1, 2, 3 });

        await InvokePrivateOnRendererAsync(cut, "CreateMapWithBasemapAsync");
        await endpoint.RequestHandled;

        Assert.Equal("Map 'Success Map' created.", GetField<string>(cut.Instance, "_createSuccess"));
        Assert.Equal(string.Empty, GetField<string>(cut.Instance, "_newMapName"));
        Assert.Null(GetField<IBrowserFile?>(cut.Instance, "_selectedBasemapFile"));
        Assert.Null(GetField<byte[]?>(cut.Instance, "_selectedBasemapBytes"));
        await _mapApi.Received(2).ListMapsForWorldAsync(worldId);

        await _mapApi.Received(1).ConfirmBasemapUploadAsync(
            worldId,
            mapId,
            $"maps/{mapId}/basemap/{file.Name}",
            file.ContentType,
            file.Name);
    }

    [Fact]
    public async Task DeleteMapAsync_WhenDeleteFails_SetsErrorAndClearsDeleteState()
    {
        var cut = RenderLoadedComponent();
        var worldId = cut.Instance.WorldId;
        var mapId = Guid.NewGuid();

        _mapApi.DeleteMapAsync(worldId, mapId).Returns(false);

        await InvokePrivateOnRendererAsync(cut, "DeleteMapAsync", mapId, "Failed Map");

        Assert.Contains("Failed to delete map 'Failed Map'.", GetField<string>(cut.Instance, "_createError"), StringComparison.Ordinal);
        Assert.DoesNotContain(mapId, GetField<HashSet<Guid>>(cut.Instance, "_mapsBeingDeleted"));
        await _mapApi.Received(1).DeleteMapAsync(worldId, mapId);
    }

    [Fact]
    public async Task DeleteMapAsync_WhenSuccessful_SetsSuccessAndReloadsMaps()
    {
        var worldId = Guid.NewGuid();
        var mapId = Guid.NewGuid();

        _worldApi.GetWorldAsync(worldId).Returns(new WorldDetailDto { Id = worldId, Slug = "delete-world", Name = "World", Campaigns = [] });
        _mapApi.ListMapsForWorldAsync(worldId).Returns(
            [new MapSummaryDto { WorldMapId = mapId, Slug = "delete-me-map", Name = "Delete Me", Scope = MapScope.WorldScoped }],
            new List<MapSummaryDto>());
        _mapApi.DeleteMapAsync(worldId, mapId).Returns(true);

        var cut = RenderMapListing(worldId);
        cut.WaitForAssertion(() => Assert.False(GetField<bool>(cut.Instance, "_isLoading")));

        await InvokePrivateOnRendererAsync(cut, "DeleteMapAsync", mapId, "Delete Me");

        Assert.Equal("Map 'Delete Me' deleted permanently.", GetField<string>(cut.Instance, "_createSuccess"));
        Assert.DoesNotContain(mapId, GetField<HashSet<Guid>>(cut.Instance, "_mapsBeingDeleted"));
        await _mapApi.Received(1).DeleteMapAsync(worldId, mapId);
        await _mapApi.Received(2).ListMapsForWorldAsync(worldId);
    }

    [Fact]
    public void PrivateHelpers_CoverScopeAndDisplayBranches()
    {
        var detailType = typeof(MapListing);
        var deriveScope = detailType.GetMethod("DeriveScope", BindingFlags.Static | BindingFlags.NonPublic);
        var resolveScope = detailType.GetMethod("ResolveScope", BindingFlags.Static | BindingFlags.NonPublic);
        var getDisplayName = detailType.GetMethod("GetDisplayName", BindingFlags.Static | BindingFlags.NonPublic);
        var getMapRoute = detailType.GetMethod("GetMapRoute", BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.NotNull(deriveScope);
        Assert.NotNull(resolveScope);
        Assert.NotNull(getDisplayName);
        Assert.NotNull(getMapRoute);

        var arcMap = new MapSummaryDto
        {
            Scope = MapScope.ArcScoped,
            ArcIds = [Guid.NewGuid()]
        };
        var campaignMap = new MapSummaryDto
        {
            Scope = MapScope.CampaignScoped,
            CampaignIds = [Guid.NewGuid()]
        };
        var worldMap = new MapSummaryDto
        {
            Scope = MapScope.WorldScoped
        };
        var mismatched = new MapSummaryDto
        {
            Scope = MapScope.WorldScoped,
            CampaignIds = [Guid.NewGuid()]
        };

        Assert.Equal(MapScope.ArcScoped, (MapScope)deriveScope!.Invoke(null, [arcMap])!);
        Assert.Equal(MapScope.CampaignScoped, (MapScope)deriveScope.Invoke(null, [campaignMap])!);
        Assert.Equal(MapScope.WorldScoped, (MapScope)deriveScope.Invoke(null, [worldMap])!);

        Assert.Equal(MapScope.WorldScoped, (MapScope)resolveScope!.Invoke(null, [worldMap])!);
        Assert.Equal(MapScope.CampaignScoped, (MapScope)resolveScope.Invoke(null, [mismatched])!);

        Assert.Equal("Fallback", (string)getDisplayName!.Invoke(null, [string.Empty, "Fallback"])!);
        Assert.Equal("Actual", (string)getDisplayName.Invoke(null, ["Actual", "Fallback"])!);

        var cut = RenderLoadedComponent();
        var route = (string)getMapRoute!.Invoke(cut.Instance, ["test-map-slug"])!;
        Assert.Equal("/test-world/maps/test-map-slug", route);
    }

    [Fact]
    public async Task BasemapDropzoneHelpers_UpdateStateAndClasses()
    {
        var cut = RenderLoadedComponent();
        var detailType = typeof(MapListing);
        var dragEnter = detailType.GetMethod("OnBasemapDragEnter", BindingFlags.Instance | BindingFlags.NonPublic);
        var dragOver = detailType.GetMethod("OnBasemapDragOver", BindingFlags.Instance | BindingFlags.NonPublic);
        var dragLeave = detailType.GetMethod("OnBasemapDragLeave", BindingFlags.Instance | BindingFlags.NonPublic);
        var drop = detailType.GetMethod("OnBasemapDrop", BindingFlags.Instance | BindingFlags.NonPublic);
        var dropClass = detailType.GetMethod("GetBasemapDropZoneClass", BindingFlags.Instance | BindingFlags.NonPublic);
        var inputStyle = detailType.GetMethod("GetBasemapInputStyle", BindingFlags.Instance | BindingFlags.NonPublic);
        var isFileDrag = detailType.GetMethod("IsFileDragEvent", BindingFlags.Static | BindingFlags.NonPublic);

        Assert.NotNull(dragEnter);
        Assert.NotNull(dragOver);
        Assert.NotNull(dragLeave);
        Assert.NotNull(drop);
        Assert.NotNull(dropClass);
        Assert.NotNull(inputStyle);
        Assert.NotNull(isFileDrag);

        var fileArgs = new DragEventArgs
        {
            DataTransfer = new DataTransfer
            {
                Types = ["Files"],
                Files = ["map.png"]
            }
        };
        var nonFileArgs = new DragEventArgs
        {
            DataTransfer = new DataTransfer
            {
                Types = ["text/plain"]
            }
        };
        var typedFilesArgs = new DragEventArgs
        {
            DataTransfer = new DataTransfer
            {
                Types = ["files"],
                Files = []
            }
        };
        var emptyTransferArgs = new DragEventArgs
        {
            DataTransfer = new DataTransfer
            {
                Files = []
            }
        };
        var nullTransferArgs = new DragEventArgs
        {
            DataTransfer = new DataTransfer
            {
                Files = null!,
                Types = null!
            }
        };

        var defaultClass = (string)dropClass!.Invoke(cut.Instance, null)!;
        Assert.Contains("maps-basemap-dropzone", defaultClass, StringComparison.Ordinal);
        Assert.DoesNotContain("maps-basemap-dropzone--dragover", defaultClass, StringComparison.Ordinal);
        var pointerStyle = (string)inputStyle!.Invoke(cut.Instance, null)!;
        Assert.Contains("cursor:pointer", pointerStyle, StringComparison.Ordinal);

        await cut.InvokeAsync(() => dragEnter!.Invoke(cut.Instance, [fileArgs]));
        Assert.True(GetField<bool>(cut.Instance, "_isBasemapDragOver"));
        Assert.Equal("copy", fileArgs.DataTransfer!.DropEffect);
        Assert.Equal("copy", fileArgs.DataTransfer.EffectAllowed);

        var dragOverClass = (string)dropClass.Invoke(cut.Instance, null)!;
        Assert.Contains("maps-basemap-dropzone--dragover", dragOverClass, StringComparison.Ordinal);
        Assert.Contains("maps-basemap-dropzone--copy", dragOverClass, StringComparison.Ordinal);
        var copyStyle = (string)inputStyle.Invoke(cut.Instance, null)!;
        Assert.Contains("cursor:copy", copyStyle, StringComparison.Ordinal);

        var emptyArgs = new DragEventArgs();
        await cut.InvokeAsync(() => dragOver.Invoke(cut.Instance, [emptyArgs]));
        Assert.False(GetField<bool>(cut.Instance, "_isBasemapDragOver"));

        await cut.InvokeAsync(() => dragEnter.Invoke(cut.Instance, [nonFileArgs]));
        Assert.False(GetField<bool>(cut.Instance, "_isBasemapDragOver"));

        await cut.InvokeAsync(() => dragEnter.Invoke(cut.Instance, [nullTransferArgs]));
        Assert.False(GetField<bool>(cut.Instance, "_isBasemapDragOver"));

        await cut.InvokeAsync(() => dragEnter.Invoke(cut.Instance, [typedFilesArgs]));
        Assert.True(GetField<bool>(cut.Instance, "_isBasemapDragOver"));
        Assert.Equal("copy", typedFilesArgs.DataTransfer!.DropEffect);
        Assert.Equal("copy", typedFilesArgs.DataTransfer.EffectAllowed);

        SetField(cut.Instance, "_isBasemapDragOver", false);
        SetField(cut.Instance, "_isCreatingMap", true);

        await cut.InvokeAsync(() => dragEnter.Invoke(cut.Instance, [fileArgs]));
        Assert.False(GetField<bool>(cut.Instance, "_isBasemapDragOver"));

        await cut.InvokeAsync(() => dragOver!.Invoke(cut.Instance, [fileArgs]));
        Assert.False(GetField<bool>(cut.Instance, "_isBasemapDragOver"));

        var disabledClass = (string)dropClass.Invoke(cut.Instance, null)!;
        Assert.Contains("maps-basemap-dropzone--disabled", disabledClass, StringComparison.Ordinal);

        SetField(cut.Instance, "_isCreatingMap", false);
        await cut.InvokeAsync(() => dragOver.Invoke(cut.Instance, [fileArgs]));
        Assert.True(GetField<bool>(cut.Instance, "_isBasemapDragOver"));
        Assert.Equal("copy", fileArgs.DataTransfer.DropEffect);
        Assert.Equal("copy", fileArgs.DataTransfer.EffectAllowed);

        await cut.InvokeAsync(() => dragLeave!.Invoke(cut.Instance, [fileArgs]));
        Assert.False(GetField<bool>(cut.Instance, "_isBasemapDragOver"));

        SetField(cut.Instance, "_isBasemapDragOver", true);
        await cut.InvokeAsync(() => drop!.Invoke(cut.Instance, [fileArgs]));
        Assert.False(GetField<bool>(cut.Instance, "_isBasemapDragOver"));

        Assert.True((bool)isFileDrag!.Invoke(null, [fileArgs])!);
        Assert.False((bool)isFileDrag.Invoke(null, [nonFileArgs])!);
        Assert.True((bool)isFileDrag.Invoke(null, [typedFilesArgs])!);
        Assert.False((bool)isFileDrag.Invoke(null, [emptyTransferArgs])!);
        Assert.False((bool)isFileDrag.Invoke(null, [nullTransferArgs])!);
        Assert.False((bool)isFileDrag.Invoke(null, [emptyArgs])!);
    }

    private IRenderedComponent<MapListing> RenderLoadedComponent()
    {
        var worldId = Guid.NewGuid();
        var cut = RenderMapListing(worldId);
        cut.WaitForAssertion(() => Assert.False(GetField<bool>(cut.Instance, "_isLoading")));
        return cut;
    }

    private IRenderedComponent<MapListing> RenderMapListing(Guid worldId)
    {
        return RenderComponent<MapListing>(parameters =>
            parameters.Add(x => x.WorldId, worldId));
    }

    private static T GetField<T>(object instance, string fieldName)
    {
        var field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        return (T)field!.GetValue(instance)!;
    }

    private static void SetField(object instance, string fieldName, object? value)
    {
        var field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        field!.SetValue(instance, value);
    }

    private static Task InvokePrivateOnRendererAsync(IRenderedComponent<MapListing> cut, string methodName, params object?[] args)
    {
        return cut.InvokeAsync(async () =>
        {
            var method = cut.Instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(method);
            var result = method!.Invoke(cut.Instance, args);
            if (result is Task task)
            {
                await task;
            }
        });
    }

    private static IBrowserFile CreateBrowserFile(
        string name,
        string contentType,
        long size,
        byte[]? bytes = null,
        Exception? throwOnRead = null)
    {
        var file = Substitute.For<IBrowserFile>();
        file.Name.Returns(name);
        file.ContentType.Returns(contentType);
        file.Size.Returns(size);
        file.OpenReadStream(Arg.Any<long>(), Arg.Any<CancellationToken>()).Returns(_ =>
        {
            if (throwOnRead != null)
            {
                throw throwOnRead;
            }

            return new MemoryStream(bytes ?? [1, 2, 3]);
        });
        return file;
    }

    private static InputFileChangeEventArgs CreateInputFileChangeArgs(IBrowserFile? file)
    {
        var constructors = typeof(InputFileChangeEventArgs)
            .GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        var singleFileCtor = constructors.FirstOrDefault(c =>
        {
            var parameters = c.GetParameters();
            return parameters.Length == 1 && parameters[0].ParameterType == typeof(IBrowserFile);
        });
        if (singleFileCtor != null)
        {
            return (InputFileChangeEventArgs)singleFileCtor.Invoke([file]);
        }

        var listCtor = constructors.FirstOrDefault(c =>
        {
            var parameters = c.GetParameters();
            return parameters.Length == 1 && typeof(IReadOnlyList<IBrowserFile>).IsAssignableFrom(parameters[0].ParameterType);
        });
        if (listCtor != null)
        {
            var list = file == null ? new List<IBrowserFile>() : new List<IBrowserFile> { file };
            return (InputFileChangeEventArgs)listCtor.Invoke([list]);
        }

        throw new InvalidOperationException("No supported InputFileChangeEventArgs constructor found.");
    }

    private static TestUploadEndpoint StartUploadEndpoint(int statusCode, string reasonPhrase)
    {
        var listener = new TcpListener(System.Net.IPAddress.Loopback, 0);
        listener.Start();
        var port = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        _ = Task.Run(async () =>
        {
            try
            {
                using var client = await listener.AcceptTcpClientAsync();
                using var stream = client.GetStream();
                using var reader = new StreamReader(stream, Encoding.ASCII, leaveOpen: true);

                string? line;
                while (!string.IsNullOrEmpty(line = await reader.ReadLineAsync()))
                {
                }

                var response = $"HTTP/1.1 {statusCode} {reasonPhrase}\r\nContent-Length: 0\r\nConnection: close\r\n\r\n";
                var responseBytes = Encoding.ASCII.GetBytes(response);
                await stream.WriteAsync(responseBytes);
                await stream.FlushAsync();
                tcs.TrySetResult();
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
            finally
            {
                listener.Stop();
            }
        });

        return new TestUploadEndpoint($"http://127.0.0.1:{port}/upload", tcs.Task, listener);
    }

    [ExcludeFromCodeCoverage]
    private sealed record TestUploadEndpoint(string Url, Task RequestHandled, TcpListener Listener) : IDisposable
    {
        public void Dispose()
        {
            Listener.Stop();
        }
    }
}

