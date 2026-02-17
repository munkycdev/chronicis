using Chronicis.Shared.DTOs;

namespace Chronicis.Client.Services;

/// <summary>
/// Client service for interacting with wiki link APIs.
/// Uses HttpClientExtensions where applicable, with custom handling for wrapped responses.
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

    public async Task<List<LinkSuggestionDto>> GetSuggestionsAsync(Guid worldId, string query)
    {
        // Response is wrapped in LinkSuggestionsResponseDto
        var response = await _http.GetEntityAsync<LinkSuggestionsResponseDto>(
            $"worlds/{worldId}/link-suggestions?query={Uri.EscapeDataString(query)}",
            _logger,
            $"link suggestions for world {worldId}");

        return response?.Suggestions ?? new List<LinkSuggestionDto>();
    }

    public async Task<List<BacklinkDto>> GetBacklinksAsync(Guid articleId)
    {
        // Response is wrapped in BacklinksResponseDto
        var response = await _http.GetEntityAsync<BacklinksResponseDto>(
            $"articles/{articleId}/backlinks",
            _logger,
            $"backlinks for article {articleId}");

        return response?.Backlinks ?? new List<BacklinkDto>();
    }

    public async Task<List<BacklinkDto>> GetOutgoingLinksAsync(Guid articleId)
    {
        // Response is wrapped in BacklinksResponseDto
        var response = await _http.GetEntityAsync<BacklinksResponseDto>(
            $"articles/{articleId}/outgoing-links",
            _logger,
            $"outgoing links for article {articleId}");

        return response?.Backlinks ?? new List<BacklinkDto>();
    }

    public async Task<Dictionary<Guid, ResolvedLinkDto>> ResolveLinksAsync(List<Guid> articleIds)
    {
        if (!articleIds.Any())
        {
            return new Dictionary<Guid, ResolvedLinkDto>();
        }

        var request = new LinkResolutionRequestDto { ArticleIds = articleIds };

        // Response is wrapped in LinkResolutionResponseDto
        var response = await _http.PostEntityAsync<LinkResolutionResponseDto>(
            "articles/resolve-links",
            request,
            _logger,
            $"resolution for {articleIds.Count} links");

        return response?.Articles ?? new Dictionary<Guid, ResolvedLinkDto>();
    }

    public async Task<AutoLinkResponseDto?> AutoLinkAsync(Guid articleId, string body)
    {
        var request = new AutoLinkRequestDto { Body = body };

        return await _http.PostEntityAsync<AutoLinkResponseDto>(
            $"articles/{articleId}/auto-link",
            request,
            _logger,
            $"auto-link for article {articleId}");
    }
}
