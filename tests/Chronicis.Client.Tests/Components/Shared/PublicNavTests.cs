using Bunit.TestDoubles;
using Chronicis.Client.Components.Shared;
using Xunit;

namespace Chronicis.Client.Tests.Components.Shared;

public class PublicNavTests : MudBlazorTestContext
{
    [Fact]
    public void PublicNav_WhenAuthenticated_ShowsDashboardButton()
    {
        var authContext = this.AddTestAuthorization();
        authContext.SetAuthorized("test");

        var cut = RenderComponent<PublicNav>();

        Assert.Contains("Open Dashboard", cut.Markup);
        Assert.Contains("/dashboard", cut.Markup);
    }

    [Fact]
    public void PublicNav_WhenUnauthenticated_ShowsLoginButton()
    {
        var authContext = this.AddTestAuthorization();
        authContext.SetNotAuthorized();

        var cut = RenderComponent<PublicNav>();

        Assert.Contains("authentication/login", cut.Markup);
    }
}
