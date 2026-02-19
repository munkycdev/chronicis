using System.Diagnostics.CodeAnalysis;
using Chronicis.Client.Components.Layout;
using Xunit;

namespace Chronicis.Client.Tests.Components.Layout;

[ExcludeFromCodeCoverage]
public class MainLayoutTests
{
    [Fact]
    public void MainLayout_InheritsAuthenticatedLayout()
    {
        Assert.True(typeof(MainLayout).IsSubclassOf(typeof(AuthenticatedLayout)));
    }
}
