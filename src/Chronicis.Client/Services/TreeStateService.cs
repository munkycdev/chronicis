using Blazored.LocalStorage;
using Chronicis.Client.Models;
using Chronicis.Client.Services.Tree;
using Chronicis.Shared.DTOs;
using Chronicis.Shared.Enums;

namespace Chronicis.Client.Services;

/// <summary>
/// Service for managing the navigation tree state.
/// Builds a hierarchical tree with Worlds, Virtual Groups, Campaigns, Arcs, and Articles.
/// 
/// This is a facade that delegates to internal components:
/// - TreeDataBuilder: Builds the tree structure from API data
/// - TreeUiState: Manages expansion, selection, search, and persistence
/// - TreeMutations: Handles create, move, delete, and update operations
/// </summary>
public class TreeStateService : ITreeStateService
{
    private readonly ILogger<TreeStateService> _logger;

    // Internal delegated components
    private readonly TreeDataBuilder _dataBuilder;
    private readonly TreeUiState _uiState;
    private readonly TreeMutations _mutations;

    // Shared state
    private TreeNodeIndex _nodeIndex = new();
    private List<ArticleTreeDto> _cachedArticles = new();
    private bool _isLoading;
    private bool _isInitialized;

    public TreeStateService(
        IArticleApiService articleApi,
        IWorldApiService worldApi,
        ICampaignApiService campaignApi,
        IArcApiService arcApi,
        ISessionApiService sessionApi,
        IAppContextService appContext,
        ILocalStorageService localStorage,
        ILogger<TreeStateService> logger)
    {
        _logger = logger;

        // Create internal components
        _dataBuilder = new TreeDataBuilder(articleApi, worldApi, campaignApi, arcApi, sessionApi, logger);
        _uiState = new TreeUiState(localStorage, logger);
        _mutations = new TreeMutations(articleApi, appContext, logger);

        // Wire up refresh callback for mutations
        _mutations.SetRefreshCallback(RefreshAsync);
    }

    // ============================================
    // State Properties
    // ============================================

    public IReadOnlyList<TreeNode> RootNodes => _nodeIndex.RootNodes;
    public Guid? SelectedNodeId => _uiState.SelectedNodeId;
    public string SearchQuery => _uiState.SearchQuery;
    public bool IsSearchActive => _uiState.IsSearchActive;
    public bool IsLoading => _isLoading;
    public bool ShouldFocusTitle { get; set; }

    /// <summary>
    /// Exposes the cached article list for other services to consume.
    /// Avoids duplicate API calls from Dashboard, etc.
    /// </summary>
    public IReadOnlyList<ArticleTreeDto> CachedArticles => _cachedArticles;

    /// <summary>
    /// Indicates whether the tree has been initialized and CachedArticles is populated.
    /// </summary>
    public bool HasCachedData => _isInitialized && _cachedArticles.Any();

    public event Action? OnStateChanged;

    private void NotifyStateChanged() => OnStateChanged?.Invoke();

    // ============================================
    // Initialization
    // ============================================

    public async Task InitializeAsync()
    {
        if (_isInitialized)
            return;

        _isLoading = true;
        NotifyStateChanged();

        try
        {
            // Build the tree
            var buildResult = await _dataBuilder.BuildTreeAsync();
            _nodeIndex = buildResult.NodeIndex;
            _cachedArticles = buildResult.CachedArticles;

            // Update internal components with new node index
            _uiState.SetNodeIndex(_nodeIndex);
            _mutations.SetNodeIndex(_nodeIndex);

            // Restore persisted state
            await _uiState.RestoreExpandedStateFromStorageAsync();

            _isInitialized = true;

            // Handle pending selection (if ExpandPathToAndSelect was called before init)
            var pendingId = _uiState.ConsumePendingSelection();
            if (pendingId.HasValue)
            {
                _uiState.ExpandPathToAndSelect(pendingId.Value, _isInitialized);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize tree");
            _nodeIndex = new TreeNodeIndex();
            _cachedArticles = new List<ArticleTreeDto>();
        }
        finally
        {
            _isLoading = false;
            NotifyStateChanged();
        }
    }

    public async Task RefreshAsync()
    {
        // Save current state before refresh
        var previouslyExpanded = new HashSet<Guid>(_uiState.ExpandedNodeIds);
        var previousSelection = _uiState.SelectedNodeId;

        _isLoading = true;
        NotifyStateChanged();

        try
        {
            // Rebuild the tree
            var buildResult = await _dataBuilder.BuildTreeAsync();
            _nodeIndex = buildResult.NodeIndex;
            _cachedArticles = buildResult.CachedArticles;

            // Update internal components with new node index
            _uiState.SetNodeIndex(_nodeIndex);
            _mutations.SetNodeIndex(_nodeIndex);

            // Restore expanded state
            _uiState.RestoreExpandedNodesPreserving(previouslyExpanded);

            // Restore selection AND ensure path is expanded
            if (previousSelection.HasValue && _nodeIndex.ContainsNode(previousSelection.Value))
            {
                _uiState.ExpandPathToAndSelect(previousSelection.Value, _isInitialized);
            }

            // Re-apply search filter if active
            if (_uiState.IsSearchActive)
            {
                _uiState.ApplySearchFilter();
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
    // Node Operations (delegated to TreeUiState)
    // ============================================

    public void ExpandNode(Guid nodeId)
    {
        if (_uiState.ExpandNode(nodeId))
        {
            NotifyStateChanged();
        }
    }

    public void CollapseNode(Guid nodeId)
    {
        if (_uiState.CollapseNode(nodeId))
        {
            NotifyStateChanged();
        }
    }

    public void ToggleNode(Guid nodeId)
    {
        if (_uiState.ToggleNode(nodeId))
        {
            NotifyStateChanged();
        }
    }

    public void SelectNode(Guid nodeId)
    {
        _uiState.SelectNode(nodeId);
        NotifyStateChanged();
    }

    public void ExpandPathToAndSelect(Guid nodeId)
    {
        _uiState.ExpandPathToAndSelect(nodeId, _isInitialized);
        NotifyStateChanged();
    }

    // ============================================
    // CRUD Operations (delegated to TreeMutations)
    // ============================================

    public async Task<Guid?> CreateRootArticleAsync()
    {
        var newId = await _mutations.CreateRootArticleAsync();

        if (newId.HasValue)
        {
            // Select the new node (refresh already happened via callback)
            _uiState.SelectNode(newId.Value);
            ShouldFocusTitle = true;
            NotifyStateChanged();
        }

        return newId;
    }

    public async Task<Guid?> CreateChildArticleAsync(Guid parentId)
    {
        var newId = await _mutations.CreateChildArticleAsync(parentId);

        if (newId.HasValue)
        {
            // Expand parent and select new node (refresh already happened via callback)
            _uiState.ExpandNode(parentId);
            _uiState.SelectNode(newId.Value);
            ShouldFocusTitle = true;
            NotifyStateChanged();
        }

        return newId;
    }

    public async Task<bool> DeleteArticleAsync(Guid articleId)
    {
        var wasSelected = _uiState.SelectedNodeId == articleId;
        var success = await _mutations.DeleteArticleAsync(articleId);

        if (success && wasSelected)
        {
            _uiState.ClearSelection();
        }

        NotifyStateChanged();
        return success;
    }

    public async Task<bool> MoveArticleAsync(Guid articleId, Guid? newParentId)
    {
        var success = await _mutations.MoveArticleAsync(articleId, newParentId);
        // Refresh and notification handled by callback
        return success;
    }

    public void UpdateNodeDisplay(Guid nodeId, string title, string? iconEmoji)
    {
        if (_mutations.UpdateNodeDisplay(nodeId, title, iconEmoji))
        {
            NotifyStateChanged();
        }
    }

    public void UpdateNodeVisibility(Guid nodeId, ArticleVisibility visibility)
    {
        if (_mutations.UpdateNodeVisibility(nodeId, visibility))
        {
            NotifyStateChanged();
        }
    }

    // ============================================
    // Search/Filter (delegated to TreeUiState)
    // ============================================

    public void SetSearchQuery(string query)
    {
        _uiState.SetSearchQuery(query);
        NotifyStateChanged();
    }

    public void ClearSearch()
    {
        _uiState.ClearSearch();
        NotifyStateChanged();
    }

    // ============================================
    // Persistence (delegated to TreeUiState)
    // ============================================

    public IReadOnlySet<Guid> GetExpandedNodeIds() => _uiState.GetExpandedNodeIds();

    public void RestoreExpandedNodes(IEnumerable<Guid> nodeIds)
    {
        _uiState.RestoreExpandedNodes(nodeIds);
        NotifyStateChanged();
    }

    // ============================================
    // Node Lookup
    // ============================================

    public bool TryGetNode(Guid nodeId, out TreeNode? node)
    {
        return _nodeIndex.TryGetNode(nodeId, out node);
    }
}
