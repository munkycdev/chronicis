using Chronicis.Client.Models;
using Chronicis.Shared.DTOs;
using Chronicis.Shared.Enums;
using Blazored.LocalStorage;

namespace Chronicis.Client.Services;

/// <summary>
/// Service for managing the navigation tree state.
/// Builds a hierarchical tree with Worlds, Virtual Groups, Campaigns, Arcs, and Articles.
/// </summary>
public class TreeStateService : ITreeStateService
{
    private readonly IArticleApiService _articleApi;
    private readonly IWorldApiService _worldApi;
    private readonly ICampaignApiService _campaignApi;
    private readonly IArcApiService _arcApi;
    private readonly IAppContextService _appContext;
    private readonly ILocalStorageService _localStorage;
    private readonly ILogger<TreeStateService> _logger;
    
    private const string ExpandedNodesStorageKey = "chronicis_expanded_nodes";
    
    // Internal state
    private List<TreeNode> _rootNodes = new();
    private Dictionary<Guid, TreeNode> _nodeIndex = new();
    private HashSet<Guid> _expandedNodeIds = new();
    private Guid? _selectedNodeId;
    private Guid? _pendingSelectionId;
    private string _searchQuery = string.Empty;
    private bool _isLoading;
    private bool _isInitialized;
    
    // Cached data for sharing with other services
    private List<ArticleTreeDto> _cachedArticles = new();
    
    public TreeStateService(
        IArticleApiService articleApi,
        IWorldApiService worldApi,
        ICampaignApiService campaignApi,
        IArcApiService arcApi,
        IAppContextService appContext,
        ILocalStorageService localStorage,
        ILogger<TreeStateService> logger)
    {
        _articleApi = articleApi;
        _worldApi = worldApi;
        _campaignApi = campaignApi;
        _arcApi = arcApi;
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
        if (_isInitialized) return;
        
        _isLoading = true;
        NotifyStateChanged();
        
        try
        {
            await BuildTreeAsync();
            await RestoreExpandedStateAsync();
            
            _isInitialized = true;
            
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
        var previouslyExpanded = new HashSet<Guid>(_expandedNodeIds);
        var previousSelection = _selectedNodeId;
        
        _isLoading = true;
        NotifyStateChanged();
        
        try
        {
            await BuildTreeAsync();
            
            // Restore expanded state
            foreach (var nodeId in previouslyExpanded)
            {
                if (_nodeIndex.TryGetValue(nodeId, out var node))
                {
                    node.IsExpanded = true;
                    _expandedNodeIds.Add(nodeId);
                }
            }
            
            // Restore selection AND ensure path is expanded
            if (previousSelection.HasValue && _nodeIndex.ContainsKey(previousSelection.Value))
            {
                ExpandPathToAndSelect(previousSelection.Value);
            }
            
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
    // Tree Building (Optimized with Parallel Loading)
    // ============================================
    
    private async Task BuildTreeAsync()
    {
        _nodeIndex.Clear();
        _expandedNodeIds.Clear();
        _rootNodes = new List<TreeNode>();
        
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        var worldsTask = _worldApi.GetWorldsAsync();
        var articlesTask = _articleApi.GetAllArticlesAsync();
        
        await Task.WhenAll(worldsTask, articlesTask);
        
        var worlds = worldsTask.Result;
        var allArticles = articlesTask.Result;
        
        // Cache articles for sharing with Dashboard, etc.
        _cachedArticles = allArticles;
        
        
        if (!worlds.Any())
        {
            return; // Nothing to build
        }
        
        // Phase 2: Fetch all world details in parallel
        _logger.LogDebug("Phase 2: Fetching world details in parallel...");
        var worldDetailTasks = worlds.Select(w => _worldApi.GetWorldAsync(w.Id)).ToList();
        var worldDetails = await Task.WhenAll(worldDetailTasks);
        
        // Create lookup: WorldId -> WorldDetailDto
        var worldDetailLookup = worldDetails
            .Where(wd => wd != null)
            .ToDictionary(wd => wd!.Id, wd => wd!);
        
        _logger.LogDebug("Phase 2 complete: {Count} world details in {Ms}ms",
            worldDetailLookup.Count, stopwatch.ElapsedMilliseconds);
        
        // Phase 3: Collect all campaigns and fetch all arcs in parallel
        var allCampaigns = worldDetails
            .Where(wd => wd != null)
            .SelectMany(wd => wd!.Campaigns ?? new List<CampaignDto>())
            .ToList();
        
        _logger.LogDebug("Phase 3: Fetching arcs for {Count} campaigns, world links, and documents in parallel...", allCampaigns.Count);

        // Fetch all arcs, world links, and world documents in parallel
        var arcTasks = allCampaigns.Select(c => _arcApi.GetArcsByCampaignAsync(c.Id)).ToList();
        var linkTasks = worlds.Select(w => _worldApi.GetWorldLinksAsync(w.Id)).ToList();
        var documentTasks = worlds.Select(w => _worldApi.GetWorldDocumentsAsync(w.Id)).ToList();

        await Task.WhenAll(
            Task.WhenAll(arcTasks),
            Task.WhenAll(linkTasks),
            Task.WhenAll(documentTasks)
        );

        var arcResults = arcTasks.Select(t => t.Result).ToArray();

        // Create lookup: WorldId -> List<WorldLinkDto>
        var linksByWorld = new Dictionary<Guid, List<WorldLinkDto>>();
        for (int i = 0; i < worlds.Count; i++)
        {
            linksByWorld[worlds[i].Id] = linkTasks[i].Result;
        }

        // Create lookup: WorldId -> List<WorldDocumentDto>
        var documentsByWorld = new Dictionary<Guid, List<WorldDocumentDto>>();
        for (int i = 0; i < worlds.Count; i++)
        {
            documentsByWorld[worlds[i].Id] = documentTasks[i].Result;
        }
        
        // Create lookup: CampaignId -> List<ArcDto>
        var arcsByCampaign = new Dictionary<Guid, List<ArcDto>>();
        for (int i = 0; i < allCampaigns.Count; i++)
        {
            arcsByCampaign[allCampaigns[i].Id] = arcResults[i];
        }
        
        _logger.LogDebug("Phase 3 complete: {Count} total arcs in {Ms}ms",
            arcResults.Sum(r => r.Count), stopwatch.ElapsedMilliseconds);
        
        // Phase 4: Build the tree structure (now synchronous with all data in memory)
        _logger.LogDebug("Phase 4: Building tree structure...");
        
        // Build article index for quick lookup
        var articleIndex = allArticles.ToDictionary(a => a.Id);
        
        // Process each world
        foreach (var world in worlds.OrderBy(w => w.Name))
        {
            if (!worldDetailLookup.TryGetValue(world.Id, out var worldDetail))
            {
                _logger.LogWarning("World detail not found for {WorldId}", world.Id);
                continue;
            }
            
            var worldLinks = linksByWorld.TryGetValue(world.Id, out var links) ? links : new List<WorldLinkDto>();
            var worldDocuments = documentsByWorld.TryGetValue(world.Id, out var docs) ? docs : new List<WorldDocumentDto>();

            var worldNode = BuildWorldNode(
                world, 
                worldDetail, 
                allArticles, 
                articleIndex, 
                arcsByCampaign,
                worldLinks,
                worldDocuments);
            
            _rootNodes.Add(worldNode);
            _nodeIndex[worldNode.Id] = worldNode;
        }
        
        // Handle articles without a world (shouldn't happen, but handle gracefully)
        var orphanArticles = allArticles.Where(a => !a.WorldId.HasValue && a.ParentId == null).ToList();
        if (orphanArticles.Any())
        {
            _logger.LogWarning("{Count} articles have no world assigned", orphanArticles.Count);
            
            var orphanNode = CreateVirtualGroupNode(
                VirtualGroupType.Uncategorized, 
                "Unassigned Articles",
                null);
            
            foreach (var article in orphanArticles)
            {
                var articleNode = CreateArticleNode(article);
                orphanNode.Children.Add(articleNode);
                _nodeIndex[articleNode.Id] = articleNode;
            }
            
            if (orphanNode.Children.Any())
            {
                orphanNode.ChildCount = orphanNode.Children.Count;
                _rootNodes.Add(orphanNode);
                _nodeIndex[orphanNode.Id] = orphanNode;
            }
        }
        
        stopwatch.Stop();
        _logger.LogDebug("Tree build complete in {Ms}ms total", stopwatch.ElapsedMilliseconds);
    }
    
    /// <summary>
    /// Builds a world node synchronously using pre-fetched data.
    /// </summary>
    private TreeNode BuildWorldNode(
        WorldDto world,
        WorldDetailDto worldDetail,
        List<ArticleTreeDto> allArticles,
        Dictionary<Guid, ArticleTreeDto> articleIndex,
        Dictionary<Guid, List<ArcDto>> arcsByCampaign,
        List<WorldLinkDto> worldLinks,
        List<WorldDocumentDto> worldDocuments)
    {
        var worldNode = new TreeNode
        {
            Id = world.Id,
            NodeType = TreeNodeType.World,
            Title = world.Name,
            WorldId = world.Id,
            IconEmoji = "fa-solid fa-globe"
        };
        
        var campaigns = worldDetail.Campaigns ?? new List<CampaignDto>();
        
        // Filter articles for this world
        var worldArticles = allArticles.Where(a => a.WorldId == world.Id).ToList();
        
        // Create virtual groups
        var campaignsGroup = CreateVirtualGroupNode(VirtualGroupType.Campaigns, "Campaigns", world.Id);
        var charactersGroup = CreateVirtualGroupNode(VirtualGroupType.PlayerCharacters, "Player Characters", world.Id);
        var wikiGroup = CreateVirtualGroupNode(VirtualGroupType.Wiki, "Wiki", world.Id);
        var uncategorizedGroup = CreateVirtualGroupNode(VirtualGroupType.Uncategorized, "Uncategorized", world.Id);
        
        // Build Campaigns group
        foreach (var campaign in campaigns.OrderBy(c => c.Name))
        {
            var arcs = arcsByCampaign.TryGetValue(campaign.Id, out var campaignArcs) 
                ? campaignArcs 
                : new List<ArcDto>();
            
            var campaignNode = BuildCampaignNode(campaign, arcs, worldArticles, articleIndex);
            campaignsGroup.Children.Add(campaignNode);
            _nodeIndex[campaignNode.Id] = campaignNode;
        }
        campaignsGroup.ChildCount = campaignsGroup.Children.Count;
        
        // Build Characters group (top-level Character articles)
        var characterArticles = worldArticles
            .Where(a => a.Type == ArticleType.Character && a.ParentId == null)
            .OrderBy(a => a.Title)
            .ToList();
        
        foreach (var article in characterArticles)
        {
            var articleNode = BuildArticleNodeWithChildren(article, worldArticles, articleIndex);
            charactersGroup.Children.Add(articleNode);
        }
        charactersGroup.ChildCount = charactersGroup.Children.Count;
        
        // Build Wiki group (top-level WikiArticle articles)
        var wikiArticles = worldArticles
            .Where(a => a.Type == ArticleType.WikiArticle && a.ParentId == null)
            .OrderBy(a => a.Title)
            .ToList();
        
        foreach (var article in wikiArticles)
        {
            var articleNode = BuildArticleNodeWithChildren(article, worldArticles, articleIndex);
            wikiGroup.Children.Add(articleNode);
        }
        wikiGroup.ChildCount = wikiGroup.Children.Count;

        // Build Links group (external resources)
        var linksGroup = CreateVirtualGroupNode(VirtualGroupType.Links, "External Resources", world.Id);
        foreach (var link in worldLinks.OrderBy(l => l.Title))
        {
            var linkNode = new TreeNode
            {
                Id = link.Id,
                NodeType = TreeNodeType.ExternalLink,
                Title = link.Title,
                Url = link.Url,
                WorldId = world.Id,
                IconEmoji = "fa-solid fa-external-link-alt"
            };
            linksGroup.Children.Add(linkNode);
            _nodeIndex[linkNode.Id] = linkNode;
        }
        
        // Add documents to the Links group
        foreach (var document in worldDocuments.OrderBy(d => d.Title))
        {
            var documentNode = new TreeNode
            {
                Id = document.Id,
                NodeType = TreeNodeType.ExternalLink, // Reuse ExternalLink type for documents
                Title = document.Title,
                Url = null, // Documents don't have direct URLs, they need download flow
                WorldId = world.Id,
                IconEmoji = GetDocumentIcon(document.ContentType),
                // Store document-specific data
                AdditionalData = new Dictionary<string, object>
                {
                    { "IsDocument", true },
                    { "ContentType", document.ContentType },
                    { "FileSizeBytes", document.FileSizeBytes },
                    { "FileName", document.FileName }
                }
            };
            linksGroup.Children.Add(documentNode);
            _nodeIndex[documentNode.Id] = documentNode;
        }
        
        linksGroup.ChildCount = linksGroup.Children.Count;
        
        // Build Uncategorized group (Legacy and untyped articles)
        var uncategorizedArticles = worldArticles
            .Where(a => a.ParentId == null && 
                       a.Type != ArticleType.WikiArticle && 
                       a.Type != ArticleType.Character &&
                       a.Type != ArticleType.Session &&
                       a.Type != ArticleType.SessionNote &&
                       a.Type != ArticleType.CharacterNote)
            .OrderBy(a => a.Title)
            .ToList();
        
        foreach (var article in uncategorizedArticles)
        {
            var articleNode = BuildArticleNodeWithChildren(article, worldArticles, articleIndex);
            uncategorizedGroup.Children.Add(articleNode);
        }
        uncategorizedGroup.ChildCount = uncategorizedGroup.Children.Count;
        
        // Add groups to world node
        worldNode.Children.Add(campaignsGroup);
        _nodeIndex[campaignsGroup.Id] = campaignsGroup;
        
        worldNode.Children.Add(charactersGroup);
        _nodeIndex[charactersGroup.Id] = charactersGroup;
        
        worldNode.Children.Add(wikiGroup);
        _nodeIndex[wikiGroup.Id] = wikiGroup;
        
        // Only add Links if it has content
        if (linksGroup.Children.Any())
        {
            worldNode.Children.Add(linksGroup);
            _nodeIndex[linksGroup.Id] = linksGroup;
        }
        
        // Only add Uncategorized if it has content
        if (uncategorizedGroup.Children.Any())
        {
            worldNode.Children.Add(uncategorizedGroup);
            _nodeIndex[uncategorizedGroup.Id] = uncategorizedGroup;
        }
        
        worldNode.ChildCount = worldNode.Children.Count;
        
        return worldNode;
    }
    
    /// <summary>
    /// Builds a campaign node synchronously using pre-fetched arc data.
    /// </summary>
    private TreeNode BuildCampaignNode(
        CampaignDto campaign,
        List<ArcDto> arcs,
        List<ArticleTreeDto> worldArticles,
        Dictionary<Guid, ArticleTreeDto> articleIndex)
    {
        var campaignNode = new TreeNode
        {
            Id = campaign.Id,
            NodeType = TreeNodeType.Campaign,
            Title = campaign.Name,
            WorldId = campaign.WorldId,
            CampaignId = campaign.Id,
            IconEmoji = "fa-solid fa-dungeon"
        };
        
        foreach (var arc in arcs.OrderBy(a => a.SortOrder).ThenBy(a => a.Name))
        {
            var arcNode = BuildArcNode(arc, worldArticles, articleIndex);
            campaignNode.Children.Add(arcNode);
            _nodeIndex[arcNode.Id] = arcNode;
        }
        
        campaignNode.ChildCount = campaignNode.Children.Count;
        
        return campaignNode;
    }
    
    private TreeNode BuildArcNode(
        ArcDto arc,
        List<ArticleTreeDto> worldArticles,
        Dictionary<Guid, ArticleTreeDto> articleIndex)
    {
        var arcNode = new TreeNode
        {
            Id = arc.Id,
            NodeType = TreeNodeType.Arc,
            Title = arc.Name,
            CampaignId = arc.CampaignId,
            ArcId = arc.Id,
            IconEmoji = "fa-solid fa-book-open"
        };
        
        // Find session articles for this arc
        var sessionArticles = worldArticles
            .Where(a => a.ArcId == arc.Id && a.Type == ArticleType.Session)
            .OrderBy(a => a.Title)
            .ToList();
        
        foreach (var article in sessionArticles)
        {
            var articleNode = BuildArticleNodeWithChildren(article, worldArticles, articleIndex);
            arcNode.Children.Add(articleNode);
        }
        
        arcNode.ChildCount = arcNode.Children.Count;
        
        return arcNode;
    }
    
    private TreeNode BuildArticleNodeWithChildren(
        ArticleTreeDto article,
        List<ArticleTreeDto> allArticles,
        Dictionary<Guid, ArticleTreeDto> articleIndex)
    {
        var node = CreateArticleNode(article);
        _nodeIndex[node.Id] = node;
        
        // Find children
        var children = allArticles
            .Where(a => a.ParentId == article.Id)
            .OrderBy(a => a.Title)
            .ToList();
        
        foreach (var child in children)
        {
            var childNode = BuildArticleNodeWithChildren(child, allArticles, articleIndex);
            childNode.ParentId = node.Id;
            node.Children.Add(childNode);
        }
        
        node.ChildCount = node.Children.Count;
        
        return node;
    }
    
    private TreeNode CreateArticleNode(ArticleTreeDto article)
    {
        return new TreeNode
        {
            Id = article.Id,
            NodeType = TreeNodeType.Article,
            ArticleType = article.Type,
            Title = article.Title,
            Slug = article.Slug,
            IconEmoji = article.IconEmoji,
            ParentId = article.ParentId,
            WorldId = article.WorldId,
            CampaignId = article.CampaignId,
            ArcId = article.ArcId,
            ChildCount = article.ChildCount,
            Visibility = article.Visibility,
            HasAISummary = article.HasAISummary
        };
    }
    
    private TreeNode CreateVirtualGroupNode(VirtualGroupType groupType, string title, Guid? worldId)
    {
        return new TreeNode
        {
            Id = Guid.NewGuid(),
            NodeType = TreeNodeType.VirtualGroup,
            VirtualGroupType = groupType,
            Title = title,
            WorldId = worldId
        };
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
                CollapseNode(nodeId);
            else
                ExpandNode(nodeId);
        }
    }
    
    public void SelectNode(Guid nodeId)
    {
        // Deselect previous
        if (_selectedNodeId.HasValue && _nodeIndex.TryGetValue(_selectedNodeId.Value, out var previousNode))
        {
            previousNode.IsSelected = false;
        }
        
        // Select new node
        if (_nodeIndex.TryGetValue(nodeId, out var node))
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
                    SaveExpandedStateAsync().ConfigureAwait(false);
                }
            }
            else
            {
                // For virtual groups, just toggle expand
                ToggleNode(nodeId);
                _selectedNodeId = null;
            }
        }
        else
        {
            _selectedNodeId = null;
        }
        
        NotifyStateChanged();
    }
    
    public void ExpandPathToAndSelect(Guid nodeId)
    {
        if (!_isInitialized)
        {
            _pendingSelectionId = nodeId;
            _selectedNodeId = nodeId;
            return;
        }
        
        if (!_nodeIndex.TryGetValue(nodeId, out var targetNode))
        {
            _logger.LogWarning("ExpandPathToAndSelect: Node {NodeId} not found", nodeId);
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
                // Check if this node is a child of a world/group node
                current = FindParentNode(current);
            }
        }
        
        // Collect the IDs of nodes in the path (for quick lookup)
        var pathNodeIds = new HashSet<Guid>(path.Select(n => n.Id));
        
        // Collapse all nodes that are NOT in the path to the target
        foreach (var node in _nodeIndex.Values)
        {
            if (node.IsExpanded && !pathNodeIds.Contains(node.Id))
            {
                node.IsExpanded = false;
                _expandedNodeIds.Remove(node.Id);
            }
        }
        
        // Expand all ancestors in the path
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
    
    private TreeNode? FindParentNode(TreeNode child)
    {
        // Search through all nodes to find the parent
        foreach (var node in _nodeIndex.Values)
        {
            if (node.Children.Contains(child))
            {
                return node;
            }
        }
        return null;
    }
    
    // ============================================
    // CRUD Operations
    // ============================================
    
    public async Task<Guid?> CreateRootArticleAsync()
    {
        // For root articles, we need to know the world context
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
        await RefreshAsync();
        
        // Select the new node
        SelectNode(created.Id);
        ShouldFocusTitle = true;
        
        return created.Id;
    }
    
    public async Task<Guid?> CreateChildArticleAsync(Guid parentId)
    {
        if (!_nodeIndex.TryGetValue(parentId, out var parentNode))
        {
            _logger.LogWarning("Cannot create child: parent {ParentId} not found", parentId);
            return null;
        }
        
        // Determine article type based on parent
        var articleType = parentNode.NodeType switch
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
        await RefreshAsync();
        
        // Expand parent and select new node
        ExpandNode(parentId);
        SelectNode(created.Id);
        ShouldFocusTitle = true;
        
        return created.Id;
    }
    
    public async Task<bool> DeleteArticleAsync(Guid articleId)
    {
        if (!_nodeIndex.TryGetValue(articleId, out var node))
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
            await RefreshAsync();
            
            // If deleted node was selected, clear selection
            if (_selectedNodeId == articleId)
            {
                _selectedNodeId = null;
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
    
    public async Task<bool> MoveArticleAsync(Guid articleId, Guid? newParentId)
    {
        if (!_nodeIndex.TryGetValue(articleId, out var node))
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
            _nodeIndex.TryGetValue(newParentId.Value, out targetNode);
        }
        
        // Handle drop onto virtual group specially
        if (targetNode?.NodeType == TreeNodeType.VirtualGroup)
        {
            // Campaigns group holds Campaign entities, not articles
            if (targetNode.VirtualGroupType == VirtualGroupType.Campaigns)
            {
                _logger.LogWarning("Cannot drop articles into Campaigns group - campaigns are separate entities");
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
                    // Fetch the full article to preserve its content
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
                
                await RefreshAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to move article to virtual group");
                return false;
            }
        }
        
        // Regular move to another article or root
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
            
            if (success)
            {
                await RefreshAsync();
            }
            
            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to move article {ArticleId}", articleId);
            return false;
        }
    }
    
    private bool IsDescendantOf(Guid nodeId, Guid potentialAncestorId)
    {
        if (!_nodeIndex.TryGetValue(nodeId, out var node))
            return false;
        
        var current = node;
        while (current.ParentId.HasValue)
        {
            if (current.ParentId.Value == potentialAncestorId)
                return true;
            
            if (!_nodeIndex.TryGetValue(current.ParentId.Value, out current))
                break;
        }
        
        return false;
    }
    
    public void UpdateNodeDisplay(Guid nodeId, string title, string? iconEmoji)
    {
        if (_nodeIndex.TryGetValue(nodeId, out var node))
        {
            node.Title = title;
            node.IconEmoji = iconEmoji;
            NotifyStateChanged();
        }
    }
    
    public void UpdateNodeVisibility(Guid nodeId, ArticleVisibility visibility)
    {
        if (_nodeIndex.TryGetValue(nodeId, out var node))
        {
            node.Visibility = visibility;
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
        
        // Find all matching nodes (articles only for search)
        foreach (var node in _nodeIndex.Values)
        {
            if (node.NodeType == TreeNodeType.Article && 
                node.Title.Contains(searchLower, StringComparison.OrdinalIgnoreCase))
            {
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
        set.Add(node.Id);
        
        // Add direct parent
        if (node.ParentId.HasValue && _nodeIndex.TryGetValue(node.ParentId.Value, out var parent))
        {
            AddNodeAndAncestors(parent, set);
        }
        
        // Also find the containing node (for virtual groups, etc.)
        var container = FindParentNode(node);
        if (container != null && !set.Contains(container.Id))
        {
            AddNodeAndAncestors(container, set);
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
            _logger.LogWarning(ex, "Failed to save expanded state");
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
            _logger.LogWarning(ex, "Failed to restore expanded state");
        }
    }
    
    private static string GetDocumentIcon(string contentType)
    {
        return contentType.ToLowerInvariant() switch
        {
            "application/pdf" => "fa-solid fa-file-pdf",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document" => "fa-solid fa-file-word",
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" => "fa-solid fa-file-excel",
            "application/vnd.openxmlformats-officedocument.presentationml.presentation" => "fa-solid fa-file-powerpoint",
            "text/plain" => "fa-solid fa-file-lines",
            "text/markdown" => "fa-solid fa-file-lines",
            string ct when ct.StartsWith("image/") => "fa-solid fa-file-image",
            _ => "fa-solid fa-file"
        };
    }
    
    // ============================================
    // Node Lookup
    // ============================================
    
    public bool TryGetNode(Guid nodeId, out TreeNode? node)
    {
        if (_nodeIndex.TryGetValue(nodeId, out var foundNode))
        {
            node = foundNode;
            return true;
        }
        
        node = null;
        return false;
    }
}
