using System.Net.Http.Json;
using Chronicis.Shared.DTOs;

namespace Chronicis.Client.Services;

/// <summary>
/// Service for interacting with the Article External Links API.
/// </summary>
public class ArticleExternalLinkApiService : IArticleExternalLinkApiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ArticleExternalLinkApiService> _logger;

    public ArticleExternalLinkApiService(
        HttpClient httpClient,
        ILogger<ArticleExternalLinkApiService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<List<ArticleExternalLinkDto>> GetExternalLinksAsync(Guid articleId)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<List<ArticleExternalLinkDto>>(
                $"articles/{articleId}/external-links");

            return response ?? new List<ArticleExternalLinkDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving external links for article {ArticleId}", articleId);
            return new List<ArticleExternalLinkDto>();
        }
    }
}
