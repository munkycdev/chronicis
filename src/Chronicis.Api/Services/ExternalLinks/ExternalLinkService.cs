using Chronicis.Api.Repositories;
using Chronicis.Shared.Extensions;
using Microsoft.Extensions.Caching.Memory;

namespace Chronicis.Api.Services.ExternalLinks;

/// <summary>
/// Consolidated service for external link operations.
/// Resolves providers via <see cref="IExternalLinkProviderRegistry"/>,
/// centralizes caching patterns, and standardizes exception handling.
/// </summary>
public class ExternalLinkService : IExternalLinkService
{
    private static readonly TimeSpan SuggestionCacheDuration = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan ContentCacheDuration = TimeSpan.FromMinutes(5);

    private readonly IExternalLinkProviderRegistry _registry;
    private readonly IResourceProviderRepository _resourceProviderRepository;
    private readonly IMemoryCache _cache;
    private readonly ILogger<ExternalLinkService> _logger;

    public ExternalLinkService(
        IExternalLinkProviderRegistry registry,
        IResourceProviderRepository resourceProviderRepository,
        IMemoryCache cache,
        ILogger<ExternalLinkService> logger)
    {
        _registry = registry;
        _resourceProviderRepository = resourceProviderRepository;
        _cache = cache;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ExternalLinkSuggestion>> GetSuggestionsAsync(
        Guid? worldId,
        string source,
        string query,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            return Array.Empty<ExternalLinkSuggestion>();
        }

        var resolvedSource = source;

        // Check world-level provider enablement and resolve lookup-key aliases
        if (worldId.HasValue)
        {
            var worldProviders = await _resourceProviderRepository.GetWorldProvidersAsync(worldId.Value);
            var enabledProvider = worldProviders.FirstOrDefault(p =>
                p.IsEnabled
                && (p.Provider.Code.Equals(source, StringComparison.OrdinalIgnoreCase)
                    || p.LookupKey.Equals(source, StringComparison.OrdinalIgnoreCase)));

            if (enabledProvider == default)
            {
                _logger.LogDebugSanitized(
                    "Provider {Source} is not enabled for world {WorldId}", source, worldId);
                return Array.Empty<ExternalLinkSuggestion>();
            }

            resolvedSource = enabledProvider.Provider.Code;
        }

        query ??= string.Empty;

        var cacheKey = BuildSuggestionCacheKey(resolvedSource, query);
        if (_cache.TryGetValue<IReadOnlyList<ExternalLinkSuggestion>>(cacheKey, out var cached) && cached != null)
        {
            return cached;
        }

        var provider = _registry.GetProvider(resolvedSource);
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
            _logger.LogErrorSanitized(
                ex,
                "External link provider {Source} failed to search for query {Query}",
                resolvedSource, query);
            return Array.Empty<ExternalLinkSuggestion>();
        }

        _cache.Set(cacheKey, suggestions, SuggestionCacheDuration);
        return suggestions;
    }

    /// <inheritdoc />
    public async Task<ExternalLinkContent?> GetContentAsync(
        string source,
        string id,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        var cacheKey = BuildContentCacheKey(source, id);
        if (_cache.TryGetValue<ExternalLinkContent>(cacheKey, out var cached) && cached != null)
        {
            return cached;
        }

        var provider = _registry.GetProvider(source);
        if (provider == null)
        {
            return null;
        }

        ExternalLinkContent content;
        try
        {
            content = await provider.GetContentAsync(id, ct);
        }
        catch (Exception ex)
        {
            _logger.LogErrorSanitized(
                ex,
                "External link provider {Source} failed to get content for {Id}",
                source, id);
            return null;
        }

        _cache.Set(cacheKey, content, ContentCacheDuration);
        return content;
    }

    /// <inheritdoc />
    public bool TryValidateSource(string source, out string error)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            error = "Source is required.";
            return false;
        }

        var provider = _registry.GetProvider(source);
        if (provider == null)
        {
            var available = _registry
                .GetAllProviders()
                .Select(p => p.Key)
                .OrderBy(key => key, StringComparer.OrdinalIgnoreCase)
                .ToList();

            error = available.Count == 0
                ? $"Unknown external link source '{source}'."
                : $"Unknown external link source '{source}'. Available sources: {string.Join(", ", available)}.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    /// <inheritdoc />
    public bool TryValidateId(string source, string id, out string error)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            error = "Id is required.";
            return false;
        }

        if (Uri.TryCreate(id, UriKind.Absolute, out _))
        {
            error = "External link id must be a relative path.";
            return false;
        }

        if (!Uri.TryCreate(id, UriKind.Relative, out _))
        {
            error = "External link id must be a relative path.";
            return false;
        }

        if (source.Equals("srd", StringComparison.OrdinalIgnoreCase)
            && !id.StartsWith("/api/", StringComparison.OrdinalIgnoreCase))
        {
            error = "SRD ids must start with /api/.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    // -- Cache key helpers --

    internal static string BuildSuggestionCacheKey(string source, string query)
        => $"external-links:suggestions:{source}:{query}".ToLowerInvariant();

    internal static string BuildContentCacheKey(string source, string id)
        => $"external-links:content:{source}:{id}".ToLowerInvariant();
}
