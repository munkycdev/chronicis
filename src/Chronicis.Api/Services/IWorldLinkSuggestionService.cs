using Chronicis.Api.Models;
using Chronicis.Shared.DTOs;

namespace Chronicis.Api.Services;

public interface IWorldLinkSuggestionService
{
    Task<ServiceResult<List<LinkSuggestionDto>>> GetSuggestionsAsync(Guid worldId, string query, Guid userId);
}

