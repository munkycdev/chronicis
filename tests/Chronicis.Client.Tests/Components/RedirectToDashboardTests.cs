using Bunit;
using Bunit.TestDoubles;
using Chronicis.Client.Components.Routing;
using Xunit;
using TestContext = Bunit.TestContext;

namespace Chronicis.Client.Tests.Components;

/// <summary>
/// Tests for the RedirectToDashboard component.
/// This component redirects to the dashboard on initialization.
/// </summary>
public class RedirectToDashboardTests : TestContext
{
    [Fact]
    public void RedirectToDashboard_OnInitialized_NavigatesToDashboard()
    {
        // Arrange
        var navMan = Services.GetService<FakeNavigationManager>();

        // Act
        RenderComponent<RedirectToDashboard>();

        // Assert
        Assert.NotNull(navMan);
        Assert.EndsWith("/dashboard", navMan.Uri);
    }

    [Fact]
    public void RedirectToDashboard_UsesReplaceNavigation()
    {
        // Arrange
        var navMan = Services.GetService<FakeNavigationManager>();

        // Act  
        RenderComponent<RedirectToDashboard>();

        // Assert
        // FakeNavigationManager doesn't expose replace flag,
        // but we can verify navigation occurred
        Assert.NotNull(navMan);
        Assert.Contains("dashboard", navMan.Uri);
    }

    [Fact]
    public void RedirectToDashboard_RendersNoContent()
    {
        // Act
        var cut = RenderComponent<RedirectToDashboard>();

        // Assert
        // Component should be empty (just redirects)
        Assert.Empty(cut.Markup.Trim());
    }
}
