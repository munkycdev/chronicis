using Chronicis.Shared.DTOs;

namespace Chronicis.Client.Services;

/// <summary>
/// Facade service for AI summary operations.
/// Simplifies component dependencies by wrapping AISummaryApi, Snackbar, and JSRuntime services.
/// </summary>
public interface IAISummaryFacade
{
    // Templates
    Task<List<SummaryTemplateDto>> GetTemplatesAsync();

    // Article-specific operations
    Task<SummaryEstimateDto?> GetEstimateAsync(Guid articleId);
    Task<SummaryGenerationDto?> GenerateSummaryAsync(Guid articleId, GenerateSummaryRequestDto? request = null);
    Task<ArticleSummaryDto?> GetSummaryAsync(Guid articleId);
    Task<bool> ClearSummaryAsync(Guid articleId);

    // Entity-generic operations (Campaign, Arc)
    Task<EntitySummaryDto?> GetEntitySummaryAsync(string entityType, Guid entityId);
    Task<SummaryEstimateDto?> GetEntityEstimateAsync(string entityType, Guid entityId);
    Task<SummaryGenerationDto?> GenerateEntitySummaryAsync(string entityType, Guid entityId, GenerateSummaryRequestDto? request = null);
    Task<bool> ClearEntitySummaryAsync(string entityType, Guid entityId);

    // User feedback operations (from ISnackbar)
    void ShowSuccess(string message);
    void ShowError(string message);
    void ShowInfo(string message);

    // Clipboard operations (from IJSRuntime)
    Task CopyToClipboardAsync(string text);
}
