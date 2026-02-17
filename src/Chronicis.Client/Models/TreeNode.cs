using System.Diagnostics.CodeAnalysis;
using Chronicis.Shared.Enums;

namespace Chronicis.Client.Models;

/// <summary>
/// Represents a node in the navigation tree.
/// Can represent a World, Campaign, Arc, Article, or Virtual Group.
/// </summary>
[ExcludeFromCodeCoverage]
public class TreeNode
{
    /// <summary>
    /// Unique identifier for this node.
    /// For virtual groups, this is a generated GUID.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The type of node (World, Campaign, Arc, Article, VirtualGroup).
    /// </summary>
    public TreeNodeType NodeType { get; set; } = TreeNodeType.Article;

    /// <summary>
    /// For virtual group nodes, identifies which group this is.
    /// </summary>
    public VirtualGroupType VirtualGroupType { get; set; } = VirtualGroupType.None;

    /// <summary>
    /// For article nodes, the article type.
    /// </summary>
    public ArticleType? ArticleType { get; set; }

    /// <summary>
    /// Display title for the node.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// URL slug (for articles).
    /// </summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>
    /// Icon emoji or FontAwesome class.
    /// </summary>
    public string? IconEmoji { get; set; }

    /// <summary>
    /// Parent node ID (for tree structure).
    /// </summary>
    public Guid? ParentId { get; set; }

    /// <summary>
    /// For World nodes, the World ID.
    /// </summary>
    public Guid? WorldId { get; set; }

    /// <summary>
    /// For Campaign/Arc/Article nodes, the Campaign ID.
    /// </summary>
    public Guid? CampaignId { get; set; }

    /// <summary>
    /// For Arc/Session nodes, the Arc ID.
    /// </summary>
    public Guid? ArcId { get; set; }

    /// <summary>
    /// For ExternalLink nodes, the URL to navigate to.
    /// </summary>
    public string? Url { get; set; }

    /// <summary>
    /// For article nodes, the visibility setting.
    /// </summary>
    public ArticleVisibility Visibility { get; set; } = ArticleVisibility.Public;

    /// <summary>
    /// Indicates whether this article has an AI-generated summary.
    /// </summary>
    public bool HasAISummary { get; set; }

    /// <summary>
    /// Number of children (from server or computed).
    /// </summary>
    public int ChildCount { get; set; }

    /// <summary>
    /// Additional data for specialized node types (e.g., documents).
    /// </summary>
    public Dictionary<string, object>? AdditionalData { get; set; }

    // ===== UI State =====

    /// <summary>
    /// Whether this node is expanded in the tree.
    /// </summary>
    public bool IsExpanded { get; set; }

    /// <summary>
    /// Whether this node is currently selected.
    /// </summary>
    public bool IsSelected { get; set; }

    /// <summary>
    /// Whether this node is visible (for search filtering).
    /// </summary>
    public bool IsVisible { get; set; } = true;

    /// <summary>
    /// Child nodes.
    /// </summary>
    public List<TreeNode> Children { get; set; } = new();

    // ===== Computed Properties =====

    /// <summary>
    /// Display title - shows "(Untitled)" for empty titles, or group names for virtual groups.
    /// </summary>
    public string DisplayTitle => NodeType == TreeNodeType.VirtualGroup
        ? GetVirtualGroupDisplayName()
        : (string.IsNullOrWhiteSpace(Title) ? "(Untitled)" : Title);

    /// <summary>
    /// Whether this node has any children.
    /// </summary>
    public bool HasChildren => ChildCount > 0 || Children.Count > 0;

    /// <summary>
    /// Whether this node can be selected/navigated to.
    /// Virtual groups cannot be selected. External links open in new tab.
    /// </summary>
    public bool IsSelectable => NodeType switch
    {
        TreeNodeType.Article => true,
        TreeNodeType.World => true,
        TreeNodeType.Campaign => true,
        TreeNodeType.Arc => true,
        TreeNodeType.ExternalLink => true, // Opens in new tab
        _ => false
    };

    /// <summary>
    /// Whether this node can have children added to it.
    /// </summary>
    public bool CanAddChildren => NodeType switch
    {
        TreeNodeType.World => false, // Add via virtual groups
        TreeNodeType.VirtualGroup => VirtualGroupType switch
        {
            // Campaigns requires creating Campaign entities (separate flow)
            VirtualGroupType.Campaigns => false, // TODO: Enable when campaign creation dialog is ready
            // Links are managed via WorldDetail page, not tree
            VirtualGroupType.Links => false,
            _ => true
        },
        TreeNodeType.Campaign => false, // Add arcs via separate flow
        TreeNodeType.Arc => true, // Add sessions
        TreeNodeType.Article => true,
        TreeNodeType.ExternalLink => false, // Links don't have children
        _ => false
    };

    /// <summary>
    /// Whether this node can be dragged for reordering.
    /// </summary>
    public bool IsDraggable => NodeType == TreeNodeType.Article;

    /// <summary>
    /// Whether this node can accept dropped items.
    /// </summary>
    public bool IsDropTarget => NodeType switch
    {
        TreeNodeType.Article => true,
        TreeNodeType.VirtualGroup => VirtualGroupType switch
        {
            // Campaigns group holds Campaign entities, not articles
            VirtualGroupType.Campaigns => false,
            // Links group holds external links, not articles
            VirtualGroupType.Links => false,
            // Other groups can accept article drops
            _ => true
        },
        _ => false
    };

    private string GetVirtualGroupDisplayName() => VirtualGroupType switch
    {
        VirtualGroupType.Campaigns => "Campaigns",
        VirtualGroupType.PlayerCharacters => "Player Characters",
        VirtualGroupType.Wiki => "Wiki",
        VirtualGroupType.Links => "External Resources",
        VirtualGroupType.Uncategorized => "Uncategorized",
        _ => Title
    };

    /// <summary>
    /// Gets the appropriate icon for this node type.
    /// </summary>
    public string GetDefaultIcon() => NodeType switch
    {
        TreeNodeType.World => "fa-solid fa-globe",
        TreeNodeType.VirtualGroup => VirtualGroupType switch
        {
            VirtualGroupType.Campaigns => "fa-solid fa-scroll",
            VirtualGroupType.PlayerCharacters => "fa-solid fa-user-group",
            VirtualGroupType.Wiki => "fa-solid fa-book-atlas",
            VirtualGroupType.Links => "fa-solid fa-link",
            VirtualGroupType.Uncategorized => "fa-solid fa-folder-open",
            _ => "fa-solid fa-folder"
        },
        TreeNodeType.Campaign => "fa-solid fa-dungeon",
        TreeNodeType.Arc => "fa-solid fa-book-open",
        TreeNodeType.ExternalLink => "fa-solid fa-external-link-alt",
        TreeNodeType.Article => ArticleType switch
        {
            Shared.Enums.ArticleType.Character => "fa-solid fa-user",
            Shared.Enums.ArticleType.CharacterNote => "fa-solid fa-scroll",
            Shared.Enums.ArticleType.Session => "fa-solid fa-calendar-day",
            Shared.Enums.ArticleType.SessionNote => "fa-solid fa-note-sticky",
            Shared.Enums.ArticleType.WikiArticle => "fa-solid fa-file-lines",
            Shared.Enums.ArticleType.Legacy => "fa-solid fa-box-archive",
            _ => "fa-solid fa-file"
        },
        _ => "fa-solid fa-file"
    };
}
