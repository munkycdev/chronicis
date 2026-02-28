using Chronicis.Shared.DTOs;

namespace Chronicis.Client.Services;

public class WikiLinkAutocompleteService : IWikiLinkAutocompleteService
{
    private readonly ILinkApiService _linkApiService;
    private readonly IExternalLinkApiService _externalLinkApiService;
    private readonly IResourceProviderApiService _resourceProviderApiService;
    private readonly ILogger<WikiLinkAutocompleteService> _logger;
    private readonly Dictionary<Guid, IReadOnlyList<WorldResourceProviderDto>> _worldProviderCache = new();

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
        IResourceProviderApiService resourceProviderApiService,
        ILogger<WikiLinkAutocompleteService> logger)
    {
        _linkApiService = linkApiService;
        _externalLinkApiService = externalLinkApiService;
        _resourceProviderApiService = resourceProviderApiService;
        _logger = logger;
    }

    public async Task ShowAsync(string query, double x, double y, Guid? worldId)
    {
        Position = (x, y);
        IsVisible = true;
        SelectedIndex = 0;

        var worldProviders = await GetWorldProvidersAsync(worldId);
        IsExternalQuery = TryParseExternalQuery(query, worldProviders, out var sourceKey, out var remainder);
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

    private async Task<IReadOnlyList<WorldResourceProviderDto>> GetWorldProvidersAsync(Guid? worldId)
    {
        if (!worldId.HasValue || worldId.Value == Guid.Empty)
        {
            return Array.Empty<WorldResourceProviderDto>();
        }

        if (_worldProviderCache.TryGetValue(worldId.Value, out var cached))
        {
            return cached;
        }

        var providers = await _resourceProviderApiService.GetWorldProvidersAsync(worldId.Value)
            ?? new List<WorldResourceProviderDto>();

        foreach (var provider in providers)
        {
            if (string.IsNullOrWhiteSpace(provider.LookupKey))
            {
                provider.LookupKey = provider.Provider.Code;
            }
        }

        _worldProviderCache[worldId.Value] = providers;
        return providers;
    }

    private static bool TryParseExternalQuery(
        string query,
        IReadOnlyList<WorldResourceProviderDto> worldProviders,
        out string sourceKey,
        out string remainder)
    {
        sourceKey = string.Empty;
        remainder = string.Empty;

        if (string.IsNullOrWhiteSpace(query))
            return false;

        var slashIndex = query.IndexOf('/');
        if (slashIndex < 0)
        {
            var lowerQuery = query.ToLowerInvariant();

            var exactMatch = worldProviders
                .Select(p => new
                {
                    ProviderCode = p.Provider.Code.ToLowerInvariant(),
                    LookupKey = NormalizeLookupKey(p.LookupKey, p.Provider.Code),
                })
                .FirstOrDefault(p =>
                    p.ProviderCode.Equals(lowerQuery, StringComparison.OrdinalIgnoreCase)
                    || p.LookupKey.Equals(lowerQuery, StringComparison.OrdinalIgnoreCase));

            if (exactMatch != null)
            {
                sourceKey = exactMatch.ProviderCode;
                remainder = string.Empty;
                return true;
            }

            var startsWithMatch = worldProviders
                .Select(p => new
                {
                    ProviderCode = p.Provider.Code.ToLowerInvariant(),
                    LookupKey = NormalizeLookupKey(p.LookupKey, p.Provider.Code),
                })
                .Where(p =>
                    lowerQuery.StartsWith(p.ProviderCode, StringComparison.OrdinalIgnoreCase)
                    || lowerQuery.StartsWith(p.LookupKey, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(p => p.LookupKey.Length)
                .ThenByDescending(p => p.ProviderCode.Length)
                .FirstOrDefault();

            if (startsWithMatch != null)
            {
                sourceKey = startsWithMatch.ProviderCode;
                remainder = string.Empty;
                return true;
            }

            return false;
        }

        var sourcePrefix = query.Substring(0, slashIndex).Trim().ToLowerInvariant();
        remainder = query.Substring(slashIndex + 1);

        var providerMatch = worldProviders
            .Select(p => new
            {
                ProviderCode = p.Provider.Code.ToLowerInvariant(),
                LookupKey = NormalizeLookupKey(p.LookupKey, p.Provider.Code),
            })
            .FirstOrDefault(p =>
                p.ProviderCode.Equals(sourcePrefix, StringComparison.OrdinalIgnoreCase)
                || p.LookupKey.Equals(sourcePrefix, StringComparison.OrdinalIgnoreCase));

        sourceKey = providerMatch?.ProviderCode ?? sourcePrefix;
        return true;
    }

    private static string NormalizeLookupKey(string? lookupKey, string providerCode)
    {
        if (string.IsNullOrWhiteSpace(lookupKey))
        {
            return providerCode.ToLowerInvariant();
        }

        return lookupKey.Trim().ToLowerInvariant();
    }
}
