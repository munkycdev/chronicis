using Chronicis.Client.Pages;
using Chronicis.Client.Tests.Components;
using Xunit;

namespace Chronicis.Client.Tests.Pages;

public class TermsTests : MudBlazorTestContext
{
    [Fact]
    public void Terms_RendersHeading() =>
        Assert.Contains("Terms of Service", RenderComponent<Terms>().Markup, StringComparison.OrdinalIgnoreCase);
}
