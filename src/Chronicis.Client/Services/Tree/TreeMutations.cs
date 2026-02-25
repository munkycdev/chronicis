using Chronicis.Client.Models;
using Chronicis.Shared.DTOs;
using Chronicis.Shared.Enums;

namespace Chronicis.Client.Services.Tree;

/// <summary>
/// Handles CRUD operations for tree nodes: create, move, delete, and update.
/// This component makes API calls and coordinates with TreeUiState for post-mutation state updates.
/// </summary>
internal sealed class TreeMutations
{
    private readonly IArticleApiService _articleApi;
    private readonly IAppContextService _appContext;
    private readonly ILogger _logger;

    // Shared node index (owned by TreeStateService, passed in)
    private TreeNodeIndex _nodeIndex = new();

    // Callback to trigger tree refresh after mutations
    private Func<Task>? _refreshCallback;

    public TreeMutations(
        IArticleApiService articleApi,
        IAppContextService appContext,
        ILogger logger)
    {
        _articleApi = articleApi;
        _appContext = appContext;
        _logger = logger;
    }

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
    /// Sets the callback to invoke when tree needs to be refreshed after a mutation.
    /// </summary>
    public void SetRefreshCallback(Func<Task> refreshCallback)
    {
        _refreshCallback = refreshCallback;
    }

    // ============================================
    // Create Operations
    // ============================================

    /// <summary>
    /// Creates a new root-level article in the current world.
    /// </summary>
    /// <returns>The ID of the created article, or null if creation failed.</returns>
    public async Task<Guid?> CreateRootArticleAsync()
    {
        var worldId = _appContext.CurrentWorldId;
        if (!worldId.HasValue)
        {
            _logger.LogWarning("Cannot create root article: no world selected");
            return null;
        }

        var createDto = new ArticleCreateDto
        {
            Title = string.Empty,
            Body = string.Empty,
            ParentId = null,
            WorldId = worldId,
            Type = ArticleType.WikiArticle,
            EffectiveDate = DateTime.Now
        };

        var created = await _articleApi.CreateArticleAsync(createDto);
        if (created == null)
        {
            _logger.LogWarning("Failed to create root article");
            return null;
        }

        // Refresh tree to show new article
        if (_refreshCallback != null)
        {
            await _refreshCallback();
        }

        return created.Id;
    }

    /// <summary>
    /// Creates a new child article under the specified parent.
    /// </summary>
    /// <param name="parentId">The parent node ID (can be article, arc, or virtual group).</param>
    /// <returns>The ID of the created article, or null if creation failed.</returns>
    public async Task<Guid?> CreateChildArticleAsync(Guid parentId)
    {
        if (!_nodeIndex.TryGetNode(parentId, out var parentNode) || parentNode == null)
        {
            _logger.LogWarning("Cannot create child: parent {ParentId} not found", parentId);
            return null;
        }

        // Determine article type based on parent
        var articleType = DetermineChildArticleType(parentNode);

        // For virtual groups, parent is null (top-level in that category)
        Guid? actualParentId = parentNode.NodeType == TreeNodeType.VirtualGroup ? null : parentId;

        var createDto = new ArticleCreateDto
        {
            Title = string.Empty,
            Body = string.Empty,
            ParentId = actualParentId,
            WorldId = parentNode.WorldId ?? _appContext.CurrentWorldId,
            CampaignId = parentNode.CampaignId,
            ArcId = parentNode.NodeType == TreeNodeType.Arc ? parentNode.Id : parentNode.ArcId,
            Type = articleType,
            EffectiveDate = DateTime.Now
        };

        var created = await _articleApi.CreateArticleAsync(createDto);
        if (created == null)
        {
            _logger.LogWarning("Failed to create child article under {ParentId}", parentId);
            return null;
        }

        // Refresh tree
        if (_refreshCallback != null)
        {
            await _refreshCallback();
        }

        return created.Id;
    }

    /// <summary>
    /// Determines the appropriate article type for a child based on its parent node.
    /// </summary>
    private static ArticleType DetermineChildArticleType(TreeNode parentNode)
    {
        return parentNode.NodeType switch
        {
            TreeNodeType.VirtualGroup => parentNode.VirtualGroupType switch
            {
                VirtualGroupType.Wiki => ArticleType.WikiArticle,
                VirtualGroupType.PlayerCharacters => ArticleType.Character,
                VirtualGroupType.Uncategorized => ArticleType.Legacy,
                _ => ArticleType.WikiArticle
            },
            TreeNodeType.Arc => ArticleType.Session,
            TreeNodeType.Article => parentNode.ArticleType ?? ArticleType.WikiArticle,
            _ => ArticleType.WikiArticle
        };
    }

    // ============================================
    // Delete Operations
    // ============================================

    /// <summary>
    /// Deletes an article and all its descendants.
    /// </summary>
    /// <param name="articleId">The article ID to delete.</param>
    /// <returns>True if deletion succeeded.</returns>
    public async Task<bool> DeleteArticleAsync(Guid articleId)
    {
        if (!_nodeIndex.TryGetNode(articleId, out var node) || node == null)
        {
            return false;
        }

        if (node.NodeType != TreeNodeType.Article)
        {
            _logger.LogWarning("Cannot delete non-article node {NodeId}", articleId);
            return false;
        }

        try
        {
            await _articleApi.DeleteArticleAsync(articleId);

            // Refresh tree
            if (_refreshCallback != null)
            {
                await _refreshCallback();
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete article {ArticleId}", articleId);
            return false;
        }
    }

    // ============================================
    // Move Operations
    // ============================================

    /// <summary>
    /// Moves an article to a new parent (or to root if newParentId is null).
    /// </summary>
    /// <param name="articleId">The article to move.</param>
    /// <param name="newParentId">The new parent ID, or null for root level.</param>
    /// <returns>True if move succeeded.</returns>
    public async Task<bool> MoveArticleAsync(Guid articleId, Guid? newParentId)
    {
        if (!_nodeIndex.TryGetNode(articleId, out var node) || node == null)
        {
            return false;
        }

        if (node.NodeType != TreeNodeType.Article)
        {
            _logger.LogWarning("Cannot move non-article node");
            return false;
        }

        // Check if target is a virtual group
        TreeNode? targetNode = null;
        if (newParentId.HasValue)
        {
            _nodeIndex.TryGetNode(newParentId.Value, out targetNode);
        }

        // Handle drop onto virtual group specially
        if (targetNode?.NodeType == TreeNodeType.VirtualGroup)
        {
            return await MoveToVirtualGroupAsync(articleId, node, targetNode);
        }

        // Handle drop onto a Session entity (attach SessionNote to session root)
        if (targetNode?.NodeType == TreeNodeType.Session)
        {
            return await MoveToSessionAsync(articleId, node, targetNode);
        }

        // Regular move to another article or root
        return await MoveToArticleOrRootAsync(articleId, node, newParentId, targetNode);
    }

    /// <summary>
    /// Handles moving an article to a virtual group.
    /// </summary>
    private async Task<bool> MoveToVirtualGroupAsync(Guid articleId, TreeNode node, TreeNode targetNode)
    {
        // Campaigns group holds Campaign entities, not articles
        if (targetNode.VirtualGroupType == VirtualGroupType.Campaigns)
        {
            _logger.LogWarning("Cannot drop articles into Campaigns group - campaigns are separate entities");
            return false;
        }

        // Links group holds external links, not articles
        if (targetNode.VirtualGroupType == VirtualGroupType.Links)
        {
            _logger.LogWarning("Cannot drop articles into Links group - links are separate entities");
            return false;
        }

        // Determine new article type based on target group
        var newType = targetNode.VirtualGroupType switch
        {
            VirtualGroupType.Wiki => ArticleType.WikiArticle,
            VirtualGroupType.PlayerCharacters => ArticleType.Character,
            VirtualGroupType.Uncategorized => ArticleType.Legacy,
            _ => node.ArticleType ?? ArticleType.WikiArticle
        };

        try
        {
            // First, move to root level (null parent)
            var moveSuccess = await _articleApi.MoveArticleAsync(articleId, null);
            if (!moveSuccess)
            {
                _logger.LogWarning("Failed to move article to root level");
                return false;
            }

            // Then update the type if it changed
            if (node.ArticleType != newType)
            {
                var fullArticle = await _articleApi.GetArticleDetailAsync(articleId);
                if (fullArticle != null)
                {
                    var updateDto = new ArticleUpdateDto
                    {
                        Title = fullArticle.Title,
                        Body = fullArticle.Body,
                        Type = newType,
                        IconEmoji = fullArticle.IconEmoji,
                        EffectiveDate = fullArticle.EffectiveDate
                    };

                    var updated = await _articleApi.UpdateArticleAsync(articleId, updateDto);
                    if (updated == null)
                    {
                        _logger.LogWarning("Failed to update article type");
                        // Move succeeded but type update failed - still refresh
                    }
                }
            }

            if (_refreshCallback != null)
            {
                await _refreshCallback();
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to move article to virtual group");
            return false;
        }
    }

    /// <summary>
    /// Handles moving an article to another article or to root level.
    /// </summary>
    private async Task<bool> MoveToArticleOrRootAsync(Guid articleId, TreeNode node, Guid? newParentId, TreeNode? targetNode)
    {
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

            // Ensure target is an article (not a World, Campaign, or Arc)
            if (targetNode != null && targetNode.NodeType != TreeNodeType.Article)
            {
                _logger.LogWarning("Cannot move article to non-article node type: {NodeType}", targetNode.NodeType);
                return false;
            }
        }

        try
        {
            var success = await _articleApi.MoveArticleAsync(articleId, newParentId);

            if (success && _refreshCallback != null)
            {
                await _refreshCallback();
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to move article {ArticleId}", articleId);
            return false;
        }
    }

    /// <summary>
    /// Handles moving a SessionNote article onto a Session entity node.
    /// </summary>
    private async Task<bool> MoveToSessionAsync(Guid articleId, TreeNode node, TreeNode targetSessionNode)
    {
        if (node.ArticleType != ArticleType.SessionNote)
        {
            _logger.LogWarning("Only SessionNote articles can be dropped onto a Session node");
            return false;
        }

        try
        {
            var success = await _articleApi.MoveArticleAsync(articleId, null, targetSessionNode.Id);

            if (success && _refreshCallback != null)
            {
                await _refreshCallback();
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to move SessionNote {ArticleId} to session {SessionId}", articleId, targetSessionNode.Id);
            return false;
        }
    }

    /// <summary>
    /// Checks if a node is a descendant of a potential ancestor.
    /// </summary>
    public bool IsDescendantOf(Guid nodeId, Guid potentialAncestorId)
    {
        if (!_nodeIndex.TryGetNode(nodeId, out var node) || node == null)
            return false;

        var current = node;
        while (current.ParentId.HasValue)
        {
            if (current.ParentId.Value == potentialAncestorId)
                return true;

            if (!_nodeIndex.TryGetNode(current.ParentId.Value, out var parent) || parent == null)
                break;

            current = parent;
        }

        return false;
    }

    // ============================================
    // Update Operations
    // ============================================

    /// <summary>
    /// Updates a node's display properties (title, icon) after an article is saved.
    /// This is a local-only update; does not make API calls.
    /// </summary>
    /// <returns>True if the node was found and updated.</returns>
    public bool UpdateNodeDisplay(Guid nodeId, string title, string? iconEmoji)
    {
        if (_nodeIndex.TryGetNode(nodeId, out var node) && node != null)
        {
            node.Title = title;
            node.IconEmoji = iconEmoji;
            return true;
        }
        return false;
    }

    /// <summary>
    /// Updates a node's visibility after privacy is toggled.
    /// This is a local-only update; does not make API calls.
    /// </summary>
    /// <returns>True if the node was found and updated.</returns>
    public bool UpdateNodeVisibility(Guid nodeId, ArticleVisibility visibility)
    {
        if (_nodeIndex.TryGetNode(nodeId, out var node) && node != null)
        {
            node.Visibility = visibility;
            return true;
        }
        return false;
    }

    // ============================================
    // Validation Helpers
    // ============================================

    /// <summary>
    /// Checks if a node exists and is an article.
    /// </summary>
    public bool IsValidArticle(Guid nodeId)
    {
        return _nodeIndex.TryGetNode(nodeId, out var node) &&
               node != null &&
               node.NodeType == TreeNodeType.Article;
    }

    /// <summary>
    /// Checks if a node can accept children.
    /// </summary>
    public bool CanAcceptChildren(Guid nodeId)
    {
        if (!_nodeIndex.TryGetNode(nodeId, out var node) || node == null)
            return false;

        return node.CanAddChildren;
    }

    /// <summary>
    /// Checks if a node can be a drop target for drag-and-drop.
    /// </summary>
    public bool IsValidDropTarget(Guid nodeId)
    {
        if (!_nodeIndex.TryGetNode(nodeId, out var node) || node == null)
            return false;

        return node.IsDropTarget;
    }
}
