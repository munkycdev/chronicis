using System.Net;
using Chronicis.Client.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Chronicis.Client.Tests.Services;

public class IHealthStatusApiServiceTests
{
    [Fact]
    public async Task GetSystemHealthAsync_ReturnsDto_OnSuccess()
    {
        var http = TestHttpMessageHandler.CreateClient(HttpStatusCode.OK, "{\"status\":\"Healthy\"}");
        var sut = new HealthStatusApiService(http, NullLogger<HealthStatusApiService>.Instance);

        var result = await sut.GetSystemHealthAsync();

        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetSystemHealthAsync_ReturnsNull_OnNonSuccess()
    {
        var http = TestHttpMessageHandler.CreateClient(HttpStatusCode.InternalServerError, "boom");
        var sut = new HealthStatusApiService(http, NullLogger<HealthStatusApiService>.Instance);

        var result = await sut.GetSystemHealthAsync();

        Assert.Null(result);
    }

    [Fact]
    public async Task GetSystemHealthAsync_ReturnsNull_OnException()
    {
        var handler = new TestHttpMessageHandler((_, _) => throw new HttpRequestException("fail"));
        var http = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
        var sut = new HealthStatusApiService(http, NullLogger<HealthStatusApiService>.Instance);

        var result = await sut.GetSystemHealthAsync();

        Assert.Null(result);
    }

    [Fact]
    public async Task GetSystemHealthAsync_ReturnsNull_OnTimeoutAndUnknownException()
    {
        var timeout = new HealthStatusApiService(
            new HttpClient(new TestHttpMessageHandler((_, _) => throw new TaskCanceledException("timeout")))
            {
                BaseAddress = new Uri("http://localhost/")
            },
            NullLogger<HealthStatusApiService>.Instance);

        var unknown = new HealthStatusApiService(
            new HttpClient(new TestHttpMessageHandler((_, _) => throw new InvalidOperationException("boom")))
            {
                BaseAddress = new Uri("http://localhost/")
            },
            NullLogger<HealthStatusApiService>.Instance);

        Assert.Null(await timeout.GetSystemHealthAsync());
        Assert.Null(await unknown.GetSystemHealthAsync());
    }
}

