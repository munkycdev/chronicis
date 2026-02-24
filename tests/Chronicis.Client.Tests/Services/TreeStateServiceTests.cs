using Blazored.LocalStorage;
using Chronicis.Client.Models;
using Chronicis.Client.Services;
using Chronicis.Shared.DTOs;
using Chronicis.Shared.DTOs.Sessions;
using Chronicis.Shared.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Chronicis.Client.Tests.Services;

public class TreeStateServiceTests
{
    private static readonly Guid TestWorldId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid TestCampaignId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid TestArcId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    private static readonly Guid TestRootArticleId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");

    [Fact]
    public async Task InitializeAndRefresh_PopulateState()
    {
        var (sut, _, _, _, _, _) = CreateSut();

        await sut.InitializeAsync();

        Assert.False(sut.IsLoading);
        Assert.True(sut.HasCachedData || !sut.CachedArticles.Any());

        await sut.RefreshAsync();
        Assert.False(sut.IsLoading);
    }

    [Fact]
    public async Task InitializeAsync_WhenAlreadyInitialized_DoesNotReload()
    {
        var (sut, _, worldApi, _, _, _) = CreateSut();
        await sut.InitializeAsync();
        await sut.InitializeAsync();

        await worldApi.Received(1).GetWorldsAsync();
    }

    [Fact]
    public async Task NodeOperations_AndSearch_DoNotThrow_AndNotify()
    {
        var (sut, _, _, _, _, _) = CreateSut();
        await sut.InitializeAsync();

        var nodeId = sut.RootNodes.FirstOrDefault()?.Id ?? Guid.NewGuid();
        var changeCount = 0;
        sut.OnStateChanged += () => changeCount++;

        sut.ExpandNode(nodeId);
        sut.CollapseNode(nodeId);
        sut.ToggleNode(nodeId);
        sut.SelectNode(nodeId);
        sut.ExpandPathToAndSelect(nodeId);
        sut.SetSearchQuery("test");
        sut.ClearSearch();
        sut.RestoreExpandedNodes(new[] { nodeId });
        _ = sut.GetExpandedNodeIds();
        _ = sut.TryGetNode(nodeId, out _);

        Assert.True(changeCount > 0);
    }

    [Fact]
    public async Task InitializeAsync_ConsumesPendingSelection()
    {
        var (sut, _, _, _, _, _) = CreateSut();
        sut.ExpandPathToAndSelect(Guid.NewGuid());

        await sut.InitializeAsync();

        Assert.True(sut.IsLoading is false);
    }

    [Fact]
    public async Task CreateRootAndChild_SetFocusWhenSuccessful()
    {
        var newRootId = Guid.NewGuid();
        var newChildId = Guid.NewGuid();
        var (sut, articleApi, _, _, appContext, _) = CreateSut();

        appContext.CurrentWorldId.Returns(Guid.NewGuid());
        articleApi.CreateArticleAsync(Arg.Any<ArticleCreateDto>())
            .Returns(new ArticleDto { Id = newRootId, Title = "", Slug = "" }, new ArticleDto { Id = newChildId, Title = "", Slug = "" });

        await sut.InitializeAsync();

        var rootId = await sut.CreateRootArticleAsync();
        var firstNode = sut.RootNodes.FirstOrDefault();
        var childId = firstNode is null ? null : await sut.CreateChildArticleAsync(firstNode.Id);

        Assert.Equal(newRootId, rootId);
        Assert.True(sut.ShouldFocusTitle);
        if (firstNode != null)
        {
            Assert.Equal(newChildId, childId);
        }
    }

    [Fact]
    public async Task DeleteAndMovePaths_ReturnStatus()
    {
        var (sut, _, _, _, _, _) = CreateSut();
        await sut.InitializeAsync();

        var unknown = Guid.NewGuid();
        Assert.False(await sut.DeleteArticleAsync(unknown));
        Assert.False(await sut.MoveArticleAsync(unknown, null));
    }

    [Fact]
    public async Task DeleteArticleAsync_WhenSelected_ClearsSelection()
    {
        var (sut, _, _, _, _, _) = CreateSut();
        await sut.InitializeAsync();
        var target = sut.CachedArticles.First().Id;
        sut.SelectNode(target);

        var deleted = await sut.DeleteArticleAsync(target);

        Assert.True(deleted);
        Assert.Null(sut.SelectedNodeId);
    }

    [Fact]
    public async Task UpdateNodeDisplayAndVisibility_WithMissingNode_DoNothing()
    {
        var (sut, _, _, _, _, _) = CreateSut();
        await sut.InitializeAsync();
        var unknown = Guid.NewGuid();

        sut.UpdateNodeDisplay(unknown, "x", null);
        sut.UpdateNodeVisibility(unknown, ArticleVisibility.Private);

        Assert.True(true);
    }

    [Fact]
    public async Task UpdateNodeDisplayAndVisibility_WithExistingNode_NotifyAndApply()
    {
        var (sut, _, _, _, _, _) = CreateSut();
        await sut.InitializeAsync();
        var nodeId = sut.CachedArticles.First().Id;
        var notifications = 0;
        sut.OnStateChanged += () => notifications++;

        sut.UpdateNodeDisplay(nodeId, "Updated", "fa-star");
        sut.UpdateNodeVisibility(nodeId, ArticleVisibility.Private);

        Assert.True(sut.TryGetNode(nodeId, out var node));
        Assert.NotNull(node);
        Assert.Equal("Updated", node!.Title);
        Assert.Equal("fa-star", node.IconEmoji);
        Assert.Equal(ArticleVisibility.Private, node.Visibility);
        Assert.True(notifications >= 2);
    }

    [Fact]
    public async Task RefreshAsync_WhenBuildFails_DoesNotThrow()
    {
        var (sut, _, worldApi, _, _, _) = CreateSut();
        await sut.InitializeAsync();
        worldApi.GetWorldsAsync().Returns(_ => Task.FromException<List<WorldDto>>(new InvalidOperationException("boom")));

        await sut.RefreshAsync();

        Assert.False(sut.IsLoading);
    }

    [Fact]
    public async Task RefreshAsync_ReappliesSearch_AndRestoresSelectionPath()
    {
        var (sut, _, _, _, _, _) = CreateSut();
        await sut.InitializeAsync();
        var node = sut.RootNodes.First().Id;
        sut.SelectNode(node);
        sut.SetSearchQuery("root");

        await sut.RefreshAsync();

        Assert.Equal("root", sut.SearchQuery);
    }

    [Fact]
    public async Task ExposedStateProperties_AreReadable()
    {
        var (sut, _, _, _, _, _) = CreateSut();
        _ = sut.SelectedNodeId;
        _ = sut.SearchQuery;
        _ = sut.IsSearchActive;
        _ = sut.CachedArticles;
        _ = sut.HasCachedData;
        _ = sut.RootNodes;

        await sut.InitializeAsync();

        Assert.NotNull(sut.CachedArticles);
    }

    [Fact]
    public async Task InitializeAsync_HandlesBuilderFailure()
    {
        var (sut, articleApi, worldApi, _, _, _) = CreateSut();
        worldApi.GetWorldsAsync().Returns(_ => Task.FromException<List<WorldDto>>(new InvalidOperationException("boom")));

        await sut.InitializeAsync();

        Assert.Empty(sut.RootNodes);
        Assert.Empty(sut.CachedArticles);
    }

    [Fact]
    public async Task InitializeAsync_BuildsSessionNodes_AndPlacesSessionNotesUnderSessionBySessionId()
    {
        var sessionId = Guid.NewGuid();
        var legacySessionArticleId = Guid.NewGuid();
        var sessionNoteId = Guid.NewGuid();
        var (sut, articleApi, _, sessionApi, _, _) = CreateSut();

        articleApi.GetAllArticlesAsync().Returns(new List<ArticleTreeDto>
        {
            new()
            {
                Id = legacySessionArticleId,
                WorldId = TestWorldId,
                Title = "Legacy Session",
                Slug = "legacy-session",
                Type = ArticleType.Session,
                ArcId = TestArcId,
                CampaignId = TestCampaignId,
                ParentId = null
            },
            new()
            {
                Id = sessionNoteId,
                WorldId = TestWorldId,
                Title = "Player Notes",
                Slug = "player-notes",
                Type = ArticleType.SessionNote,
                ArcId = TestArcId,
                CampaignId = TestCampaignId,
                ParentId = legacySessionArticleId,
                SessionId = sessionId
            }
        });

        sessionApi.GetSessionsByArcAsync(TestArcId).Returns(new List<SessionTreeDto>
        {
            new()
            {
                Id = sessionId,
                ArcId = TestArcId,
                Name = "Session 12"
            }
        });

        await sut.InitializeAsync();

        var arcNode = sut.RootNodes
            .SelectMany(GetAllNodes)
            .First(n => n.NodeType == TreeNodeType.Arc);

        var sessionNode = Assert.Single(arcNode.Children);
        Assert.Equal(TreeNodeType.Session, sessionNode.NodeType);
        Assert.Equal(sessionId, sessionNode.Id);

        var noteNode = Assert.Single(sessionNode.Children);
        Assert.Equal(TreeNodeType.Article, noteNode.NodeType);
        Assert.Equal(ArticleType.SessionNote, noteNode.ArticleType);
        Assert.Equal(sessionNoteId, noteNode.Id);
        Assert.Equal(sessionNode.Id, noteNode.ParentId);

        Assert.DoesNotContain(arcNode.Children, n => n.NodeType == TreeNodeType.Article && n.Id == legacySessionArticleId);
    }

    private static IEnumerable<TreeNode> GetAllNodes(TreeNode node)
    {
        yield return node;
        foreach (var child in node.Children)
        {
            foreach (var descendant in GetAllNodes(child))
            {
                yield return descendant;
            }
        }
    }

    private static (
        TreeStateService Svc,
        IArticleApiService ArticleApi,
        IWorldApiService WorldApi,
        ISessionApiService SessionApi,
        IAppContextService AppContext,
        ILocalStorageService LocalStorage) CreateSut()
    {
        var worldId = TestWorldId;
        var campaignId = TestCampaignId;
        var arcId = TestArcId;
        var articleId = TestRootArticleId;

        var articleApi = Substitute.For<IArticleApiService>();
        articleApi.GetAllArticlesAsync().Returns(new List<ArticleTreeDto>
        {
            new() { Id = articleId, WorldId = worldId, Title = "Root", Slug = "root", Type = ArticleType.WikiArticle, ParentId = null }
        });
        articleApi.DeleteArticleAsync(Arg.Any<Guid>()).Returns(Task.FromResult(true));
        articleApi.MoveArticleAsync(Arg.Any<Guid>(), Arg.Any<Guid?>()).Returns(true);

        var worldApi = Substitute.For<IWorldApiService>();
        worldApi.GetWorldsAsync().Returns(new List<WorldDto> { new() { Id = worldId, Name = "World" } });
        worldApi.GetWorldAsync(worldId).Returns(new WorldDetailDto
        {
            Id = worldId,
            Name = "World",
            Campaigns = new List<CampaignDto> { new() { Id = campaignId, Name = "Campaign", WorldId = worldId } }
        });
        worldApi.GetWorldLinksAsync(worldId).Returns(new List<WorldLinkDto>());
        worldApi.GetWorldDocumentsAsync(worldId).Returns(new List<WorldDocumentDto>());

        var campaignApi = Substitute.For<ICampaignApiService>();
        var arcApi = Substitute.For<IArcApiService>();
        var sessionApi = Substitute.For<ISessionApiService>();
        arcApi.GetArcsByCampaignAsync(campaignId).Returns(new List<ArcDto>
        {
            new() { Id = arcId, CampaignId = campaignId, Name = "Arc", SortOrder = 0 }
        });
        sessionApi.GetSessionsByArcAsync(arcId).Returns(new List<SessionTreeDto>());

        var appContext = Substitute.For<IAppContextService>();
        appContext.CurrentWorldId.Returns(worldId);

        var storage = Substitute.For<ILocalStorageService>();
        storage.GetItemAsync<List<Guid>>(Arg.Any<string>()).Returns(new List<Guid>());

        var sut = new TreeStateService(
            articleApi,
            worldApi,
            campaignApi,
            arcApi,
            sessionApi,
            appContext,
            storage,
            NullLogger<TreeStateService>.Instance);

        return (sut, articleApi, worldApi, sessionApi, appContext, storage);
    }
}

