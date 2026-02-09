using Chronicis.Shared.DTOs;

namespace Chronicis.Client.Services;

/// <summary>
/// Service for managing wiki link autocomplete state and operations
/// </summary>
public interface IWikiLinkAutocompleteService
{
    /// <summary>
    /// Event raised when autocomplete should be shown
    /// </summary>
    event Action? OnShow;
    
    /// <summary>
    /// Event raised when autocomplete should be hidden
    /// </summary>
    event Action? OnHide;
    
    /// <summary>
    /// Event raised when suggestions have been updated
    /// </summary>
    event Action? OnSuggestionsUpdated;
    
    /// <summary>
    /// Current autocomplete position (x, y)
    /// </summary>
    (double X, double Y) Position { get; }
    
    /// <summary>
    /// Whether autocomplete is currently visible
    /// </summary>
    bool IsVisible { get; }
    
    /// <summary>
    /// Current search query
    /// </summary>
    string Query { get; }
    
    /// <summary>
    /// Whether this is an external resource query (e.g., srd/...)
    /// </summary>
    bool IsExternalQuery { get; }
    
    /// <summary>
    /// External source key (e.g., "srd")
    /// </summary>
    string? ExternalSourceKey { get; }
    
    /// <summary>
    /// Current autocomplete suggestions
    /// </summary>
    List<WikiLinkAutocompleteItem> Suggestions { get; }
    
    /// <summary>
    /// Currently selected suggestion index
    /// </summary>
    int SelectedIndex { get; }
    
    /// <summary>
    /// Whether suggestions are currently loading
    /// </summary>
    bool IsLoading { get; }
    
    /// <summary>
    /// Show autocomplete at the specified position with the given query
    /// </summary>
    Task ShowAsync(string query, double x, double y, Guid? worldId);
    
    /// <summary>
    /// Hide the autocomplete
    /// </summary>
    void Hide();
    
    /// <summary>
    /// Move selection down
    /// </summary>
    void SelectNext();
    
    /// <summary>
    /// Move selection up
    /// </summary>
    void SelectPrevious();
    
    /// <summary>
    /// Set the selected index directly
    /// </summary>
    void SetSelectedIndex(int index);
    
    /// <summary>
    /// Get the currently selected suggestion
    /// </summary>
    WikiLinkAutocompleteItem? GetSelectedSuggestion();
}

/// <summary>
/// Autocomplete item representing either an internal article or external resource
/// </summary>
public class WikiLinkAutocompleteItem
{
    public string DisplayText { get; set; } = string.Empty;
    public string? ArticleId { get; set; }
    public string? ExternalKey { get; set; }
    public string? Tooltip { get; set; }
    public bool IsExternal { get; set; }
    public bool IsCategory { get; set; }
    
    public static WikiLinkAutocompleteItem FromInternal(LinkSuggestionDto suggestion)
    {
        return new WikiLinkAutocompleteItem
        {
            DisplayText = suggestion.Title,
            ArticleId = suggestion.ArticleId.ToString(),
            Tooltip = suggestion.DisplayPath,
            IsExternal = false,
            IsCategory = false
        };
    }
    
    public static WikiLinkAutocompleteItem FromExternal(ExternalLinkSuggestionDto suggestion)
    {
        return new WikiLinkAutocompleteItem
        {
            DisplayText = suggestion.Title,
            ExternalKey = suggestion.Id,
            Tooltip = suggestion.Source,
            IsExternal = true,
            IsCategory = !string.IsNullOrEmpty(suggestion.Category)
        };
    }
}
