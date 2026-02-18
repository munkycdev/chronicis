using System.Net;
using Chronicis.Client.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Chronicis.Client.Tests.Services;

public class UserApiServiceTests
{
    [Fact]
    public async Task GetUserProfileAsync_ReturnsNull_On404()
    {
        var handler = new TestHttpMessageHandler((_, _) => throw new HttpRequestException("", null, HttpStatusCode.NotFound));
        var http = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
        var sut = new UserApiService(http, NullLogger<UserApiService>.Instance);

        var result = await sut.GetUserProfileAsync();

        Assert.Null(result);
    }

    [Fact]
    public async Task GetUserProfileAsync_ReturnsNull_OnOtherException()
    {
        var handler = new TestHttpMessageHandler((_, _) => throw new InvalidOperationException("boom"));
        var http = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
        var sut = new UserApiService(http, NullLogger<UserApiService>.Instance);

        var result = await sut.GetUserProfileAsync();

        Assert.Null(result);
    }

    [Fact]
    public async Task CompleteOnboardingAsync_ReturnsBoolean()
    {
        var ok = new UserApiService(TestHttpMessageHandler.CreateClient(HttpStatusCode.OK), NullLogger<UserApiService>.Instance);
        var bad = new UserApiService(TestHttpMessageHandler.CreateClient(HttpStatusCode.BadRequest), NullLogger<UserApiService>.Instance);
        var boom = new UserApiService(new HttpClient(new TestHttpMessageHandler((_, _) => throw new InvalidOperationException("boom"))) { BaseAddress = new Uri("http://localhost/") }, NullLogger<UserApiService>.Instance);

        Assert.True(await ok.CompleteOnboardingAsync());
        Assert.False(await bad.CompleteOnboardingAsync());
        Assert.False(await boom.CompleteOnboardingAsync());
    }
}

