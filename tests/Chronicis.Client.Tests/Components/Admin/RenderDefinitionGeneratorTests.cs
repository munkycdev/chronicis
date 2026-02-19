using System.Diagnostics.CodeAnalysis;
using Chronicis.Client.Components.Admin;
using Xunit;

namespace Chronicis.Client.Tests.Components.Admin;

[ExcludeFromCodeCoverage]
public class RenderDefinitionGeneratorTests
{
    [Fact]
    public void ComponentType_IsAvailable()
    {
        Assert.Equal("RenderDefinitionGenerator", typeof(RenderDefinitionGenerator).Name);
    }
}
