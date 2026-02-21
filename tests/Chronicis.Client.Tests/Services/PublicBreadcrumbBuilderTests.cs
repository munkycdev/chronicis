using System.Diagnostics.CodeAnalysis;
using Chronicis.Client.Services;
using Chronicis.Shared.DTOs;
using Chronicis.Shared.Enums;
using Xunit;

namespace Chronicis.Client.Tests.Services;

[ExcludeFromCodeCoverage]
public class PublicBreadcrumbBuilderTests
{
    private const string Slug = "my-world";
    private static readonly Guid WorldId = Guid.NewGuid();
    private static readonly Guid CampaignId = Guid.NewGuid();
    private static readonly Guid ArcId = Guid.NewGuid();
    private static readonly Guid ArticleId = Guid.NewGuid();
    private static readonly Guid ParentId = Guid.NewGuid();

    [Fact]
    public void Build_ReturnsEmpty_WhenNoBreadcrumbs()
    {
        var article = MakeArticle(breadcrumbs: null);
        Assert.Empty(PublicBreadcrumbBuilder.Build(Slug, article));
    }

    [Fact]
    public void Build_ReturnsEmpty_WhenEmptyBreadcrumbs()
    {
        var article = MakeArticle(breadcrumbs: new List<BreadcrumbDto>());
        Assert.Empty(PublicBreadcrumbBuilder.Build(Slug, article));
    }

    [Fact]
    public void Build_WorldCrumb_IsClickable()
    {
        var article = MakeArticle(breadcrumbs: new List<BreadcrumbDto>
        {
            new() { Id = WorldId, Title = "World", Slug = "world", IsWorld = true },
            new() { Id = ArticleId, Title = "Article", Slug = "article" }
        });

        var items = PublicBreadcrumbBuilder.Build(Slug, article);
        Assert.Equal("/w/my-world", items[0].Href);
    }

    [Theory]
    [InlineData("characters")]
    [InlineData("wiki")]
    [InlineData("campaigns")]
    [InlineData("uncategorized")]
    public void IsVirtualGroup_RecognizesAllVirtualSlugs(string slug)
    {
        var crumb = new BreadcrumbDto { Id = Guid.NewGuid(), Title = "X", Slug = slug };
        Assert.True(PublicBreadcrumbBuilder.IsVirtualGroup(crumb));
    }

    [Fact]
    public void IsVirtualGroup_EmptyGuid_IsVirtual()
    {
        var crumb = new BreadcrumbDto { Id = Guid.Empty, Title = "X", Slug = "something" };
        Assert.True(PublicBreadcrumbBuilder.IsVirtualGroup(crumb));
    }

    [Fact]
    public void IsVirtualGroup_RealArticle_IsNotVirtual()
    {
        var crumb = new BreadcrumbDto { Id = Guid.NewGuid(), Title = "X", Slug = "real-slug" };
        Assert.False(PublicBreadcrumbBuilder.IsVirtualGroup(crumb));
    }

    [Fact]
    public void IsVirtualGroup_CaseInsensitive()
    {
        var crumb = new BreadcrumbDto { Id = Guid.NewGuid(), Title = "Wiki", Slug = "WIKI" };
        Assert.True(PublicBreadcrumbBuilder.IsVirtualGroup(crumb));
    }

    [Fact]
    public void IsVirtualEntity_MatchesCampaignId()
    {
        var crumb = new BreadcrumbDto { Id = CampaignId, Title = "Campaign", Slug = "camp" };
        var article = MakeArticle(campaignId: CampaignId);
        Assert.True(PublicBreadcrumbBuilder.IsVirtualEntity(crumb, article));
    }

    [Fact]
    public void IsVirtualEntity_MatchesArcId()
    {
        var crumb = new BreadcrumbDto { Id = ArcId, Title = "Arc", Slug = "arc" };
        var article = MakeArticle(arcId: ArcId);
        Assert.True(PublicBreadcrumbBuilder.IsVirtualEntity(crumb, article));
    }

    [Fact]
    public void IsVirtualEntity_DoesNotMatchUnrelatedId()
    {
        var crumb = new BreadcrumbDto { Id = Guid.NewGuid(), Title = "Other", Slug = "other" };
        var article = MakeArticle(campaignId: CampaignId);
        Assert.False(PublicBreadcrumbBuilder.IsVirtualEntity(crumb, article));
    }

    [Fact]
    public void Build_CurrentArticle_IsDisabled()
    {
        var article = MakeArticle(breadcrumbs: new List<BreadcrumbDto>
        {
            new() { Id = WorldId, Title = "World", Slug = "world", IsWorld = true },
            new() { Id = ArticleId, Title = "Current", Slug = "current" }
        });

        var items = PublicBreadcrumbBuilder.Build(Slug, article);
        Assert.True(items[1].Disabled);
    }

    [Fact]
    public void Build_RealArticleAncestor_IsClickable()
    {
        var article = MakeArticle(breadcrumbs: new List<BreadcrumbDto>
        {
            new() { Id = WorldId, Title = "World", Slug = "world", IsWorld = true },
            new() { Id = ParentId, Title = "Parent", Slug = "parent" },
            new() { Id = ArticleId, Title = "Child", Slug = "child" }
        });

        var items = PublicBreadcrumbBuilder.Build(Slug, article);
        Assert.Equal("/w/my-world/parent", items[1].Href);
        Assert.True(items[2].Disabled);
    }

    [Fact]
    public void Build_PathSkipsVirtualGroups()
    {
        var article = MakeArticle(breadcrumbs: new List<BreadcrumbDto>
        {
            new() { Id = WorldId, Title = "World", Slug = "world", IsWorld = true },
            new() { Id = Guid.Empty, Title = "Wiki", Slug = "wiki" },
            new() { Id = ParentId, Title = "Parent", Slug = "parent" },
            new() { Id = ArticleId, Title = "Child", Slug = "child" }
        });

        var items = PublicBreadcrumbBuilder.Build(Slug, article);
        Assert.Equal("/w/my-world", items[0].Href);
        Assert.True(items[1].Disabled);
        Assert.Equal("/w/my-world/parent", items[2].Href);
        Assert.True(items[3].Disabled);
    }

    [Fact]
    public void Build_PathSkipsCampaignAndArc()
    {
        var article = MakeArticle(
            campaignId: CampaignId,
            arcId: ArcId,
            breadcrumbs: new List<BreadcrumbDto>
            {
                new() { Id = WorldId, Title = "World", Slug = "world", IsWorld = true },
                new() { Id = CampaignId, Title = "Campaign", Slug = "campaign" },
                new() { Id = ArcId, Title = "Arc", Slug = "arc" },
                new() { Id = ParentId, Title = "Parent", Slug = "parent" },
                new() { Id = ArticleId, Title = "Current", Slug = "current" }
            });

        var items = PublicBreadcrumbBuilder.Build(Slug, article);
        Assert.True(items[1].Disabled);
        Assert.True(items[2].Disabled);
        Assert.Equal("/w/my-world/parent", items[3].Href);
        Assert.True(items[4].Disabled);
    }

    [Fact]
    public void BuildPathTo_WithNullBreadcrumbs_ReturnsSingleSlug()
    {
        var target = new BreadcrumbDto { Id = ParentId, Title = "Target", Slug = "target-slug" };
        var article = MakeArticle(breadcrumbs: null);
        var path = PublicBreadcrumbBuilder.BuildPathTo(target, article);
        Assert.Equal("target-slug", path);
    }

    private static ArticleDto MakeArticle(
        List<BreadcrumbDto>? breadcrumbs = null,
        Guid? campaignId = null,
        Guid? arcId = null)
    {
        return new ArticleDto
        {
            Id = ArticleId,
            Title = "Test Article",
            Slug = "test-article",
            Type = ArticleType.WikiArticle,
            Visibility = ArticleVisibility.Public,
            Breadcrumbs = breadcrumbs,
            CampaignId = campaignId,
            ArcId = arcId
        };
    }
}
