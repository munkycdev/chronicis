using Chronicis.Shared.DTOs;

namespace Chronicis.Client.Services;

public interface IAISummaryApiService
{
    Task<SummaryEstimateDto?> GetEstimateAsync(Guid articleId);
    Task<SummaryGenerationDto?> GenerateSummaryAsync(Guid articleId, int maxOutputTokens = 1500);
    Task<ArticleSummaryDto?> GetSummaryAsync(Guid articleId);
    Task<bool> ClearSummaryAsync(Guid articleId);
}
