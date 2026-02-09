using Chronicis.Client.Services;
using Microsoft.AspNetCore.Components;

namespace Chronicis.Client.Components.Shared;

public partial class WikiLinkAutocomplete : IDisposable
{
    [Inject] private IWikiLinkAutocompleteService _autocompleteService { get; set; } = null!;
    
    [Parameter] public string EditorId { get; set; } = null!;
    [Parameter] public Guid? WorldId { get; set; }
    [Parameter] public EventCallback<WikiLinkAutocompleteItem> OnSuggestionSelected { get; set; }
    
    private ElementReference _autocompleteElement;
    
    protected override void OnInitialized()
    {
        // Subscribe to service events
        _autocompleteService.OnShow += HandleShow;
        _autocompleteService.OnHide += HandleHide;
        _autocompleteService.OnSuggestionsUpdated += HandleSuggestionsUpdated;
    }
    
    private void HandleShow()
    {
        StateHasChanged();
    }
    
    private void HandleHide()
    {
        StateHasChanged();
    }
    
    private void HandleSuggestionsUpdated()
    {
        StateHasChanged();
    }
    
    private string GetPositionStyle()
    {
        if (!_autocompleteService.IsVisible)
        {
            return "display: none;";
        }
        
        var (x, y) = _autocompleteService.Position;
        return $"left: {x}px; top: {y}px;";
    }
    
    private void HandleMouseEnter(int index)
    {
        // Update selected index when mouse hovers over an item
        // This syncs mouse and keyboard navigation
        _autocompleteService.SetSelectedIndex(index);
    }
    
    private async Task HandleSuggestionClick(WikiLinkAutocompleteItem suggestion)
    {
        // Notify parent component of selection
        await OnSuggestionSelected.InvokeAsync(suggestion);
        
        // Hide autocomplete
        _autocompleteService.Hide();
    }
    
    public void Dispose()
    {
        // Unsubscribe from events
        _autocompleteService.OnShow -= HandleShow;
        _autocompleteService.OnHide -= HandleHide;
        _autocompleteService.OnSuggestionsUpdated -= HandleSuggestionsUpdated;
    }
}
