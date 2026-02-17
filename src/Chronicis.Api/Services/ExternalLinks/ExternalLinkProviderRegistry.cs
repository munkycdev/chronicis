namespace Chronicis.Api.Services.ExternalLinks;

public class ExternalLinkProviderRegistry : IExternalLinkProviderRegistry
{
    private readonly Dictionary<string, IExternalLinkProvider> _providers;

    public ExternalLinkProviderRegistry(IEnumerable<IExternalLinkProvider> providers)
    {
        _providers = providers
            .GroupBy(p => p.Key, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
    }

    public IExternalLinkProvider? GetProvider(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return null;
        }

        return _providers.TryGetValue(key, out var provider) ? provider : null;
    }

    public IReadOnlyList<IExternalLinkProvider> GetAllProviders()
    {
        return _providers.Values.ToList();
    }
}
