using System.Net;
using Chronicis.Client.Services;
using Chronicis.Shared.DTOs.Maps;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Chronicis.Client.Tests.Services;

public class MapApiServiceTests
{
    private static MapApiService CreateSut(TestHttpMessageHandler handler) =>
        new(new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") }, NullLogger<MapApiService>.Instance);

    [Fact]
    public async Task ListMapsForWorldAsync_UsesExpectedRoute()
    {
        var calls = new List<string>();
        var handler = new TestHttpMessageHandler((req, _) =>
        {
            calls.Add($"{req.Method} {req.RequestUri!.PathAndQuery.TrimStart('/')}");
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("[]")
            });
        });

        var http = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
        var sut = new MapApiService(http, NullLogger<MapApiService>.Instance);
        var worldId = Guid.NewGuid();

        await sut.ListMapsForWorldAsync(worldId);

        Assert.Contains(calls, c => c == $"GET world/{worldId}/maps");
    }

    [Fact]
    public async Task GetMapAsync_Success_UsesRouteAndReturnsDto()
    {
        var worldId = Guid.NewGuid();
        var mapId = Guid.NewGuid();
        var handler = new TestHttpMessageHandler((req, _) =>
        {
            Assert.Equal($"world/{worldId}/maps/{mapId}", req.RequestUri!.PathAndQuery.TrimStart('/'));
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    $$"""{"worldMapId":"{{mapId}}","worldId":"{{worldId}}","name":"Map A","hasBasemap":true}""")
            });
        });

        var sut = CreateSut(handler);

        var result = await sut.GetMapAsync(worldId, mapId);

        Assert.NotNull(result.Map);
        Assert.Equal("Map A", result.Map!.Name);
        Assert.Equal(200, result.StatusCode);
        Assert.Null(result.Error);
    }

    [Fact]
    public async Task GetBasemapReadUrlAsync_NotFound_ReturnsStatusAndError()
    {
        var worldId = Guid.NewGuid();
        var mapId = Guid.NewGuid();
        var handler = new TestHttpMessageHandler((req, _) =>
        {
            Assert.Equal($"world/{worldId}/maps/{mapId}/basemap", req.RequestUri!.PathAndQuery.TrimStart('/'));
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = new StringContent("""{"error":"Basemap not found for this map"}""")
            });
        });

        var sut = CreateSut(handler);

        var result = await sut.GetBasemapReadUrlAsync(worldId, mapId);

        Assert.Null(result.Basemap);
        Assert.Equal(404, result.StatusCode);
        Assert.Equal("Basemap not found for this map", result.Error);
    }

    [Fact]
    public async Task GetBasemapReadUrlAsync_InvalidJsonError_ReturnsNullError()
    {
        var sut = CreateSut(new TestHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = new StringContent("{invalid-json")
            })));

        var result = await sut.GetBasemapReadUrlAsync(Guid.NewGuid(), Guid.NewGuid());

        Assert.Null(result.Basemap);
        Assert.Equal(404, result.StatusCode);
        Assert.Null(result.Error);
    }

    [Fact]
    public async Task GetBasemapReadUrlAsync_NonJsonError_ReturnsNullError()
    {
        var content = new StringContent("plain text");
        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/plain");

        var sut = CreateSut(new TestHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = content
            })));

        var result = await sut.GetBasemapReadUrlAsync(Guid.NewGuid(), Guid.NewGuid());

        Assert.Null(result.Basemap);
        Assert.Equal(404, result.StatusCode);
        Assert.Null(result.Error);
    }

    [Fact]
    public async Task GetBasemapReadUrlAsync_Success_ReturnsDto()
    {
        var worldId = Guid.NewGuid();
        var mapId = Guid.NewGuid();
        var handler = new TestHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"readUrl":"https://blob.example.com/read"}""")
            }));

        var sut = CreateSut(handler);

        var result = await sut.GetBasemapReadUrlAsync(worldId, mapId);

        Assert.NotNull(result.Basemap);
        Assert.Equal("https://blob.example.com/read", result.Basemap!.ReadUrl);
        Assert.Equal(200, result.StatusCode);
    }

    [Fact]
    public async Task GetMapAsync_WhenHttpThrows_ReturnsNullAndError()
    {
        var sut = CreateSut(new TestHttpMessageHandler((_, _) => throw new HttpRequestException("network")));

        var result = await sut.GetMapAsync(Guid.NewGuid(), Guid.NewGuid());

        Assert.Null(result.Map);
        Assert.Null(result.StatusCode);
        Assert.Equal("network", result.Error);
    }

    [Fact]
    public async Task UpdateMapAsync_UsesExpectedRoute()
    {
        var calls = new List<string>();
        var worldId = Guid.NewGuid();
        var mapId = Guid.NewGuid();

        var handler = new TestHttpMessageHandler((req, _) =>
        {
            calls.Add($"{req.Method} {req.RequestUri!.PathAndQuery.TrimStart('/')}");
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    $$"""{"worldMapId":"{{mapId}}","worldId":"{{worldId}}","name":"Renamed","hasBasemap":true}""")
            });
        });

        var sut = CreateSut(handler);
        var result = await sut.UpdateMapAsync(worldId, mapId, new MapUpdateDto { Name = "Renamed" });

        Assert.NotNull(result);
        Assert.Equal("Renamed", result!.Name);
        Assert.Contains(calls, c => c == $"PUT world/{worldId}/maps/{mapId}");
    }

    [Fact]
    public async Task DeleteMapAsync_UsesExpectedRoute()
    {
        var calls = new List<string>();
        var worldId = Guid.NewGuid();
        var mapId = Guid.NewGuid();

        var handler = new TestHttpMessageHandler((req, _) =>
        {
            calls.Add($"{req.Method} {req.RequestUri!.PathAndQuery.TrimStart('/')}");
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent));
        });

        var sut = CreateSut(handler);

        var result = await sut.DeleteMapAsync(worldId, mapId);

        Assert.True(result);
        Assert.Contains(calls, c => c == $"DELETE world/{worldId}/maps/{mapId}");
    }
}
