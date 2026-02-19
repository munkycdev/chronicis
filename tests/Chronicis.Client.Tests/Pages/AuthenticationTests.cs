using Chronicis.Client.Pages;
using Chronicis.Client.Tests.Components;
using Xunit;

namespace Chronicis.Client.Tests.Pages;

public class AuthenticationTests : MudBlazorTestContext
{
    [Fact]
    public void Authentication_Action_CanBeSet()
    {
        var sut = new Authentication
        {
            Action = "login"
        };

        Assert.Equal("login", sut.Action);
    }
}
