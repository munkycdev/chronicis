using Chronicis.Client.Pages;
using Chronicis.Client.Tests.Components;
using Xunit;

namespace Chronicis.Client.Tests.Pages;

public class AboutTests : MudBlazorTestContext
{
    [Fact]
    public void About_RendersHeading() =>
        Assert.Contains("About Chronicis", RenderComponent<About>().Markup, StringComparison.OrdinalIgnoreCase);
}
