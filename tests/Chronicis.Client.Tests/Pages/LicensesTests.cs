using Chronicis.Client.Pages;
using Chronicis.Client.Tests.Components;
using Xunit;

namespace Chronicis.Client.Tests.Pages;

public class LicensesTests : MudBlazorTestContext
{
    [Fact]
    public void Licenses_RendersHeading() =>
        Assert.Contains("Licenses", RenderComponent<Licenses>().Markup, StringComparison.OrdinalIgnoreCase);
}
