using System.Diagnostics.CodeAnalysis;
using Chronicis.Client.Components.Settings;
using Xunit;

namespace Chronicis.Client.Tests.Components.Settings;

[ExcludeFromCodeCoverage]
public class PreferencesSectionTests : MudBlazorTestContext
{
    [Fact]
    public void RendersPreferencePlaceholders()
    {
        var cut = RenderComponent<PreferencesSection>();

        Assert.Contains("Theme", cut.Markup);
        Assert.Contains("Editor", cut.Markup);
        Assert.Contains("Notifications", cut.Markup);
        Assert.Contains("coming soon", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }
}
