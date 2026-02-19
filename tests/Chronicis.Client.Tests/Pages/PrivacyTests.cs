using Chronicis.Client.Pages;
using Chronicis.Client.Tests.Components;
using Xunit;

namespace Chronicis.Client.Tests.Pages;

public class PrivacyTests : MudBlazorTestContext
{
    [Fact]
    public void Privacy_RendersHeading() =>
        Assert.Contains("Privacy Policy", RenderComponent<Privacy>().Markup, StringComparison.OrdinalIgnoreCase);
}
