using Chronicis.Client.Services;
using Chronicis.Shared.DTOs;
using Microsoft.Extensions.Logging;

namespace Chronicis.Client.Services;

public class WikiLinkAutocompleteService : IWikiLinkAutocompleteService
{
    private readonly ILinkApiService _linkApiService;
    private readonly IExternalLinkApiService _externalLinkApiService;
    private readonly ILogger<WikiLinkAutocompleteService> _logger;
    
    public event Action? OnShow;
    public event Action? OnHide;
    public event Action? OnSuggestionsUpdated;
    
    public (double X, double Y) Position { get; private set; }
    public bool IsVisible { get; private set; }
    public string Query { get; private set; } = string.Empty;
    public bool IsExternalQuery { get; private set; }
    public string? ExternalSourceKey { get; private set; }
    public List<WikiLinkAutocompleteItem> Suggestions { get; private set; } = new();
    public int SelectedIndex { get; private set; }
    public bool IsLoading { get; private set; }
    
    public WikiLinkAutocompleteService(
        ILinkApiService linkApiService,
        IExternalLinkApiService externalLinkApiService,
        ILogger<WikiLinkAutocompleteService> logger)
    {
        _linkApiService = linkApiService;
        _externalLinkApiService = externalLinkApiService;
        _logger = logger;
    }
    
    public async Task ShowAsync(string query, double x, double y, Guid? worldId)
    {
        Position = (x, y);
        IsVisible = true;
        SelectedIndex = 0;
        
        IsExternalQuery = TryParseExternalQuery(query, out var sourceKey, out var remainder);
        ExternalSourceKey = IsExternalQuery ? sourceKey : null;
        Query = IsExternalQuery ? remainder : query;
        
        OnShow?.Invoke();
        
        // For external queries: no minimum length (show categories)
        // For internal queries: require 3 characters minimum
        var minLength = IsExternalQuery ? 0 : 3;
        
        if (Query.Length < minLength)
        {
            Suggestions = new();
            OnSuggestionsUpdated?.Invoke();
            return;
        }
        
        IsLoading = true;
        OnSuggestionsUpdated?.Invoke();
        
        try
        {
            if (IsExternalQuery)
            {
                var externalSuggestions = await _externalLinkApiService.GetSuggestionsAsync(
                    worldId ?? Guid.Empty,
                    ExternalSourceKey ?? string.Empty,
                    Query,
                    CancellationToken.None);
                    
                Suggestions = externalSuggestions
                    .Select(WikiLinkAutocompleteItem.FromExternal)
                    .ToList();
            }
            else
            {
                var internalSuggestions = await _linkApiService.GetSuggestionsAsync(
                    worldId ?? Guid.Empty,
                    Query);
                    
                Suggestions = internalSuggestions
                    .Select(WikiLinkAutocompleteItem.FromInternal)
                    .ToList();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting autocomplete suggestions for query: {Query}", query);
            Suggestions = new();
        }
        finally
        {
            IsLoading = false;
            OnSuggestionsUpdated?.Invoke();
        }
    }
    
    public void Hide()
    {
        IsVisible = false;
        Suggestions = new();
        IsExternalQuery = false;
        ExternalSourceKey = null;
        Query = string.Empty;
        SelectedIndex = 0;
        
        OnHide?.Invoke();
    }
    
    public void SelectNext()
    {
        if (Suggestions.Any())
        {
            SelectedIndex = (SelectedIndex + 1) % Suggestions.Count;
            OnSuggestionsUpdated?.Invoke();
        }
    }
    
    public void SelectPrevious()
    {
        if (Suggestions.Any())
        {
            SelectedIndex = (SelectedIndex - 1 + Suggestions.Count) % Suggestions.Count;
            OnSuggestionsUpdated?.Invoke();
        }
    }
    
    public void SetSelectedIndex(int index)
    {
        if (index >= 0 && index < Suggestions.Count)
        {
            SelectedIndex = index;
            OnSuggestionsUpdated?.Invoke();
        }
    }
    
    public WikiLinkAutocompleteItem? GetSelectedSuggestion()
    {
        if (Suggestions.Any() && SelectedIndex >= 0 && SelectedIndex < Suggestions.Count)
        {
            return Suggestions[SelectedIndex];
        }
        return null;
    }
    
    private static bool TryParseExternalQuery(string query, out string sourceKey, out string remainder)
    {
        sourceKey = string.Empty;
        remainder = string.Empty;
        
        if (string.IsNullOrWhiteSpace(query))
            return false;
        
        var slashIndex = query.IndexOf('/');
        if (slashIndex < 0)
        {
            // No slash found - check if it could be a source key prefix
            var lowerQuery = query.ToLowerInvariant();
            if (lowerQuery.StartsWith("srd") || 
                lowerQuery.StartsWith("open5e") ||
                lowerQuery.StartsWith("ros"))
            {
                sourceKey = lowerQuery;
                remainder = string.Empty;
                return true;
            }
            return false;
        }
        
        sourceKey = query.Substring(0, slashIndex).ToLowerInvariant();
        remainder = query.Substring(slashIndex + 1);
        return true;
    }
}
