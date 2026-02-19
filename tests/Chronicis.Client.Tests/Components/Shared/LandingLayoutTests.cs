using Bunit.TestDoubles;
using Chronicis.Client.Components.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using Xunit;

namespace Chronicis.Client.Tests.Components.Shared;

public class LandingLayoutTests : MudBlazorTestContext
{
    public LandingLayoutTests()
    {
        Services.AddSingleton(new MudTheme());
        Services.AddAuthorizationCore();
    }

    [Fact]
    public void LandingLayout_RendersUsingPublicLayout()
    {
        var auth = this.AddTestAuthorization();
        auth.SetNotAuthorized();

        var cut = RenderComponent<LandingLayout>(parameters => parameters
            .Add(p => p.Body, (RenderFragment)(builder => builder.AddContent(0, "landing-body"))));

        Assert.NotNull(cut);
    }
}
