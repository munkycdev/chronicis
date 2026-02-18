using System.Net;
using Chronicis.Client.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Chronicis.Client.Tests.Services;

public class DashboardApiServiceTests
{
    [Fact]
    public async Task GetDashboardAsync_ReturnsNull_OnException()
    {
        var handler = new TestHttpMessageHandler((_, _) => throw new InvalidOperationException("boom"));
        var http = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
        var sut = new DashboardApiService(http, NullLogger<DashboardApiService>.Instance);

        var result = await sut.GetDashboardAsync();

        Assert.Null(result);
    }

    [Fact]
    public async Task GetDashboardAsync_ReturnsDto_OnSuccess()
    {
        var http = TestHttpMessageHandler.CreateClient(HttpStatusCode.OK, "{}");
        var sut = new DashboardApiService(http, NullLogger<DashboardApiService>.Instance);

        var result = await sut.GetDashboardAsync();

        Assert.NotNull(result);
    }
}

