using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Bunit.TestDoubles;
using Chronicis.Client.Components.Routing;
using Chronicis.Client.Components.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Xunit;

namespace Chronicis.Client.Tests.Components.Routing;

[ExcludeFromCodeCoverage]
public class ChronicisRouteViewTests : MudBlazorTestContext
{
    private readonly TestAuthorizationContext _authContext;

    public ChronicisRouteViewTests()
    {
        _authContext = this.AddTestAuthorization();
    }

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

    [Fact]
    public void Render_WithExplicitSimpleLayout_RendersPublicPageContent()
    {
        var routeData = new RouteData(typeof(TestSimplePublicPage), new Dictionary<string, object?>());
        var cut = RenderComponent<ChronicisRouteView>(p => p.Add(x => x.RouteData, routeData));

        Assert.Contains("PUBLIC-PAGE", cut.Markup);
    }

    [Fact]
    public void Render_WithExplicitSimpleLayoutAndAuthorize_RendersNotAuthorizedFlow()
    {
        _authContext.SetNotAuthorized();
        var routeData = new RouteData(typeof(TestSimpleAuthorizedPage), new Dictionary<string, object?>());
        var cut = RenderComponent<ChronicisRouteView>(p => p.Add(x => x.RouteData, routeData));

        Assert.Contains("LAYOUT:", cut.Markup);
        Assert.DoesNotContain("AUTH-PAGE", cut.Markup);
    }

    [Fact]
    public void Render_WithExplicitSimpleLayoutAndAuthorize_WhenAuthorizing_ShowsAuthorizingScreen()
    {
        _authContext.SetAuthorizing();
        var routeData = new RouteData(typeof(TestSimpleAuthorizedPage), new Dictionary<string, object?>());

        var cut = RenderComponent<ChronicisRouteView>(p => p.Add(x => x.RouteData, routeData));

        Assert.Contains("Loading Chronicis", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Render_PublicPage_PassesRouteValuesToPageComponent()
    {
        var routeData = new RouteData(
            typeof(TestSimplePublicPageWithRouteValue),
            new Dictionary<string, object?> { ["Slug"] = "acid-arrow" });

        var cut = RenderComponent<ChronicisRouteView>(p => p.Add(x => x.RouteData, routeData));

        Assert.Contains("ROUTE:acid-arrow", cut.Markup);
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

    private sealed class TestSimpleLayout : LayoutComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "section");
            builder.AddContent(1, "LAYOUT:");
            builder.AddContent(2, Body);
            builder.CloseElement();
        }
    }

    [Route("/simple-public")]
    [Layout(typeof(TestSimpleLayout))]
    private sealed class TestSimplePublicPage : ComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.AddContent(0, "PUBLIC-PAGE");
        }
    }

    [Route("/simple-public-with-route-value/{Slug}")]
    [Layout(typeof(TestSimpleLayout))]
    private sealed class TestSimplePublicPageWithRouteValue : ComponentBase
    {
        [Parameter]
        public string? Slug { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.AddContent(0, $"ROUTE:{Slug}");
        }
    }

    [Route("/simple-auth")]
    [Authorize]
    [Layout(typeof(TestSimpleLayout))]
    private sealed class TestSimpleAuthorizedPage : ComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.AddContent(0, "AUTH-PAGE");
        }
    }
}
