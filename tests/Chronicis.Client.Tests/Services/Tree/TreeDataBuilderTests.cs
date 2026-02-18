using Chronicis.Client.Models;
using Chronicis.Client.Services;
using Chronicis.Client.Services.Tree;
using Chronicis.Shared.DTOs;
using Chronicis.Shared.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Chronicis.Client.Tests.Services.Tree;

public class TreeDataBuilderTests
{
    [Fact]
    public async Task BuildTreeAsync_ReturnsEmpty_WhenNoWorlds()
    {
        var articleApi = Substitute.For<IArticleApiService>();
        var worldApi = Substitute.For<IWorldApiService>();
        var campaignApi = Substitute.For<ICampaignApiService>();
        var arcApi = Substitute.For<IArcApiService>();

        worldApi.GetWorldsAsync().Returns(new List<WorldDto>());
        articleApi.GetAllArticlesAsync().Returns(new List<ArticleTreeDto>());

        var sut = new TreeDataBuilder(articleApi, worldApi, campaignApi, arcApi, NullLogger.Instance);

        var result = await sut.BuildTreeAsync();

        Assert.Empty(result.NodeIndex.RootNodes);
        Assert.Empty(result.CachedArticles);
    }

    [Fact]
    public async Task BuildTreeAsync_BuildsWorldGroups_AndOrphanRoot()
    {
        var worldId = Guid.NewGuid();
        var campaignId = Guid.NewGuid();
        var arcId = Guid.NewGuid();
        var wikiId = Guid.NewGuid();
        var orphanId = Guid.NewGuid();

        var worldApi = Substitute.For<IWorldApiService>();
        worldApi.GetWorldsAsync().Returns(new List<WorldDto> { new() { Id = worldId, Name = "World" } });
        worldApi.GetWorldAsync(worldId).Returns(new WorldDetailDto
        {
            Id = worldId,
            Name = "World",
            Campaigns = new List<CampaignDto> { new() { Id = campaignId, Name = "Campaign", WorldId = worldId } }
        });
        worldApi.GetWorldLinksAsync(worldId).Returns(new List<WorldLinkDto> { new() { Id = Guid.NewGuid(), Title = "Docs", Url = "http://x", WorldId = worldId } });
        worldApi.GetWorldDocumentsAsync(worldId).Returns(new List<WorldDocumentDto>
        {
            new() { Id = Guid.NewGuid(), WorldId = worldId, Title = "Guide", FileName = "guide.pdf", ContentType = "application/pdf", FileSizeBytes = 10 }
        });

        var articleApi = Substitute.For<IArticleApiService>();
        articleApi.GetAllArticlesAsync().Returns(new List<ArticleTreeDto>
        {
            new() { Id = wikiId, WorldId = worldId, Title = "Wiki", Slug = "wiki", Type = ArticleType.WikiArticle },
            new() { Id = orphanId, WorldId = null, Title = "Orphan", Slug = "orphan", Type = ArticleType.WikiArticle }
        });

        var campaignApi = Substitute.For<ICampaignApiService>();
        var arcApi = Substitute.For<IArcApiService>();
        arcApi.GetArcsByCampaignAsync(campaignId).Returns(new List<ArcDto>
        {
            new() { Id = arcId, CampaignId = campaignId, Name = "Arc", SortOrder = 0 }
        });

        var sut = new TreeDataBuilder(articleApi, worldApi, campaignApi, arcApi, NullLogger.Instance);

        var result = await sut.BuildTreeAsync();

        Assert.Equal(2, result.NodeIndex.RootNodes.Count);

        var worldRoot = result.NodeIndex.RootNodes.First(r => r.NodeType == TreeNodeType.World);
        Assert.Contains(worldRoot.Children, c => c.VirtualGroupType == VirtualGroupType.Campaigns);
        Assert.Contains(worldRoot.Children, c => c.VirtualGroupType == VirtualGroupType.Wiki);
        Assert.Contains(worldRoot.Children, c => c.VirtualGroupType == VirtualGroupType.Links);

        var orphanRoot = result.NodeIndex.RootNodes.First(r => r.VirtualGroupType == VirtualGroupType.Uncategorized && r.WorldId == null);
        Assert.Equal("Unassigned Articles", orphanRoot.Title);
        Assert.Single(orphanRoot.Children);
    }

    [Theory]
    [InlineData("application/pdf", "fa-solid fa-file-pdf")]
    [InlineData("application/vnd.openxmlformats-officedocument.wordprocessingml.document", "fa-solid fa-file-word")]
    [InlineData("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "fa-solid fa-file-excel")]
    [InlineData("application/vnd.openxmlformats-officedocument.presentationml.presentation", "fa-solid fa-file-powerpoint")]
    [InlineData("text/plain", "fa-solid fa-file-lines")]
    [InlineData("text/markdown", "fa-solid fa-file-lines")]
    [InlineData("image/png", "fa-solid fa-file-image")]
    [InlineData("application/octet-stream", "fa-solid fa-file")]
    public void GetDocumentIcon_ReturnsExpectedIcon(string contentType, string expected)
    {
        Assert.Equal(expected, TreeDataBuilder.GetDocumentIcon(contentType));
    }

    [Fact]
    public void CreateNodeHelpers_MapExpectedFields()
    {
        var article = new ArticleTreeDto
        {
            Id = Guid.NewGuid(),
            Title = "Title",
            Slug = "slug",
            IconEmoji = "icon",
            Type = ArticleType.Session
        };

        var articleNode = TreeDataBuilder.CreateArticleNode(article);
        var groupNode = TreeDataBuilder.CreateVirtualGroupNode(VirtualGroupType.Wiki, "Wiki", Guid.NewGuid());

        Assert.Equal(TreeNodeType.Article, articleNode.NodeType);
        Assert.Equal(article.Id, articleNode.Id);
        Assert.Equal(TreeNodeType.VirtualGroup, groupNode.NodeType);
        Assert.Equal(VirtualGroupType.Wiki, groupNode.VirtualGroupType);
    }

    [Fact]
    public async Task BuildTreeAsync_CoversNullWorldDetails_AndAllCategoryGroups()
    {
        var world1Id = Guid.NewGuid();
        var world2Id = Guid.NewGuid();
        var campaignId = Guid.NewGuid();
        var arcId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var sessionChildId = Guid.NewGuid();
        var wikiId = Guid.NewGuid();
        var characterId = Guid.NewGuid();
        var legacyId = Guid.NewGuid();

        var worldApi = Substitute.For<IWorldApiService>();
        worldApi.GetWorldsAsync().Returns(new List<WorldDto>
        {
            new() { Id = world1Id, Name = "World A" },
            new() { Id = world2Id, Name = "World B" }
        });
        worldApi.GetWorldAsync(world1Id).Returns(new WorldDetailDto
        {
            Id = world1Id,
            Name = "World A",
            Campaigns = new List<CampaignDto> { new() { Id = campaignId, Name = "Campaign", WorldId = world1Id } }
        });
        worldApi.GetWorldAsync(world2Id).Returns((WorldDetailDto?)null);
        worldApi.GetWorldLinksAsync(world1Id).Returns(new List<WorldLinkDto>());
        worldApi.GetWorldLinksAsync(world2Id).Returns(new List<WorldLinkDto>());
        worldApi.GetWorldDocumentsAsync(world1Id).Returns(new List<WorldDocumentDto>());
        worldApi.GetWorldDocumentsAsync(world2Id).Returns(new List<WorldDocumentDto>());

        var articleApi = Substitute.For<IArticleApiService>();
        articleApi.GetAllArticlesAsync().Returns(new List<ArticleTreeDto>
        {
            new() { Id = sessionId, Title = "Session", Slug = "session", WorldId = world1Id, ArcId = arcId, Type = ArticleType.Session },
            new() { Id = sessionChildId, Title = "Session Child", Slug = "session-child", WorldId = world1Id, ParentId = sessionId, Type = ArticleType.SessionNote },
            new() { Id = wikiId, Title = "Wiki Root", Slug = "wiki", WorldId = world1Id, Type = ArticleType.WikiArticle },
            new() { Id = characterId, Title = "Char Root", Slug = "char", WorldId = world1Id, Type = ArticleType.Character },
            new() { Id = legacyId, Title = "Legacy Root", Slug = "legacy", WorldId = world1Id, Type = ArticleType.Legacy }
        });

        var campaignApi = Substitute.For<ICampaignApiService>();
        var arcApi = Substitute.For<IArcApiService>();
        arcApi.GetArcsByCampaignAsync(campaignId).Returns(new List<ArcDto>
        {
            new() { Id = arcId, CampaignId = campaignId, Name = "Arc", SortOrder = 1 }
        });

        var sut = new TreeDataBuilder(articleApi, worldApi, campaignApi, arcApi, NullLogger.Instance);

        var result = await sut.BuildTreeAsync();

        var worldRoot = result.NodeIndex.RootNodes.Single(n => n.NodeType == TreeNodeType.World);
        Assert.Contains(worldRoot.Children, c => c.VirtualGroupType == VirtualGroupType.Campaigns);
        Assert.Contains(worldRoot.Children, c => c.VirtualGroupType == VirtualGroupType.PlayerCharacters);
        Assert.Contains(worldRoot.Children, c => c.VirtualGroupType == VirtualGroupType.Wiki);
        Assert.Contains(worldRoot.Children, c => c.VirtualGroupType == VirtualGroupType.Uncategorized);
        Assert.DoesNotContain(worldRoot.Children, c => c.VirtualGroupType == VirtualGroupType.Links);
        Assert.True(result.NodeIndex.ContainsNode(sessionChildId));
    }

    [Fact]
    public async Task BuildTreeAsync_UsesFallbacks_ForNullCampaignsMissingArcs_AndChildSorting()
    {
        var worldId = Guid.NewGuid();
        var campaignId = Guid.NewGuid();
        var arcId = Guid.NewGuid();
        var parentId = Guid.NewGuid();
        var childAId = Guid.NewGuid();
        var childBId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var wikiId = Guid.NewGuid();
        var characterId = Guid.NewGuid();
        var legacyId = Guid.NewGuid();

        var worldApi = Substitute.For<IWorldApiService>();
        worldApi.GetWorldsAsync().Returns(new List<WorldDto> { new() { Id = worldId, Name = "World" } });
        worldApi.GetWorldAsync(worldId).Returns(new WorldDetailDto { Id = worldId, Name = "World", Campaigns = null });
        worldApi.GetWorldLinksAsync(worldId).Returns(new List<WorldLinkDto>());
        worldApi.GetWorldDocumentsAsync(worldId).Returns(new List<WorldDocumentDto>());

        var articleApi = Substitute.For<IArticleApiService>();
        articleApi.GetAllArticlesAsync().Returns(new List<ArticleTreeDto>
        {
            new() { Id = sessionId, Title = "Session B", Slug = "s", WorldId = worldId, ArcId = arcId, Type = ArticleType.Session },
            new() { Id = wikiId, Title = "Wiki Root", Slug = "w", WorldId = worldId, Type = ArticleType.WikiArticle },
            new() { Id = characterId, Title = "Character Root", Slug = "c", WorldId = worldId, Type = ArticleType.Character },
            new() { Id = legacyId, Title = "Legacy Root", Slug = "l", WorldId = worldId, Type = ArticleType.Legacy },
            new() { Id = parentId, Title = "Parent", Slug = "p", WorldId = worldId, Type = ArticleType.WikiArticle },
            new() { Id = childBId, Title = "Z Child", Slug = "z", WorldId = worldId, ParentId = parentId, Type = ArticleType.WikiArticle },
            new() { Id = childAId, Title = "A Child", Slug = "a", WorldId = worldId, ParentId = parentId, Type = ArticleType.WikiArticle }
        });

        var campaignApi = Substitute.For<ICampaignApiService>();
        var arcApi = Substitute.For<IArcApiService>();
        arcApi.GetArcsByCampaignAsync(campaignId).Returns(new List<ArcDto>());

        var sut = new TreeDataBuilder(articleApi, worldApi, campaignApi, arcApi, NullLogger.Instance);
        var result = await sut.BuildTreeAsync();

        var worldRoot = Assert.Single(result.NodeIndex.RootNodes.Where(r => r.NodeType == TreeNodeType.World));
        Assert.Contains(worldRoot.Children, c => c.VirtualGroupType == VirtualGroupType.Wiki);
        Assert.Contains(worldRoot.Children, c => c.VirtualGroupType == VirtualGroupType.PlayerCharacters);
        Assert.Contains(worldRoot.Children, c => c.VirtualGroupType == VirtualGroupType.Uncategorized);
        Assert.True(result.NodeIndex.ContainsNode(childAId));
        Assert.True(result.NodeIndex.ContainsNode(childBId));
    }
}

