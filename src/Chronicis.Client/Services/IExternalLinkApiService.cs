using Chronicis.Shared.DTOs;

namespace Chronicis.Client.Services;

public interface IExternalLinkApiService
{
    Task<List<ExternalLinkSuggestionDto>> GetSuggestionsAsync(
        string source,
        string query,
        CancellationToken ct);

    Task<ExternalLinkContentDto?> GetContentAsync(
        string source,
        string id,
        CancellationToken ct);
}
