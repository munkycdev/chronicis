using System.Net;
using Chronicis.Client.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Chronicis.Client.Tests.Services;

public class ResourceProviderApiServiceTests
{
    [Fact]
    public async Task GetWorldProvidersAsync_ReturnsNull_OnFailureOrException()
    {
        var fail = new ResourceProviderApiService(TestHttpMessageHandler.CreateClient(HttpStatusCode.BadRequest), NullLogger<ResourceProviderApiService>.Instance);
        var ex = new ResourceProviderApiService(new HttpClient(new TestHttpMessageHandler((_, _) => throw new InvalidOperationException("boom"))) { BaseAddress = new Uri("http://localhost/") }, NullLogger<ResourceProviderApiService>.Instance);

        Assert.Null(await fail.GetWorldProvidersAsync(Guid.NewGuid()));
        Assert.Null(await ex.GetWorldProvidersAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task ToggleProviderAsync_ReturnsExpectedStatus()
    {
        var ok = new ResourceProviderApiService(TestHttpMessageHandler.CreateClient(HttpStatusCode.OK), NullLogger<ResourceProviderApiService>.Instance);
        var bad = new ResourceProviderApiService(TestHttpMessageHandler.CreateClient(HttpStatusCode.BadRequest), NullLogger<ResourceProviderApiService>.Instance);
        var ex = new ResourceProviderApiService(new HttpClient(new TestHttpMessageHandler((_, _) => throw new InvalidOperationException("boom"))) { BaseAddress = new Uri("http://localhost/") }, NullLogger<ResourceProviderApiService>.Instance);

        Assert.True(await ok.ToggleProviderAsync(Guid.NewGuid(), "ros", true));
        Assert.True(await ok.ToggleProviderAsync(Guid.NewGuid(), "ros", false));
        Assert.False(await bad.ToggleProviderAsync(Guid.NewGuid(), "ros", false));
        Assert.False(await ex.ToggleProviderAsync(Guid.NewGuid(), "ros", true));
    }

    [Fact]
    public async Task GetWorldProvidersAsync_ReturnsList_OnSuccess()
    {
        var ok = new ResourceProviderApiService(
            TestHttpMessageHandler.CreateClient(HttpStatusCode.OK, "[]"),
            NullLogger<ResourceProviderApiService>.Instance);

        var result = await ok.GetWorldProvidersAsync(Guid.NewGuid());

        Assert.NotNull(result);
        Assert.Empty(result);
    }
}

