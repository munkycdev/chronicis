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
}

