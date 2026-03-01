using Chronicis.Shared.DTOs;

namespace Chronicis.Api.Services;

public interface ISearchReadService
{
    Task<GlobalSearchResultsDto> SearchAsync(string query, Guid userId);
}

