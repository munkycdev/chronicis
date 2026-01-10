namespace Chronicis.Api.Services.ExternalLinks;

public interface IExternalLinkProvider
{
    string Key { get; }

    Task<IReadOnlyList<ExternalLinkSuggestion>> SearchAsync(string query, CancellationToken ct);

    Task<ExternalLinkContent> GetContentAsync(string id, CancellationToken ct);
}
