using Chronicis.Shared.DTOs;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;

namespace Chronicis.Client.Services;

/// <summary>
/// Client service for interacting with wiki link APIs.
/// </summary>
public class LinkApiService : ILinkApiService
{
    private readonly HttpClient _http;
    private readonly ILogger<LinkApiService> _logger;

    public LinkApiService(HttpClient http, ILogger<LinkApiService> logger)
    {
        _http = http;
        _logger = logger;
    }

    /// <summary>
    /// Gets link suggestions for autocomplete based on a search query.
    /// </summary>
    public async Task<List<LinkSuggestionDto>> GetSuggestionsAsync(Guid worldId, string query)
    {
        try
        {
            var response = await _http.GetFromJsonAsync<LinkSuggestionsResponseDto>(
                $"api/worlds/{worldId}/link-suggestions?query={Uri.EscapeDataString(query)}");

            return response?.Suggestions ?? new List<LinkSuggestionDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting link suggestions for world {WorldId}", worldId);
            return new List<LinkSuggestionDto>();
        }
    }

    /// <summary>
    /// Gets all articles that link to the specified article (backlinks).
    /// </summary>
    public async Task<List<BacklinkDto>> GetBacklinksAsync(Guid articleId)
    {
        try
        {
            var response = await _http.GetFromJsonAsync<BacklinksResponseDto>(
                $"api/articles/{articleId}/backlinks");

            return response?.Backlinks ?? new List<BacklinkDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting backlinks for article {ArticleId}", articleId);
            return new List<BacklinkDto>();
        }
    }

    /// <summary>
    /// Gets all articles that this article links to (outgoing links).
    /// </summary>
    public async Task<List<BacklinkDto>> GetOutgoingLinksAsync(Guid articleId)
    {
        try
        {
            var response = await _http.GetFromJsonAsync<BacklinksResponseDto>(
                $"api/articles/{articleId}/outgoing-links");

            return response?.Backlinks ?? new List<BacklinkDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting outgoing links for article {ArticleId}", articleId);
            return new List<BacklinkDto>();
        }
    }

    /// <summary>
    /// Resolves multiple article IDs to check if they exist (for broken link detection).
    /// </summary>
    public async Task<Dictionary<Guid, ResolvedLinkDto>> ResolveLinksAsync(List<Guid> articleIds)
    {
        try
        {
            if (!articleIds.Any())
            {
                return new Dictionary<Guid, ResolvedLinkDto>();
            }

            var request = new LinkResolutionRequestDto
            {
                ArticleIds = articleIds
            };

            var response = await _http.PostAsJsonAsync("api/articles/resolve-links", request);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<LinkResolutionResponseDto>();
            return result?.Articles ?? new Dictionary<Guid, ResolvedLinkDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resolving {Count} article links", articleIds.Count);
            return new Dictionary<Guid, ResolvedLinkDto>();
        }
    }

    /// <summary>
    /// Scans article content and returns modified content with wiki links auto-inserted.
    /// </summary>
    public async Task<AutoLinkResponseDto?> AutoLinkAsync(Guid articleId, string body)
    {
        try
        {
            var request = new AutoLinkRequestDto { Body = body };
            var response = await _http.PostAsJsonAsync($"api/articles/{articleId}/auto-link", request);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Auto-link request failed with status {Status}", response.StatusCode);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<AutoLinkResponseDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error auto-linking article {ArticleId}", articleId);
            return null;
        }
    }
}
