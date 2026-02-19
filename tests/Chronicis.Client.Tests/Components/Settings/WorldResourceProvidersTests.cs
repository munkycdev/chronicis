using System.Diagnostics.CodeAnalysis;
using Chronicis.Client.Components.Settings;
using Xunit;

namespace Chronicis.Client.Tests.Components.Settings;

[ExcludeFromCodeCoverage]
public class WorldResourceProvidersTests
{
    [Fact]
    public void ComponentType_IsAvailable()
    {
        Assert.Equal("WorldResourceProviders", typeof(WorldResourceProviders).Name);
    }
}
