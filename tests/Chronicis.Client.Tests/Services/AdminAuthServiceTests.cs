using Chronicis.Client.Services;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Chronicis.Client.Tests.Services;

public class AdminAuthServiceTests
{
    [Fact]
    public async Task IsSysAdminAsync_ReturnsFalse_WhenNoUser()
    {
        var auth = Substitute.For<IAuthService>();
        auth.GetCurrentUserAsync().Returns((UserInfo?)null);
        var sut = new AdminAuthService(auth, NullLogger<AdminAuthService>.Instance);

        var result = await sut.IsSysAdminAsync();

        Assert.False(result);
    }

    [Fact]
    public async Task IsSysAdminAsync_ReturnsTrue_ForKnownEmail()
    {
        var auth = Substitute.For<IAuthService>();
        auth.GetCurrentUserAsync().Returns(new UserInfo { Email = "DAVE@CHRONICIS.APP" });
        var sut = new AdminAuthService(auth, NullLogger<AdminAuthService>.Instance);

        var result = await sut.IsSysAdminAsync();

        Assert.True(result);
    }

    [Fact]
    public async Task IsSysAdminAsync_ReturnsTrue_ForKnownAuth0Id()
    {
        var auth = Substitute.For<IAuthService>();
        auth.GetCurrentUserAsync().Returns(new UserInfo { Auth0UserId = "oauth2|discord|992501439685460139" });
        var sut = new AdminAuthService(auth, NullLogger<AdminAuthService>.Instance);

        var result = await sut.IsSysAdminAsync();

        Assert.True(result);
    }
}

