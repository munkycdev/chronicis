using System.Net.Http.Json;
using Chronicis.Shared.DTOs;
using Chronicis.Shared.DTOs.Sessions;

namespace Chronicis.Client.Services;

/// <summary>
/// Service for Session API operations used by tree/navigation.
/// </summary>
public class SessionApiService : ISessionApiService
{
    private readonly HttpClient _http;
    private readonly ILogger<SessionApiService> _logger;

    public SessionApiService(HttpClient http, ILogger<SessionApiService> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<List<SessionTreeDto>> GetSessionsByArcAsync(Guid arcId)
    {
        return await _http.GetListAsync<SessionTreeDto>(
            $"arcs/{arcId}/sessions",
            _logger,
            $"sessions for arc {arcId}");
    }

    public async Task<SessionDto?> GetSessionAsync(Guid sessionId)
    {
        return await _http.GetEntityAsync<SessionDto>(
            $"sessions/{sessionId}",
            _logger,
            $"session {sessionId}");
    }

    public async Task<SessionDto?> UpdateSessionNotesAsync(Guid sessionId, SessionUpdateDto dto)
    {
        try
        {
            var response = await _http.PatchAsJsonAsync($"sessions/{sessionId}", dto);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<SessionDto>();
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogWarning("Failed to update session notes for {SessionId}: {StatusCode} - {Error}",
                sessionId, response.StatusCode, errorContent);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating session notes for {SessionId}", sessionId);
            return null;
        }
    }

    public async Task<SummaryGenerationDto?> GenerateAiSummaryAsync(Guid sessionId)
    {
        try
        {
            var response = await _http.PostAsJsonAsync($"sessions/{sessionId}/ai-summary/generate", new { });
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<SummaryGenerationDto>();
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogWarning("Failed to generate session AI summary for {SessionId}: {StatusCode} - {Error}",
                sessionId, response.StatusCode, errorContent);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating session AI summary for {SessionId}", sessionId);
            return null;
        }
    }
}
