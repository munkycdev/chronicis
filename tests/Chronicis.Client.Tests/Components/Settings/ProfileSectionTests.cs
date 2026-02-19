using System.Diagnostics.CodeAnalysis;
using Bunit;
using Chronicis.Client.Components.Settings;
using Chronicis.Client.Services;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Xunit;

namespace Chronicis.Client.Tests.Components.Settings;

[ExcludeFromCodeCoverage]
public class ProfileSectionTests : MudBlazorTestContext
{
    private readonly IAuthService _authService = Substitute.For<IAuthService>();

    public ProfileSectionTests()
    {
        Services.AddSingleton(_authService);
    }

    [Fact]
    public void RendersUserWithAvatar()
    {
        _authService.GetCurrentUserAsync().Returns(new UserInfo
        {
            DisplayName = "Aster",
            Email = "aster@example.com",
            AvatarUrl = "https://example.com/avatar.png"
        });

        var cut = RenderComponent<ProfileSection>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Aster", cut.Markup);
            Assert.Contains("aster@example.com", cut.Markup);
            Assert.Contains("avatar.png", cut.Markup);
        });
    }

    [Fact]
    public void RendersUserInitial_WhenNoAvatar()
    {
        _authService.GetCurrentUserAsync().Returns(new UserInfo
        {
            DisplayName = "Mira",
            Email = "mira@example.com",
            AvatarUrl = null
        });

        var cut = RenderComponent<ProfileSection>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("M", cut.Markup);
            Assert.Contains("mira@example.com", cut.Markup);
        });
    }

    [Fact]
    public void RendersWarning_WhenUserMissing()
    {
        _authService.GetCurrentUserAsync().Returns((UserInfo?)null);

        var cut = RenderComponent<ProfileSection>();

        cut.WaitForAssertion(() =>
            Assert.Contains("Unable to load profile information", cut.Markup, StringComparison.OrdinalIgnoreCase));
    }
}
