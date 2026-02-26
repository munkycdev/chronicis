using System.Net.Http.Json;

namespace Chronicis.Client.Services;

/// <summary>
/// Fetches build version information from wwwroot/version.json (stamped by CI).
/// The result is cached after the first successful fetch.
/// </summary>
public class VersionService : IVersionService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<VersionService> _logger;
    private BuildInfo? _cached;

    public VersionService(HttpClient httpClient, ILogger<VersionService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<BuildInfo> GetBuildInfoAsync()
    {
        if (_cached is not null)
            return _cached;

        try
        {
            var info = await _httpClient.GetFromJsonAsync<BuildInfo>("version.json");
            _cached = info ?? Fallback();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch version.json — using fallback version info");
            _cached = Fallback();
        }

        return _cached;
    }

    private static BuildInfo Fallback() => new()
    {
        Version = "0.0.0",
        BuildNumber = "0",
        Sha = "unknown",
        BuildDate = string.Empty
    };
}
