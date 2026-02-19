using Bunit;
using Bunit.TestDoubles;
using Chronicis.Client.Components.Shared;
using Chronicis.Client.Models;
using Chronicis.Client.Services;
using Chronicis.Shared.DTOs;
using Chronicis.Shared.Enums;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using NSubstitute;
using Xunit;

namespace Chronicis.Client.Tests.Components.Shared;

public class QuickAddSessionTests : MudBlazorTestContext
{
    private readonly ICampaignApiService _campaignApi = Substitute.For<ICampaignApiService>();
    private readonly IArticleApiService _articleApi = Substitute.For<IArticleApiService>();
    private readonly ISnackbar _snackbar = Substitute.For<ISnackbar>();
    private readonly FakeTreeStateService _treeState = new();

    public QuickAddSessionTests()
    {
        Services.AddSingleton(_campaignApi);
        Services.AddSingleton(_articleApi);
        Services.AddSingleton<ITreeStateService>(_treeState);
        Services.AddSingleton(_snackbar);
    }

    [Fact]
    public void WithoutActiveContext_ButtonIsHidden()
    {
        _treeState.RootNodesInternal =
        [
            new TreeNode { Id = Guid.NewGuid(), NodeType = TreeNodeType.World }
        ];
        _campaignApi.GetActiveContextAsync(Arg.Any<Guid>()).Returns((ActiveContextDto?)null);

        var cut = RenderComponent<QuickAddSession>();
        _treeState.RaiseStateChanged();
        cut.WaitForAssertion(() => Assert.DoesNotContain("New Session Note", cut.Markup));
    }

    [Fact]
    public void ActiveContext_ShowsButtonAndContextLabel()
    {
        var worldId = Guid.NewGuid();
        _treeState.RootNodesInternal = [new TreeNode { Id = worldId, NodeType = TreeNodeType.World }];
        _campaignApi.GetActiveContextAsync(worldId).Returns(new ActiveContextDto
        {
            CampaignId = Guid.NewGuid(),
            ArcId = Guid.NewGuid(),
            CampaignName = "Campaign",
            ArcName = "Arc"
        });

        var cut = RenderComponent<QuickAddSession>();
        _treeState.RaiseStateChanged();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("New Session Note", cut.Markup);
            Assert.Contains("Campaign", cut.Markup);
            Assert.Contains("Arc", cut.Markup);
        });
    }

    [Fact]
    public async Task CreateSession_Success_RefreshesTreeAndNavigates()
    {
        var worldId = Guid.NewGuid();
        var campaignId = Guid.NewGuid();
        var arcId = Guid.NewGuid();
        var articleId = Guid.NewGuid();

        _treeState.RootNodesInternal = [new TreeNode { Id = worldId, NodeType = TreeNodeType.World }];
        _campaignApi.GetActiveContextAsync(worldId).Returns(new ActiveContextDto
        {
            CampaignId = campaignId,
            ArcId = arcId,
            CampaignName = "Campaign",
            ArcName = "Arc"
        });

        _articleApi.CreateArticleAsync(Arg.Any<ArticleCreateDto>()).Returns(new ArticleDto { Id = articleId });
        _articleApi.GetArticleDetailAsync(articleId).Returns(new ArticleDto
        {
            Id = articleId,
            Breadcrumbs =
            [
                new BreadcrumbDto { Slug = "world" },
                new BreadcrumbDto { Slug = "campaign" },
                new BreadcrumbDto { Slug = "session" }
            ]
        });

        var nav = Services.GetRequiredService<FakeNavigationManager>();
        var cut = RenderComponent<QuickAddSession>();
        _treeState.RaiseStateChanged();

        cut.WaitForAssertion(() => Assert.Contains("New Session Note", cut.Markup));
        await cut.Find("button").ClickAsync(new());

        Assert.EndsWith("/article/world/campaign/session", nav.Uri, StringComparison.OrdinalIgnoreCase);
        Assert.True(_treeState.RefreshCalled);
        Assert.Equal(articleId, _treeState.LastExpandedAndSelectedId);
    }

    [Fact]
    public async Task CreateSession_WhenCreateFails_ShowsErrorPath()
    {
        var worldId = Guid.NewGuid();
        _treeState.RootNodesInternal = [new TreeNode { Id = worldId, NodeType = TreeNodeType.World }];
        _campaignApi.GetActiveContextAsync(worldId).Returns(new ActiveContextDto
        {
            CampaignId = Guid.NewGuid(),
            ArcId = Guid.NewGuid(),
            CampaignName = "Campaign",
            ArcName = "Arc"
        });

        _articleApi.CreateArticleAsync(Arg.Any<ArticleCreateDto>()).Returns((ArticleDto?)null);

        var cut = RenderComponent<QuickAddSession>();
        _treeState.RaiseStateChanged();
        cut.WaitForAssertion(() => Assert.Contains("New Session Note", cut.Markup));

        await cut.Find("button").ClickAsync(new());

        _snackbar.Received().Add("Failed to create session", Severity.Error, Arg.Any<Action<SnackbarOptions>?>());
    }

    [Fact]
    public void Dispose_UnsubscribesFromTreeState()
    {
        var cut = RenderComponent<QuickAddSession>();

        cut.Instance.Dispose();
        _treeState.RaiseStateChanged();

        Assert.True(true);
    }

    private sealed class FakeTreeStateService : ITreeStateService
    {
        public List<TreeNode> RootNodesInternal { get; set; } = [];
        public bool RefreshCalled { get; private set; }
        public Guid? LastExpandedAndSelectedId { get; private set; }

        public IReadOnlyList<TreeNode> RootNodes => RootNodesInternal;
        public Guid? SelectedNodeId => null;
        public string SearchQuery => string.Empty;
        public bool IsSearchActive => false;
        public bool IsLoading => false;
        public bool ShouldFocusTitle { get; set; }
        public IReadOnlyList<ArticleTreeDto> CachedArticles => [];
        public bool HasCachedData => false;
        public event Action? OnStateChanged;

        public void RaiseStateChanged() => OnStateChanged?.Invoke();

        public Task InitializeAsync() => Task.CompletedTask;
        public Task RefreshAsync()
        {
            RefreshCalled = true;
            return Task.CompletedTask;
        }

        public void ExpandNode(Guid nodeId) { }
        public void CollapseNode(Guid nodeId) { }
        public void ToggleNode(Guid nodeId) { }
        public void SelectNode(Guid nodeId) { }
        public void ExpandPathToAndSelect(Guid nodeId) => LastExpandedAndSelectedId = nodeId;
        public Task<Guid?> CreateRootArticleAsync() => Task.FromResult<Guid?>(null);
        public Task<Guid?> CreateChildArticleAsync(Guid parentId) => Task.FromResult<Guid?>(null);
        public Task<bool> DeleteArticleAsync(Guid articleId) => Task.FromResult(false);
        public Task<bool> MoveArticleAsync(Guid articleId, Guid? newParentId) => Task.FromResult(false);
        public void UpdateNodeDisplay(Guid nodeId, string title, string? iconEmoji) { }
        public void UpdateNodeVisibility(Guid nodeId, ArticleVisibility visibility) { }
        public void SetSearchQuery(string query) { }
        public void ClearSearch() { }
        public IReadOnlySet<Guid> GetExpandedNodeIds() => new HashSet<Guid>();
        public void RestoreExpandedNodes(IEnumerable<Guid> nodeIds) { }
        public bool TryGetNode(Guid nodeId, out TreeNode? node)
        {
            node = null;
            return false;
        }
    }
}
