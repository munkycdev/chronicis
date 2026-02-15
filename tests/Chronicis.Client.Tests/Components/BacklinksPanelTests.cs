using Bunit;
using Chronicis.Client.Components.Articles;
using Chronicis.Shared.DTOs;
using Xunit;
using TestContext = Bunit.TestContext;

namespace Chronicis.Client.Tests.Components;

/// <summary>
/// Tests for the REFACTORED BacklinksPanel component.
/// This component now accepts data as parameters - MUCH easier to test!
/// 
/// Benefits of refactoring:
/// - No service dependencies to mock
/// - Simple data-driven tests
/// - Fast, focused assertions
/// - Clear component responsibility (display only)
/// </summary>
public class BacklinksPanelTests : TestContext
{
    [Fact]
    public void BacklinksPanel_DisplaysBacklinks()
    {
        // Arrange
        var backlinks = new List<BacklinkDto>
        {
            new() { ArticleId = Guid.NewGuid(), Title = "First Article", DisplayPath = "path1" },
            new() { ArticleId = Guid.NewGuid(), Title = "Second Article", DisplayPath = "path2" }
        };

        // Act
        var cut = RenderComponent<BacklinksPanel>(parameters => parameters
            .Add(p => p.Backlinks, backlinks));

        // Assert
        Assert.Contains("First Article", cut.Markup);
        Assert.Contains("Second Article", cut.Markup);
    }

    [Fact]
    public void BacklinksPanel_ShowsEmptyState_WhenNoBacklinks()
    {
        // Arrange - empty list
        var backlinks = new List<BacklinkDto>();

        // Act
        var cut = RenderComponent<BacklinksPanel>(parameters => parameters
            .Add(p => p.Backlinks, backlinks));

        // Assert
        Assert.Contains("No incoming links", cut.Markup);
    }

    [Fact]
    public void BacklinksPanel_ShowsLoadingIndicator_WhenLoading()
    {
        // Arrange
        var backlinks = new List<BacklinkDto>();

        // Act
        var cut = RenderComponent<BacklinksPanel>(parameters => parameters
            .Add(p => p.Backlinks, backlinks)
            .Add(p => p.IsLoading, true));

        // Assert
        Assert.Contains("...", cut.Markup);
    }

    [Fact]
    public void BacklinksPanel_ShowsCorrectCount()
    {
        // Arrange
        var backlinks = new List<BacklinkDto>
        {
            new() { ArticleId = Guid.NewGuid(), Title = "Article 1", DisplayPath = "path1" },
            new() { ArticleId = Guid.NewGuid(), Title = "Article 2", DisplayPath = "path2" },
            new() { ArticleId = Guid.NewGuid(), Title = "Article 3", DisplayPath = "path3" }
        };

        // Act
        var cut = RenderComponent<BacklinksPanel>(parameters => parameters
            .Add(p => p.Backlinks, backlinks));

        // Assert
        var countElement = cut.Find(".links-panel-count");
        Assert.Equal("3", countElement.TextContent);
    }

    [Fact]
    public async Task BacklinksPanel_ClickingBacklink_TriggersCallback()
    {
        // Arrange
        var articleId = Guid.NewGuid();
        var backlinks = new List<BacklinkDto>
        {
            new() { ArticleId = articleId, Title = "Test Article", DisplayPath = "display" }
        };

        Guid? clickedArticleId = null;

        // Act
        var cut = RenderComponent<BacklinksPanel>(parameters => parameters
            .Add(p => p.Backlinks, backlinks)
            .Add(p => p.OnNavigateToArticle, (Guid id) => clickedArticleId = id));

        var chip = cut.Find(".link-chip");
        await chip.ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        // Assert
        Assert.Equal(articleId, clickedArticleId);
    }

    [Fact]
    public void BacklinksPanel_RendersDisplayPath_AsTooltip()
    {
        // Arrange
        var backlinks = new List<BacklinkDto>
        {
            new() { ArticleId = Guid.NewGuid(), Title = "Article", DisplayPath = "World/Campaign/Article" }
        };

        // Act
        var cut = RenderComponent<BacklinksPanel>(parameters => parameters
            .Add(p => p.Backlinks, backlinks));

        // Assert
        var chip = cut.Find(".link-chip");
        Assert.Equal("World/Campaign/Article", chip.GetAttribute("title"));
    }

    [Fact]
    public void BacklinksPanel_DoesNotShowLoading_WhenNotLoading()
    {
        // Arrange
        var backlinks = new List<BacklinkDto>
        {
            new() { ArticleId = Guid.NewGuid(), Title = "Article", DisplayPath = "path" }
        };

        // Act
        var cut = RenderComponent<BacklinksPanel>(parameters => parameters
            .Add(p => p.Backlinks, backlinks)
            .Add(p => p.IsLoading, false));

        // Assert
        Assert.DoesNotContain("...", cut.Markup);
    }
}
