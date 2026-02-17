using System.Diagnostics.CodeAnalysis;
using Bunit;
using Chronicis.Client.Components.Shared;
using Xunit;

namespace Chronicis.Client.Tests.Components;

/// <summary>
/// Tests for the LoadingSkeleton component.
/// This component displays a loading skeleton for article detail pages.
/// </summary>
[ExcludeFromCodeCoverage]
public class LoadingSkeletonTests : MudBlazorTestContext
{
    [Fact]
    public void LoadingSkeleton_Renders_Successfully()
    {
        // Act
        var cut = RenderComponent<LoadingSkeleton>();

        // Assert
        Assert.NotNull(cut);
    }

    [Fact]
    public void LoadingSkeleton_ContainsSkeletonElements()
    {
        // Act
        var cut = RenderComponent<LoadingSkeleton>();

        // Assert
        var skeletons = cut.FindAll(".chronicis-loading-skeleton");
        Assert.NotEmpty(skeletons);
        // Should have breadcrumb (3), title (1), metadata (2), body lines (9), buttons (3) = 18 total
        Assert.True(skeletons.Count >= 15, $"Expected at least 15 skeleton elements, found {skeletons.Count}");
    }

    [Fact]
    public void LoadingSkeleton_HasBreadcrumbSkeleton()
    {
        // Act
        var cut = RenderComponent<LoadingSkeleton>();

        // Assert - Breadcrumb separators
        Assert.Contains("›", cut.Markup);
    }

    [Fact]
    public void LoadingSkeleton_HasDivider()
    {
        // Act
        var cut = RenderComponent<LoadingSkeleton>();

        // Assert
        var divider = cut.Find(".chronicis-rune-divider");
        Assert.NotNull(divider);
    }

    [Fact]
    public void LoadingSkeleton_IsWrappedInPaper()
    {
        // Act
        var cut = RenderComponent<LoadingSkeleton>();

        // Assert
        var paper = cut.Find(".chronicis-article-card");
        Assert.NotNull(paper);
    }
}
