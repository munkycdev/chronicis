using System.Diagnostics.CodeAnalysis;
using Blazored.LocalStorage;
using Chronicis.Client.Models;
using Chronicis.Client.Services.Tree;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Chronicis.Client.Tests;

[ExcludeFromCodeCoverage]
public class TreeUiStateTests
{
    private readonly ILocalStorageService _localStorage;
    private readonly ILogger _logger;
    private readonly TreeUiState _uiState;

    public TreeUiStateTests()
    {
        _localStorage = Substitute.For<ILocalStorageService>();
        _logger = NullLogger.Instance;
        _uiState = new TreeUiState(_localStorage, _logger);
    }

    // ============================================
    // LocalStorage Persistence Tests
    // ============================================

    [Fact]
    public void GetExpandedNodesStorageKey_ShouldReturnCorrectKey()
    {
        // Act
        var key = TreeUiState.GetExpandedNodesStorageKey();

        // Assert
        Assert.Equal("chronicis_expanded_nodes", key);
    }

    [Fact]
    public async Task ExpandNode_ShouldPersistToLocalStorage()
    {
        // Arrange
        var nodeIndex = CreateIndexWithSingleNode(out var node);
        _uiState.SetNodeIndex(nodeIndex);

        // Act
        _uiState.ExpandNode(node.Id);

        // Assert - give async save time to execute
        await Task.Delay(50);
        await _localStorage.Received().SetItemAsync(
            "chronicis_expanded_nodes",
            Arg.Is<List<Guid>>(list => list.Contains(node.Id)));
    }

    [Fact]
    public async Task CollapseNode_ShouldPersistToLocalStorage()
    {
        // Arrange
        var nodeIndex = CreateIndexWithSingleNode(out var node);
        _uiState.SetNodeIndex(nodeIndex);
        _uiState.ExpandNode(node.Id);

        // Clear previous calls
        _localStorage.ClearReceivedCalls();

        // Act
        _uiState.CollapseNode(node.Id);

        // Assert
        await Task.Delay(50);
        await _localStorage.Received().SetItemAsync(
            "chronicis_expanded_nodes",
            Arg.Is<List<Guid>>(list => !list.Contains(node.Id)));
    }

    [Fact]
    public async Task RestoreExpandedStateFromStorageAsync_ShouldRestorePersistedNodes()
    {
        // Arrange
        var nodeIndex = CreateIndexWithSingleNode(out var node);
        _uiState.SetNodeIndex(nodeIndex);

        var savedIds = new List<Guid> { node.Id };
        _localStorage.GetItemAsync<List<Guid>>("chronicis_expanded_nodes")
            .Returns(savedIds);

        // Act
        await _uiState.RestoreExpandedStateFromStorageAsync();

        // Assert
        Assert.True(node.IsExpanded);
        Assert.Contains(node.Id, _uiState.ExpandedNodeIds);
    }

    [Fact]
    public async Task RestoreExpandedStateFromStorageAsync_ShouldHandleNullGracefully()
    {
        // Arrange
        var nodeIndex = CreateIndexWithSingleNode(out var node);
        _uiState.SetNodeIndex(nodeIndex);

        _localStorage.GetItemAsync<List<Guid>>("chronicis_expanded_nodes")
            .Returns((List<Guid>?)null);

        // Act
        await _uiState.RestoreExpandedStateFromStorageAsync();

        // Assert - should not throw, node should remain collapsed
        Assert.False(node.IsExpanded);
    }

    // ============================================
    // Selection Invariant Tests
    // ============================================

    [Fact]
    public void SelectNode_ShouldDeselectPreviousNode()
    {
        // Arrange
        var nodeIndex = new TreeNodeIndex();
        var node1 = CreateArticleNode(Guid.NewGuid(), "Article 1");
        var node2 = CreateArticleNode(Guid.NewGuid(), "Article 2");
        nodeIndex.AddNode(node1);
        nodeIndex.AddNode(node2);
        _uiState.SetNodeIndex(nodeIndex);

        // Act
        _uiState.SelectNode(node1.Id);
        _uiState.SelectNode(node2.Id);

        // Assert
        Assert.False(node1.IsSelected);
        Assert.True(node2.IsSelected);
        Assert.Equal(node2.Id, _uiState.SelectedNodeId);
    }

    [Fact]
    public void SelectNode_OnArticle_ShouldAutoExpandIfHasChildren()
    {
        // Arrange
        var nodeIndex = new TreeNodeIndex();
        var parent = CreateArticleNode(Guid.NewGuid(), "Parent");
        var child = CreateArticleNode(Guid.NewGuid(), "Child");
        parent.Children.Add(child);
        parent.ChildCount = 1;
        nodeIndex.AddNode(parent);
        nodeIndex.AddNode(child);
        _uiState.SetNodeIndex(nodeIndex);

        // Act
        _uiState.SelectNode(parent.Id);

        // Assert
        Assert.True(parent.IsExpanded);
        Assert.Contains(parent.Id, _uiState.ExpandedNodeIds);
    }

    [Fact]
    public void SelectNode_OnVirtualGroup_ShouldToggleExpandInsteadOfSelect()
    {
        // Arrange
        var nodeIndex = new TreeNodeIndex();
        var virtualGroup = CreateVirtualGroupNode(VirtualGroupType.Wiki, "Wiki");
        nodeIndex.AddNode(virtualGroup);
        _uiState.SetNodeIndex(nodeIndex);

        // Act
        _uiState.SelectNode(virtualGroup.Id);

        // Assert
        Assert.Null(_uiState.SelectedNodeId); // Virtual groups don't get selected
        Assert.True(virtualGroup.IsExpanded); // They toggle expansion instead
    }

    [Fact]
    public void SelectNode_OnNonExistentNode_ShouldClearSelection()
    {
        // Arrange
        var nodeIndex = CreateIndexWithSingleNode(out var node);
        _uiState.SetNodeIndex(nodeIndex);
        _uiState.SelectNode(node.Id);

        // Act
        _uiState.SelectNode(Guid.NewGuid()); // Non-existent

        // Assert
        Assert.Null(_uiState.SelectedNodeId);
    }

    [Fact]
    public void ClearSelection_ShouldDeselectCurrentNode()
    {
        // Arrange
        var nodeIndex = CreateIndexWithSingleNode(out var node);
        _uiState.SetNodeIndex(nodeIndex);
        _uiState.SelectNode(node.Id);

        // Act
        _uiState.ClearSelection();

        // Assert
        Assert.False(node.IsSelected);
        Assert.Null(_uiState.SelectedNodeId);
    }

    [Fact]
    public void ClearSelection_WhenSelectedNodeNoLongerExists_ClearsId()
    {
        var firstIndex = CreateIndexWithSingleNode(out var node);
        _uiState.SetNodeIndex(firstIndex);
        _uiState.SelectNode(node.Id);
        _uiState.SetNodeIndex(new TreeNodeIndex());

        _uiState.ClearSelection();

        Assert.Null(_uiState.SelectedNodeId);
    }

    // ============================================
    // Expansion Invariant Tests
    // ============================================

    [Fact]
    public void ExpandNode_ShouldSetIsExpandedAndAddToSet()
    {
        // Arrange
        var nodeIndex = CreateIndexWithSingleNode(out var node);
        _uiState.SetNodeIndex(nodeIndex);

        // Act
        var result = _uiState.ExpandNode(node.Id);

        // Assert
        Assert.True(result);
        Assert.True(node.IsExpanded);
        Assert.Contains(node.Id, _uiState.ExpandedNodeIds);
    }

    [Fact]
    public void CollapseNode_ShouldClearIsExpandedAndRemoveFromSet()
    {
        // Arrange
        var nodeIndex = CreateIndexWithSingleNode(out var node);
        _uiState.SetNodeIndex(nodeIndex);
        _uiState.ExpandNode(node.Id);

        // Act
        var result = _uiState.CollapseNode(node.Id);

        // Assert
        Assert.True(result);
        Assert.False(node.IsExpanded);
        Assert.DoesNotContain(node.Id, _uiState.ExpandedNodeIds);
    }

    [Fact]
    public void ToggleNode_ShouldAlternateExpandedState()
    {
        // Arrange
        var nodeIndex = CreateIndexWithSingleNode(out var node);
        _uiState.SetNodeIndex(nodeIndex);

        // Act & Assert
        _uiState.ToggleNode(node.Id);
        Assert.True(node.IsExpanded);

        _uiState.ToggleNode(node.Id);
        Assert.False(node.IsExpanded);

        _uiState.ToggleNode(node.Id);
        Assert.True(node.IsExpanded);
    }

    [Fact]
    public void ExpandNode_OnNonExistentNode_ShouldReturnFalse()
    {
        // Arrange
        var nodeIndex = new TreeNodeIndex();
        _uiState.SetNodeIndex(nodeIndex);

        // Act
        var result = _uiState.ExpandNode(Guid.NewGuid());

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void CollapseNode_OnNonExistentNode_ShouldReturnFalse()
    {
        _uiState.SetNodeIndex(new TreeNodeIndex());

        var result = _uiState.CollapseNode(Guid.NewGuid());

        Assert.False(result);
    }

    [Fact]
    public void ToggleNode_OnNonExistentNode_ShouldReturnFalse()
    {
        _uiState.SetNodeIndex(new TreeNodeIndex());

        var result = _uiState.ToggleNode(Guid.NewGuid());

        Assert.False(result);
    }

    [Fact]
    public void RestoreExpandedNodes_ShouldOnlyExpandExistingNodes()
    {
        // Arrange
        var nodeIndex = new TreeNodeIndex();
        var existingNode = CreateArticleNode(Guid.NewGuid(), "Existing");
        nodeIndex.AddNode(existingNode);
        _uiState.SetNodeIndex(nodeIndex);

        var nonExistentId = Guid.NewGuid();
        var nodeIds = new List<Guid> { existingNode.Id, nonExistentId };

        // Act
        _uiState.RestoreExpandedNodes(nodeIds);

        // Assert
        Assert.True(existingNode.IsExpanded);
        Assert.Contains(existingNode.Id, _uiState.ExpandedNodeIds);
        Assert.DoesNotContain(nonExistentId, _uiState.ExpandedNodeIds);
    }

    // ============================================
    // ExpandPathToAndSelect Tests
    // ============================================

    [Fact]
    public void ExpandPathToAndSelect_WhenNotInitialized_ShouldSetPendingSelection()
    {
        // Arrange
        var nodeId = Guid.NewGuid();

        // Act
        var result = _uiState.ExpandPathToAndSelect(nodeId, isInitialized: false);

        // Assert
        Assert.False(result); // Deferred
        Assert.Equal(nodeId, _uiState.PendingSelectionId);
    }

    [Fact]
    public void ExpandPathToAndSelect_ShouldExpandAncestorsAndSelectTarget()
    {
        // Arrange
        var nodeIndex = new TreeNodeIndex();
        var grandparent = CreateArticleNode(Guid.NewGuid(), "Grandparent");
        var parent = CreateArticleNode(Guid.NewGuid(), "Parent");
        var child = CreateArticleNode(Guid.NewGuid(), "Child");

        parent.ParentId = grandparent.Id;
        child.ParentId = parent.Id;

        grandparent.Children.Add(parent);
        parent.Children.Add(child);

        nodeIndex.AddNode(grandparent);
        nodeIndex.AddNode(parent);
        nodeIndex.AddNode(child);
        _uiState.SetNodeIndex(nodeIndex);

        // Act
        var result = _uiState.ExpandPathToAndSelect(child.Id, isInitialized: true);

        // Assert
        Assert.True(result);
        Assert.True(grandparent.IsExpanded);
        Assert.True(parent.IsExpanded);
        Assert.True(child.IsSelected);
        Assert.Equal(child.Id, _uiState.SelectedNodeId);
    }

    [Fact]
    public void ExpandPathToAndSelect_ShouldCollapseNodesNotInPath()
    {
        // Arrange
        var nodeIndex = new TreeNodeIndex();
        var target = CreateArticleNode(Guid.NewGuid(), "Target");
        var unrelatedNode = CreateArticleNode(Guid.NewGuid(), "Unrelated");

        nodeIndex.AddNode(target);
        nodeIndex.AddNode(unrelatedNode);
        _uiState.SetNodeIndex(nodeIndex);

        // Expand the unrelated node first
        _uiState.ExpandNode(unrelatedNode.Id);
        Assert.True(unrelatedNode.IsExpanded);

        // Act
        _uiState.ExpandPathToAndSelect(target.Id, isInitialized: true);

        // Assert
        Assert.False(unrelatedNode.IsExpanded); // Should be collapsed
    }

    [Fact]
    public void ConsumePendingSelection_ShouldReturnAndClearPending()
    {
        // Arrange
        var nodeId = Guid.NewGuid();
        _uiState.ExpandPathToAndSelect(nodeId, isInitialized: false);

        // Act
        var pending = _uiState.ConsumePendingSelection();

        // Assert
        Assert.Equal(nodeId, pending);
        Assert.Null(_uiState.PendingSelectionId);
    }

    [Fact]
    public void ClearPendingSelection_ShouldClearPendingId()
    {
        var nodeId = Guid.NewGuid();
        _uiState.ExpandPathToAndSelect(nodeId, isInitialized: false);

        _uiState.ClearPendingSelection();

        Assert.Null(_uiState.PendingSelectionId);
    }

    [Fact]
    public void ExpandPathToAndSelect_WhenNodeMissing_ReturnsFalse()
    {
        _uiState.SetNodeIndex(new TreeNodeIndex());

        var result = _uiState.ExpandPathToAndSelect(Guid.NewGuid(), isInitialized: true);

        Assert.False(result);
    }

    [Fact]
    public void ExpandPathToAndSelect_WhenNodeMissing_ClearsPreviousSelectedNodeFlag()
    {
        var index = CreateIndexWithSingleNode(out var selectedNode);
        _uiState.SetNodeIndex(index);
        _uiState.SelectNode(selectedNode.Id);
        Assert.True(selectedNode.IsSelected);

        var result = _uiState.ExpandPathToAndSelect(Guid.NewGuid(), isInitialized: true);

        Assert.False(result);
        Assert.False(selectedNode.IsSelected);
    }

    [Fact]
    public void ExpandPathToAndSelect_WhenCycleDetected_DoesNotLoopForever()
    {
        var index = new TreeNodeIndex();
        var cyclic = CreateArticleNode(Guid.NewGuid(), "Cyclic");
        cyclic.Children.Add(cyclic); // malformed graph to exercise cycle guard in path traversal
        index.AddNode(cyclic);
        _uiState.SetNodeIndex(index);

        var result = _uiState.ExpandPathToAndSelect(cyclic.Id, isInitialized: true);

        Assert.True(result);
        Assert.True(cyclic.IsSelected);
    }

    // ============================================
    // Search Tests
    // ============================================

    [Fact]
    public void SetSearchQuery_ShouldFilterArticlesByTitle()
    {
        // Arrange
        var nodeIndex = new TreeNodeIndex();
        var matchingNode = CreateArticleNode(Guid.NewGuid(), "Magic Spells");
        var nonMatchingNode = CreateArticleNode(Guid.NewGuid(), "Weapons");
        nodeIndex.AddNode(matchingNode);
        nodeIndex.AddNode(nonMatchingNode);
        _uiState.SetNodeIndex(nodeIndex);

        // Act
        _uiState.SetSearchQuery("magic");

        // Assert
        Assert.True(_uiState.IsSearchActive);
        Assert.Equal("magic", _uiState.SearchQuery);
        Assert.True(matchingNode.IsVisible);
        Assert.False(nonMatchingNode.IsVisible);
    }

    [Fact]
    public void SetSearchQuery_ShouldBeCaseInsensitive()
    {
        // Arrange
        var nodeIndex = new TreeNodeIndex();
        var node = CreateArticleNode(Guid.NewGuid(), "Dragon's Lair");
        nodeIndex.AddNode(node);
        _uiState.SetNodeIndex(nodeIndex);

        // Act
        _uiState.SetSearchQuery("DRAGON");

        // Assert
        Assert.True(node.IsVisible);
    }

    [Fact]
    public void SetSearchQuery_ShouldShowAncestorsOfMatchingNodes()
    {
        // Arrange
        var nodeIndex = new TreeNodeIndex();
        var parent = CreateArticleNode(Guid.NewGuid(), "Locations");
        var child = CreateArticleNode(Guid.NewGuid(), "Waterdeep");
        child.ParentId = parent.Id;
        parent.Children.Add(child);

        nodeIndex.AddNode(parent);
        nodeIndex.AddNode(child);
        _uiState.SetNodeIndex(nodeIndex);

        // Act
        _uiState.SetSearchQuery("waterdeep");

        // Assert
        Assert.True(child.IsVisible);
        Assert.True(parent.IsVisible); // Parent should be visible too
    }

    [Fact]
    public void ClearSearch_ShouldMakeAllNodesVisible()
    {
        // Arrange
        var nodeIndex = new TreeNodeIndex();
        var node1 = CreateArticleNode(Guid.NewGuid(), "Visible");
        var node2 = CreateArticleNode(Guid.NewGuid(), "Hidden");
        nodeIndex.AddNode(node1);
        nodeIndex.AddNode(node2);
        _uiState.SetNodeIndex(nodeIndex);

        _uiState.SetSearchQuery("visible"); // This hides node2

        // Act
        _uiState.ClearSearch();

        // Assert
        Assert.False(_uiState.IsSearchActive);
        Assert.True(node1.IsVisible);
        Assert.True(node2.IsVisible);
    }

    [Fact]
    public void SetSearchQuery_DoesNotExpandNode_WhenVisibleChildrenAreAbsent()
    {
        var index = new TreeNodeIndex();
        var node = CreateArticleNode(Guid.NewGuid(), "Dragon");
        index.AddNode(node);
        _uiState.SetNodeIndex(index);

        _uiState.SetSearchQuery("dragon");

        Assert.False(node.IsExpanded);
    }

    [Fact]
    public void SetSearchQuery_UsesContainerParent_WhenNoDirectParentId()
    {
        var index = new TreeNodeIndex();
        var group = CreateVirtualGroupNode(VirtualGroupType.Wiki, "Wiki");
        var child = CreateArticleNode(Guid.NewGuid(), "Magic");
        group.Children.Add(child);
        index.AddNode(group);
        index.AddNode(child);
        _uiState.SetNodeIndex(index);

        _uiState.SetSearchQuery("magic");

        Assert.True(group.IsVisible);
    }

    [Fact]
    public void SetSearchQuery_WithEmptyString_ShouldClearSearch()
    {
        // Arrange
        var nodeIndex = CreateIndexWithSingleNode(out var node);
        _uiState.SetNodeIndex(nodeIndex);
        _uiState.SetSearchQuery("test");

        // Act
        _uiState.SetSearchQuery("");

        // Assert
        Assert.False(_uiState.IsSearchActive);
        Assert.True(node.IsVisible);
    }

    [Fact]
    public void SetSearchQuery_WithNullValue_TreatsAsEmpty()
    {
        var nodeIndex = CreateIndexWithSingleNode(out _);
        _uiState.SetNodeIndex(nodeIndex);

        _uiState.SetSearchQuery(null!);

        Assert.False(_uiState.IsSearchActive);
        Assert.Equal(string.Empty, _uiState.SearchQuery);
    }

    [Fact]
    public void ApplySearchFilter_ExpandsParent_WhenItHasVisibleChildren()
    {
        var index = new TreeNodeIndex();
        var parent = CreateArticleNode(Guid.NewGuid(), "Parent");
        var child = CreateArticleNode(Guid.NewGuid(), "Magic Child");
        child.ParentId = parent.Id;
        parent.Children.Add(child);
        index.AddNode(parent);
        index.AddNode(child);
        _uiState.SetNodeIndex(index);

        _uiState.SetSearchQuery("magic");

        Assert.True(parent.IsExpanded);
    }

    // ============================================
    // Reset Tests
    // ============================================

    [Fact]
    public void Reset_ShouldClearAllUiState()
    {
        // Arrange
        var nodeIndex = CreateIndexWithSingleNode(out var node);
        _uiState.SetNodeIndex(nodeIndex);
        _uiState.ExpandNode(node.Id);
        _uiState.SelectNode(node.Id);
        _uiState.SetSearchQuery("test");

        // Act
        _uiState.Reset();

        // Assert
        Assert.Empty(_uiState.ExpandedNodeIds);
        Assert.Null(_uiState.SelectedNodeId);
        Assert.Equal(string.Empty, _uiState.SearchQuery);
        Assert.False(_uiState.IsSearchActive);
    }

    [Fact]
    public void RestoreExpandedNodesPreserving_ExpandsOnlyExistingNodes()
    {
        var index = new TreeNodeIndex();
        var existing = CreateArticleNode(Guid.NewGuid(), "Existing");
        index.AddNode(existing);
        _uiState.SetNodeIndex(index);

        _uiState.RestoreExpandedNodesPreserving(new[] { existing.Id, Guid.NewGuid() });

        Assert.True(existing.IsExpanded);
        Assert.Contains(existing.Id, _uiState.ExpandedNodeIds);
    }

    [Fact]
    public async Task SaveExpandedStateAsync_WhenStorageThrows_DoesNotThrow()
    {
        var index = CreateIndexWithSingleNode(out var node);
        _uiState.SetNodeIndex(index);
        _uiState.ExpandNode(node.Id);
        _localStorage.SetItemAsync(Arg.Any<string>(), Arg.Any<List<Guid>>())
            .Returns(_ => ValueTask.FromException(new InvalidOperationException("boom")));

        await _uiState.SaveExpandedStateAsync();
    }

    [Fact]
    public async Task RestoreExpandedStateFromStorageAsync_WhenStorageThrows_DoesNotThrow()
    {
        _localStorage.GetItemAsync<List<Guid>>(Arg.Any<string>())
            .Returns(_ => new ValueTask<List<Guid>>(Task.FromException<List<Guid>>(new InvalidOperationException("boom"))));

        await _uiState.RestoreExpandedStateFromStorageAsync();
    }

    // ============================================
    // Helper Methods
    // ============================================

    private static TreeNodeIndex CreateIndexWithSingleNode(out TreeNode node)
    {
        var index = new TreeNodeIndex();
        node = CreateArticleNode(Guid.NewGuid(), "Test Article");
        index.AddNode(node);
        return index;
    }

    private static TreeNode CreateArticleNode(Guid id, string title)
    {
        return new TreeNode
        {
            Id = id,
            NodeType = TreeNodeType.Article,
            Title = title,
            IsVisible = true
        };
    }

    private static TreeNode CreateVirtualGroupNode(VirtualGroupType groupType, string title)
    {
        return new TreeNode
        {
            Id = Guid.NewGuid(),
            NodeType = TreeNodeType.VirtualGroup,
            VirtualGroupType = groupType,
            Title = title,
            IsVisible = true
        };
    }
}
