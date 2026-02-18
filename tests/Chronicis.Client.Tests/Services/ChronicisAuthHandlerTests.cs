using System.Net;
using Chronicis.Client.Services;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using NSubstitute;
using Xunit;

namespace Chronicis.Client.Tests.Services;

public class ChronicisAuthHandlerTests
{
    [Fact]
    public async Task SendAsync_AddsBearerHeader_WhenTokenAvailable()
    {
        var provider = Substitute.For<IAccessTokenProvider>();
        provider.RequestAccessToken(Arg.Any<AccessTokenRequestOptions>())
            .Returns(new ValueTask<AccessTokenResult>(
                new AccessTokenResult(
                    AccessTokenResultStatus.Success,
                    new AccessToken { Value = "token-value" },
                    interactiveRequestUrl: null,
                    interactiveRequest: null)));

        var inner = new CapturingHandler();
        var sut = new TestableChronicisAuthHandler(provider) { InnerHandler = inner };

        await sut.SendAsyncPublic(new HttpRequestMessage(HttpMethod.Get, "http://localhost/test"), CancellationToken.None);

        Assert.Equal("Bearer", inner.LastRequest!.Headers.Authorization!.Scheme);
        Assert.Equal("token-value", inner.LastRequest.Headers.Authorization.Parameter);
    }

    [Fact]
    public async Task SendAsync_DoesNotAddHeader_WhenTokenUnavailable()
    {
        var provider = Substitute.For<IAccessTokenProvider>();
        provider.RequestAccessToken(Arg.Any<AccessTokenRequestOptions>())
            .Returns(new ValueTask<AccessTokenResult>(
                new AccessTokenResult(
                    AccessTokenResultStatus.RequiresRedirect,
                    new AccessToken(),
                    interactiveRequestUrl: "/login",
                    interactiveRequest: new InteractiveRequestOptions
                    {
                        Interaction = InteractionType.GetToken,
                        ReturnUrl = "/"
                    })));

        var inner = new CapturingHandler();
        var sut = new TestableChronicisAuthHandler(provider) { InnerHandler = inner };

        await sut.SendAsyncPublic(new HttpRequestMessage(HttpMethod.Get, "http://localhost/test"), CancellationToken.None);

        Assert.Null(inner.LastRequest!.Headers.Authorization);
    }

    private sealed class TestableChronicisAuthHandler : ChronicisAuthHandler
    {
        public TestableChronicisAuthHandler(IAccessTokenProvider provider) : base(provider) { }

        public Task<HttpResponseMessage> SendAsyncPublic(HttpRequestMessage request, CancellationToken ct)
            => SendAsync(request, ct);
    }

    private sealed class CapturingHandler : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }
}

