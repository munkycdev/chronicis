using Chronicis.Shared.DTOs;

namespace Chronicis.Api.Services;

public interface ITutorialService
{
    Task<TutorialDto?> ResolveAsync(string pageType);

    Task<List<TutorialMappingDto>> GetMappingsAsync();

    Task<TutorialMappingDto> CreateMappingAsync(TutorialMappingCreateDto dto);

    Task<TutorialMappingDto?> UpdateMappingAsync(Guid id, TutorialMappingUpdateDto dto);

    Task<bool> DeleteMappingAsync(Guid id);
}
