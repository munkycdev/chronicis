using System.Diagnostics.CodeAnalysis;
using Chronicis.Client.Services;
using Chronicis.Client.Services.Routing;
using Chronicis.Shared.DTOs;
using Xunit;

namespace Chronicis.Client.Tests.Services;

[ExcludeFromCodeCoverage]
public class BreadcrumbServiceTests
{
    private readonly BreadcrumbService _sut;

    public BreadcrumbServiceTests()
    {
        _sut = new BreadcrumbService(new AppUrlBuilder());
    }

    // ════════════════════════════════════════════════════════════════
    //  ForWorld Tests
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void ForWorld_ReturnsDashboardAndWorld()
    {
        var world = new WorldDto { Id = Guid.NewGuid(), Name = "Middle Earth", Slug = "middle-earth" };

        var breadcrumbs = _sut.ForWorld(world);

        Assert.Equal(2, breadcrumbs.Count);
        Assert.Equal("Dashboard", breadcrumbs[0].Text);
        Assert.Equal("/dashboard", breadcrumbs[0].Href);
        Assert.Equal("Middle Earth", breadcrumbs[1].Text);
    }

    [Fact]
    public void ForWorld_WithCurrentDisabledTrue_DisablesWorldBreadcrumb()
    {
        var world = new WorldDto { Id = Guid.NewGuid(), Name = "Middle Earth", Slug = "middle-earth" };

        var breadcrumbs = _sut.ForWorld(world, currentDisabled: true);

        Assert.True(breadcrumbs[1].Disabled);
        Assert.Null(breadcrumbs[1].Href);
    }

    [Fact]
    public void ForWorld_WithCurrentDisabledFalse_EnablesWorldBreadcrumb()
    {
        var world = new WorldDto { Id = Guid.NewGuid(), Name = "Middle Earth", Slug = "middle-earth" };

        var breadcrumbs = _sut.ForWorld(world, currentDisabled: false);

        Assert.False(breadcrumbs[1].Disabled);
        Assert.Equal("/middle-earth", breadcrumbs[1].Href);
    }

    // ════════════════════════════════════════════════════════════════
    //  ForCampaign Tests
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void ForCampaign_ReturnsDashboardWorldAndCampaign()
    {
        var world = new WorldDto { Id = Guid.NewGuid(), Name = "Middle Earth", Slug = "middle-earth" };
        var campaign = new CampaignDto { Id = Guid.NewGuid(), Name = "War of the Ring", Slug = "war-of-the-ring" };

        var breadcrumbs = _sut.ForCampaign(campaign, world);

        Assert.Equal(3, breadcrumbs.Count);
        Assert.Equal("Dashboard", breadcrumbs[0].Text);
        Assert.Equal("Middle Earth", breadcrumbs[1].Text);
        Assert.Equal("/middle-earth", breadcrumbs[1].Href);
        Assert.Equal("War of the Ring", breadcrumbs[2].Text);
    }

    [Fact]
    public void ForCampaign_WithCurrentDisabledTrue_DisablesCampaignBreadcrumb()
    {
        var world = new WorldDto { Id = Guid.NewGuid(), Name = "Middle Earth", Slug = "middle-earth" };
        var campaign = new CampaignDto { Id = Guid.NewGuid(), Name = "War of the Ring", Slug = "war-of-the-ring" };

        var breadcrumbs = _sut.ForCampaign(campaign, world, currentDisabled: true);

        Assert.True(breadcrumbs[2].Disabled);
        Assert.Null(breadcrumbs[2].Href);
    }

    [Fact]
    public void ForCampaign_WithCurrentDisabledFalse_EnablesCampaignBreadcrumb()
    {
        var world = new WorldDto { Id = Guid.NewGuid(), Name = "Middle Earth", Slug = "middle-earth" };
        var campaign = new CampaignDto { Id = Guid.NewGuid(), Name = "War of the Ring", Slug = "war-of-the-ring" };

        var breadcrumbs = _sut.ForCampaign(campaign, world, currentDisabled: false);

        Assert.False(breadcrumbs[2].Disabled);
        Assert.Equal("/middle-earth/war-of-the-ring", breadcrumbs[2].Href);
    }

    // ════════════════════════════════════════════════════════════════
    //  ForArc Tests
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void ForArc_ReturnsDashboardWorldCampaignAndArc()
    {
        var world = new WorldDto { Id = Guid.NewGuid(), Name = "Middle Earth", Slug = "middle-earth" };
        var campaign = new CampaignDto { Id = Guid.NewGuid(), Name = "War of the Ring", Slug = "war-of-the-ring" };
        var arc = new ArcDto { Id = Guid.NewGuid(), Name = "The Fellowship", Slug = "fellowship" };

        var breadcrumbs = _sut.ForArc(arc, campaign, world);

        Assert.Equal(4, breadcrumbs.Count);
        Assert.Equal("Dashboard", breadcrumbs[0].Text);
        Assert.Equal("Middle Earth", breadcrumbs[1].Text);
        Assert.Equal("/middle-earth", breadcrumbs[1].Href);
        Assert.Equal("War of the Ring", breadcrumbs[2].Text);
        Assert.Equal("/middle-earth/war-of-the-ring", breadcrumbs[2].Href);
        Assert.Equal("The Fellowship", breadcrumbs[3].Text);
    }

    [Fact]
    public void ForArc_WithCurrentDisabledTrue_DisablesArcBreadcrumb()
    {
        var world = new WorldDto { Id = Guid.NewGuid(), Name = "Middle Earth", Slug = "middle-earth" };
        var campaign = new CampaignDto { Id = Guid.NewGuid(), Name = "War of the Ring", Slug = "war-of-the-ring" };
        var arc = new ArcDto { Id = Guid.NewGuid(), Name = "The Fellowship", Slug = "fellowship" };

        var breadcrumbs = _sut.ForArc(arc, campaign, world, currentDisabled: true);

        Assert.True(breadcrumbs[3].Disabled);
        Assert.Null(breadcrumbs[3].Href);
    }

    [Fact]
    public void ForArc_WithCurrentDisabledFalse_EnablesArcBreadcrumb()
    {
        var world = new WorldDto { Id = Guid.NewGuid(), Name = "Middle Earth", Slug = "middle-earth" };
        var campaign = new CampaignDto { Id = Guid.NewGuid(), Name = "War of the Ring", Slug = "war-of-the-ring" };
        var arc = new ArcDto { Id = Guid.NewGuid(), Name = "The Fellowship", Slug = "fellowship" };

        var breadcrumbs = _sut.ForArc(arc, campaign, world, currentDisabled: false);

        Assert.False(breadcrumbs[3].Disabled);
        Assert.Equal("/middle-earth/war-of-the-ring/fellowship", breadcrumbs[3].Href);
    }

    // ════════════════════════════════════════════════════════════════
    //  ForArticle Tests
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void ForArticle_WithNullBreadcrumbs_ReturnsDashboardOnly()
    {
        var breadcrumbs = _sut.ForArticle(null!);

        Assert.Single(breadcrumbs);
        Assert.Equal("Dashboard", breadcrumbs[0].Text);
    }

    [Fact]
    public void ForArticle_WithEmptyBreadcrumbs_ReturnsDashboardOnly()
    {
        var breadcrumbs = _sut.ForArticle(new List<BreadcrumbDto>());

        Assert.Single(breadcrumbs);
        Assert.Equal("Dashboard", breadcrumbs[0].Text);
    }

    [Fact]
    public void ForArticle_WithWorldOnly_ReturnsDashboardAndWorld()
    {
        var apiBreadcrumbs = new List<BreadcrumbDto>
        {
            new() { Id = Guid.NewGuid(), Title = "Middle Earth", Slug = "middle-earth", IsWorld = true }
        };

        var breadcrumbs = _sut.ForArticle(apiBreadcrumbs);

        Assert.Equal(2, breadcrumbs.Count);
        Assert.Equal("Dashboard", breadcrumbs[0].Text);
        Assert.Equal("Middle Earth", breadcrumbs[1].Text);
        Assert.True(breadcrumbs[1].Disabled);
        Assert.Null(breadcrumbs[1].Href);
    }

    [Fact]
    public void ForArticle_WithWorldAndArticle_BuildsCorrectPath()
    {
        var apiBreadcrumbs = new List<BreadcrumbDto>
        {
            new() { Id = Guid.NewGuid(), Title = "Middle Earth", Slug = "middle-earth", IsWorld = true },
            new() { Id = Guid.NewGuid(), Title = "Rivendell", Slug = "rivendell", IsWorld = false }
        };

        var breadcrumbs = _sut.ForArticle(apiBreadcrumbs);

        Assert.Equal(3, breadcrumbs.Count);
        Assert.Equal("Dashboard", breadcrumbs[0].Text);
        Assert.Equal("Middle Earth", breadcrumbs[1].Text);
        Assert.Equal("/middle-earth", breadcrumbs[1].Href);
        Assert.Equal("Rivendell", breadcrumbs[2].Text);
        Assert.True(breadcrumbs[2].Disabled);
        Assert.Null(breadcrumbs[2].Href);
    }

    [Fact]
    public void ForArticle_WithMultipleArticles_BuildsFullHierarchy()
    {
        var apiBreadcrumbs = new List<BreadcrumbDto>
        {
            new() { Id = Guid.NewGuid(), Title = "Middle Earth", Slug = "middle-earth", IsWorld = true },
            new() { Id = Guid.NewGuid(), Title = "Locations", Slug = "locations", IsWorld = false },
            new() { Id = Guid.NewGuid(), Title = "Rivendell", Slug = "rivendell", IsWorld = false }
        };

        var breadcrumbs = _sut.ForArticle(apiBreadcrumbs);

        Assert.Equal(4, breadcrumbs.Count);
        Assert.Equal("Dashboard", breadcrumbs[0].Text);
        Assert.Equal("Middle Earth", breadcrumbs[1].Text);
        Assert.Equal("/middle-earth", breadcrumbs[1].Href);
        Assert.Equal("Locations", breadcrumbs[2].Text);
        Assert.Equal("/middle-earth/locations", breadcrumbs[2].Href);
        Assert.Equal("Rivendell", breadcrumbs[3].Text);
        Assert.True(breadcrumbs[3].Disabled);
    }

    // ════════════════════════════════════════════════════════════════
    //  BuildArticleUrl Tests
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void BuildArticleUrl_WithNullBreadcrumbs_ReturnsDashboard()
    {
        Assert.Equal("/dashboard", _sut.BuildArticleUrl(null!));
    }

    [Fact]
    public void BuildArticleUrl_WithEmptyBreadcrumbs_ReturnsDashboard()
    {
        Assert.Equal("/dashboard", _sut.BuildArticleUrl(new List<BreadcrumbDto>()));
    }

    [Fact]
    public void BuildArticleUrl_WithWorldOnlyBreadcrumb_ReturnsWorldSlugPath()
    {
        var breadcrumbs = new List<BreadcrumbDto>
        {
            new() { Slug = "middle-earth" }
        };

        Assert.Equal("/middle-earth", _sut.BuildArticleUrl(breadcrumbs));
    }

    [Fact]
    public void BuildArticleUrl_WithMultipleBreadcrumbs_BuildsSlugPath()
    {
        var breadcrumbs = new List<BreadcrumbDto>
        {
            new() { Slug = "middle-earth" },
            new() { Slug = "locations" },
            new() { Slug = "rivendell" }
        };

        Assert.Equal("/middle-earth/locations/rivendell", _sut.BuildArticleUrl(breadcrumbs));
    }

    // ════════════════════════════════════════════════════════════════
    //  BuildArticleUrlToIndex Tests
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void BuildArticleUrlToIndex_WithNullBreadcrumbs_ReturnsDashboard()
    {
        Assert.Equal("/dashboard", _sut.BuildArticleUrlToIndex(null!, 0));
    }

    [Fact]
    public void BuildArticleUrlToIndex_WithEmptyBreadcrumbs_ReturnsDashboard()
    {
        Assert.Equal("/dashboard", _sut.BuildArticleUrlToIndex(new List<BreadcrumbDto>(), 0));
    }

    [Fact]
    public void BuildArticleUrlToIndex_WithNegativeIndex_ReturnsDashboard()
    {
        var breadcrumbs = new List<BreadcrumbDto> { new() { Slug = "middle-earth" } };

        Assert.Equal("/dashboard", _sut.BuildArticleUrlToIndex(breadcrumbs, -1));
    }

    [Fact]
    public void BuildArticleUrlToIndex_WithIndexZero_ReturnsWorldSlugPath()
    {
        var breadcrumbs = new List<BreadcrumbDto>
        {
            new() { Slug = "middle-earth" },
            new() { Slug = "locations" }
        };

        Assert.Equal("/middle-earth", _sut.BuildArticleUrlToIndex(breadcrumbs, 0));
    }

    [Fact]
    public void BuildArticleUrlToIndex_WithMiddleIndex_ReturnsPartialSlugPath()
    {
        var breadcrumbs = new List<BreadcrumbDto>
        {
            new() { Slug = "middle-earth" },
            new() { Slug = "locations" },
            new() { Slug = "rivendell" }
        };

        Assert.Equal("/middle-earth/locations", _sut.BuildArticleUrlToIndex(breadcrumbs, 1));
    }

    [Fact]
    public void BuildArticleUrlToIndex_WithIndexBeyondRange_ClampsToLastElement()
    {
        var breadcrumbs = new List<BreadcrumbDto>
        {
            new() { Slug = "middle-earth" },
            new() { Slug = "locations" }
        };

        Assert.Equal("/middle-earth/locations", _sut.BuildArticleUrlToIndex(breadcrumbs, 10));
    }
}
