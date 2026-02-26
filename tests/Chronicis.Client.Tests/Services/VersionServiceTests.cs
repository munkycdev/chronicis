using System.Net;
using Chronicis.Client.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Chronicis.Client.Tests.Services;

public class VersionServiceTests
{
    private static VersionService CreateSut(HttpStatusCode status, string json) =>
        new(TestHttpMessageHandler.CreateClient(status, json), NullLogger<VersionService>.Instance);

    // ── Happy path ────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetBuildInfoAsync_ReturnsDeserializedInfo_OnSuccess()
    {
        var json = """{"version":"3.0.42","buildNumber":"42","sha":"abc1234","buildDate":"2026-01-01T00:00:00Z"}""";
        var sut = CreateSut(HttpStatusCode.OK, json);

        var result = await sut.GetBuildInfoAsync();

        Assert.Equal("3.0.42", result.Version);
        Assert.Equal("42", result.BuildNumber);
        Assert.Equal("abc1234", result.Sha);
        Assert.Equal("2026-01-01T00:00:00Z", result.BuildDate);
    }

    // ── Caching ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetBuildInfoAsync_ReturnsCachedResult_OnSecondCall()
    {
        var json = """{"version":"3.0.1","buildNumber":"1","sha":"aaa","buildDate":""}""";
        var handler = new TestHttpMessageHandler((_, _) =>
        {
            // Track call count via closure
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json)
            });
        });
        var sut = new VersionService(
            new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") },
            NullLogger<VersionService>.Instance);

        var first = await sut.GetBuildInfoAsync();
        var second = await sut.GetBuildInfoAsync();

        // Same reference means cache was hit
        Assert.Same(first, second);
    }

    // ── Fallback: HTTP error ──────────────────────────────────────────────────

    [Fact]
    public async Task GetBuildInfoAsync_ReturnsFallback_OnHttpError()
    {
        var sut = CreateSut(HttpStatusCode.InternalServerError, string.Empty);

        var result = await sut.GetBuildInfoAsync();

        Assert.Equal("0.0.0", result.Version);
        Assert.Equal("0", result.BuildNumber);
        Assert.Equal("unknown", result.Sha);
        Assert.Equal(string.Empty, result.BuildDate);
    }

    // ── Fallback: null JSON payload ───────────────────────────────────────────

    [Fact]
    public async Task GetBuildInfoAsync_ReturnsFallback_OnNullJson()
    {
        var sut = CreateSut(HttpStatusCode.OK, "null");

        var result = await sut.GetBuildInfoAsync();

        Assert.Equal("0.0.0", result.Version);
        Assert.Equal("unknown", result.Sha);
    }

    // ── Fallback: network exception ───────────────────────────────────────────

    [Fact]
    public async Task GetBuildInfoAsync_ReturnsFallback_OnException()
    {
        var sut = new VersionService(
            new HttpClient(new TestHttpMessageHandler((_, _) => throw new HttpRequestException("network down")))
            {
                BaseAddress = new Uri("http://localhost/")
            },
            NullLogger<VersionService>.Instance);

        var result = await sut.GetBuildInfoAsync();

        Assert.Equal("0.0.0", result.Version);
        Assert.Equal("unknown", result.Sha);
    }

    // ── BuildInfo defaults ────────────────────────────────────────────────────

    [Fact]
    public void BuildInfo_Defaults_AreEmpty()
    {
        var info = new BuildInfo();

        Assert.Equal(string.Empty, info.Version);
        Assert.Equal(string.Empty, info.BuildNumber);
        Assert.Equal(string.Empty, info.Sha);
        Assert.Equal(string.Empty, info.BuildDate);
    }
}
