using Chronicis.Client.Extensions;
using Chronicis.Client.Services;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Xunit;

namespace Chronicis.Client.Tests.Extensions;

public class HttpClientServiceExtensionsTests
{
    [Fact]
    public void AddChronicisHttpClients_UsesConfiguredApiBaseUrl()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(CreateTokenProvider());
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["ApiBaseUrl"] = "https://api.example.test" })
            .Build();

        var returned = services.AddChronicisHttpClients(config);

        Assert.Same(services, returned);
        using var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IHttpClientFactory>();
        var authClient = factory.CreateClient("ChronicisApi");
        var publicClient = factory.CreateClient("ChronicisPublicApi");
        var quoteService = provider.GetRequiredService<IQuoteService>();

        Assert.Equal("https://api.example.test/", authClient.BaseAddress!.ToString());
        Assert.Equal("https://api.example.test/", publicClient.BaseAddress!.ToString());
        Assert.IsType<QuoteService>(quoteService);
        Assert.NotNull(provider.GetRequiredService<ChronicisAuthHandler>());
    }

    [Fact]
    public void AddChronicisHttpClients_UsesLocalhostFallback_WhenConfigValueMissing()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(CreateTokenProvider());
        var config = new ConfigurationBuilder().AddInMemoryCollection().Build();

        services.AddChronicisHttpClients(config);

        using var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IHttpClientFactory>();
        var authClient = factory.CreateClient("ChronicisApi");
        var publicClient = factory.CreateClient("ChronicisPublicApi");

        Assert.Equal("http://localhost:7071/", authClient.BaseAddress!.ToString());
        Assert.Equal("http://localhost:7071/", publicClient.BaseAddress!.ToString());
    }

    private static IAccessTokenProvider CreateTokenProvider()
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
        return provider;
    }
}

