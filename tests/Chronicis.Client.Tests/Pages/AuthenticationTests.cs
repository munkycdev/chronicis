using System.Reflection;
using Chronicis.Client.Pages;
using Chronicis.Client.Tests.Components;
using Xunit;

namespace Chronicis.Client.Tests.Pages;

public class AuthenticationTests : MudBlazorTestContext
{
    [Fact]
    public void Authentication_Action_CanBeSet()
    {
        var sut = new Authentication();
        SetProperty(sut, nameof(Authentication.Action), "login");

        Assert.Equal("login", sut.Action);
    }

    private static void SetProperty(object instance, string propertyName, object? value)
    {
        var property = instance.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(property);
        property!.SetValue(instance, value);
    }
}
