using Chronicis.Shared.DTOs;
using Microsoft.JSInterop;
using MudBlazor;

namespace Chronicis.Client.Services;

/// <summary>
/// Facade implementation for AI summary operations.
/// Wraps AISummaryApi, Snackbar, and JSRuntime to simplify component dependencies.
/// </summary>
public class AISummaryFacade : IAISummaryFacade
{
    private readonly IAISummaryApiService _summaryApi;
    private readonly ISnackbar _snackbar;
    private readonly IJSRuntime _jsRuntime;

    public AISummaryFacade(
        IAISummaryApiService summaryApi,
        ISnackbar snackbar,
        IJSRuntime jsRuntime)
    {
        _summaryApi = summaryApi;
        _snackbar = snackbar;
        _jsRuntime = jsRuntime;
    }

    // Templates
    public Task<List<SummaryTemplateDto>> GetTemplatesAsync()
        => _summaryApi.GetTemplatesAsync();

    // Article-specific operations
    public Task<SummaryEstimateDto?> GetEstimateAsync(Guid articleId)
        => _summaryApi.GetEstimateAsync(articleId);

    public Task<SummaryGenerationDto?> GenerateSummaryAsync(Guid articleId, GenerateSummaryRequestDto? request = null)
        => _summaryApi.GenerateSummaryAsync(articleId, request);

    public Task<ArticleSummaryDto?> GetSummaryAsync(Guid articleId)
        => _summaryApi.GetSummaryAsync(articleId);

    public Task<bool> ClearSummaryAsync(Guid articleId)
        => _summaryApi.ClearSummaryAsync(articleId);

    // Entity-generic operations (Campaign, Arc)
    public Task<EntitySummaryDto?> GetEntitySummaryAsync(string entityType, Guid entityId)
        => _summaryApi.GetEntitySummaryAsync(entityType, entityId);

    public Task<SummaryEstimateDto?> GetEntityEstimateAsync(string entityType, Guid entityId)
        => _summaryApi.GetEntityEstimateAsync(entityType, entityId);

    public Task<SummaryGenerationDto?> GenerateEntitySummaryAsync(string entityType, Guid entityId, GenerateSummaryRequestDto? request = null)
        => _summaryApi.GenerateEntitySummaryAsync(entityType, entityId, request);

    public Task<bool> ClearEntitySummaryAsync(string entityType, Guid entityId)
        => _summaryApi.ClearEntitySummaryAsync(entityType, entityId);

    // User feedback operations (from ISnackbar)
    public void ShowSuccess(string message)
        => _snackbar.Add(message, Severity.Success);

    public void ShowError(string message)
        => _snackbar.Add(message, Severity.Error);

    public void ShowInfo(string message)
        => _snackbar.Add(message, Severity.Info);

    // Clipboard operations (from IJSRuntime)
    public async Task CopyToClipboardAsync(string text)
    {
        await _jsRuntime.InvokeVoidAsync("navigator.clipboard.writeText", text);
    }
}
