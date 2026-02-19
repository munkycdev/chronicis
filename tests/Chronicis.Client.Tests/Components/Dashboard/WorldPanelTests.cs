using System.Diagnostics.CodeAnalysis;
using Chronicis.Client.Components.Dashboard;
using Xunit;

namespace Chronicis.Client.Tests.Components.Dashboard;

[ExcludeFromCodeCoverage]
public class WorldPanelTests
{
    [Fact]
    public void ComponentType_IsAvailable()
    {
        Assert.Equal("WorldPanel", typeof(WorldPanel).Name);
    }
}
