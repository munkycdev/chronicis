namespace Chronicis.Client.Models;

/// <summary>
/// Represents a node in the article tree navigation.
/// This is the single canonical representation used throughout the UI.
/// </summary>
public class TreeNode
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? IconEmoji { get; set; }
    public Guid? ParentId { get; set; }
    public int ChildCount { get; set; }
    
    // UI State
    public bool IsExpanded { get; set; }
    public bool IsSelected { get; set; }
    public bool IsVisible { get; set; } = true; // For search filtering
    
    // Children - populated after building the tree
    public List<TreeNode> Children { get; set; } = new();
    
    /// <summary>
    /// Display title - shows "(Untitled)" for empty titles.
    /// </summary>
    public string DisplayTitle => string.IsNullOrWhiteSpace(Title) ? "(Untitled)" : Title;
    
    /// <summary>
    /// Whether this node has any children.
    /// </summary>
    public bool HasChildren => ChildCount > 0 || Children.Count > 0;
}
