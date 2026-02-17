using Chronicis.Client.Services;
using Chronicis.Shared.DTOs;
using Xunit;

namespace Chronicis.Client.Tests.Services;

public class BreadcrumbServiceTests
{
    private readonly BreadcrumbService _sut;

    public BreadcrumbServiceTests()
    {
        _sut = new BreadcrumbService();
    }

    // ════════════════════════════════════════════════════════════════
    //  ForWorld Tests
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void ForWorld_ReturnsDashboardAndWorld()
    {
        // Arrange
        var world = new WorldDto { Id = Guid.NewGuid(), Name = "Middle Earth" };

        // Act
        var breadcrumbs = _sut.ForWorld(world);

        // Assert
        Assert.Equal(2, breadcrumbs.Count);
        Assert.Equal("Dashboard", breadcrumbs[0].Text);
        Assert.Equal("/dashboard", breadcrumbs[0].Href);
        Assert.Equal("Middle Earth", breadcrumbs[1].Text);
    }

    [Fact]
    public void ForWorld_WithCurrentDisabledTrue_DisablesWorldBreadcrumb()
    {
        // Arrange
        var world = new WorldDto { Id = Guid.NewGuid(), Name = "Middle Earth" };

        // Act
        var breadcrumbs = _sut.ForWorld(world, currentDisabled: true);

        // Assert
        Assert.True(breadcrumbs[1].Disabled);
        Assert.Null(breadcrumbs[1].Href);
    }

    [Fact]
    public void ForWorld_WithCurrentDisabledFalse_EnablesWorldBreadcrumb()
    {
        // Arrange
        var worldId = Guid.NewGuid();
        var world = new WorldDto { Id = worldId, Name = "Middle Earth" };

        // Act
        var breadcrumbs = _sut.ForWorld(world, currentDisabled: false);

        // Assert
        Assert.False(breadcrumbs[1].Disabled);
        Assert.Equal($"/world/{worldId}", breadcrumbs[1].Href);
    }

    // ════════════════════════════════════════════════════════════════
    //  ForCampaign Tests
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void ForCampaign_ReturnsDashboardWorldAndCampaign()
    {
        // Arrange
        var worldId = Guid.NewGuid();
        var campaignId = Guid.NewGuid();
        var world = new WorldDto { Id = worldId, Name = "Middle Earth" };
        var campaign = new CampaignDto { Id = campaignId, Name = "War of the Ring" };

        // Act
        var breadcrumbs = _sut.ForCampaign(campaign, world);

        // Assert
        Assert.Equal(3, breadcrumbs.Count);
        Assert.Equal("Dashboard", breadcrumbs[0].Text);
        Assert.Equal("Middle Earth", breadcrumbs[1].Text);
        Assert.Equal($"/world/{worldId}", breadcrumbs[1].Href);
        Assert.Equal("War of the Ring", breadcrumbs[2].Text);
    }

    [Fact]
    public void ForCampaign_WithCurrentDisabledTrue_DisablesCampaignBreadcrumb()
    {
        // Arrange
        var world = new WorldDto { Id = Guid.NewGuid(), Name = "Middle Earth" };
        var campaign = new CampaignDto { Id = Guid.NewGuid(), Name = "War of the Ring" };

        // Act
        var breadcrumbs = _sut.ForCampaign(campaign, world, currentDisabled: true);

        // Assert
        Assert.True(breadcrumbs[2].Disabled);
        Assert.Null(breadcrumbs[2].Href);
    }

    [Fact]
    public void ForCampaign_WithCurrentDisabledFalse_EnablesCampaignBreadcrumb()
    {
        // Arrange
        var campaignId = Guid.NewGuid();
        var world = new WorldDto { Id = Guid.NewGuid(), Name = "Middle Earth" };
        var campaign = new CampaignDto { Id = campaignId, Name = "War of the Ring" };

        // Act
        var breadcrumbs = _sut.ForCampaign(campaign, world, currentDisabled: false);

        // Assert
        Assert.False(breadcrumbs[2].Disabled);
        Assert.Equal($"/campaign/{campaignId}", breadcrumbs[2].Href);
    }

    // ════════════════════════════════════════════════════════════════
    //  ForArc Tests
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void ForArc_ReturnsDashboardWorldCampaignAndArc()
    {
        // Arrange
        var worldId = Guid.NewGuid();
        var campaignId = Guid.NewGuid();
        var arcId = Guid.NewGuid();
        var world = new WorldDto { Id = worldId, Name = "Middle Earth" };
        var campaign = new CampaignDto { Id = campaignId, Name = "War of the Ring" };
        var arc = new ArcDto { Id = arcId, Name = "The Fellowship" };

        // Act
        var breadcrumbs = _sut.ForArc(arc, campaign, world);

        // Assert
        Assert.Equal(4, breadcrumbs.Count);
        Assert.Equal("Dashboard", breadcrumbs[0].Text);
        Assert.Equal("Middle Earth", breadcrumbs[1].Text);
        Assert.Equal($"/world/{worldId}", breadcrumbs[1].Href);
        Assert.Equal("War of the Ring", breadcrumbs[2].Text);
        Assert.Equal($"/campaign/{campaignId}", breadcrumbs[2].Href);
        Assert.Equal("The Fellowship", breadcrumbs[3].Text);
    }

    [Fact]
    public void ForArc_WithCurrentDisabledTrue_DisablesArcBreadcrumb()
    {
        // Arrange
        var world = new WorldDto { Id = Guid.NewGuid(), Name = "Middle Earth" };
        var campaign = new CampaignDto { Id = Guid.NewGuid(), Name = "War of the Ring" };
        var arc = new ArcDto { Id = Guid.NewGuid(), Name = "The Fellowship" };

        // Act
        var breadcrumbs = _sut.ForArc(arc, campaign, world, currentDisabled: true);

        // Assert
        Assert.True(breadcrumbs[3].Disabled);
        Assert.Null(breadcrumbs[3].Href);
    }

    [Fact]
    public void ForArc_WithCurrentDisabledFalse_EnablesArcBreadcrumb()
    {
        // Arrange
        var arcId = Guid.NewGuid();
        var world = new WorldDto { Id = Guid.NewGuid(), Name = "Middle Earth" };
        var campaign = new CampaignDto { Id = Guid.NewGuid(), Name = "War of the Ring" };
        var arc = new ArcDto { Id = arcId, Name = "The Fellowship" };

        // Act
        var breadcrumbs = _sut.ForArc(arc, campaign, world, currentDisabled: false);

        // Assert
        Assert.False(breadcrumbs[3].Disabled);
        Assert.Equal($"/arc/{arcId}", breadcrumbs[3].Href);
    }

    // ════════════════════════════════════════════════════════════════
    //  ForArticle Tests
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void ForArticle_WithNullBreadcrumbs_ReturnsDashboardOnly()
    {
        // Act
        var breadcrumbs = _sut.ForArticle(null!);

        // Assert
        Assert.Single(breadcrumbs);
        Assert.Equal("Dashboard", breadcrumbs[0].Text);
    }

    [Fact]
    public void ForArticle_WithEmptyBreadcrumbs_ReturnsDashboardOnly()
    {
        // Act
        var breadcrumbs = _sut.ForArticle(new List<BreadcrumbDto>());

        // Assert
        Assert.Single(breadcrumbs);
        Assert.Equal("Dashboard", breadcrumbs[0].Text);
    }

    [Fact]
    public void ForArticle_WithWorldOnly_ReturnsDashboardAndWorld()
    {
        // Arrange
        var worldId = Guid.NewGuid();
        var apiBreadcrumbs = new List<BreadcrumbDto>
        {
            new BreadcrumbDto { Id = worldId, Title = "Middle Earth", Slug = "middle-earth", IsWorld = true }
        };

        // Act
        var breadcrumbs = _sut.ForArticle(apiBreadcrumbs);

        // Assert
        Assert.Equal(2, breadcrumbs.Count);
        Assert.Equal("Dashboard", breadcrumbs[0].Text);
        Assert.Equal("Middle Earth", breadcrumbs[1].Text);
        Assert.True(breadcrumbs[1].Disabled);
        Assert.Null(breadcrumbs[1].Href);
    }

    [Fact]
    public void ForArticle_WithWorldAndArticle_BuildsCorrectPath()
    {
        // Arrange
        var worldId = Guid.NewGuid();
        var articleId = Guid.NewGuid();
        var apiBreadcrumbs = new List<BreadcrumbDto>
        {
            new BreadcrumbDto { Id = worldId, Title = "Middle Earth", Slug = "middle-earth", IsWorld = true },
            new BreadcrumbDto { Id = articleId, Title = "Rivendell", Slug = "rivendell", IsWorld = false }
        };

        // Act
        var breadcrumbs = _sut.ForArticle(apiBreadcrumbs);

        // Assert
        Assert.Equal(3, breadcrumbs.Count);
        Assert.Equal("Dashboard", breadcrumbs[0].Text);
        Assert.Equal("Middle Earth", breadcrumbs[1].Text);
        Assert.Equal($"/world/{worldId}", breadcrumbs[1].Href);
        Assert.Equal("Rivendell", breadcrumbs[2].Text);
        Assert.True(breadcrumbs[2].Disabled);
        Assert.Null(breadcrumbs[2].Href);
    }

    [Fact]
    public void ForArticle_WithMultipleArticles_BuildsFullHierarchy()
    {
        // Arrange
        var worldId = Guid.NewGuid();
        var article1Id = Guid.NewGuid();
        var article2Id = Guid.NewGuid();
        var apiBreadcrumbs = new List<BreadcrumbDto>
        {
            new BreadcrumbDto { Id = worldId, Title = "Middle Earth", Slug = "middle-earth", IsWorld = true },
            new BreadcrumbDto { Id = article1Id, Title = "Locations", Slug = "locations", IsWorld = false },
            new BreadcrumbDto { Id = article2Id, Title = "Rivendell", Slug = "rivendell", IsWorld = false }
        };

        // Act
        var breadcrumbs = _sut.ForArticle(apiBreadcrumbs);

        // Assert
        Assert.Equal(4, breadcrumbs.Count);
        Assert.Equal("Dashboard", breadcrumbs[0].Text);
        Assert.Equal("Middle Earth", breadcrumbs[1].Text);
        Assert.Equal($"/world/{worldId}", breadcrumbs[1].Href);
        Assert.Equal("Locations", breadcrumbs[2].Text);
        Assert.Equal("/article/middle-earth/locations", breadcrumbs[2].Href);
        Assert.Equal("Rivendell", breadcrumbs[3].Text);
        Assert.True(breadcrumbs[3].Disabled);
    }

    // ════════════════════════════════════════════════════════════════
    //  BuildArticleUrl Tests
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void BuildArticleUrl_WithNullBreadcrumbs_ReturnsDashboard()
    {
        // Act
        var url = _sut.BuildArticleUrl(null!);

        // Assert
        Assert.Equal("/dashboard", url);
    }

    [Fact]
    public void BuildArticleUrl_WithEmptyBreadcrumbs_ReturnsDashboard()
    {
        // Act
        var url = _sut.BuildArticleUrl(new List<BreadcrumbDto>());

        // Assert
        Assert.Equal("/dashboard", url);
    }

    [Fact]
    public void BuildArticleUrl_WithSingleBreadcrumb_BuildsCorrectPath()
    {
        // Arrange
        var breadcrumbs = new List<BreadcrumbDto>
        {
            new BreadcrumbDto { Slug = "middle-earth" }
        };

        // Act
        var url = _sut.BuildArticleUrl(breadcrumbs);

        // Assert
        Assert.Equal("/article/middle-earth", url);
    }

    [Fact]
    public void BuildArticleUrl_WithMultipleBreadcrumbs_BuildsCorrectPath()
    {
        // Arrange
        var breadcrumbs = new List<BreadcrumbDto>
        {
            new BreadcrumbDto { Slug = "middle-earth" },
            new BreadcrumbDto { Slug = "locations" },
            new BreadcrumbDto { Slug = "rivendell" }
        };

        // Act
        var url = _sut.BuildArticleUrl(breadcrumbs);

        // Assert
        Assert.Equal("/article/middle-earth/locations/rivendell", url);
    }

    // ════════════════════════════════════════════════════════════════
    //  BuildArticleUrlToIndex Tests
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void BuildArticleUrlToIndex_WithNullBreadcrumbs_ReturnsDashboard()
    {
        // Act
        var url = _sut.BuildArticleUrlToIndex(null!, 0);

        // Assert
        Assert.Equal("/dashboard", url);
    }

    [Fact]
    public void BuildArticleUrlToIndex_WithEmptyBreadcrumbs_ReturnsDashboard()
    {
        // Act
        var url = _sut.BuildArticleUrlToIndex(new List<BreadcrumbDto>(), 0);

        // Assert
        Assert.Equal("/dashboard", url);
    }

    [Fact]
    public void BuildArticleUrlToIndex_WithNegativeIndex_ReturnsDashboard()
    {
        // Arrange
        var breadcrumbs = new List<BreadcrumbDto>
        {
            new BreadcrumbDto { Slug = "middle-earth" }
        };

        // Act
        var url = _sut.BuildArticleUrlToIndex(breadcrumbs, -1);

        // Assert
        Assert.Equal("/dashboard", url);
    }

    [Fact]
    public void BuildArticleUrlToIndex_WithIndexZero_ReturnsFirstElement()
    {
        // Arrange
        var breadcrumbs = new List<BreadcrumbDto>
        {
            new BreadcrumbDto { Slug = "middle-earth" },
            new BreadcrumbDto { Slug = "locations" }
        };

        // Act
        var url = _sut.BuildArticleUrlToIndex(breadcrumbs, 0);

        // Assert
        Assert.Equal("/article/middle-earth", url);
    }

    [Fact]
    public void BuildArticleUrlToIndex_WithMiddleIndex_ReturnsPartialPath()
    {
        // Arrange
        var breadcrumbs = new List<BreadcrumbDto>
        {
            new BreadcrumbDto { Slug = "middle-earth" },
            new BreadcrumbDto { Slug = "locations" },
            new BreadcrumbDto { Slug = "rivendell" }
        };

        // Act
        var url = _sut.BuildArticleUrlToIndex(breadcrumbs, 1);

        // Assert
        Assert.Equal("/article/middle-earth/locations", url);
    }

    [Fact]
    public void BuildArticleUrlToIndex_WithIndexBeyondRange_ClampsToLastElement()
    {
        // Arrange
        var breadcrumbs = new List<BreadcrumbDto>
        {
            new BreadcrumbDto { Slug = "middle-earth" },
            new BreadcrumbDto { Slug = "locations" }
        };

        // Act
        var url = _sut.BuildArticleUrlToIndex(breadcrumbs, 10);

        // Assert
        Assert.Equal("/article/middle-earth/locations", url);
    }
}
