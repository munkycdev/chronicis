using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Chronicis.Api.Services.ExternalLinks;

public class ExternalLinkSuggestionService
{
    private readonly IExternalLinkProviderRegistry _registry;
    private readonly IMemoryCache _cache;
    private readonly ILogger<ExternalLinkSuggestionService> _logger;

    public ExternalLinkSuggestionService(
        IExternalLinkProviderRegistry registry,
        IMemoryCache cache,
        ILogger<ExternalLinkSuggestionService> logger)
    {
        _registry = registry;
        _cache = cache;
        _logger = logger;
    }

    public async Task<IReadOnlyList<ExternalLinkSuggestion>> GetSuggestionsAsync(
        string source,
        string query,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(query))
        {
            return Array.Empty<ExternalLinkSuggestion>();
        }

        var cacheKey = $"external-links:suggestions:{source}:{query}".ToLowerInvariant();
        if (_cache.TryGetValue<IReadOnlyList<ExternalLinkSuggestion>>(cacheKey, out var cached) && cached != null)
        {
            return cached;
        }

        var provider = _registry.GetProvider(source);
        if (provider == null)
        {
            return Array.Empty<ExternalLinkSuggestion>();
        }

        IReadOnlyList<ExternalLinkSuggestion> suggestions;
        try
        {
            suggestions = await provider.SearchAsync(query, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "External link provider {Source} failed to search for query {Query}", source, query);
            suggestions = Array.Empty<ExternalLinkSuggestion>();
        }

        _cache.Set(cacheKey, suggestions, TimeSpan.FromMinutes(2));
        return suggestions;
    }
}
