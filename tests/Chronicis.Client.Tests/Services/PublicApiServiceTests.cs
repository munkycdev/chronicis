using System.Net;
using Chronicis.Client.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Chronicis.Client.Tests.Services;

public class PublicApiServiceTests
{
    [Fact]
    public async Task GetPublicWorldAsync_ReturnsNull_OnNotFoundOrError()
    {
        var notFound = new PublicApiService(TestHttpMessageHandler.CreateClient(HttpStatusCode.NotFound), NullLogger<PublicApiService>.Instance);
        var error = new PublicApiService(TestHttpMessageHandler.CreateClient(HttpStatusCode.BadRequest), NullLogger<PublicApiService>.Instance);
        var ex = new PublicApiService(new HttpClient(new TestHttpMessageHandler((_, _) => throw new InvalidOperationException("boom"))) { BaseAddress = new Uri("http://localhost/") }, NullLogger<PublicApiService>.Instance);

        Assert.Null(await notFound.GetPublicWorldAsync("slug"));
        Assert.Null(await error.GetPublicWorldAsync("slug"));
        Assert.Null(await ex.GetPublicWorldAsync("slug"));
    }

    [Fact]
    public async Task GetPublicArticleTreeAsync_ReturnsEmpty_OnFailure()
    {
        var fail = new PublicApiService(TestHttpMessageHandler.CreateClient(HttpStatusCode.BadRequest), NullLogger<PublicApiService>.Instance);
        var ex = new PublicApiService(new HttpClient(new TestHttpMessageHandler((_, _) => throw new InvalidOperationException("boom"))) { BaseAddress = new Uri("http://localhost/") }, NullLogger<PublicApiService>.Instance);

        Assert.Empty(await fail.GetPublicArticleTreeAsync("slug"));
        Assert.Empty(await ex.GetPublicArticleTreeAsync("slug"));
    }

    [Fact]
    public async Task GetPublicArticleTreeAsync_ReturnsEmpty_WhenBodyIsNull()
    {
        var sut = new PublicApiService(
            TestHttpMessageHandler.CreateClient(HttpStatusCode.OK, "null"),
            NullLogger<PublicApiService>.Instance);

        var result = await sut.GetPublicArticleTreeAsync("slug");

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetPublicArticleAsync_AndResolvePath_HandleNotFoundAndFailures()
    {
        var notFound = new PublicApiService(TestHttpMessageHandler.CreateClient(HttpStatusCode.NotFound), NullLogger<PublicApiService>.Instance);
        var fail = new PublicApiService(TestHttpMessageHandler.CreateClient(HttpStatusCode.BadRequest), NullLogger<PublicApiService>.Instance);
        var ex = new PublicApiService(new HttpClient(new TestHttpMessageHandler((_, _) => throw new InvalidOperationException("boom"))) { BaseAddress = new Uri("http://localhost/") }, NullLogger<PublicApiService>.Instance);

        Assert.Null(await notFound.GetPublicArticleAsync("slug", "path"));
        Assert.Null(await fail.GetPublicArticleAsync("slug", "path"));
        Assert.Null(await ex.GetPublicArticleAsync("slug", "path"));

        Assert.Null(await notFound.ResolvePublicArticlePathAsync("slug", Guid.NewGuid()));
        Assert.Null(await fail.ResolvePublicArticlePathAsync("slug", Guid.NewGuid()));
        Assert.Null(await ex.ResolvePublicArticlePathAsync("slug", Guid.NewGuid()));
    }

    [Fact]
    public async Task SuccessPaths_ReturnData()
    {
        var world = new PublicApiService(TestHttpMessageHandler.CreateClient(HttpStatusCode.OK, "{}"), NullLogger<PublicApiService>.Instance);
        var tree = new PublicApiService(TestHttpMessageHandler.CreateClient(HttpStatusCode.OK, "[]"), NullLogger<PublicApiService>.Instance);
        var article = new PublicApiService(TestHttpMessageHandler.CreateClient(HttpStatusCode.OK, "{}"), NullLogger<PublicApiService>.Instance);
        var path = new PublicApiService(TestHttpMessageHandler.CreateClient(HttpStatusCode.OK, "resolved/path"), NullLogger<PublicApiService>.Instance);

        Assert.NotNull(await world.GetPublicWorldAsync("slug"));
        Assert.Empty(await tree.GetPublicArticleTreeAsync("slug"));
        Assert.NotNull(await article.GetPublicArticleAsync("slug", "article"));
        Assert.Equal("resolved/path", await path.ResolvePublicArticlePathAsync("slug", Guid.NewGuid()));
    }

    [Fact]
    public async Task GetPublicMapBasemapReadUrlAsync_ReturnsStatusAndError()
    {
        var mapId = Guid.NewGuid();
        var sut = new PublicApiService(
            TestHttpMessageHandler.CreateClient(
                HttpStatusCode.NotFound,
                "{\"error\":\"Basemap is missing for this map.\"}"),
            NullLogger<PublicApiService>.Instance);

        var result = await sut.GetPublicMapBasemapReadUrlAsync("slug", mapId);

        Assert.Null(result.Basemap);
        Assert.Equal(404, result.StatusCode);
        Assert.Equal("Basemap is missing for this map.", result.Error);
    }

    [Fact]
    public async Task GetPublicMapBasemapReadUrlAsync_Success_UsesExpectedRoute()
    {
        var mapId = Guid.NewGuid();
        var handler = new TestHttpMessageHandler((request, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"readUrl\":\"https://cdn.test/map.png\"}")
        }));

        var sut = new PublicApiService(
            new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") },
            NullLogger<PublicApiService>.Instance);

        var result = await sut.GetPublicMapBasemapReadUrlAsync("slug", mapId);

        Assert.Equal($"/public/worlds/slug/maps/{mapId}/basemap", handler.Requests[0].RequestUri!.AbsolutePath);
        Assert.NotNull(result.Basemap);
        Assert.Equal(200, result.StatusCode);
        Assert.Equal("https://cdn.test/map.png", result.Basemap!.ReadUrl);
    }

    [Fact]
    public async Task GetPublicMapBasemapReadUrlAsync_InvalidErrorPayload_ReturnsNullError()
    {
        var sut = new PublicApiService(
            TestHttpMessageHandler.CreateClient(HttpStatusCode.NotFound, "not-json"),
            NullLogger<PublicApiService>.Instance);

        var result = await sut.GetPublicMapBasemapReadUrlAsync("slug", Guid.NewGuid());

        Assert.Null(result.Basemap);
        Assert.Equal(404, result.StatusCode);
        Assert.Null(result.Error);
    }

    [Fact]
    public async Task PublicMapListEndpoints_UseExpectedRoutes()
    {
        var mapId = Guid.NewGuid();

        var layersHandler = new TestHttpMessageHandler((request, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("[]")
        }));
        var layersSut = new PublicApiService(
            new HttpClient(layersHandler) { BaseAddress = new Uri("http://localhost/") },
            NullLogger<PublicApiService>.Instance);

        var pinsHandler = new TestHttpMessageHandler((request, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("[]")
        }));
        var pinsSut = new PublicApiService(
            new HttpClient(pinsHandler) { BaseAddress = new Uri("http://localhost/") },
            NullLogger<PublicApiService>.Instance);

        var featuresHandler = new TestHttpMessageHandler((request, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("[]")
        }));
        var featuresSut = new PublicApiService(
            new HttpClient(featuresHandler) { BaseAddress = new Uri("http://localhost/") },
            NullLogger<PublicApiService>.Instance);

        await layersSut.GetPublicMapLayersAsync("slug", mapId);
        await pinsSut.GetPublicMapPinsAsync("slug", mapId);
        await featuresSut.GetPublicMapFeaturesAsync("slug", mapId);

        Assert.Equal($"/public/worlds/slug/maps/{mapId}/layers", layersHandler.Requests[0].RequestUri!.AbsolutePath);
        Assert.Equal($"/public/worlds/slug/maps/{mapId}/pins", pinsHandler.Requests[0].RequestUri!.AbsolutePath);
        Assert.Equal($"/public/worlds/slug/maps/{mapId}/features", featuresHandler.Requests[0].RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task GetPublicMapLayersAsync_ReturnsEmpty_OnFailure()
    {
        var fail = new PublicApiService(TestHttpMessageHandler.CreateClient(HttpStatusCode.BadRequest), NullLogger<PublicApiService>.Instance);
        var ex = new PublicApiService(new HttpClient(new TestHttpMessageHandler((_, _) => throw new InvalidOperationException("boom"))) { BaseAddress = new Uri("http://localhost/") }, NullLogger<PublicApiService>.Instance);

        Assert.Empty(await fail.GetPublicMapLayersAsync("slug", Guid.NewGuid()));
        Assert.Empty(await ex.GetPublicMapLayersAsync("slug", Guid.NewGuid()));
    }
}

