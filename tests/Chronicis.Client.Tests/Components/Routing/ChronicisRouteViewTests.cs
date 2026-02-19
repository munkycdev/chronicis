using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Chronicis.Client.Components.Routing;
using Chronicis.Client.Components.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Xunit;

namespace Chronicis.Client.Tests.Components.Routing;

[ExcludeFromCodeCoverage]
public class ChronicisRouteViewTests : MudBlazorTestContext
{
    [Fact]
    public void DetermineLayoutType_UsesExplicitLayout()
    {
        var instance = new ChronicisRouteView
        {
            RouteData = new RouteData(typeof(TestLayoutPage), new Dictionary<string, object?>())
        };
        var result = InvokeDetermineLayoutType(instance, typeof(TestLayoutPage));
        Assert.Equal(typeof(PublicLayout), result);
    }

    [Fact]
    public void DetermineLayoutType_UsesAuthenticatedLayout_ForAuthorizePages()
    {
        var instance = new ChronicisRouteView
        {
            RouteData = new RouteData(typeof(TestAuthorizedPage), new Dictionary<string, object?>())
        };
        var result = InvokeDetermineLayoutType(instance, typeof(TestAuthorizedPage));
        Assert.Equal(typeof(Chronicis.Client.Components.Layout.AuthenticatedLayout), result);
    }

    [Fact]
    public void DetermineLayoutType_DefaultsToPublicLayout()
    {
        var instance = new ChronicisRouteView
        {
            RouteData = new RouteData(typeof(TestPublicPage), new Dictionary<string, object?>())
        };
        var result = InvokeDetermineLayoutType(instance, typeof(TestPublicPage));
        Assert.Equal(typeof(PublicLayout), result);
    }

    private static Type? InvokeDetermineLayoutType(ChronicisRouteView instance, Type pageType)
    {
        var method = typeof(ChronicisRouteView).GetMethod("DetermineLayoutType", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);
        return (Type?)method!.Invoke(instance, [pageType]);
    }

    [Route("/test-public")]
    private sealed class TestPublicPage : ComponentBase { }

    [Route("/test-auth")]
    [Authorize]
    private sealed class TestAuthorizedPage : ComponentBase { }

    [Route("/test-layout")]
    [Layout(typeof(PublicLayout))]
    private sealed class TestLayoutPage : ComponentBase { }
}
