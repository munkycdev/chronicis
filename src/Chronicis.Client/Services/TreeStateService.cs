using Chronicis.Client.Models;
using Chronicis.Shared.DTOs;
using Blazored.LocalStorage;

namespace Chronicis.Client.Services;

/// <summary>
/// Service for managing the article tree navigation state.
/// This is the single source of truth for all tree-related UI state.
/// </summary>
public class TreeStateService : ITreeStateService
{
    private readonly IArticleApiService _articleApi;
    private readonly IAppContextService _appContext;
    private readonly ILocalStorageService _localStorage;
    private readonly ILogger<TreeStateService> _logger;
    
    private const string ExpandedNodesStorageKey = "chronicis_expanded_nodes";
    
    // Internal state
    private List<TreeNode> _rootNodes = new();
    private Dictionary<Guid, TreeNode> _nodeIndex = new();
    private HashSet<Guid> _expandedNodeIds = new();
    private Guid? _selectedNodeId;
    private Guid? _pendingSelectionId; // For selections requested before tree is loaded
    private string _searchQuery = string.Empty;
    private bool _isLoading;
    private bool _isInitialized;
    
    public TreeStateService(
        IArticleApiService articleApi,
        IAppContextService appContext,
        ILocalStorageService localStorage,
        ILogger<TreeStateService> logger)
    {
        _articleApi = articleApi;
        _appContext = appContext;
        _localStorage = localStorage;
        _logger = logger;
    }
    
    // ============================================
    // State Properties
    // ============================================
    
    public IReadOnlyList<TreeNode> RootNodes => _rootNodes;
    public Guid? SelectedNodeId => _selectedNodeId;
    public string SearchQuery => _searchQuery;
    public bool IsSearchActive => !string.IsNullOrWhiteSpace(_searchQuery);
    public bool IsLoading => _isLoading;
    public bool ShouldFocusTitle { get; set; }
    
    // ============================================
    // Events
    // ============================================
    
    public event Action? OnStateChanged;
    
    private void NotifyStateChanged() => OnStateChanged?.Invoke();
    
    // ============================================
    // Initialization
    // ============================================
    
    public async Task InitializeAsync()
    {
        if (_isInitialized) return; // Prevent double initialization
        
        _isLoading = true;
        NotifyStateChanged();
        
        try
        {
            // Load all articles from the server
            var allArticles = await _articleApi.GetAllArticlesAsync();
            
            // Build the tree structure
            BuildTree(allArticles);
            
            // Restore expanded state from local storage
            await RestoreExpandedStateAsync();
            
            _isInitialized = true;
            _logger.LogInformation("Tree initialized with {Count} total articles", allArticles.Count);
            
            // Apply any pending selection that was requested before initialization
            if (_pendingSelectionId.HasValue)
            {
                var pendingId = _pendingSelectionId.Value;
                _pendingSelectionId = null;
                ExpandPathToAndSelect(pendingId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize tree");
            _rootNodes = new List<TreeNode>();
            _nodeIndex.Clear();
        }
        finally
        {
            _isLoading = false;
            NotifyStateChanged();
        }
    }
    
    public async Task RefreshAsync()
    {
        // Preserve current expanded state
        var previouslyExpanded = new HashSet<Guid>(_expandedNodeIds);
        var previousSelection = _selectedNodeId;
        
        _isLoading = true;
        NotifyStateChanged();
        
        try
        {
            var allArticles = await _articleApi.GetAllArticlesAsync();
            BuildTree(allArticles);
            
            // Restore expanded state (nodes that still exist)
            foreach (var nodeId in previouslyExpanded)
            {
                if (_nodeIndex.TryGetValue(nodeId, out var node))
                {
                    node.IsExpanded = true;
                    _expandedNodeIds.Add(nodeId);
                }
            }
            
            // Restore selection if still valid
            if (previousSelection.HasValue && _nodeIndex.ContainsKey(previousSelection.Value))
            {
                SelectNode(previousSelection.Value);
            }
            
            // Re-apply search filter if active
            if (IsSearchActive)
            {
                ApplySearchFilter();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh tree");
        }
        finally
        {
            _isLoading = false;
            NotifyStateChanged();
        }
    }
    
    // ============================================
    // Tree Building
    // ============================================
    
    private void BuildTree(List<ArticleTreeDto> articles)
    {
        _nodeIndex.Clear();
        _expandedNodeIds.Clear();
        
        // Create all nodes first
        foreach (var dto in articles)
        {
            var node = new TreeNode
            {
                Id = dto.Id,
                Title = dto.Title,
                Slug = dto.Slug,
                IconEmoji = dto.IconEmoji,
                ParentId = dto.ParentId,
                ChildCount = dto.ChildCount
            };
            _nodeIndex[dto.Id] = node;
        }
        
        // Build parent-child relationships
        _rootNodes = new List<TreeNode>();
        
        foreach (var node in _nodeIndex.Values)
        {
            if (node.ParentId.HasValue)
            {
                if (_nodeIndex.TryGetValue(node.ParentId.Value, out var parent))
                {
                    parent.Children.Add(node);
                }
                else
                {
                    // Orphaned node - treat as root
                    _logger.LogWarning("Article {Id} has invalid parent {ParentId}, treating as root", 
                        node.Id, node.ParentId);
                    _rootNodes.Add(node);
                }
            }
            else
            {
                _rootNodes.Add(node);
            }
        }
        
        // Sort children by title at each level
        SortChildren(_rootNodes);
    }
    
    private void SortChildren(List<TreeNode> nodes)
    {
        nodes.Sort((a, b) => string.Compare(a.Title, b.Title, StringComparison.OrdinalIgnoreCase));
        
        foreach (var node in nodes)
        {
            if (node.Children.Count > 0)
            {
                SortChildren(node.Children);
            }
        }
    }
    
    // ============================================
    // Node Operations
    // ============================================
    
    public void ExpandNode(Guid nodeId)
    {
        if (_nodeIndex.TryGetValue(nodeId, out var node))
        {
            node.IsExpanded = true;
            _expandedNodeIds.Add(nodeId);
            SaveExpandedStateAsync().ConfigureAwait(false);
            NotifyStateChanged();
        }
    }
    
    public void CollapseNode(Guid nodeId)
    {
        if (_nodeIndex.TryGetValue(nodeId, out var node))
        {
            node.IsExpanded = false;
            _expandedNodeIds.Remove(nodeId);
            SaveExpandedStateAsync().ConfigureAwait(false);
            NotifyStateChanged();
        }
    }
    
    public void ToggleNode(Guid nodeId)
    {
        if (_nodeIndex.TryGetValue(nodeId, out var node))
        {
            if (node.IsExpanded)
            {
                CollapseNode(nodeId);
            }
            else
            {
                ExpandNode(nodeId);
            }
        }
    }
    
    public void SelectNode(Guid nodeId)
    {
        // Deselect previous
        if (_selectedNodeId.HasValue && _nodeIndex.TryGetValue(_selectedNodeId.Value, out var previousNode))
        {
            previousNode.IsSelected = false;
        }
        
        // Select new
        if (_nodeIndex.TryGetValue(nodeId, out var node))
        {
            node.IsSelected = true;
            _selectedNodeId = nodeId;
        }
        else
        {
            _selectedNodeId = null;
        }
        
        NotifyStateChanged();
    }
    
    public void ExpandPathToAndSelect(Guid nodeId)
    {
        // If tree isn't initialized yet, queue this for later
        if (!_isInitialized)
        {
            _pendingSelectionId = nodeId;
            _selectedNodeId = nodeId; // Set this so UI knows something is selected
            return;
        }
        
        if (!_nodeIndex.TryGetValue(nodeId, out var targetNode))
        {
            _logger.LogWarning("ExpandPathToAndSelect: Node {NodeId} not found in tree", nodeId);
            return;
        }
        
        // Build path from root to target
        var path = new List<TreeNode>();
        var current = targetNode;
        
        while (current != null)
        {
            path.Insert(0, current);
            
            if (current.ParentId.HasValue && _nodeIndex.TryGetValue(current.ParentId.Value, out var parent))
            {
                current = parent;
            }
            else
            {
                current = null;
            }
        }
        
        // Expand all ancestors (not the target itself unless it has children)
        for (int i = 0; i < path.Count - 1; i++)
        {
            var node = path[i];
            node.IsExpanded = true;
            _expandedNodeIds.Add(node.Id);
        }
        
        // Select the target
        SelectNode(nodeId);
        
        SaveExpandedStateAsync().ConfigureAwait(false);
        NotifyStateChanged();
    }
    
    // ============================================
    // CRUD Operations
    // ============================================
    
    public async Task<Guid?> CreateRootArticleAsync()
    {
        try
        {
            var createDto = new ArticleCreateDto
            {
                Title = string.Empty,
                Body = string.Empty,
                ParentId = null,
                WorldId = _appContext.CurrentWorldId,
                EffectiveDate = DateTime.Now
            };
            
            var created = await _articleApi.CreateArticleAsync(createDto);
            
            // Add to tree
            var newNode = new TreeNode
            {
                Id = created.Id,
                Title = created.Title,
                Slug = created.Slug,
                IconEmoji = created.IconEmoji,
                ParentId = null,
                ChildCount = 0
            };
            
            _nodeIndex[created.Id] = newNode;
            _rootNodes.Add(newNode);
            SortChildren(_rootNodes);
            
            // Select the new node
            SelectNode(created.Id);
            ShouldFocusTitle = true;
            
            NotifyStateChanged();
            
            return created.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create root article");
            return null;
        }
    }
    
    public async Task<Guid?> CreateChildArticleAsync(Guid parentId)
    {
        if (!_nodeIndex.TryGetValue(parentId, out var parentNode))
        {
            _logger.LogWarning("Cannot create child: parent {ParentId} not found", parentId);
            return null;
        }
        
        try
        {
            var createDto = new ArticleCreateDto
            {
                Title = string.Empty,
                Body = string.Empty,
                ParentId = parentId,
                WorldId = _appContext.CurrentWorldId,
                EffectiveDate = DateTime.Now
            };
            
            var created = await _articleApi.CreateArticleAsync(createDto);
            
            // Add to tree
            var newNode = new TreeNode
            {
                Id = created.Id,
                Title = created.Title,
                Slug = created.Slug,
                IconEmoji = created.IconEmoji,
                ParentId = parentId,
                ChildCount = 0
            };
            
            _nodeIndex[created.Id] = newNode;
            parentNode.Children.Add(newNode);
            parentNode.ChildCount = parentNode.Children.Count;
            SortChildren(parentNode.Children);
            
            // Expand parent and select new node
            parentNode.IsExpanded = true;
            _expandedNodeIds.Add(parentId);
            SelectNode(created.Id);
            ShouldFocusTitle = true;
            
            await SaveExpandedStateAsync();
            NotifyStateChanged();
            
            return created.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create child article under {ParentId}", parentId);
            return null;
        }
    }
    
    public async Task<bool> DeleteArticleAsync(Guid articleId)
    {
        if (!_nodeIndex.TryGetValue(articleId, out var node))
        {
            return false;
        }
        
        try
        {
            await _articleApi.DeleteArticleAsync(articleId);
            
            // Remove from tree
            RemoveNodeFromTree(node);
            
            // If the deleted node was selected, select the parent (or nothing)
            if (_selectedNodeId == articleId)
            {
                if (node.ParentId.HasValue && _nodeIndex.ContainsKey(node.ParentId.Value))
                {
                    SelectNode(node.ParentId.Value);
                }
                else
                {
                    _selectedNodeId = null;
                }
            }
            
            NotifyStateChanged();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete article {ArticleId}", articleId);
            return false;
        }
    }
    
    private void RemoveNodeFromTree(TreeNode node)
    {
        // Remove all descendants from index
        RemoveDescendantsFromIndex(node);
        
        // Remove from parent's children list
        if (node.ParentId.HasValue && _nodeIndex.TryGetValue(node.ParentId.Value, out var parent))
        {
            parent.Children.Remove(node);
            parent.ChildCount = parent.Children.Count;
        }
        else
        {
            _rootNodes.Remove(node);
        }
        
        // Remove from index
        _nodeIndex.Remove(node.Id);
        _expandedNodeIds.Remove(node.Id);
    }
    
    private void RemoveDescendantsFromIndex(TreeNode node)
    {
        foreach (var child in node.Children)
        {
            RemoveDescendantsFromIndex(child);
            _nodeIndex.Remove(child.Id);
            _expandedNodeIds.Remove(child.Id);
        }
    }
    
    public async Task<bool> MoveArticleAsync(Guid articleId, Guid? newParentId)
    {
        if (!_nodeIndex.TryGetValue(articleId, out var node))
        {
            return false;
        }
        
        // Prevent moving to self or descendant
        if (newParentId.HasValue)
        {
            if (newParentId.Value == articleId)
            {
                _logger.LogWarning("Cannot move article to itself");
                return false;
            }
            
            if (IsDescendantOf(newParentId.Value, articleId))
            {
                _logger.LogWarning("Cannot move article to its descendant");
                return false;
            }
        }
        
        try
        {
            var success = await _articleApi.MoveArticleAsync(articleId, newParentId);
            
            if (!success)
            {
                return false;
            }
            
            // Update tree structure
            // Remove from old parent
            if (node.ParentId.HasValue && _nodeIndex.TryGetValue(node.ParentId.Value, out var oldParent))
            {
                oldParent.Children.Remove(node);
                oldParent.ChildCount = oldParent.Children.Count;
            }
            else
            {
                _rootNodes.Remove(node);
            }
            
            // Add to new parent
            node.ParentId = newParentId;
            
            if (newParentId.HasValue && _nodeIndex.TryGetValue(newParentId.Value, out var newParent))
            {
                newParent.Children.Add(node);
                newParent.ChildCount = newParent.Children.Count;
                SortChildren(newParent.Children);
                
                // Expand new parent to show the moved node
                newParent.IsExpanded = true;
                _expandedNodeIds.Add(newParentId.Value);
            }
            else
            {
                _rootNodes.Add(node);
                SortChildren(_rootNodes);
            }
            
            await SaveExpandedStateAsync();
            NotifyStateChanged();
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to move article {ArticleId} to {NewParentId}", articleId, newParentId);
            return false;
        }
    }
    
    private bool IsDescendantOf(Guid nodeId, Guid potentialAncestorId)
    {
        if (!_nodeIndex.TryGetValue(nodeId, out var node))
        {
            return false;
        }
        
        var current = node;
        while (current.ParentId.HasValue)
        {
            if (current.ParentId.Value == potentialAncestorId)
            {
                return true;
            }
            
            if (!_nodeIndex.TryGetValue(current.ParentId.Value, out current))
            {
                break;
            }
        }
        
        return false;
    }
    
    public void UpdateNodeDisplay(Guid nodeId, string title, string? iconEmoji)
    {
        if (_nodeIndex.TryGetValue(nodeId, out var node))
        {
            var titleChanged = node.Title != title;
            
            node.Title = title;
            node.IconEmoji = iconEmoji;
            
            // Re-sort if title changed
            if (titleChanged)
            {
                if (node.ParentId.HasValue && _nodeIndex.TryGetValue(node.ParentId.Value, out var parent))
                {
                    SortChildren(parent.Children);
                }
                else
                {
                    SortChildren(_rootNodes);
                }
            }
            
            NotifyStateChanged();
        }
    }
    
    // ============================================
    // Search/Filter
    // ============================================
    
    public void SetSearchQuery(string query)
    {
        _searchQuery = query?.Trim() ?? string.Empty;
        ApplySearchFilter();
        NotifyStateChanged();
    }
    
    public void ClearSearch()
    {
        _searchQuery = string.Empty;
        
        // Reset visibility for all nodes
        foreach (var node in _nodeIndex.Values)
        {
            node.IsVisible = true;
        }
        
        NotifyStateChanged();
    }
    
    private void ApplySearchFilter()
    {
        if (!IsSearchActive)
        {
            ClearSearch();
            return;
        }
        
        var searchLower = _searchQuery.ToLowerInvariant();
        var matchingNodeIds = new HashSet<Guid>();
        
        // Find all matching nodes
        foreach (var node in _nodeIndex.Values)
        {
            if (node.Title.Contains(searchLower, StringComparison.OrdinalIgnoreCase))
            {
                // Add this node and all its ancestors
                AddNodeAndAncestors(node, matchingNodeIds);
            }
        }
        
        // Set visibility
        foreach (var node in _nodeIndex.Values)
        {
            node.IsVisible = matchingNodeIds.Contains(node.Id);
        }
        
        // Expand nodes that have visible children
        foreach (var nodeId in matchingNodeIds)
        {
            if (_nodeIndex.TryGetValue(nodeId, out var node))
            {
                // If this node has visible children, expand it
                if (node.Children.Any(c => c.IsVisible))
                {
                    node.IsExpanded = true;
                    _expandedNodeIds.Add(nodeId);
                }
            }
        }
    }
    
    private void AddNodeAndAncestors(TreeNode node, HashSet<Guid> set)
    {
        var current = node;
        while (current != null)
        {
            set.Add(current.Id);
            
            if (current.ParentId.HasValue && _nodeIndex.TryGetValue(current.ParentId.Value, out var parent))
            {
                current = parent;
            }
            else
            {
                current = null;
            }
        }
    }
    
    // ============================================
    // Persistence
    // ============================================
    
    public IReadOnlySet<Guid> GetExpandedNodeIds() => _expandedNodeIds;
    
    public void RestoreExpandedNodes(IEnumerable<Guid> nodeIds)
    {
        _expandedNodeIds.Clear();
        
        foreach (var nodeId in nodeIds)
        {
            if (_nodeIndex.TryGetValue(nodeId, out var node))
            {
                node.IsExpanded = true;
                _expandedNodeIds.Add(nodeId);
            }
        }
        
        NotifyStateChanged();
    }
    
    private async Task SaveExpandedStateAsync()
    {
        try
        {
            await _localStorage.SetItemAsync(ExpandedNodesStorageKey, _expandedNodeIds.ToList());
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to save expanded state to local storage");
        }
    }
    
    private async Task RestoreExpandedStateAsync()
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
            _logger.LogWarning(ex, "Failed to restore expanded state from local storage");
        }
    }
}
