using Chronicis.Shared.DTOs;

namespace Chronicis.Api.Services;

public interface IAISummaryService
{
    Task<SummaryEstimateDto> EstimateCostAsync(Guid articleId);
    Task<SummaryGenerationDto> GenerateSummaryAsync(Guid articleId, int maxOutputTokens = 1500);
}
