using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Chronicis.Api.Services.ExternalLinks;

public class ExternalLinkContentService
{
    private readonly IExternalLinkProviderRegistry _registry;
    private readonly IMemoryCache _cache;
    private readonly ILogger<ExternalLinkContentService> _logger;

    public ExternalLinkContentService(
        IExternalLinkProviderRegistry registry,
        IMemoryCache cache,
        ILogger<ExternalLinkContentService> logger)
    {
        _registry = registry;
        _cache = cache;
        _logger = logger;
    }

    public async Task<ExternalLinkContent?> GetContentAsync(
        string source,
        string id,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        var cacheKey = $"external-links:content:{source}:{id}".ToLowerInvariant();
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
            _logger.LogWarning(ex, "External link provider {Source} failed to get content for {Id}", source, id);
            return null;
        }

        _cache.Set(cacheKey, content, TimeSpan.FromMinutes(5));
        return content;
    }
}
