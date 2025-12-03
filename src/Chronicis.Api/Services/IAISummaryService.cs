using Chronicis.Shared.DTOs;

namespace Chronicis.Api.Services;

public interface IAISummaryService
{
    Task<SummaryEstimateDto> EstimateCostAsync(int articleId);
    Task<SummaryGenerationDto> GenerateSummaryAsync(int articleId, int maxOutputTokens = 1500);
}