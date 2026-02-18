using System.Security.Claims;
using Chronicis.Client.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Xunit;

namespace Chronicis.Client.Tests.Services;

public class AuthServiceTests
{
    [Fact]
    public async Task GetCurrentUserAsync_ReturnsNull_WhenUnauthenticated()
    {
        var provider = new TestAuthStateProvider(new ClaimsPrincipal(new ClaimsIdentity()));
        var sut = new AuthService(provider);

        var user = await sut.GetCurrentUserAsync();

        Assert.Null(user);
        Assert.False(await sut.IsAuthenticatedAsync());
    }

    [Fact]
    public async Task GetCurrentUserAsync_MapsClaims_AndCaches()
    {
        var identity = new ClaimsIdentity(new[]
        {
            new Claim("sub", "auth0|1"),
            new Claim("email", "test@example.com"),
            new Claim("preferred_username", "Tester"),
            new Claim("picture", "http://img")
        }, "test");

        var provider = new TestAuthStateProvider(new ClaimsPrincipal(identity));
        var sut = new AuthService(provider);

        var first = await sut.GetCurrentUserAsync();
        var second = await sut.GetCurrentUserAsync();

        Assert.NotNull(first);
        Assert.Equal("auth0|1", first.Auth0UserId);
        Assert.Equal("test@example.com", first.Email);
        Assert.Equal("Tester", first.DisplayName);
        Assert.Equal("http://img", first.AvatarUrl);
        Assert.Same(first, second);
        Assert.True(await sut.IsAuthenticatedAsync());
    }

    [Fact]
    public async Task GetCurrentUserAsync_UsesDefaults_WhenClaimsMissing()
    {
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "id")
        }, "test");
        var provider = new TestAuthStateProvider(new ClaimsPrincipal(identity));
        var sut = new AuthService(provider);

        var user = await sut.GetCurrentUserAsync();

        Assert.NotNull(user);
        Assert.Equal("id", user.Auth0UserId);
        Assert.Equal(string.Empty, user.Email);
        Assert.Equal("Unknown User", user.DisplayName);
        Assert.Null(user.AvatarUrl);
    }

    [Fact]
    public async Task GetCurrentUserAsync_PrefersCustomNamespaceClaims()
    {
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "fallback-id"),
            new Claim("https://chronicis.app/email", "custom@example.com"),
            new Claim("https://chronicis.app/name", "Custom Name"),
            new Claim("https://chronicis.app/picture", "https://custom/img")
        }, "test");
        var provider = new TestAuthStateProvider(new ClaimsPrincipal(identity));
        var sut = new AuthService(provider);

        var user = await sut.GetCurrentUserAsync();

        Assert.NotNull(user);
        Assert.Equal("custom@example.com", user.Email);
        Assert.Equal("Custom Name", user.DisplayName);
        Assert.Equal("https://custom/img", user.AvatarUrl);
    }

    [Fact]
    public async Task GetCurrentUserAsync_UsesStandardClaimTypesFallbacks()
    {
        var identity = new ClaimsIdentity(new[]
        {
            new Claim("sub", "sub-id"),
            new Claim(ClaimTypes.Email, "typed@example.com"),
            new Claim(ClaimTypes.Name, "Typed Name"),
            new Claim("picture", "pic")
        }, "test");
        var sut = new AuthService(new TestAuthStateProvider(new ClaimsPrincipal(identity)));

        var user = await sut.GetCurrentUserAsync();

        Assert.NotNull(user);
        Assert.Equal("sub-id", user.Auth0UserId);
        Assert.Equal("typed@example.com", user.Email);
        Assert.Equal("Typed Name", user.DisplayName);
        Assert.Equal("pic", user.AvatarUrl);
    }

    [Fact]
    public async Task IsAuthenticatedAsync_ReturnsFalse_WhenIdentityMissing()
    {
        var principal = new ClaimsPrincipal(Array.Empty<ClaimsIdentity>());
        var sut = new AuthService(new TestAuthStateProvider(principal));

        Assert.False(await sut.IsAuthenticatedAsync());
        Assert.Null(await sut.GetCurrentUserAsync());
    }

    [Fact]
    public async Task GetCurrentUserAsync_UsesNameClaim_WhenPreferredUsernameMissing()
    {
        var identity = new ClaimsIdentity(new[]
        {
            new Claim("sub", "sub-id"),
            new Claim("name", "Display From Name")
        }, "test");
        var sut = new AuthService(new TestAuthStateProvider(new ClaimsPrincipal(identity)));

        var user = await sut.GetCurrentUserAsync();

        Assert.NotNull(user);
        Assert.Equal("Display From Name", user.DisplayName);
    }

    [Fact]
    public async Task GetCurrentUserAsync_UsesEmptyAuth0Id_WhenNoIdClaims()
    {
        var identity = new ClaimsIdentity(new[]
        {
            new Claim("email", "user@example.com")
        }, "test");
        var sut = new AuthService(new TestAuthStateProvider(new ClaimsPrincipal(identity)));

        var user = await sut.GetCurrentUserAsync();

        Assert.NotNull(user);
        Assert.Equal(string.Empty, user.Auth0UserId);
    }

    private sealed class TestAuthStateProvider : AuthenticationStateProvider
    {
        private readonly AuthenticationState _state;

        public TestAuthStateProvider(ClaimsPrincipal principal)
        {
            _state = new AuthenticationState(principal);
        }

        public override Task<AuthenticationState> GetAuthenticationStateAsync() => Task.FromResult(_state);
    }
}

