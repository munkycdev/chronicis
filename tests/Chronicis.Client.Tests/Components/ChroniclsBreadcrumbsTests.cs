using Bunit;
using Chronicis.Client.Components.Shared;
using MudBlazor;
using Xunit;

namespace Chronicis.Client.Tests.Components;

/// <summary>
/// Tests for the ChroniclsBreadcrumbs component.
/// This component displays breadcrumb navigation with optional custom styling and actions.
/// </summary>
public class ChroniclsBreadcrumbsTests : MudBlazorTestContext
{
    [Fact]
    public void ChroniclsBreadcrumbs_WithNoItems_RendersEmpty()
    {
        // Act
        var cut = RenderComponent<ChroniclsBreadcrumbs>(parameters => parameters
            .Add(p => p.Items, null));

        // Assert
        var breadcrumbs = cut.FindComponents<MudBreadcrumbs>();
        Assert.Empty(breadcrumbs);
    }

    [Fact]
    public void ChroniclsBreadcrumbs_WithEmptyList_RendersEmpty()
    {
        // Act
        var cut = RenderComponent<ChroniclsBreadcrumbs>(parameters => parameters
            .Add(p => p.Items, new List<BreadcrumbItem>()));

        // Assert
        var breadcrumbs = cut.FindComponents<MudBreadcrumbs>();
        Assert.Empty(breadcrumbs);
    }

    [Fact]
    public void ChroniclsBreadcrumbs_WithItems_RendersBreadcrumbs()
    {
        // Arrange
        var items = new List<BreadcrumbItem>
        {
            new BreadcrumbItem("Home", "/"),
            new BreadcrumbItem("Worlds", "/worlds"),
            new BreadcrumbItem("My World", null, disabled: true)
        };

        // Act
        var cut = RenderComponent<ChroniclsBreadcrumbs>(parameters => parameters
            .Add(p => p.Items, items));

        // Assert
        var breadcrumbs = cut.FindComponent<MudBreadcrumbs>();
        Assert.NotNull(breadcrumbs);
        Assert.Contains("Home", cut.Markup);
        Assert.Contains("Worlds", cut.Markup);
        Assert.Contains("My World", cut.Markup);
    }

    [Fact]
    public void ChroniclsBreadcrumbs_WithDefaultLinks_UsesStandardRendering()
    {
        // Arrange
        var items = new List<BreadcrumbItem>
        {
            new BreadcrumbItem("Home", "/"),
            new BreadcrumbItem("Current", null, disabled: true)
        };

        // Act
        var cut = RenderComponent<ChroniclsBreadcrumbs>(parameters => parameters
            .Add(p => p.Items, items)
            .Add(p => p.UseCustomLinks, false));

        // Assert
        var breadcrumbs = cut.FindComponent<MudBreadcrumbs>();
        Assert.NotNull(breadcrumbs);
        // Should NOT have custom link class
        Assert.DoesNotContain("chronicis-breadcrumb-link", cut.Markup);
    }

    [Fact]
    public void ChroniclsBreadcrumbs_WithCustomLinks_UsesCustomStyling()
    {
        // Arrange
        var items = new List<BreadcrumbItem>
        {
            new BreadcrumbItem("Home", "/home"),
            new BreadcrumbItem("Current", null, disabled: true)
        };

        // Act
        var cut = RenderComponent<ChroniclsBreadcrumbs>(parameters => parameters
            .Add(p => p.Items, items)
            .Add(p => p.UseCustomLinks, true));

        // Assert
        var breadcrumbs = cut.FindComponent<MudBreadcrumbs>();
        Assert.NotNull(breadcrumbs);
        // Should have custom link class
        Assert.Contains("chronicis-breadcrumb-link", cut.Markup);
    }

    [Fact]
    public void ChroniclsBreadcrumbs_WithCssClass_AppliesClass()
    {
        // Arrange
        var items = new List<BreadcrumbItem>
        {
            new BreadcrumbItem("Test", "/test")
        };
        var cssClass = "my-custom-class";

        // Act
        var cut = RenderComponent<ChroniclsBreadcrumbs>(parameters => parameters
            .Add(p => p.Items, items)
            .Add(p => p.Class, cssClass));

        // Assert
        Assert.Contains(cssClass, cut.Markup);
    }

    [Fact]
    public void ChroniclsBreadcrumbs_HasToolbarClass()
    {
        // Arrange
        var items = new List<BreadcrumbItem>
        {
            new BreadcrumbItem("Test", "/test")
        };

        // Act
        var cut = RenderComponent<ChroniclsBreadcrumbs>(parameters => parameters
            .Add(p => p.Items, items));

        // Assert
        Assert.Contains("mud-toolbar", cut.Markup);
    }

    [Fact]
    public void ChroniclsBreadcrumbs_WithChildContent_RendersChildContent()
    {
        // Arrange
        var items = new List<BreadcrumbItem>
        {
            new BreadcrumbItem("Test", "/test")
        };

        // Act
        var cut = RenderComponent<ChroniclsBreadcrumbs>(parameters => parameters
            .Add(p => p.Items, items)
            .AddChildContent("<button class='action-button'>Action</button>"));

        // Assert
        Assert.Contains("action-button", cut.Markup);
        Assert.Contains("Action", cut.Markup);
    }

    [Fact]
    public void ChroniclsBreadcrumbs_IncludesSpacer()
    {
        // Arrange
        var items = new List<BreadcrumbItem>
        {
            new BreadcrumbItem("Test", "/test")
        };

        // Act
        var cut = RenderComponent<ChroniclsBreadcrumbs>(parameters => parameters
            .Add(p => p.Items, items));

        // Assert
        var spacer = cut.FindComponent<MudSpacer>();
        Assert.NotNull(spacer);
    }

    [Fact]
    public void ChroniclsBreadcrumbs_WithMultipleItems_RendersAll()
    {
        // Arrange
        var items = new List<BreadcrumbItem>
        {
            new BreadcrumbItem("Worlds", "/worlds"),
            new BreadcrumbItem("Forgotten Realms", "/worlds/1"),
            new BreadcrumbItem("Sword Coast", "/worlds/1/regions/1"),
            new BreadcrumbItem("Waterdeep", null, disabled: true)
        };

        // Act
        var cut = RenderComponent<ChroniclsBreadcrumbs>(parameters => parameters
            .Add(p => p.Items, items));

        // Assert
        Assert.Contains("Worlds", cut.Markup);
        Assert.Contains("Forgotten Realms", cut.Markup);
        Assert.Contains("Sword Coast", cut.Markup);
        Assert.Contains("Waterdeep", cut.Markup);
    }
}
