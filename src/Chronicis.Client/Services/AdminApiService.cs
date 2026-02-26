using Chronicis.Shared.DTOs;

namespace Chronicis.Client.Services;

/// <summary>
/// HTTP client wrapper for the Admin API endpoints.
/// </summary>
public class AdminApiService : IAdminApiService
{
    private readonly HttpClient _http;
    private readonly ILogger<AdminApiService> _logger;

    public AdminApiService(HttpClient http, ILogger<AdminApiService> logger)
    {
        _http = http;
        _logger = logger;
    }

    /// <inheritdoc/>
    public Task<List<AdminWorldSummaryDto>> GetWorldSummariesAsync()
        => _http.GetListAsync<AdminWorldSummaryDto>("admin/worlds", _logger, "AdminWorldSummary");

    /// <inheritdoc/>
    public Task<bool> DeleteWorldAsync(Guid worldId)
        => _http.DeleteEntityAsync($"admin/worlds/{worldId}", _logger, "World");

    /// <inheritdoc/>
    public Task<List<TutorialMappingDto>> GetTutorialMappingsAsync()
        => _http.GetListAsync<TutorialMappingDto>(
            "sysadmin/tutorials",
            _logger,
            "tutorial mappings");

    /// <inheritdoc/>
    public Task<TutorialMappingDto?> CreateTutorialMappingAsync(TutorialMappingCreateDto dto)
        => _http.PostEntityAsync<TutorialMappingDto>(
            "sysadmin/tutorials",
            dto,
            _logger,
            "tutorial mapping");
}
