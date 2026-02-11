using Blazored.LocalStorage;
using Chronicis.Client.Models;
using Microsoft.Extensions.Logging;

namespace Chronicis.Client.Services.Tree;

/// <summary>
/// Manages UI-related state for the tree: expansion, selection, search filtering, and persistence.
/// This component does not perform any API calls - it operates purely on the in-memory node index.
/// </summary>
internal sealed class TreeUiState
{
    private readonly ILocalStorageService _localStorage;
    private readonly ILogger _logger;
    
    private const string ExpandedNodesStorageKey = "chronicis_expanded_nodes";
    
    // Shared node index (owned by TreeStateService, passed in)
    private TreeNodeIndex _nodeIndex = new();
    
    // UI State
    private readonly HashSet<Guid> _expandedNodeIds = new();
    private Guid? _selectedNodeId;
    private Guid? _pendingSelectionId;
    private string _searchQuery = string.Empty;

    public TreeUiState(ILocalStorageService localStorage, ILogger logger)
    {
        _localStorage = localStorage;
        _logger = logger;
    }

    // ============================================
    // State Properties
    // ============================================

    /// <summary>
    /// Gets the currently selected node ID.
    /// </summary>
    public Guid? SelectedNodeId => _selectedNodeId;

    /// <summary>
    /// Gets the current search query.
    /// </summary>
    public string SearchQuery => _searchQuery;

    /// <summary>
    /// Gets whether a search filter is currently active.
    /// </summary>
    public bool IsSearchActive => !string.IsNullOrWhiteSpace(_searchQuery);

    /// <summary>
    /// Gets the pending selection ID (for selection before tree is initialized).
    /// </summary>
    public Guid? PendingSelectionId => _pendingSelectionId;

    /// <summary>
    /// Gets the set of expanded node IDs.
    /// </summary>
    public IReadOnlySet<Guid> ExpandedNodeIds => _expandedNodeIds;

    // ============================================
    // Initialization
    // ============================================

    /// <summary>
    /// Sets the node index reference. Called after tree is built.
    /// </summary>
    public void SetNodeIndex(TreeNodeIndex nodeIndex)
    {
        _nodeIndex = nodeIndex;
    }

    /// <summary>
    /// Resets UI state (called before tree rebuild).
    /// </summary>
    public void Reset()
    {
        _expandedNodeIds.Clear();
        _selectedNodeId = null;
        _searchQuery = string.Empty;
    }

    /// <summary>
    /// Clears the pending selection.
    /// </summary>
    public void ClearPendingSelection()
    {
        _pendingSelectionId = null;
    }

    /// <summary>
    /// Checks if there's a pending selection and returns it, clearing the pending state.
    /// </summary>
    public Guid? ConsumePendingSelection()
    {
        var pending = _pendingSelectionId;
        _pendingSelectionId = null;
        return pending;
    }

    // ============================================
    // Node Operations
    // ============================================

    /// <summary>
    /// Expands a node to show its children.
    /// </summary>
    /// <returns>True if the node was found and expanded.</returns>
    public bool ExpandNode(Guid nodeId)
    {
        if (_nodeIndex.TryGetNode(nodeId, out var node) && node != null)
        {
            node.IsExpanded = true;
            _expandedNodeIds.Add(nodeId);
            _ = SaveExpandedStateAsync();
            return true;
        }
        return false;
    }

    /// <summary>
    /// Collapses a node to hide its children.
    /// </summary>
    /// <returns>True if the node was found and collapsed.</returns>
    public bool CollapseNode(Guid nodeId)
    {
        if (_nodeIndex.TryGetNode(nodeId, out var node) && node != null)
        {
            node.IsExpanded = false;
            _expandedNodeIds.Remove(nodeId);
            _ = SaveExpandedStateAsync();
            return true;
        }
        return false;
    }

    /// <summary>
    /// Toggles a node's expanded state.
    /// </summary>
    /// <returns>True if the node was found and toggled.</returns>
    public bool ToggleNode(Guid nodeId)
    {
        if (_nodeIndex.TryGetNode(nodeId, out var node) && node != null)
        {
            if (node.IsExpanded)
                return CollapseNode(nodeId);
            else
                return ExpandNode(nodeId);
        }
        return false;
    }

    /// <summary>
    /// Selects a node. For virtual groups, toggles expansion instead.
    /// </summary>
    /// <returns>True if selection changed or node was toggled.</returns>
    public bool SelectNode(Guid nodeId)
    {
        // Deselect previous
        if (_selectedNodeId.HasValue && _nodeIndex.TryGetNode(_selectedNodeId.Value, out var previousNode) && previousNode != null)
        {
            previousNode.IsSelected = false;
        }

        if (_nodeIndex.TryGetNode(nodeId, out var node) && node != null)
        {
            // Selectable node types: Article, World, Campaign, Arc
            if (node.NodeType == TreeNodeType.Article ||
                node.NodeType == TreeNodeType.World ||
                node.NodeType == TreeNodeType.Campaign ||
                node.NodeType == TreeNodeType.Arc)
            {
                node.IsSelected = true;
                _selectedNodeId = nodeId;

                // Auto-expand if node has children
                if (node.HasChildren && !node.IsExpanded)
                {
                    node.IsExpanded = true;
                    _expandedNodeIds.Add(nodeId);
                    _ = SaveExpandedStateAsync();
                }
                return true;
            }
            else
            {
                // For virtual groups, just toggle expand
                ToggleNode(nodeId);
                _selectedNodeId = null;
                return true;
            }
        }
        else
        {
            _selectedNodeId = null;
            return false;
        }
    }

    /// <summary>
    /// Expands the path to a node, collapses nodes not in the path, and selects the target.
    /// If tree is not initialized, stores as pending selection.
    /// </summary>
    /// <param name="nodeId">The node to navigate to.</param>
    /// <param name="isInitialized">Whether the tree is initialized.</param>
    /// <returns>True if the operation was performed, false if deferred as pending.</returns>
    public bool ExpandPathToAndSelect(Guid nodeId, bool isInitialized)
    {
        if (!isInitialized)
        {
            _pendingSelectionId = nodeId;
            _selectedNodeId = nodeId;
            return false;
        }

        if (!_nodeIndex.TryGetNode(nodeId, out var targetNode) || targetNode == null)
        {
            _logger.LogWarning("ExpandPathToAndSelect: Node {NodeId} not found", nodeId);
            return false;
        }

        // Build path from root to target
        var path = BuildPathToNode(targetNode);
        var pathNodeIds = new HashSet<Guid>(path.Select(n => n.Id));

        // Collapse all nodes that are NOT in the path to the target
        foreach (var node in _nodeIndex.AllNodes)
        {
            if (node.IsExpanded && !pathNodeIds.Contains(node.Id))
            {
                node.IsExpanded = false;
                _expandedNodeIds.Remove(node.Id);
            }
        }

        // Expand all ancestors in the path (except the target itself)
        for (int i = 0; i < path.Count - 1; i++)
        {
            var node = path[i];
            node.IsExpanded = true;
            _expandedNodeIds.Add(node.Id);
        }

        // Select the target
        SelectNode(nodeId);

        _ = SaveExpandedStateAsync();
        return true;
    }

    /// <summary>
    /// Builds the path from root to the given node.
    /// </summary>
    private List<TreeNode> BuildPathToNode(TreeNode targetNode)
    {
        var path = new List<TreeNode>();
        var current = targetNode;

        while (current != null)
        {
            path.Insert(0, current);

            if (current.ParentId.HasValue && _nodeIndex.TryGetNode(current.ParentId.Value, out var parent) && parent != null)
            {
                current = parent;
            }
            else
            {
                // Check if this node is a child of a world/group node
                current = _nodeIndex.FindParentNode(current);
            }
        }

        return path;
    }

    /// <summary>
    /// Clears the current selection.
    /// </summary>
    public void ClearSelection()
    {
        if (_selectedNodeId.HasValue && _nodeIndex.TryGetNode(_selectedNodeId.Value, out var node) && node != null)
        {
            node.IsSelected = false;
        }
        _selectedNodeId = null;
    }

    // ============================================
    // Search/Filter
    // ============================================

    /// <summary>
    /// Sets the search query and filters the tree.
    /// </summary>
    public void SetSearchQuery(string query)
    {
        _searchQuery = query?.Trim() ?? string.Empty;
        ApplySearchFilter();
    }

    /// <summary>
    /// Clears the search filter and makes all nodes visible.
    /// </summary>
    public void ClearSearch()
    {
        _searchQuery = string.Empty;

        foreach (var node in _nodeIndex.AllNodes)
        {
            node.IsVisible = true;
        }
    }

    /// <summary>
    /// Applies the current search filter to the tree.
    /// </summary>
    public void ApplySearchFilter()
    {
        if (!IsSearchActive)
        {
            ClearSearch();
            return;
        }

        var searchLower = _searchQuery.ToLowerInvariant();
        var matchingNodeIds = new HashSet<Guid>();

        // Find all matching nodes (articles only for search)
        foreach (var node in _nodeIndex.AllNodes)
        {
            if (node.NodeType == TreeNodeType.Article &&
                node.Title.Contains(searchLower, StringComparison.OrdinalIgnoreCase))
            {
                AddNodeAndAncestors(node, matchingNodeIds);
            }
        }

        // Set visibility
        foreach (var node in _nodeIndex.AllNodes)
        {
            node.IsVisible = matchingNodeIds.Contains(node.Id);
        }

        // Expand nodes that have visible children
        foreach (var nodeId in matchingNodeIds)
        {
            if (_nodeIndex.TryGetNode(nodeId, out var node) && node != null)
            {
                if (node.Children.Any(c => c.IsVisible))
                {
                    node.IsExpanded = true;
                    _expandedNodeIds.Add(nodeId);
                }
            }
        }
    }

    /// <summary>
    /// Recursively adds a node and all its ancestors to the set.
    /// </summary>
    private void AddNodeAndAncestors(TreeNode node, HashSet<Guid> set)
    {
        set.Add(node.Id);

        // Add direct parent
        if (node.ParentId.HasValue && _nodeIndex.TryGetNode(node.ParentId.Value, out var parent) && parent != null)
        {
            AddNodeAndAncestors(parent, set);
        }

        // Also find the containing node (for virtual groups, etc.)
        var container = _nodeIndex.FindParentNode(node);
        if (container != null && !set.Contains(container.Id))
        {
            AddNodeAndAncestors(container, set);
        }
    }

    // ============================================
    // Persistence
    // ============================================

    /// <summary>
    /// Gets the IDs of all currently expanded nodes.
    /// </summary>
    public IReadOnlySet<Guid> GetExpandedNodeIds() => _expandedNodeIds;

    /// <summary>
    /// Restores expanded state from a set of node IDs.
    /// </summary>
    public void RestoreExpandedNodes(IEnumerable<Guid> nodeIds)
    {
        _expandedNodeIds.Clear();

        foreach (var nodeId in nodeIds)
        {
            if (_nodeIndex.TryGetNode(nodeId, out var node) && node != null)
            {
                node.IsExpanded = true;
                _expandedNodeIds.Add(nodeId);
            }
        }
    }

    /// <summary>
    /// Restores expanded state from a previously saved set, preserving additional expanded nodes.
    /// Used during refresh to maintain state.
    /// </summary>
    public void RestoreExpandedNodesPreserving(IEnumerable<Guid> nodeIds)
    {
        foreach (var nodeId in nodeIds)
        {
            if (_nodeIndex.TryGetNode(nodeId, out var node) && node != null)
            {
                node.IsExpanded = true;
                _expandedNodeIds.Add(nodeId);
            }
        }
    }

    /// <summary>
    /// Saves the current expanded state to localStorage.
    /// </summary>
    public async Task SaveExpandedStateAsync()
    {
        try
        {
            await _localStorage.SetItemAsync(ExpandedNodesStorageKey, _expandedNodeIds.ToList());
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to save expanded state");
        }
    }

    /// <summary>
    /// Restores expanded state from localStorage.
    /// </summary>
    public async Task RestoreExpandedStateFromStorageAsync()
    {
        try
        {
            var savedIds = await _localStorage.GetItemAsync<List<Guid>>(ExpandedNodesStorageKey);

            if (savedIds != null)
            {
                RestoreExpandedNodes(savedIds);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to restore expanded state");
        }
    }

    /// <summary>
    /// Gets the localStorage key used for expanded nodes persistence.
    /// Exposed for testing purposes.
    /// </summary>
    public static string GetExpandedNodesStorageKey() => ExpandedNodesStorageKey;
}
