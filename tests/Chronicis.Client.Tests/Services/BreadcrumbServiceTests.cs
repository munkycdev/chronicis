using System.Diagnostics.CodeAnalysis;
using Chronicis.Client.Services;
using Chronicis.Client.Services.Routing;
using Chronicis.Shared.DTOs;
using Chronicis.Shared.Enums;
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
    //  ForArticle — WikiArticle / Character / default types
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void ForArticle_WithNullBreadcrumbs_ReturnsDashboardOnly()
    {
        var article = new ArticleDto { Type = ArticleType.WikiArticle, Breadcrumbs = null };

        var breadcrumbs = _sut.ForArticle(article);

        Assert.Single(breadcrumbs);
        Assert.Equal("Dashboard", breadcrumbs[0].Text);
    }

    [Fact]
    public void ForArticle_WithEmptyBreadcrumbs_ReturnsDashboardOnly()
    {
        var article = new ArticleDto { Type = ArticleType.WikiArticle, Breadcrumbs = new List<BreadcrumbDto>() };

        var breadcrumbs = _sut.ForArticle(article);

        Assert.Single(breadcrumbs);
        Assert.Equal("Dashboard", breadcrumbs[0].Text);
    }

    [Fact]
    public void ForArticle_WithWorldOnly_ReturnsDashboardAndWorld()
    {
        var article = new ArticleDto
        {
            Type = ArticleType.WikiArticle,
            Breadcrumbs = new List<BreadcrumbDto>
            {
                new() { Id = Guid.NewGuid(), Title = "Middle Earth", Slug = "middle-earth", IsWorld = true }
            }
        };

        var breadcrumbs = _sut.ForArticle(article);

        Assert.Equal(2, breadcrumbs.Count);
        Assert.Equal("Dashboard", breadcrumbs[0].Text);
        Assert.Equal("Middle Earth", breadcrumbs[1].Text);
        Assert.True(breadcrumbs[1].Disabled);
        Assert.Null(breadcrumbs[1].Href);
    }

    [Fact]
    public void ForArticle_WikiArticle_WorldLinksToWorld_ArticleIsDisabled()
    {
        var article = new ArticleDto
        {
            Type = ArticleType.WikiArticle,
            Breadcrumbs = new List<BreadcrumbDto>
            {
                new() { Id = Guid.NewGuid(), Title = "Middle Earth", Slug = "middle-earth", IsWorld = true },
                new() { Id = Guid.NewGuid(), Title = "Rivendell", Slug = "rivendell", IsWorld = false }
            }
        };

        var breadcrumbs = _sut.ForArticle(article);

        Assert.Equal(3, breadcrumbs.Count);
        Assert.Equal("Dashboard", breadcrumbs[0].Text);
        Assert.Equal("Middle Earth", breadcrumbs[1].Text);
        Assert.Equal("/middle-earth", breadcrumbs[1].Href);
        Assert.Equal("Rivendell", breadcrumbs[2].Text);
        Assert.True(breadcrumbs[2].Disabled);
        Assert.Null(breadcrumbs[2].Href);
    }

    [Fact]
    public void ForArticle_WikiArticle_IntermediateNodeLinksViaWikiUrl()
    {
        var article = new ArticleDto
        {
            Type = ArticleType.WikiArticle,
            Breadcrumbs = new List<BreadcrumbDto>
            {
                new() { Id = Guid.NewGuid(), Title = "Middle Earth", Slug = "middle-earth", IsWorld = true },
                new() { Id = Guid.NewGuid(), Title = "Locations", Slug = "locations", IsWorld = false },
                new() { Id = Guid.NewGuid(), Title = "Rivendell", Slug = "rivendell", IsWorld = false }
            }
        };

        var breadcrumbs = _sut.ForArticle(article);

        Assert.Equal(4, breadcrumbs.Count);
        Assert.Equal("Dashboard", breadcrumbs[0].Text);
        Assert.Equal("Middle Earth", breadcrumbs[1].Text);
        Assert.Equal("/middle-earth", breadcrumbs[1].Href);
        Assert.Equal("Locations", breadcrumbs[2].Text);
        Assert.Equal("/middle-earth/wiki/locations", breadcrumbs[2].Href);
        Assert.Equal("Rivendell", breadcrumbs[3].Text);
        Assert.True(breadcrumbs[3].Disabled);
    }

    [Fact]
    public void ForArticle_WikiArticle_NoWorldBreadcrumb_UsesEmptyWorldSlug()
    {
        // Exercises the null branch of FirstOrDefault(b => b.IsWorld)?.Slug ?? string.Empty
        var article = new ArticleDto
        {
            Type = ArticleType.WikiArticle,
            Breadcrumbs = new List<BreadcrumbDto>
            {
                new() { Id = Guid.NewGuid(), Title = "Orphan", Slug = "orphan", IsWorld = false }
            }
        };

        var breadcrumbs = _sut.ForArticle(article);

        Assert.Equal(2, breadcrumbs.Count);
        Assert.True(breadcrumbs[1].Disabled);
        Assert.Null(breadcrumbs[1].Href);
    }

    // ════════════════════════════════════════════════════════════════
    //  ForArticle — SessionNote type
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void ForArticle_SessionNote_BuildsPositionalHierarchy()
    {
        var article = new ArticleDto
        {
            Type = ArticleType.SessionNote,
            Breadcrumbs = new List<BreadcrumbDto>
            {
                new() { Title = "Middle Earth", Slug = "middle-earth", IsWorld = true },
                new() { Title = "War of the Ring", Slug = "war" },
                new() { Title = "The Fellowship", Slug = "fellowship" },
                new() { Title = "Into Moria", Slug = "moria" },
                new() { Title = "Session 7", Slug = "session-7" }
            }
        };

        var breadcrumbs = _sut.ForArticle(article);

        Assert.Equal(6, breadcrumbs.Count);
        Assert.Equal("/dashboard", breadcrumbs[0].Href);
        Assert.Equal("/middle-earth", breadcrumbs[1].Href);
        Assert.Equal("/middle-earth/war", breadcrumbs[2].Href);
        Assert.Equal("/middle-earth/war/fellowship", breadcrumbs[3].Href);
        Assert.Equal("/middle-earth/war/fellowship/moria", breadcrumbs[4].Href);
        Assert.True(breadcrumbs[5].Disabled);
        Assert.Null(breadcrumbs[5].Href);
    }

    [Fact]
    public void ForArticle_SessionNote_WithExtraDepth_FallsBackToNullHref()
    {
        // Exercises the _ => null arm of the session-note switch for indices >= 4 when not last
        var article = new ArticleDto
        {
            Type = ArticleType.SessionNote,
            Breadcrumbs = new List<BreadcrumbDto>
            {
                new() { Title = "World", Slug = "world", IsWorld = true },
                new() { Title = "Campaign", Slug = "campaign" },
                new() { Title = "Arc", Slug = "arc" },
                new() { Title = "Session", Slug = "session" },
                new() { Title = "Part 1", Slug = "part-1" },
                new() { Title = "Part 2", Slug = "part-2" }
            }
        };

        var breadcrumbs = _sut.ForArticle(article);

        // Part 1 at breadcrumb index 4 — not last, hits _ => null
        Assert.Null(breadcrumbs[5].Href);
        Assert.False(breadcrumbs[5].Disabled);
        // Part 2 at breadcrumb index 5 — last item, disabled
        Assert.True(breadcrumbs[6].Disabled);
    }
}
