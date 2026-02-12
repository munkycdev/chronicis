using Chronicis.Client.Models;
using Chronicis.Shared.DTOs;
using Chronicis.Shared.Enums;

namespace Chronicis.Client.Services.Tree;

/// <summary>
/// Responsible for building the navigation tree structure from API data.
/// Fetches worlds, campaigns, arcs, and articles and constructs the hierarchical tree.
/// </summary>
internal sealed class TreeDataBuilder
{
    private readonly IArticleApiService _articleApi;
    private readonly IWorldApiService _worldApi;
    private readonly ICampaignApiService _campaignApi;
    private readonly IArcApiService _arcApi;
    private readonly ILogger _logger;

    public TreeDataBuilder(
        IArticleApiService articleApi,
        IWorldApiService worldApi,
        ICampaignApiService campaignApi,
        IArcApiService arcApi,
        ILogger logger)
    {
        _articleApi = articleApi;
        _worldApi = worldApi;
        _campaignApi = campaignApi;
        _arcApi = arcApi;
        _logger = logger;
    }

    /// <summary>
    /// Result of building the tree, containing the node index and cached articles.
    /// </summary>
    public sealed class BuildResult
    {
        public required TreeNodeIndex NodeIndex { get; init; }
        public required List<ArticleTreeDto> CachedArticles { get; init; }
    }

    /// <summary>
    /// Builds the complete tree structure by fetching all data from APIs.
    /// </summary>
    public async Task<BuildResult> BuildTreeAsync()
    {
        var nodeIndex = new TreeNodeIndex();
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Phase 1: Fetch worlds and articles in parallel
        var worldsTask = _worldApi.GetWorldsAsync();
        var articlesTask = _articleApi.GetAllArticlesAsync();

        await Task.WhenAll(worldsTask, articlesTask);

        var worlds = worldsTask.Result;
        var allArticles = articlesTask.Result;

        if (!worlds.Any())
        {
            return new BuildResult
            {
                NodeIndex = nodeIndex,
                CachedArticles = allArticles
            };
        }

        // Phase 2: Fetch all world details in parallel
        _logger.LogDebug("Phase 2: Fetching world details in parallel...");
        var worldDetailTasks = worlds.Select(w => _worldApi.GetWorldAsync(w.Id)).ToList();
        var worldDetails = await Task.WhenAll(worldDetailTasks);

        var worldDetailLookup = worldDetails
            .Where(wd => wd != null)
            .ToDictionary(wd => wd!.Id, wd => wd!);

        _logger.LogDebug("Phase 2 complete: {Count} world details in {Ms}ms",
            worldDetailLookup.Count, stopwatch.ElapsedMilliseconds);

        // Phase 3: Collect all campaigns and fetch arcs, links, documents in parallel
        var allCampaigns = worldDetails
            .Where(wd => wd != null)
            .SelectMany(wd => wd!.Campaigns ?? new List<CampaignDto>())
            .ToList();

        _logger.LogDebug("Phase 3: Fetching arcs for {Count} campaigns, world links, and documents in parallel...", allCampaigns.Count);

        var arcTasks = allCampaigns.Select(c => _arcApi.GetArcsByCampaignAsync(c.Id)).ToList();
        var linkTasks = worlds.Select(w => _worldApi.GetWorldLinksAsync(w.Id)).ToList();
        var documentTasks = worlds.Select(w => _worldApi.GetWorldDocumentsAsync(w.Id)).ToList();

        await Task.WhenAll(
            Task.WhenAll(arcTasks),
            Task.WhenAll(linkTasks),
            Task.WhenAll(documentTasks)
        );

        var arcResults = arcTasks.Select(t => t.Result).ToArray();

        var linksByWorld = new Dictionary<Guid, List<WorldLinkDto>>();
        for (int i = 0; i < worlds.Count; i++)
        {
            linksByWorld[worlds[i].Id] = linkTasks[i].Result;
        }

        var documentsByWorld = new Dictionary<Guid, List<WorldDocumentDto>>();
        for (int i = 0; i < worlds.Count; i++)
        {
            documentsByWorld[worlds[i].Id] = documentTasks[i].Result;
        }

        var arcsByCampaign = new Dictionary<Guid, List<ArcDto>>();
        for (int i = 0; i < allCampaigns.Count; i++)
        {
            arcsByCampaign[allCampaigns[i].Id] = arcResults[i];
        }

        _logger.LogDebug("Phase 3 complete: {Count} total arcs in {Ms}ms",
            arcResults.Sum(r => r.Count), stopwatch.ElapsedMilliseconds);

        // Phase 4: Build the tree structure
        _logger.LogDebug("Phase 4: Building tree structure...");

        var articleIndex = allArticles.ToDictionary(a => a.Id);

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
                worldDocuments,
                nodeIndex);

            nodeIndex.AddRootNode(worldNode);
        }

        // Handle orphan articles (no world assigned)
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
                nodeIndex.AddNode(articleNode);
            }

            if (orphanNode.Children.Any())
            {
                orphanNode.ChildCount = orphanNode.Children.Count;
                nodeIndex.AddRootNode(orphanNode);
            }
        }

        stopwatch.Stop();
        _logger.LogDebug("Tree build complete in {Ms}ms total", stopwatch.ElapsedMilliseconds);

        return new BuildResult
        {
            NodeIndex = nodeIndex,
            CachedArticles = allArticles
        };
    }

    private TreeNode BuildWorldNode(
        WorldDto world,
        WorldDetailDto worldDetail,
        List<ArticleTreeDto> allArticles,
        Dictionary<Guid, ArticleTreeDto> articleIndex,
        Dictionary<Guid, List<ArcDto>> arcsByCampaign,
        List<WorldLinkDto> worldLinks,
        List<WorldDocumentDto> worldDocuments,
        TreeNodeIndex nodeIndex)
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

            var campaignNode = BuildCampaignNode(campaign, arcs, worldArticles, articleIndex, nodeIndex);
            campaignsGroup.Children.Add(campaignNode);
            nodeIndex.AddNode(campaignNode);
        }
        campaignsGroup.ChildCount = campaignsGroup.Children.Count;

        // Build Characters group
        var characterArticles = worldArticles
            .Where(a => a.Type == ArticleType.Character && a.ParentId == null)
            .OrderBy(a => a.Title)
            .ToList();

        foreach (var article in characterArticles)
        {
            var articleNode = BuildArticleNodeWithChildren(article, worldArticles, articleIndex, nodeIndex);
            charactersGroup.Children.Add(articleNode);
        }
        charactersGroup.ChildCount = charactersGroup.Children.Count;

        // Build Wiki group
        var wikiArticles = worldArticles
            .Where(a => a.Type == ArticleType.WikiArticle && a.ParentId == null)
            .OrderBy(a => a.Title)
            .ToList();

        foreach (var article in wikiArticles)
        {
            var articleNode = BuildArticleNodeWithChildren(article, worldArticles, articleIndex, nodeIndex);
            wikiGroup.Children.Add(articleNode);
        }
        wikiGroup.ChildCount = wikiGroup.Children.Count;

        // Build Links group
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
            nodeIndex.AddNode(linkNode);
        }

        foreach (var document in worldDocuments.OrderBy(d => d.Title))
        {
            var documentNode = new TreeNode
            {
                Id = document.Id,
                NodeType = TreeNodeType.ExternalLink,
                Title = document.Title,
                Url = null,
                WorldId = world.Id,
                IconEmoji = GetDocumentIcon(document.ContentType),
                AdditionalData = new Dictionary<string, object>
                {
                    { "IsDocument", true },
                    { "ContentType", document.ContentType },
                    { "FileSizeBytes", document.FileSizeBytes },
                    { "FileName", document.FileName }
                }
            };
            linksGroup.Children.Add(documentNode);
            nodeIndex.AddNode(documentNode);
        }
        linksGroup.ChildCount = linksGroup.Children.Count;

        // Build Uncategorized group
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
            var articleNode = BuildArticleNodeWithChildren(article, worldArticles, articleIndex, nodeIndex);
            uncategorizedGroup.Children.Add(articleNode);
        }
        uncategorizedGroup.ChildCount = uncategorizedGroup.Children.Count;

        // Add groups to world node
        worldNode.Children.Add(campaignsGroup);
        nodeIndex.AddNode(campaignsGroup);

        worldNode.Children.Add(charactersGroup);
        nodeIndex.AddNode(charactersGroup);

        worldNode.Children.Add(wikiGroup);
        nodeIndex.AddNode(wikiGroup);

        if (linksGroup.Children.Any())
        {
            worldNode.Children.Add(linksGroup);
            nodeIndex.AddNode(linksGroup);
        }

        if (uncategorizedGroup.Children.Any())
        {
            worldNode.Children.Add(uncategorizedGroup);
            nodeIndex.AddNode(uncategorizedGroup);
        }

        worldNode.ChildCount = worldNode.Children.Count;

        return worldNode;
    }

    private TreeNode BuildCampaignNode(
        CampaignDto campaign,
        List<ArcDto> arcs,
        List<ArticleTreeDto> worldArticles,
        Dictionary<Guid, ArticleTreeDto> articleIndex,
        TreeNodeIndex nodeIndex)
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
            var arcNode = BuildArcNode(arc, worldArticles, articleIndex, nodeIndex);
            campaignNode.Children.Add(arcNode);
            nodeIndex.AddNode(arcNode);
        }

        campaignNode.ChildCount = campaignNode.Children.Count;

        return campaignNode;
    }

    private TreeNode BuildArcNode(
        ArcDto arc,
        List<ArticleTreeDto> worldArticles,
        Dictionary<Guid, ArticleTreeDto> articleIndex,
        TreeNodeIndex nodeIndex)
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

        var sessionArticles = worldArticles
            .Where(a => a.ArcId == arc.Id && a.Type == ArticleType.Session)
            .OrderBy(a => a.Title)
            .ToList();

        foreach (var article in sessionArticles)
        {
            var articleNode = BuildArticleNodeWithChildren(article, worldArticles, articleIndex, nodeIndex);
            arcNode.Children.Add(articleNode);
        }

        arcNode.ChildCount = arcNode.Children.Count;

        return arcNode;
    }

    private TreeNode BuildArticleNodeWithChildren(
        ArticleTreeDto article,
        List<ArticleTreeDto> allArticles,
        Dictionary<Guid, ArticleTreeDto> articleIndex,
        TreeNodeIndex nodeIndex)
    {
        var node = CreateArticleNode(article);
        nodeIndex.AddNode(node);

        var children = allArticles
            .Where(a => a.ParentId == article.Id)
            .OrderBy(a => a.Title)
            .ToList();

        foreach (var child in children)
        {
            var childNode = BuildArticleNodeWithChildren(child, allArticles, articleIndex, nodeIndex);
            childNode.ParentId = node.Id;
            node.Children.Add(childNode);
        }

        node.ChildCount = node.Children.Count;

        return node;
    }

    /// <summary>
    /// Creates a TreeNode from an ArticleTreeDto.
    /// </summary>
    public static TreeNode CreateArticleNode(ArticleTreeDto article)
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

    /// <summary>
    /// Creates a virtual group node.
    /// </summary>
    public static TreeNode CreateVirtualGroupNode(VirtualGroupType groupType, string title, Guid? worldId)
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

    /// <summary>
    /// Gets the appropriate icon for a document based on its content type.
    /// </summary>
    public static string GetDocumentIcon(string contentType)
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
}
