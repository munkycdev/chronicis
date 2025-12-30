using Chronicis.Shared.DTOs;

namespace Chronicis.Client.Services;

public interface IAISummaryApiService
{
    Task<List<SummaryTemplateDto>> GetTemplatesAsync();
    Task<SummaryEstimateDto?> GetEstimateAsync(Guid articleId);
    Task<SummaryGenerationDto?> GenerateSummaryAsync(Guid articleId, GenerateSummaryRequestDto? request = null);
    Task<ArticleSummaryDto?> GetSummaryAsync(Guid articleId);
    Task<bool> ClearSummaryAsync(Guid articleId);
}
