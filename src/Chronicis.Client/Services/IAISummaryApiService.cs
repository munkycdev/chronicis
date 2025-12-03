using Chronicis.Shared.DTOs;

namespace Chronicis.Client.Services;

public interface IAISummaryApiService
{
    Task<SummaryEstimateDto?> GetEstimateAsync(int articleId);
    Task<SummaryGenerationDto?> GenerateSummaryAsync(int articleId, int maxOutputTokens = 1500);
    Task<ArticleSummaryDto?> GetSummaryAsync(int articleId);
    Task<bool> ClearSummaryAsync(int articleId);
}
