using Chronicis.Shared.DTOs;

namespace Chronicis.Client.Services;

public interface IAISummaryApiService
{
    // Templates
    Task<List<SummaryTemplateDto>> GetTemplatesAsync();

    // Article-specific (existing)
    Task<SummaryEstimateDto?> GetEstimateAsync(Guid articleId);
    Task<SummaryGenerationDto?> GenerateSummaryAsync(Guid articleId, GenerateSummaryRequestDto? request = null);
    Task<ArticleSummaryDto?> GetSummaryAsync(Guid articleId);
    Task<SummaryPreviewDto?> GetSummaryPreviewAsync(Guid articleId);
    Task<bool> ClearSummaryAsync(Guid articleId);

    // Entity-generic (Campaign, Arc)
    Task<EntitySummaryDto?> GetEntitySummaryAsync(string entityType, Guid entityId);
    Task<SummaryEstimateDto?> GetEntityEstimateAsync(string entityType, Guid entityId);
    Task<SummaryGenerationDto?> GenerateEntitySummaryAsync(string entityType, Guid entityId, GenerateSummaryRequestDto? request = null);
    Task<bool> ClearEntitySummaryAsync(string entityType, Guid entityId);
}
