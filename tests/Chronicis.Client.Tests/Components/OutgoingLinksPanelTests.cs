using Bunit;
using Chronicis.Client.Components.Articles;
using Chronicis.Shared.DTOs;
using Xunit;

namespace Chronicis.Client.Tests.Components;

/// <summary>
/// Tests for the REFACTORED OutgoingLinksPanel component.
/// This component now accepts data as parameters - trivially easy to test!
/// 
/// Following the same pattern as BacklinksPanel.
/// </summary>
public class OutgoingLinksPanelTests : TestContext
{
    [Fact]
    public void OutgoingLinksPanel_DisplaysOutgoingLinks()
    {
        // Arrange
        var outgoingLinks = new List<BacklinkDto>
        {
            new() { ArticleId = Guid.NewGuid(), Title = "First Link", DisplayPath = "path1" },
            new() { ArticleId = Guid.NewGuid(), Title = "Second Link", DisplayPath = "path2" }
        };

        // Act
        var cut = RenderComponent<OutgoingLinksPanel>(parameters => parameters
            .Add(p => p.OutgoingLinks, outgoingLinks));

        // Assert
        Assert.Contains("First Link", cut.Markup);
        Assert.Contains("Second Link", cut.Markup);
    }

    [Fact]
    public void OutgoingLinksPanel_ShowsEmptyState_WhenNoLinks()
    {
        // Arrange - empty list
        var outgoingLinks = new List<BacklinkDto>();

        // Act
        var cut = RenderComponent<OutgoingLinksPanel>(parameters => parameters
            .Add(p => p.OutgoingLinks, outgoingLinks));

        // Assert
        Assert.Contains("No outgoing links", cut.Markup);
    }

    [Fact]
    public void OutgoingLinksPanel_ShowsLoadingIndicator_WhenLoading()
    {
        // Arrange
        var outgoingLinks = new List<BacklinkDto>();

        // Act
        var cut = RenderComponent<OutgoingLinksPanel>(parameters => parameters
            .Add(p => p.OutgoingLinks, outgoingLinks)
            .Add(p => p.IsLoading, true));

        // Assert
        Assert.Contains("...", cut.Markup);
    }

    [Fact]
    public void OutgoingLinksPanel_ShowsCorrectCount()
    {
        // Arrange
        var outgoingLinks = new List<BacklinkDto>
        {
            new() { ArticleId = Guid.NewGuid(), Title = "Link 1", DisplayPath = "path1" },
            new() { ArticleId = Guid.NewGuid(), Title = "Link 2", DisplayPath = "path2" },
            new() { ArticleId = Guid.NewGuid(), Title = "Link 3", DisplayPath = "path3" }
        };

        // Act
        var cut = RenderComponent<OutgoingLinksPanel>(parameters => parameters
            .Add(p => p.OutgoingLinks, outgoingLinks));

        // Assert
        var countElement = cut.Find(".links-panel-count");
        Assert.Equal("3", countElement.TextContent);
    }

    [Fact]
    public async Task OutgoingLinksPanel_ClickingLink_TriggersCallback()
    {
        // Arrange
        var articleId = Guid.NewGuid();
        var outgoingLinks = new List<BacklinkDto>
        {
            new() { ArticleId = articleId, Title = "Test Link", DisplayPath = "display" }
        };

        Guid? clickedArticleId = null;

        // Act
        var cut = RenderComponent<OutgoingLinksPanel>(parameters => parameters
            .Add(p => p.OutgoingLinks, outgoingLinks)
            .Add(p => p.OnNavigateToArticle, (Guid id) => clickedArticleId = id));

        var chip = cut.Find(".link-chip");
        await chip.ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        // Assert
        Assert.Equal(articleId, clickedArticleId);
    }

    [Fact]
    public void OutgoingLinksPanel_RendersDisplayPath_AsTooltip()
    {
        // Arrange
        var outgoingLinks = new List<BacklinkDto>
        {
            new() { ArticleId = Guid.NewGuid(), Title = "Link", DisplayPath = "World/Campaign/Article" }
        };

        // Act
        var cut = RenderComponent<OutgoingLinksPanel>(parameters => parameters
            .Add(p => p.OutgoingLinks, outgoingLinks));

        // Assert
        var chip = cut.Find(".link-chip");
        Assert.Equal("World/Campaign/Article", chip.GetAttribute("title"));
    }

    [Fact]
    public void OutgoingLinksPanel_DoesNotShowLoading_WhenNotLoading()
    {
        // Arrange
        var outgoingLinks = new List<BacklinkDto>
        {
            new() { ArticleId = Guid.NewGuid(), Title = "Link", DisplayPath = "path" }
        };

        // Act
        var cut = RenderComponent<OutgoingLinksPanel>(parameters => parameters
            .Add(p => p.OutgoingLinks, outgoingLinks)
            .Add(p => p.IsLoading, false));

        // Assert
        Assert.DoesNotContain("...", cut.Markup);
    }

    [Fact]
    public void OutgoingLinksPanel_ShowsCorrectHeader()
    {
        // Arrange
        var outgoingLinks = new List<BacklinkDto>();

        // Act
        var cut = RenderComponent<OutgoingLinksPanel>(parameters => parameters
            .Add(p => p.OutgoingLinks, outgoingLinks));

        // Assert - Should say "Links To" not "Links From"
        Assert.Contains("Links To", cut.Markup);
    }
}
