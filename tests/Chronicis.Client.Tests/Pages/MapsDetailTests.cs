using System.Diagnostics.CodeAnalysis;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using Bunit;
using Chronicis.Client.Pages.Maps;
using Chronicis.Client.Services;
using Chronicis.Client.Tests.Components;
using Chronicis.Shared.DTOs;
using Chronicis.Shared.DTOs.Maps;
using Chronicis.Shared.Enums;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Xunit;

namespace Chronicis.Client.Tests.Pages;

public class MapsDetailTests : MudBlazorTestContext
{
    private readonly IMapApiService _mapApi = Substitute.For<IMapApiService>();
    private readonly IWorldApiService _worldApi = Substitute.For<IWorldApiService>();
    private readonly IArcApiService _arcApi = Substitute.For<IArcApiService>();

    public MapsDetailTests()
    {
        Services.AddSingleton(_mapApi);
        Services.AddSingleton(_worldApi);
        Services.AddSingleton(_arcApi);

        _worldApi.GetWorldAsync(Arg.Any<Guid>()).Returns(call =>
        {
            var worldId = call.Arg<Guid>();
            return new WorldDetailDto
            {
                Id = worldId,
                Name = "Test World",
                Campaigns = []
            };
        });
        _mapApi.ListMapsForWorldAsync(Arg.Any<Guid>()).Returns(new List<MapSummaryDto>());
        _arcApi.GetArcsByCampaignAsync(Arg.Any<Guid>()).Returns(new List<ArcDto>());
    }

    [Fact]
    public void MapsDetail_WhenLoading_RendersLoadingSkeleton()
    {
        var worldId = Guid.NewGuid();
        var tcs = new TaskCompletionSource<WorldDetailDto?>();
        _worldApi.GetWorldAsync(worldId).Returns(tcs.Task);

        var cut = RenderMapsDetail(worldId);

        Assert.Contains("chronicis-loading-skeleton", cut.Markup, StringComparison.OrdinalIgnoreCase);

        tcs.SetResult(new WorldDetailDto { Id = worldId, Name = "Loaded World", Campaigns = [] });
    }

    [Fact]
    public void MapsDetail_WhenWorldMissing_RendersLoadFailureAlert()
    {
        var worldId = Guid.NewGuid();
        _worldApi.GetWorldAsync(worldId).Returns((WorldDetailDto?)null);

        var cut = RenderMapsDetail(worldId);

        cut.WaitForAssertion(() =>
            Assert.Contains("World not found or access denied.", cut.Markup, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void MapsDetail_WhenWorldApiThrows_RendersLoadFailureAlert()
    {
        var worldId = Guid.NewGuid();
        _worldApi.GetWorldAsync(worldId)
            .Returns(Task.FromException<WorldDetailDto?>(new InvalidOperationException("boom")));

        var cut = RenderMapsDetail(worldId);

        cut.WaitForAssertion(() =>
            Assert.Contains("World not found or access denied.", cut.Markup, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void MapsDetail_WhenMapsLoaded_BuildsAllScopeGroupingsAndFallbackNames()
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
                Name = "World Map",
                Scope = MapScope.WorldScoped
            },
            new MapSummaryDto
            {
                WorldMapId = unknownWorldMapId,
                Name = "",
                Scope = MapScope.WorldScoped
            },
            new MapSummaryDto
            {
                WorldMapId = campaignMapId,
                Name = "Campaign Map",
                Scope = MapScope.CampaignScoped,
                CampaignIds = [campaignKnownId]
            },
            new MapSummaryDto
            {
                WorldMapId = campaignUnknownMapId,
                Name = "Unknown Campaign Map",
                Scope = MapScope.CampaignScoped,
                CampaignIds = [unknownCampaignId]
            },
            new MapSummaryDto
            {
                WorldMapId = arcKnownMapId,
                Name = "Arc Map",
                Scope = MapScope.ArcScoped,
                ArcIds = [arcKnownId]
            },
            new MapSummaryDto
            {
                WorldMapId = arcUnknownMapId,
                Name = "Arc Unknown",
                Scope = MapScope.ArcScoped,
                ArcIds = [arcUnknownId]
            },
            new MapSummaryDto
            {
                WorldMapId = mismatchedScopeMapId,
                Name = "Mismatched Scope",
                Scope = MapScope.WorldScoped,
                CampaignIds = [campaignKnownId]
            }
        ]);

        var cut = RenderMapsDetail(worldId);

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
            Assert.Contains($"/world/{worldId}/maps/{knownWorldMapId}", cut.Markup, StringComparison.OrdinalIgnoreCase);
        });
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

        _worldApi.GetWorldAsync(worldId).Returns(new WorldDetailDto { Id = worldId, Name = "World", Campaigns = [] });
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

        var cut = RenderMapsDetail(worldId);
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
    public void PrivateHelpers_CoverScopeAndDisplayBranches()
    {
        var detailType = typeof(MapsDetail);
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
        var route = (string)getMapRoute!.Invoke(cut.Instance, [Guid.Empty])!;
        Assert.Equal($"/world/{cut.Instance.WorldId}/maps/{Guid.Empty}", route);
    }

    private IRenderedComponent<MapsDetail> RenderLoadedComponent()
    {
        var worldId = Guid.NewGuid();
        var cut = RenderMapsDetail(worldId);
        cut.WaitForAssertion(() => Assert.False(GetField<bool>(cut.Instance, "_isLoading")));
        return cut;
    }

    private IRenderedComponent<MapsDetail> RenderMapsDetail(Guid worldId)
    {
        return RenderComponent<MapsDetail>(parameters =>
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

    private static Task InvokePrivateOnRendererAsync(IRenderedComponent<MapsDetail> cut, string methodName, params object?[] args)
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
