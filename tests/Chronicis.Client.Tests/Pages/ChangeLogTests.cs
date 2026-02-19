using Chronicis.Client.Pages;
using Chronicis.Client.Tests.Components;
using Xunit;

namespace Chronicis.Client.Tests.Pages;

public class ChangeLogTests : MudBlazorTestContext
{
    [Fact]
    public void ChangeLog_RendersHeading() =>
        Assert.Contains("What's New", RenderComponent<ChangeLog>().Markup, StringComparison.OrdinalIgnoreCase);
}
