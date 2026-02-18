using Chronicis.Client.Extensions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Chronicis.Client.Tests.Extensions;

public class AuthenticationServiceExtensionsTests
{
    [Fact]
    public void AddChronicisAuthentication_ConfiguresOidcOptions()
    {
        var services = new ServiceCollection();
        services.AddSingleton<NavigationManager>(new TestNavigationManager("https://client.example/"));

        var returned = services.AddChronicisAuthentication("https://client.example");

        Assert.Same(services, returned);
        using var provider = services.BuildServiceProvider();
        var options = provider
            .GetRequiredService<IOptions<RemoteAuthenticationOptions<OidcProviderOptions>>>()
            .Value;

        Assert.Equal("https://auth.chronicis.app", options.ProviderOptions.Authority);
        Assert.Equal("Itq22vH9FBHKlYHL1j0A9EgVjA9f6NZQ", options.ProviderOptions.ClientId);
        Assert.Equal("code", options.ProviderOptions.ResponseType);
        Assert.Equal("https://client.example/authentication/login-callback", options.ProviderOptions.RedirectUri);
        Assert.Equal("https://client.example", options.ProviderOptions.PostLogoutRedirectUri);
        Assert.Equal("https://api.chronicis.app", options.ProviderOptions.AdditionalProviderParameters["audience"]);
        Assert.Equal(new[] { "openid", "profile", "email" }, options.ProviderOptions.DefaultScopes);
    }

    private sealed class TestNavigationManager : NavigationManager
    {
        public TestNavigationManager(string baseUri)
        {
            Initialize(baseUri, baseUri);
        }

        protected override void NavigateToCore(string uri, bool forceLoad)
        {
        }
    }
}
