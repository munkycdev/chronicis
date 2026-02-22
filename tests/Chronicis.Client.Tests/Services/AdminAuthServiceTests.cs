using Chronicis.Client.Services;
using Chronicis.Shared.Admin;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Chronicis.Client.Tests.Services;

public class AdminAuthServiceTests
{
    private static AdminAuthService CreateSut(
        IAuthService authService,
        ISysAdminChecker? checker = null)
    {
        checker ??= Substitute.For<ISysAdminChecker>();
        return new AdminAuthService(authService, checker, NullLogger<AdminAuthService>.Instance);
    }

    // ── No user ──────────────────────────────────────────────────────

    [Fact]
    public async Task IsSysAdminAsync_ReturnsFalse_WhenNoUser()
    {
        var auth = Substitute.For<IAuthService>();
        auth.GetCurrentUserAsync().Returns((UserInfo?)null);
        var sut = CreateSut(auth);

        var result = await sut.IsSysAdminAsync();

        Assert.False(result);
    }

    // ── Delegates to ISysAdminChecker ────────────────────────────────

    [Fact]
    public async Task IsSysAdminAsync_ReturnsTrue_WhenCheckerReturnsTrue()
    {
        var auth = Substitute.For<IAuthService>();
        auth.GetCurrentUserAsync().Returns(new UserInfo
        {
            Auth0UserId = "oauth2|discord|123",
            Email = "admin@example.com"
        });

        var checker = Substitute.For<ISysAdminChecker>();
        checker.IsSysAdmin("oauth2|discord|123", "admin@example.com").Returns(true);

        var sut = CreateSut(auth, checker);

        var result = await sut.IsSysAdminAsync();

        Assert.True(result);
    }

    [Fact]
    public async Task IsSysAdminAsync_ReturnsFalse_WhenCheckerReturnsFalse()
    {
        var auth = Substitute.For<IAuthService>();
        auth.GetCurrentUserAsync().Returns(new UserInfo
        {
            Auth0UserId = "oauth2|discord|999",
            Email = "stranger@example.com"
        });

        var checker = Substitute.For<ISysAdminChecker>();
        checker.IsSysAdmin(Arg.Any<string>(), Arg.Any<string?>()).Returns(false);

        var sut = CreateSut(auth, checker);

        var result = await sut.IsSysAdminAsync();

        Assert.False(result);
    }

    [Fact]
    public async Task IsSysAdminAsync_PassesEmailAndAuth0IdToChecker()
    {
        var auth = Substitute.For<IAuthService>();
        auth.GetCurrentUserAsync().Returns(new UserInfo
        {
            Auth0UserId = "oauth2|discord|992501439685460139",
            Email = "dave@chronicis.app"
        });

        var checker = Substitute.For<ISysAdminChecker>();
        checker.IsSysAdmin(Arg.Any<string>(), Arg.Any<string?>()).Returns(true);

        var sut = CreateSut(auth, checker);
        await sut.IsSysAdminAsync();

        checker.Received(1).IsSysAdmin("oauth2|discord|992501439685460139", "dave@chronicis.app");
    }

    [Fact]
    public async Task IsSysAdminAsync_DoesNotCallChecker_WhenNoUser()
    {
        var auth = Substitute.For<IAuthService>();
        auth.GetCurrentUserAsync().Returns((UserInfo?)null);
        var checker = Substitute.For<ISysAdminChecker>();
        var sut = CreateSut(auth, checker);

        await sut.IsSysAdminAsync();

        checker.DidNotReceive().IsSysAdmin(Arg.Any<string>(), Arg.Any<string?>());
    }
}
