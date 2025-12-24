namespace Chronicis.Client.Models;

/// <summary>
/// Defines the type of node in the navigation tree.
/// Different node types have different behaviors and rendering.
/// </summary>
public enum TreeNodeType
{
    /// <summary>
    /// A World entity - top level container.
    /// </summary>
    World,
    
    /// <summary>
    /// A virtual grouping node (Campaigns, Player Characters, Wiki, Uncategorized).
    /// Not backed by a database entity.
    /// </summary>
    VirtualGroup,
    
    /// <summary>
    /// A Campaign entity.
    /// </summary>
    Campaign,
    
    /// <summary>
    /// An Arc entity within a Campaign.
    /// </summary>
    Arc,
    
    /// <summary>
    /// An Article entity (WikiArticle, Character, Session, etc.).
    /// </summary>
    Article
}

/// <summary>
/// Identifies the type of virtual group for display and behavior.
/// </summary>
public enum VirtualGroupType
{
    None,
    Campaigns,
    PlayerCharacters,
    Wiki,
    Uncategorized
}
