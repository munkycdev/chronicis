using System.Net.Http.Json;
using Chronicis.Shared.DTOs;
using Microsoft.Extensions.Logging;

namespace Chronicis.Client.Services;

public class ExternalLinkApiService : IExternalLinkApiService
{
    private readonly HttpClient _http;
    private readonly ILogger<ExternalLinkApiService> _logger;

    public ExternalLinkApiService(HttpClient http, ILogger<ExternalLinkApiService> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<List<ExternalLinkSuggestionDto>> GetSuggestionsAsync(
        string source,
        string query,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            return new List<ExternalLinkSuggestionDto>();
        }

        var url =
            $"external-links/suggestions?source={Uri.EscapeDataString(source)}&query={Uri.EscapeDataString(query ?? string.Empty)}";

        try
        {
            var results = await _http.GetFromJsonAsync<List<ExternalLinkSuggestionDto>>(url, ct);
            return results ?? new List<ExternalLinkSuggestionDto>();
        }
        catch (OperationCanceledException)
        {
            return new List<ExternalLinkSuggestionDto>();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch external link suggestions for source {Source}", source);
            return new List<ExternalLinkSuggestionDto>();
        }
    }

    public async Task<ExternalLinkContentDto?> GetContentAsync(
        string source,
        string id,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        var url =
            $"external-links/content?source={Uri.EscapeDataString(source)}&id={Uri.EscapeDataString(id)}";

        try
        {
            return await _http.GetFromJsonAsync<ExternalLinkContentDto>(url, ct);
        }
        catch (OperationCanceledException)
        {
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch external link content for source {Source} and id {Id}", source, id);
            return null;
        }
    }
}
