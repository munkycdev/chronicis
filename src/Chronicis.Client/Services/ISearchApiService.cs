using Chronicis.Shared.DTOs;

namespace Chronicis.Client.Services;

public interface ISearchApiService
{
    Task<GlobalSearchResultsDto?> SearchContentAsync(string query);
}
