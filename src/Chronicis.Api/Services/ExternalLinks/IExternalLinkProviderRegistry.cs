namespace Chronicis.Api.Services.ExternalLinks;

public interface IExternalLinkProviderRegistry
{
    IExternalLinkProvider? GetProvider(string key);
    IReadOnlyList<IExternalLinkProvider> GetAllProviders();
}
