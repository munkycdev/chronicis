using System.Net;
using System.Text.Json;
using Chronicis.Client.Services;
using Chronicis.Shared.Routing;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Chronicis.Client.Tests.Services;

public class PathApiServiceTests
{
    private static PathApiService CreateSut(HttpStatusCode status, string body = "{}")
    {
        var http = TestHttpMessageHandler.CreateClient(status, body);
        return new PathApiService(http, NullLogger<PathApiService>.Instance);
    }

    private static string SerializeResolution(ResolvedEntityKind kind, Guid worldId) =>
        JsonSerializer.Serialize(new
        {
            kind = (int)kind,
            worldId,
            campaignId = (Guid?)null,
            arcId = (Guid?)null,
            sessionId = (Guid?)null,
            mapId = (Guid?)null,
            articleId = (Guid?)null,
            breadcrumbs = Array.Empty<object>()
        }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

    // ─────────────────────────────────────────────────────────────────────
    // 200 → returns resolution
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task ResolveAsync_OkResponse_ReturnsResolution()
    {
        var worldId = Guid.NewGuid();
        var json = SerializeResolution(ResolvedEntityKind.World, worldId);
        var sut = CreateSut(HttpStatusCode.OK, json);

        var result = await sut.ResolveAsync("my-world");

        Assert.NotNull(result);
        Assert.Equal(worldId, result!.WorldId);
    }

    // ─────────────────────────────────────────────────────────────────────
    // 404 → returns null
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task ResolveAsync_NotFound_ReturnsNull()
    {
        var sut = CreateSut(HttpStatusCode.NotFound);

        var result = await sut.ResolveAsync("unknown-world");

        Assert.Null(result);
    }

    // ─────────────────────────────────────────────────────────────────────
    // Non-success status → returns null
    // ─────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.Unauthorized)]
    public async Task ResolveAsync_NonSuccessStatus_ReturnsNull(HttpStatusCode status)
    {
        var sut = CreateSut(status);

        var result = await sut.ResolveAsync("my-world");

        Assert.Null(result);
    }

    // ─────────────────────────────────────────────────────────────────────
    // HTTP exception → returns null (logs warning)
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task ResolveAsync_HttpException_ReturnsNull()
    {
        var handler = new TestHttpMessageHandler((_, _) => throw new HttpRequestException("network error"));
        var http = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
        var sut = new PathApiService(http, NullLogger<PathApiService>.Instance);

        var result = await sut.ResolveAsync("my-world");

        Assert.Null(result);
    }

    // ─────────────────────────────────────────────────────────────────────
    // Path encoding: segments are percent-encoded
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task ResolveAsync_PathSegments_AreEncoded()
    {
        string? capturedPath = null;
        var handler = new TestHttpMessageHandler((req, _) =>
        {
            capturedPath = req.RequestUri!.PathAndQuery;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        });
        var http = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/api/") };
        var sut = new PathApiService(http, NullLogger<PathApiService>.Instance);

        await sut.ResolveAsync("my world/arc one");

        Assert.NotNull(capturedPath);
        Assert.Contains("my%20world", capturedPath);
        Assert.Contains("arc%20one", capturedPath);
    }

    // ─────────────────────────────────────────────────────────────────────
    // Simple path hits correct route
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task ResolveAsync_SimplePath_HitsCorrectRoute()
    {
        string? capturedPath = null;
        var handler = new TestHttpMessageHandler((req, _) =>
        {
            capturedPath = req.RequestUri!.PathAndQuery;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        });
        var http = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
        var sut = new PathApiService(http, NullLogger<PathApiService>.Instance);

        await sut.ResolveAsync("my-world/campaign-1");

        Assert.NotNull(capturedPath);
        Assert.Contains("paths/resolve/my-world/campaign-1", capturedPath);
    }
}
