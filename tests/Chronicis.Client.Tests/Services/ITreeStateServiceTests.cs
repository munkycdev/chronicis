using Chronicis.Client.Models;
using Chronicis.Client.Services;
using Chronicis.Shared.DTOs;
using Chronicis.Shared.Enums;
using Xunit;

namespace Chronicis.Client.Tests.Services;

public class ITreeStateServiceTests
{
    [Fact]
    public void SelectedArticleId_DefaultAlias_UsesSelectedNodeId()
    {
        ITreeStateService sut = new MinimalTreeStateService();

        Assert.Equal(Guid.Parse("11111111-1111-1111-1111-111111111111"), sut.SelectedArticleId);
    }

    private sealed class MinimalTreeStateService : ITreeStateService
    {
        public IReadOnlyList<TreeNode> RootNodes => Array.Empty<TreeNode>();
        public Guid? SelectedNodeId => Guid.Parse("11111111-1111-1111-1111-111111111111");
        public string SearchQuery => string.Empty;
        public bool IsSearchActive => false;
        public bool IsLoading => false;
        public bool ShouldFocusTitle { get; set; }
        public IReadOnlyList<ArticleTreeDto> CachedArticles => Array.Empty<ArticleTreeDto>();
        public bool HasCachedData => false;
        public event Action? OnStateChanged;
        public Task InitializeAsync() => Task.CompletedTask;
        public Task RefreshAsync() => Task.CompletedTask;
        public void ExpandNode(Guid nodeId) { }
        public void CollapseNode(Guid nodeId) { }
        public void ToggleNode(Guid nodeId) { }
        public void SelectNode(Guid nodeId) { }
        public void ExpandPathToAndSelect(Guid nodeId) { }
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
