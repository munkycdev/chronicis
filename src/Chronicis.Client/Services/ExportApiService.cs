using Microsoft.JSInterop;

namespace Chronicis.Client.Services;

/// <summary>
/// Service for export operations - triggers file downloads via the API
/// </summary>
public class ExportApiService : IExportApiService
{
    private readonly HttpClient _httpClient;
    private readonly IJSRuntime _jsRuntime;
    private readonly ILogger<ExportApiService> _logger;

    public ExportApiService(
        HttpClient httpClient,
        IJSRuntime jsRuntime,
        ILogger<ExportApiService> logger)
    {
        _httpClient = httpClient;
        _jsRuntime = jsRuntime;
        _logger = logger;
    }

    public async Task<bool> ExportWorldToMarkdownAsync(Guid worldId, string worldName)
    {
        try
        {
            _logger.LogInformation("Starting export for world {WorldId} ({WorldName})", worldId, worldName);

            var response = await _httpClient.GetAsync($"api/worlds/{worldId}/export");

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Export failed for world {WorldId}. Status: {StatusCode}", 
                    worldId, response.StatusCode);
                return false;
            }

            // Get the zip file content
            var content = await response.Content.ReadAsByteArrayAsync();

            // Build filename
            var safeWorldName = string.Join("_", worldName.Split(Path.GetInvalidFileNameChars()));
            if (safeWorldName.Length > 50) safeWorldName = safeWorldName.Substring(0, 50);
            var fileName = $"{safeWorldName}_export_{DateTime.UtcNow:yyyyMMdd_HHmmss}.zip";

            // Trigger browser download via JavaScript
            await _jsRuntime.InvokeVoidAsync("chronicisDownloadFile", fileName, "application/zip", content);

            _logger.LogInformation("Export download triggered for world {WorldId}. File: {FileName}, Size: {Size} bytes",
                worldId, fileName, content.Length);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting world {WorldId}", worldId);
            return false;
        }
    }
}
