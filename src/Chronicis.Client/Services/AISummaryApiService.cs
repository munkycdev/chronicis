using Chronicis.Shared.DTOs;

namespace Chronicis.Client.Services;

/// <summary>
/// Service for AI Summary API operations.
/// Uses HttpClientExtensions for consistent error handling and logging.
/// </summary>
public class AISummaryApiService : IAISummaryApiService
{
    private readonly HttpClient _http;
    private readonly ILogger<AISummaryApiService> _logger;

    public AISummaryApiService(HttpClient http, ILogger<AISummaryApiService> logger)
    {
        _http = http;
        _logger = logger;
    }

    #region Templates

    public async Task<List<SummaryTemplateDto>> GetTemplatesAsync()
    {
        var result = await _http.GetEntityAsync<List<SummaryTemplateDto>>(
            "api/summary/templates",
            _logger,
            "summary templates");
        return result ?? new List<SummaryTemplateDto>();
    }

    #endregion

    #region Article Summary (existing)

    public async Task<SummaryEstimateDto?> GetEstimateAsync(Guid articleId)
    {
        return await _http.GetEntityAsync<SummaryEstimateDto>(
            $"api/articles/{articleId}/summary/estimate",
            _logger,
            $"summary estimate for article {articleId}");
    }

    public async Task<SummaryGenerationDto?> GenerateSummaryAsync(Guid articleId, GenerateSummaryRequestDto? request = null)
    {
        request ??= new GenerateSummaryRequestDto();

        return await _http.PostEntityAsync<SummaryGenerationDto>(
            $"api/articles/{articleId}/summary/generate",
            request,
            _logger,
            $"summary generation for article {articleId}");
    }

    public async Task<ArticleSummaryDto?> GetSummaryAsync(Guid articleId)
    {
        return await _http.GetEntityAsync<ArticleSummaryDto>(
            $"api/articles/{articleId}/summary",
            _logger,
            $"summary for article {articleId}");
    }

    public async Task<bool> ClearSummaryAsync(Guid articleId)
    {
        return await _http.DeleteEntityAsync(
            $"api/articles/{articleId}/summary",
            _logger,
            $"summary for article {articleId}");
    }

    #endregion

    #region Entity Summary (Campaign, Arc)

    public async Task<EntitySummaryDto?> GetEntitySummaryAsync(string entityType, Guid entityId)
    {
        var route = GetEntityRoute(entityType);
        return await _http.GetEntityAsync<EntitySummaryDto>(
            $"api/{route}/{entityId}/summary",
            _logger,
            $"summary for {entityType} {entityId}");
    }

    public async Task<SummaryEstimateDto?> GetEntityEstimateAsync(string entityType, Guid entityId)
    {
        var route = GetEntityRoute(entityType);
        return await _http.GetEntityAsync<SummaryEstimateDto>(
            $"api/{route}/{entityId}/summary/estimate",
            _logger,
            $"summary estimate for {entityType} {entityId}");
    }

    public async Task<SummaryGenerationDto?> GenerateEntitySummaryAsync(string entityType, Guid entityId, GenerateSummaryRequestDto? request = null)
    {
        request ??= new GenerateSummaryRequestDto();
        var route = GetEntityRoute(entityType);

        return await _http.PostEntityAsync<SummaryGenerationDto>(
            $"api/{route}/{entityId}/summary/generate",
            request,
            _logger,
            $"summary generation for {entityType} {entityId}");
    }

    public async Task<bool> ClearEntitySummaryAsync(string entityType, Guid entityId)
    {
        var route = GetEntityRoute(entityType);
        return await _http.DeleteEntityAsync(
            $"api/{route}/{entityId}/summary",
            _logger,
            $"summary for {entityType} {entityId}");
    }

    private static string GetEntityRoute(string entityType)
    {
        return entityType.ToLowerInvariant() switch
        {
            "campaign" => "campaigns",
            "arc" => "arcs",
            _ => throw new ArgumentException($"Unknown entity type: {entityType}")
        };
    }

    #endregion
}
