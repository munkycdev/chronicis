using System.Security.Claims;
using Bunit.TestDoubles;
using Chronicis.Client.Pages;
using Chronicis.Client.Tests.Components;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Chronicis.Client.Tests.Pages;

public class LandingTests : MudBlazorTestContext
{
    [Fact]
    public void Landing_Unauthenticated_DoesNotRedirect()
    {
        Services.AddAuthorizationCore();
        Services.AddSingleton<IAuthorizationService, AllowAuthorizationService>();
        Services.AddSingleton<AuthenticationStateProvider>(new TestAuthStateProvider(false));

        var nav = Services.GetRequiredService<FakeNavigationManager>();
        var cut = RenderComponent<CascadingAuthenticationState>(p => p.AddChildContent<Landing>());

        Assert.DoesNotContain("/dashboard", nav.Uri, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Never Lose Track of Your Campaign Again", cut.Markup);
    }

    [Fact]
    public void Landing_Authenticated_RedirectsToDashboard()
    {
        Services.AddAuthorizationCore();
        Services.AddSingleton<IAuthorizationService, AllowAuthorizationService>();
        Services.AddSingleton<AuthenticationStateProvider>(new TestAuthStateProvider(true));

        var nav = Services.GetRequiredService<FakeNavigationManager>();
        RenderComponent<CascadingAuthenticationState>(p => p.AddChildContent<Landing>());

        Assert.EndsWith("/dashboard", nav.Uri, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class TestAuthStateProvider(bool isAuthenticated) : AuthenticationStateProvider
    {
        private readonly AuthenticationState _state = new(
            new ClaimsPrincipal(
                isAuthenticated
                    ? new ClaimsIdentity([new Claim(ClaimTypes.Name, "test")], "TestAuth")
                    : new ClaimsIdentity()));

        public override Task<AuthenticationState> GetAuthenticationStateAsync() => Task.FromResult(_state);
    }

    private sealed class AllowAuthorizationService : IAuthorizationService
    {
        public Task<AuthorizationResult> AuthorizeAsync(ClaimsPrincipal user, object? resource, IEnumerable<IAuthorizationRequirement> requirements) =>
            Task.FromResult(AuthorizationResult.Success());

        public Task<AuthorizationResult> AuthorizeAsync(ClaimsPrincipal user, object? resource, string policyName) =>
            Task.FromResult(AuthorizationResult.Success());
    }
}
