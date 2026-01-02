using Chronicis.Client.Models;
using Chronicis.Shared.DTOs;
using Chronicis.Shared.Enums;

namespace Chronicis.Client.Services;

/// <summary>
/// Service for managing the article tree navigation state.
/// This is the single source of truth for all tree-related UI state.
/// </summary>
public interface ITreeStateService
{
    // ============================================
    // State Properties
    // ============================================
    
    /// <summary>
    /// Root-level nodes of the tree.
    /// </summary>
    IReadOnlyList<TreeNode> RootNodes { get; }
    
    /// <summary>
    /// Currently selected node ID.
    /// </summary>
    Guid? SelectedNodeId { get; }
    
    /// <summary>
    /// Alias for SelectedNodeId for backwards compatibility.
    /// </summary>
    Guid? SelectedArticleId => SelectedNodeId;
    
    /// <summary>
    /// Current search/filter query.
    /// </summary>
    string SearchQuery { get; }
    
    /// <summary>
    /// Whether a search filter is currently active.
    /// </summary>
    bool IsSearchActive { get; }
    
    /// <summary>
    /// Whether the tree is currently loading.
    /// </summary>
    bool IsLoading { get; }
    
    /// <summary>
    /// Flag indicating the title field should be focused when loading an article.
    /// Used when creating new articles.
    /// </summary>
    bool ShouldFocusTitle { get; set; }
    
    /// <summary>
    /// Exposes the cached article list for other services/components to consume.
    /// Avoids duplicate API calls from Dashboard, etc.
    /// </summary>
    IReadOnlyList<ArticleTreeDto> CachedArticles { get; }
    
    /// <summary>
    /// Indicates whether the tree has been initialized and CachedArticles is populated.
    /// </summary>
    bool HasCachedData { get; }
    
    // ============================================
    // Events
    // ============================================
    
    /// <summary>
    /// Fired when any tree state changes. Components should re-render.
    /// </summary>
    event Action? OnStateChanged;
    
    // ============================================
    // Initialization
    // ============================================
    
    /// <summary>
    /// Initialize the tree by loading all articles.
    /// </summary>
    Task InitializeAsync();
    
    /// <summary>
    /// Refresh the tree from the server.
    /// </summary>
    Task RefreshAsync();
    
    // ============================================
    // Node Operations
    // ============================================
    
    /// <summary>
    /// Expand a node to show its children.
    /// </summary>
    void ExpandNode(Guid nodeId);
    
    /// <summary>
    /// Collapse a node to hide its children.
    /// </summary>
    void CollapseNode(Guid nodeId);
    
    /// <summary>
    /// Toggle a node's expanded state.
    /// </summary>
    void ToggleNode(Guid nodeId);
    
    /// <summary>
    /// Select a node.
    /// </summary>
    void SelectNode(Guid nodeId);
    
    /// <summary>
    /// Expand the path to a node and select it.
    /// Used when navigating to an article from outside the tree.
    /// </summary>
    void ExpandPathToAndSelect(Guid nodeId);
    
    // ============================================
    // CRUD Operations
    // ============================================
    
    /// <summary>
    /// Create a new root-level article.
    /// </summary>
    Task<Guid?> CreateRootArticleAsync();
    
    /// <summary>
    /// Create a new child article under the specified parent.
    /// </summary>
    Task<Guid?> CreateChildArticleAsync(Guid parentId);
    
    /// <summary>
    /// Delete an article and all its descendants.
    /// </summary>
    Task<bool> DeleteArticleAsync(Guid articleId);
    
    /// <summary>
    /// Move an article to a new parent (or to root if newParentId is null).
    /// </summary>
    Task<bool> MoveArticleAsync(Guid articleId, Guid? newParentId);
    
    /// <summary>
    /// Update a node's display properties (title, icon) after an article is saved.
    /// </summary>
    void UpdateNodeDisplay(Guid nodeId, string title, string? iconEmoji);
    
    /// <summary>
    /// Update a node's visibility after privacy is toggled.
    /// </summary>
    void UpdateNodeVisibility(Guid nodeId, ArticleVisibility visibility);
    
    // ============================================
    // Search/Filter
    // ============================================
    
    /// <summary>
    /// Set the search query and filter the tree.
    /// </summary>
    void SetSearchQuery(string query);
    
    /// <summary>
    /// Clear the search filter.
    /// </summary>
    void ClearSearch();
    
    // ============================================
    // Persistence
    // ============================================
    
    /// <summary>
    /// Get the IDs of all currently expanded nodes.
    /// </summary>
    IReadOnlySet<Guid> GetExpandedNodeIds();
    
    /// <summary>
    /// Restore expanded state from a saved set of IDs.
    /// </summary>
    void RestoreExpandedNodes(IEnumerable<Guid> nodeIds);
}
