using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;
using Chronicis.Api.Infrastructure;
using Chronicis.Api.Services;
using Chronicis.Shared.Admin;
using Chronicis.Shared.Models;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using Xunit;

namespace Chronicis.Api.Tests;

[ExcludeFromCodeCoverage]
public class CurrentUserServiceTests
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IUserService _userService;
    private readonly ISysAdminChecker _sysAdminChecker;
    private readonly CurrentUserService _sut;

    public CurrentUserServiceTests()
    {
        _httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        _userService = Substitute.For<IUserService>();
        _sysAdminChecker = Substitute.For<ISysAdminChecker>();
        _sut = new CurrentUserService(_httpContextAccessor, _userService, _sysAdminChecker);
    }

    // ── Helpers ───────────────────────────────────────────────────

    private void SetupHttpContext(bool authenticated, params Claim[] claims)
    {
        var identity = new ClaimsIdentity(
            claims,
            authenticated ? "Bearer" : null); // non-null authType = IsAuthenticated
        var principal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext { User = principal };
        _httpContextAccessor.HttpContext.Returns(httpContext);
    }

    private void SetupNullHttpContext()
    {
        _httpContextAccessor.HttpContext.Returns((HttpContext?)null);
    }

    private User CreateTestUser(string auth0Id = "auth0|123")
    {
        return new User
        {
            Id = Guid.NewGuid(),
            Auth0UserId = auth0Id,
            Email = "test@example.com",
            DisplayName = "Test User"
        };
    }

    // ── IsAuthenticated ──────────────────────────────────────────

    [Fact]
    public void IsAuthenticated_ReturnsFalse_WhenHttpContextIsNull()
    {
        SetupNullHttpContext();

        Assert.False(_sut.IsAuthenticated);
    }

    [Fact]
    public void IsAuthenticated_ReturnsFalse_WhenNotAuthenticated()
    {
        SetupHttpContext(authenticated: false);

        Assert.False(_sut.IsAuthenticated);
    }

    [Fact]
    public void IsAuthenticated_ReturnsTrue_WhenAuthenticated()
    {
        SetupHttpContext(authenticated: true,
            new Claim(ClaimTypes.NameIdentifier, "auth0|123"));

        Assert.True(_sut.IsAuthenticated);
    }

    [Fact]
    public void IsAuthenticated_ReturnsFalse_WhenPrincipalHasNoIdentity()
    {
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal()
        };
        _httpContextAccessor.HttpContext.Returns(httpContext);

        Assert.False(_sut.IsAuthenticated);
    }

    [Fact]
    public void IsAuthenticated_ReturnsFalse_WhenUserIsNull()
    {
        var httpContext = Substitute.For<HttpContext>();
        httpContext.User.Returns((ClaimsPrincipal)null!);
        _httpContextAccessor.HttpContext.Returns(httpContext);

        Assert.False(_sut.IsAuthenticated);
    }

    // ── GetAuth0UserId ───────────────────────────────────────────

    [Fact]
    public void GetAuth0UserId_ReturnsNull_WhenNotAuthenticated()
    {
        SetupHttpContext(authenticated: false);

        Assert.Null(_sut.GetAuth0UserId());
    }

    [Fact]
    public void GetAuth0UserId_ReturnsNull_WhenHttpContextIsNull()
    {
        SetupNullHttpContext();

        Assert.Null(_sut.GetAuth0UserId());
    }

    [Fact]
    public void GetAuth0UserId_ReturnsNameIdentifier_WhenPresent()
    {
        SetupHttpContext(authenticated: true,
            new Claim(ClaimTypes.NameIdentifier, "auth0|primary"));

        Assert.Equal("auth0|primary", _sut.GetAuth0UserId());
    }

    [Fact]
    public void GetAuth0UserId_FallsBackToSub_WhenNoNameIdentifier()
    {
        SetupHttpContext(authenticated: true,
            new Claim("sub", "auth0|fallback"));

        Assert.Equal("auth0|fallback", _sut.GetAuth0UserId());
    }

    [Fact]
    public void GetAuth0UserId_ReturnsNull_WhenNoClaims()
    {
        SetupHttpContext(authenticated: true);

        Assert.Null(_sut.GetAuth0UserId());
    }

    // ── GetCurrentUserAsync ──────────────────────────────────────

    [Fact]
    public async Task GetCurrentUser_ReturnsNull_WhenNotAuthenticated()
    {
        SetupHttpContext(authenticated: false);

        var result = await _sut.GetCurrentUserAsync();

        Assert.Null(result);
    }

    [Fact]
    public async Task GetCurrentUser_CallsGetOrCreateUser_WithExtractedClaims()
    {
        var testUser = CreateTestUser();
        SetupHttpContext(authenticated: true,
            new Claim(ClaimTypes.NameIdentifier, "auth0|123"),
            new Claim("https://chronicis.app/email", "dave@example.com"),
            new Claim("https://chronicis.app/name", "Dave"),
            new Claim("https://chronicis.app/picture", "https://img.example.com/dave.jpg"));

        _userService.GetOrCreateUserAsync(
            Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string>(), Arg.Any<string?>())
            .Returns(testUser);

        var result = await _sut.GetCurrentUserAsync();

        Assert.Equal(testUser, result);
        await _userService.Received(1).GetOrCreateUserAsync(
            "auth0|123",
            "dave@example.com",
            "Dave",
            "https://img.example.com/dave.jpg");
    }

    [Fact]
    public async Task GetCurrentUser_UsesSafeFallbacks_WhenContextDisappearsAfterAuthCheck()
    {
        var testUser = CreateTestUser();
        var firstContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, "auth0|123")],
                "Bearer"))
        };

        _httpContextAccessor.HttpContext.Returns(firstContext, firstContext, null);

        _userService.GetOrCreateUserAsync(
            Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string>(), Arg.Any<string?>())
            .Returns(testUser);

        var result = await _sut.GetCurrentUserAsync();

        Assert.Equal(testUser, result);
        await _userService.Received(1).GetOrCreateUserAsync(
            "auth0|123",
            "",
            "Unknown User",
            null);
    }

    [Fact]
    public async Task GetCurrentUser_CachesResult_OnSecondCall()
    {
        var testUser = CreateTestUser();
        SetupHttpContext(authenticated: true,
            new Claim(ClaimTypes.NameIdentifier, "auth0|123"),
            new Claim(ClaimTypes.Email, "test@test.com"),
            new Claim(ClaimTypes.Name, "Test"));

        _userService.GetOrCreateUserAsync(
            Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string>(), Arg.Any<string?>())
            .Returns(testUser);

        var first = await _sut.GetCurrentUserAsync();
        var second = await _sut.GetCurrentUserAsync();

        Assert.Same(first, second);
        await _userService.Received(1).GetOrCreateUserAsync(
            Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string>(), Arg.Any<string?>());
    }

    [Fact]
    public async Task GetCurrentUser_FallsBackToStandardEmailClaim()
    {
        var testUser = CreateTestUser();
        SetupHttpContext(authenticated: true,
            new Claim(ClaimTypes.NameIdentifier, "auth0|123"),
            new Claim(ClaimTypes.Email, "standard@example.com"),
            new Claim(ClaimTypes.Name, "Standard User"));

        _userService.GetOrCreateUserAsync(
            Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string>(), Arg.Any<string?>())
            .Returns(testUser);

        await _sut.GetCurrentUserAsync();

        await _userService.Received(1).GetOrCreateUserAsync(
            "auth0|123",
            "standard@example.com",
            "Standard User",
            null);
    }

    [Fact]
    public async Task GetCurrentUser_FallsBackToNicknameClaim()
    {
        var testUser = CreateTestUser();
        SetupHttpContext(authenticated: true,
            new Claim(ClaimTypes.NameIdentifier, "auth0|123"),
            new Claim("email", "nick@example.com"),
            new Claim("nickname", "nicky"));

        _userService.GetOrCreateUserAsync(
            Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string>(), Arg.Any<string?>())
            .Returns(testUser);

        await _sut.GetCurrentUserAsync();

        await _userService.Received(1).GetOrCreateUserAsync(
            "auth0|123",
            "nick@example.com",
            "nicky",
            null);
    }

    [Fact]
    public async Task GetCurrentUser_FallsBackToRawNameClaim()
    {
        var testUser = CreateTestUser();
        SetupHttpContext(authenticated: true,
            new Claim(ClaimTypes.NameIdentifier, "auth0|123"),
            new Claim("email", "rawname@example.com"),
            new Claim("name", "Raw Name"));

        _userService.GetOrCreateUserAsync(
            Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string>(), Arg.Any<string?>())
            .Returns(testUser);

        await _sut.GetCurrentUserAsync();

        await _userService.Received(1).GetOrCreateUserAsync(
            "auth0|123",
            "rawname@example.com",
            "Raw Name",
            null);
    }

    [Fact]
    public async Task GetCurrentUser_FallsBackToPreferredUsername()
    {
        var testUser = CreateTestUser();
        SetupHttpContext(authenticated: true,
            new Claim(ClaimTypes.NameIdentifier, "auth0|123"),
            new Claim("email", "pref@example.com"),
            new Claim("preferred_username", "pref-user"));

        _userService.GetOrCreateUserAsync(
            Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string>(), Arg.Any<string?>())
            .Returns(testUser);

        await _sut.GetCurrentUserAsync();

        await _userService.Received(1).GetOrCreateUserAsync(
            "auth0|123",
            "pref@example.com",
            "pref-user",
            null);
    }

    [Fact]
    public async Task GetCurrentUser_FallsBackToGivenName()
    {
        var testUser = CreateTestUser();
        SetupHttpContext(authenticated: true,
            new Claim(ClaimTypes.NameIdentifier, "auth0|123"),
            new Claim("email", "given@example.com"),
            new Claim("given_name", "Given"));

        _userService.GetOrCreateUserAsync(
            Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string>(), Arg.Any<string?>())
            .Returns(testUser);

        await _sut.GetCurrentUserAsync();

        await _userService.Received(1).GetOrCreateUserAsync(
            "auth0|123",
            "given@example.com",
            "Given",
            null);
    }

    [Fact]
    public async Task GetCurrentUser_FallsBackToPictureClaim_WhenCustomPictureMissing()
    {
        var testUser = CreateTestUser();
        SetupHttpContext(authenticated: true,
            new Claim(ClaimTypes.NameIdentifier, "auth0|123"),
            new Claim(ClaimTypes.Email, "pic@example.com"),
            new Claim(ClaimTypes.Name, "Pic User"),
            new Claim("picture", "https://img.example.com/pic.jpg"));

        _userService.GetOrCreateUserAsync(
            Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string>(), Arg.Any<string?>())
            .Returns(testUser);

        await _sut.GetCurrentUserAsync();

        await _userService.Received(1).GetOrCreateUserAsync(
            "auth0|123",
            "pic@example.com",
            "Pic User",
            "https://img.example.com/pic.jpg");
    }

    [Fact]
    public async Task GetCurrentUser_ExtractsNameFromEmail_WhenNoNameClaims()
    {
        var testUser = CreateTestUser();
        SetupHttpContext(authenticated: true,
            new Claim(ClaimTypes.NameIdentifier, "auth0|123"),
            new Claim(ClaimTypes.Email, "john.doe@example.com"));

        _userService.GetOrCreateUserAsync(
            Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string>(), Arg.Any<string?>())
            .Returns(testUser);

        await _sut.GetCurrentUserAsync();

        await _userService.Received(1).GetOrCreateUserAsync(
            "auth0|123",
            "john.doe@example.com",
            "John Doe",
            null);
    }

    [Fact]
    public async Task GetCurrentUser_UsesUnknownUser_WhenNoNameOrEmail()
    {
        var testUser = CreateTestUser();
        SetupHttpContext(authenticated: true,
            new Claim(ClaimTypes.NameIdentifier, "auth0|123"));

        _userService.GetOrCreateUserAsync(
            Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string>(), Arg.Any<string?>())
            .Returns(testUser);

        await _sut.GetCurrentUserAsync();

        await _userService.Received(1).GetOrCreateUserAsync(
            "auth0|123",
            "",
            "Unknown User",
            null);
    }

    [Fact]
    public async Task GetCurrentUser_ExtractsName_WithUnderscoreSeparator()
    {
        var testUser = CreateTestUser();
        SetupHttpContext(authenticated: true,
            new Claim(ClaimTypes.NameIdentifier, "auth0|123"),
            new Claim(ClaimTypes.Email, "jane_smith@test.com"));

        _userService.GetOrCreateUserAsync(
            Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string>(), Arg.Any<string?>())
            .Returns(testUser);

        await _sut.GetCurrentUserAsync();

        await _userService.Received(1).GetOrCreateUserAsync(
            "auth0|123",
            "jane_smith@test.com",
            "Jane Smith",
            null);
    }

    [Fact]
    public async Task GetCurrentUser_FallsBackToUnknown_WhenEmailIsAllDigits()
    {
        var testUser = CreateTestUser();
        SetupHttpContext(authenticated: true,
            new Claim(ClaimTypes.NameIdentifier, "auth0|123"),
            new Claim(ClaimTypes.Email, "12345@test.com"));

        _userService.GetOrCreateUserAsync(
            Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string>(), Arg.Any<string?>())
            .Returns(testUser);

        await _sut.GetCurrentUserAsync();

        // "12345" is all digits, so ExtractNameFromEmail returns null → "Unknown User"
        await _userService.Received(1).GetOrCreateUserAsync(
            "auth0|123",
            "12345@test.com",
            "Unknown User",
            null);
    }

    [Fact]
    public async Task GetCurrentUser_FallsBackToUnknown_WhenEmailLocalPartTooShort()
    {
        var testUser = CreateTestUser();
        SetupHttpContext(authenticated: true,
            new Claim(ClaimTypes.NameIdentifier, "auth0|123"),
            new Claim(ClaimTypes.Email, "a@test.com"));

        _userService.GetOrCreateUserAsync(
            Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string>(), Arg.Any<string?>())
            .Returns(testUser);

        await _sut.GetCurrentUserAsync();

        // "a" is too short (length < 2 after title case), → "Unknown User"
        await _userService.Received(1).GetOrCreateUserAsync(
            "auth0|123",
            "a@test.com",
            "Unknown User",
            null);
    }

    // ── GetRequiredUserAsync ─────────────────────────────────────

    [Fact]
    public async Task GetRequiredUser_ReturnsUser_WhenAuthenticated()
    {
        var testUser = CreateTestUser();
        SetupHttpContext(authenticated: true,
            new Claim(ClaimTypes.NameIdentifier, "auth0|123"),
            new Claim(ClaimTypes.Email, "test@test.com"),
            new Claim(ClaimTypes.Name, "Test"));

        _userService.GetOrCreateUserAsync(
            Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string>(), Arg.Any<string?>())
            .Returns(testUser);

        var result = await _sut.GetRequiredUserAsync();

        Assert.Equal(testUser, result);
    }

    [Fact]
    public async Task GetRequiredUser_Throws_WhenNotAuthenticated()
    {
        SetupHttpContext(authenticated: false);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.GetRequiredUserAsync());
    }

    // ── IsSysAdminAsync ──────────────────────────────────────────

    [Fact]
    public async Task IsSysAdminAsync_ReturnsFalse_WhenNotAuthenticated()
    {
        SetupHttpContext(authenticated: false);

        Assert.False(await _sut.IsSysAdminAsync());
        _sysAdminChecker.DidNotReceive().IsSysAdmin(Arg.Any<string>(), Arg.Any<string?>());
    }

    [Fact]
    public async Task IsSysAdminAsync_ReturnsFalse_WhenHttpContextIsNull()
    {
        SetupNullHttpContext();

        Assert.False(await _sut.IsSysAdminAsync());
        _sysAdminChecker.DidNotReceive().IsSysAdmin(Arg.Any<string>(), Arg.Any<string?>());
    }

    [Fact]
    public async Task IsSysAdminAsync_ReturnsTrue_WhenCheckerConfirms()
    {
        var testUser = CreateTestUser("auth0|sysadmin");
        SetupHttpContext(authenticated: true,
            new Claim(ClaimTypes.NameIdentifier, "auth0|sysadmin"),
            new Claim(ClaimTypes.Email, "admin@chronicis.app"),
            new Claim(ClaimTypes.Name, "Admin"));

        _userService.GetOrCreateUserAsync(Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string>(), Arg.Any<string?>())
            .Returns((User?)testUser);

        _sysAdminChecker.IsSysAdmin("auth0|sysadmin", testUser.Email).Returns(true);

        Assert.True(await _sut.IsSysAdminAsync());
    }

    [Fact]
    public async Task IsSysAdminAsync_ReturnsFalse_WhenCheckerDenies()
    {
        var testUser = CreateTestUser("auth0|regular");
        SetupHttpContext(authenticated: true,
            new Claim(ClaimTypes.NameIdentifier, "auth0|regular"),
            new Claim(ClaimTypes.Email, "player@example.com"),
            new Claim(ClaimTypes.Name, "Player"));

        _userService.GetOrCreateUserAsync(Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string>(), Arg.Any<string?>())
            .Returns(testUser);

        _sysAdminChecker.IsSysAdmin(Arg.Any<string>(), Arg.Any<string?>()).Returns(false);

        Assert.False(await _sut.IsSysAdminAsync());
    }

    [Fact]
    public async Task IsSysAdminAsync_PassesNullEmail_WhenUserNotFound()
    {
        SetupHttpContext(authenticated: true,
            new Claim(ClaimTypes.NameIdentifier, "auth0|ghost"));

        _userService.GetOrCreateUserAsync(Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string>(), Arg.Any<string?>())
            .Returns((User?)null);

        _sysAdminChecker.IsSysAdmin("auth0|ghost", null).Returns(false);

        Assert.False(await _sut.IsSysAdminAsync());
        _sysAdminChecker.Received(1).IsSysAdmin("auth0|ghost", null);
    }
}
