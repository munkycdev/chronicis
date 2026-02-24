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
}
