using Chronicis.Client.Models;

namespace Chronicis.Client.Services.Tree;

/// <summary>
/// Shared container for tree node state.
/// Provides indexed access to all nodes in the tree and holds the root node collection.
/// This class is shared between TreeDataBuilder, TreeUiState, and TreeMutations.
/// </summary>
internal sealed class TreeNodeIndex
{
    private readonly Dictionary<Guid, TreeNode> _nodes = new();
    private List<TreeNode> _rootNodes = new();

    /// <summary>
    /// Gets the root-level nodes of the tree.
    /// </summary>
    public IReadOnlyList<TreeNode> RootNodes => _rootNodes;

    /// <summary>
    /// Gets all nodes indexed by their ID.
    /// </summary>
    public IReadOnlyDictionary<Guid, TreeNode> Nodes => _nodes;

    /// <summary>
    /// Gets the count of all indexed nodes.
    /// </summary>
    public int Count => _nodes.Count;

    /// <summary>
    /// Attempts to get a node by its ID.
    /// </summary>
    public bool TryGetNode(Guid id, out TreeNode? node)
    {
        if (_nodes.TryGetValue(id, out var foundNode))
        {
            node = foundNode;
            return true;
        }
        node = null;
        return false;
    }

    /// <summary>
    /// Gets a node by ID, or null if not found.
    /// </summary>
    public TreeNode? GetNode(Guid id)
    {
        return _nodes.TryGetValue(id, out var node) ? node : null;
    }

    /// <summary>
    /// Checks if a node with the given ID exists.
    /// </summary>
    public bool ContainsNode(Guid id) => _nodes.ContainsKey(id);

    /// <summary>
    /// Adds a node to the index.
    /// </summary>
    public void AddNode(TreeNode node)
    {
        _nodes[node.Id] = node;
    }

    /// <summary>
    /// Adds a node to both the index and the root nodes collection.
    /// </summary>
    public void AddRootNode(TreeNode node)
    {
        _nodes[node.Id] = node;
        _rootNodes.Add(node);
    }

    /// <summary>
    /// Sets the root nodes collection (used during tree building).
    /// </summary>
    public void SetRootNodes(List<TreeNode> rootNodes)
    {
        _rootNodes = rootNodes;
    }

    /// <summary>
    /// Clears all nodes and root nodes.
    /// </summary>
    public void Clear()
    {
        _nodes.Clear();
        _rootNodes = new List<TreeNode>();
    }

    /// <summary>
    /// Gets all nodes as an enumerable (for iteration).
    /// </summary>
    public IEnumerable<TreeNode> AllNodes => _nodes.Values;

    /// <summary>
    /// Finds the parent node that contains the given child in its Children collection.
    /// This searches through all nodes to find structural parents (for virtual groups, etc.).
    /// </summary>
    public TreeNode? FindParentNode(TreeNode child)
    {
        foreach (var node in _nodes.Values)
        {
            if (node.Children.Contains(child))
            {
                return node;
            }
        }
        return null;
    }
}
