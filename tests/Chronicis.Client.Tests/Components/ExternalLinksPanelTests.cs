using Bunit;
using Chronicis.Client.Components.Articles;
using Chronicis.Shared.DTOs;
using Xunit;

namespace Chronicis.Client.Tests.Components;

/// <summary>
/// Tests for the REFACTORED ExternalLinksPanel component.
/// This component now accepts data as parameters - trivially easy to test!
/// 
/// Following the same pattern as BacklinksPanel and OutgoingLinksPanel.
/// </summary>
public class ExternalLinksPanelTests : TestContext
{
    [Fact]
    public void ExternalLinksPanel_DisplaysExternalLinks()
    {
        // Arrange
        var externalLinks = new List<ArticleExternalLinkDto>
        {
            new() { Source = "SRD", DisplayTitle = "Fireball", ExternalId = "fireball" },
            new() { Source = "Open5e", DisplayTitle = "Magic Missile", ExternalId = "magic-missile" }
        };

        // Act
        var cut = RenderComponent<ExternalLinksPanel>(parameters => parameters
            .Add(p => p.ExternalLinks, externalLinks));

        // Assert
        Assert.Contains("Fireball", cut.Markup);
        Assert.Contains("Magic Missile", cut.Markup);
    }

    [Fact]
    public void ExternalLinksPanel_ShowsEmptyState_WhenNoLinks()
    {
        // Arrange - empty list
        var externalLinks = new List<ArticleExternalLinkDto>();

        // Act
        var cut = RenderComponent<ExternalLinksPanel>(parameters => parameters
            .Add(p => p.ExternalLinks, externalLinks));

        // Assert
        Assert.Contains("No external resources", cut.Markup);
    }

    [Fact]
    public void ExternalLinksPanel_ShowsLoadingIndicator_WhenLoading()
    {
        // Arrange
        var externalLinks = new List<ArticleExternalLinkDto>();

        // Act
        var cut = RenderComponent<ExternalLinksPanel>(parameters => parameters
            .Add(p => p.ExternalLinks, externalLinks)
            .Add(p => p.IsLoading, true));

        // Assert
        Assert.Contains("...", cut.Markup);
    }

    [Fact]
    public void ExternalLinksPanel_ShowsCorrectCount()
    {
        // Arrange
        var externalLinks = new List<ArticleExternalLinkDto>
        {
            new() { Source = "SRD", DisplayTitle = "Link 1", ExternalId = "key1" },
            new() { Source = "SRD", DisplayTitle = "Link 2", ExternalId = "key2" },
            new() { Source = "Open5e", DisplayTitle = "Link 3", ExternalId = "key3" }
        };

        // Act
        var cut = RenderComponent<ExternalLinksPanel>(parameters => parameters
            .Add(p => p.ExternalLinks, externalLinks));

        // Assert
        var countElement = cut.Find(".links-panel-count");
        Assert.Equal("3", countElement.TextContent);
    }

    [Fact]
    public void ExternalLinksPanel_GroupsBySource()
    {
        // Arrange
        var externalLinks = new List<ArticleExternalLinkDto>
        {
            new() { Source = "SRD", DisplayTitle = "Fireball", ExternalId = "fireball" },
            new() { Source = "SRD", DisplayTitle = "Magic Missile", ExternalId = "magic-missile" },
            new() { Source = "Open5e", DisplayTitle = "Lightning Bolt", ExternalId = "lightning-bolt" }
        };

        // Act
        var cut = RenderComponent<ExternalLinksPanel>(parameters => parameters
            .Add(p => p.ExternalLinks, externalLinks));

        // Assert - Should show both source groups
        Assert.Contains("SRD", cut.Markup);
        Assert.Contains("Open5e", cut.Markup); // Displayed as "Open5e"
    }

    [Fact]
    public void ExternalLinksPanel_DoesNotShowLoading_WhenNotLoading()
    {
        // Arrange
        var externalLinks = new List<ArticleExternalLinkDto>
        {
            new() { Source = "SRD", DisplayTitle = "Link", ExternalId = "key" }
        };

        // Act
        var cut = RenderComponent<ExternalLinksPanel>(parameters => parameters
            .Add(p => p.ExternalLinks, externalLinks)
            .Add(p => p.IsLoading, false));

        // Assert
        Assert.DoesNotContain("...", cut.Markup);
    }

    [Fact]
    public void ExternalLinksPanel_ShowsCorrectHeader()
    {
        // Arrange
        var externalLinks = new List<ArticleExternalLinkDto>();

        // Act
        var cut = RenderComponent<ExternalLinksPanel>(parameters => parameters
            .Add(p => p.ExternalLinks, externalLinks));

        // Assert - Should say "External Resources"
        Assert.Contains("External Resources", cut.Markup);
    }

    [Fact]
    public void ExternalLinksPanel_DisplaysSourceBadges()
    {
        // Arrange
        var externalLinks = new List<ArticleExternalLinkDto>
        {
            new() { Source = "SRD", DisplayTitle = "Spell 1", ExternalId = "key1" },
            new() { Source = "Open5e", DisplayTitle = "Spell 2", ExternalId = "key2" }
        };

        // Act
        var cut = RenderComponent<ExternalLinksPanel>(parameters => parameters
            .Add(p => p.ExternalLinks, externalLinks));

        // Assert
        var badges = cut.FindAll(".external-links-source-badge");
        Assert.Equal(2, badges.Count);
    }
}
