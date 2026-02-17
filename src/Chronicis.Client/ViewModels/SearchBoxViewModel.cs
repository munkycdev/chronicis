namespace Chronicis.Client.ViewModels;

/// <summary>
/// ViewModel for SearchBox component.
/// Encapsulates search state and behavior, allowing the component to be testable
/// without requiring ITreeStateService dependency.
/// </summary>
public class SearchBoxViewModel
{
    private string _searchText = string.Empty;

    /// <summary>
    /// Current search text.
    /// </summary>
    public string SearchText
    {
        get => _searchText;
        set
        {
            if (_searchText != value)
            {
                _searchText = value;
                SearchTextChanged?.Invoke(value);
            }
        }
    }

    /// <summary>
    /// Whether the search box has text.
    /// </summary>
    public bool HasText => !string.IsNullOrWhiteSpace(_searchText);

    /// <summary>
    /// Event raised when search text changes.
    /// Parent can use this to update UI or trigger incremental search.
    /// </summary>
    public event Action<string>? SearchTextChanged;

    /// <summary>
    /// Event raised when user requests to execute search (Enter key or search button).
    /// </summary>
    public event Action<string>? SearchRequested;

    /// <summary>
    /// Event raised when user requests to clear search (Escape key or clear button).
    /// </summary>
    public event Action? ClearRequested;

    /// <summary>
    /// Execute search with current text.
    /// Called when user presses Enter or clicks search icon.
    /// </summary>
    public void ExecuteSearch()
    {
        SearchRequested?.Invoke(_searchText);
    }

    /// <summary>
    /// Clear the search.
    /// Called when user presses Escape or clicks clear icon.
    /// </summary>
    public void Clear()
    {
        SearchText = string.Empty;
        ClearRequested?.Invoke();
    }
}
