using System.Diagnostics.CodeAnalysis;
using Chronicis.Client.Components.Layout;
using Xunit;

namespace Chronicis.Client.Tests.Components.Layout;

[ExcludeFromCodeCoverage]
public class AuthenticatedLayoutTests
{
    [Fact]
    public void Component_ImplementsAsyncDisposable()
    {
        Assert.Contains(typeof(IAsyncDisposable), typeof(AuthenticatedLayout).GetInterfaces());
    }
}
