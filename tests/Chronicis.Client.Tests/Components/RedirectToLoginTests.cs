using Bunit.TestDoubles;
using Chronicis.Client.Components.Routing;
using Xunit;
using TestContext = Bunit.TestContext;

namespace Chronicis.Client.Tests.Components;

/// <summary>
/// Tests for the RedirectToLogin component.
/// This component redirects to the login page on initialization.
/// </summary>
public class RedirectToLoginTests : TestContext
{
    [Fact]
    public void RedirectToLogin_OnInitialized_NavigatesToLogin()
    {
        // Arrange
        var navMan = Services.GetService<FakeNavigationManager>();

        // Act
        RenderComponent<RedirectToLogin>();

        // Assert
        Assert.NotNull(navMan);
        Assert.EndsWith("authentication/login", navMan.Uri);
    }

    [Fact]
    public void RedirectToLogin_NavigatesToAuthenticationPath()
    {
        // Arrange
        var navMan = Services.GetService<FakeNavigationManager>();

        // Act
        RenderComponent<RedirectToLogin>();

        // Assert
        Assert.NotNull(navMan);
        Assert.Contains("authentication", navMan.Uri);
        Assert.Contains("login", navMan.Uri);
    }

    [Fact]
    public void RedirectToLogin_RendersNoContent()
    {
        // Act
        var cut = RenderComponent<RedirectToLogin>();

        // Assert
        // Component should be empty (just redirects)
        Assert.Empty(cut.Markup.Trim());
    }
}
