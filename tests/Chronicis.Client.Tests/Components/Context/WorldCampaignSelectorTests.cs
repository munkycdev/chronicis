using System.Diagnostics.CodeAnalysis;
using Chronicis.Client.Components.Context;
using Xunit;

namespace Chronicis.Client.Tests.Components.Context;

[ExcludeFromCodeCoverage]
public class WorldCampaignSelectorTests
{
    [Fact]
    public void ComponentType_IsAvailable()
    {
        Assert.Equal("WorldCampaignSelector", typeof(WorldCampaignSelector).Name);
    }
}
