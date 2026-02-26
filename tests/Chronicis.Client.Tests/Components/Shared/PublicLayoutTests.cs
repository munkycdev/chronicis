using System.Net;
using Bunit.TestDoubles;
using Chronicis.Client.Components.Shared;
using Chronicis.Client.Services;
using Chronicis.Client.Tests.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using MudBlazor;
using Xunit;

namespace Chronicis.Client.Tests.Components.Shared;

public class PublicLayoutTests : MudBlazorTestContext
{
    public PublicLayoutTests()
    {
        Services.AddSingleton(new MudTheme());
        Services.AddAuthorizationCore();

        // PublicFooter requires IVersionService
        var json = """{"version":"3.0.0-test","buildNumber":"0","sha":"test","buildDate":""}""";
        Services.AddSingleton<IVersionService>(
            new VersionService(
                TestHttpMessageHandler.CreateClient(HttpStatusCode.OK, json),
                NullLogger<VersionService>.Instance));
    }

    [Fact]
    public void PublicLayout_RendersBodyNavAndFooter()
    {
        var auth = this.AddTestAuthorization();
        auth.SetNotAuthorized();

        var cut = RenderComponent<PublicLayout>(parameters => parameters
            .Add(p => p.Body, (RenderFragment)(builder => builder.AddContent(0, "layout-body"))));

        Assert.Contains("public-layout", cut.Markup);
        Assert.Contains("public-main", cut.Markup);
        Assert.Contains("layout-body", cut.Markup);
        Assert.Contains("public-nav", cut.Markup);
        Assert.Contains("chronicis-footer", cut.Markup);
    }
}
