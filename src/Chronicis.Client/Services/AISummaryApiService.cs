using System.Net.Http.Json;
using Chronicis.Shared.DTOs;

namespace Chronicis.Client.Services;

public interface IAISummaryApiService
{
    Task<SummaryEstimateDto?> GetEstimateAsync(int articleId);
    Task<SummaryGenerationDto?> GenerateSummaryAsync(int articleId, int maxOutputTokens = 1500);
    Task<ArticleSummaryDto?> GetSummaryAsync(int articleId);
    Task<bool> ClearSummaryAsync(int articleId);
}

public class AISummaryApiService : IAISummaryApiService
{
    private readonly HttpClient _http;

    public AISummaryApiService(HttpClient http)
    {
        _http = http;
    }

    public async Task<SummaryEstimateDto?> GetEstimateAsync(int articleId)
    {
        try
        {
            return await _http.GetFromJsonAsync<SummaryEstimateDto>($"/api/articles/{articleId}/summary/estimate");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting summary estimate: {ex.Message}");
            return null;
        }
    }

    public async Task<SummaryGenerationDto?> GenerateSummaryAsync(int articleId, int maxOutputTokens = 1500)
    {
        try
        {
            var request = new GenerateSummaryRequestDto
            {
                ArticleId = articleId,
                MaxOutputTokens = maxOutputTokens
            };

            var response = await _http.PostAsJsonAsync($"/api/articles/{articleId}/summary/generate", request);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<SummaryGenerationDto>();
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Error generating summary: {response.StatusCode} - {errorContent}");
                return null;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error generating summary: {ex.Message}");
            return null;
        }
    }

    public async Task<ArticleSummaryDto?> GetSummaryAsync(int articleId)
    {
        try
        {
            return await _http.GetFromJsonAsync<ArticleSummaryDto>($"/api/articles/{articleId}/summary");
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting summary: {ex.Message}");
            return null;
        }
    }

    public async Task<bool> ClearSummaryAsync(int articleId)
    {
        try
        {
            var response = await _http.DeleteAsync($"/api/articles/{articleId}/summary");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error clearing summary: {ex.Message}");
            return false;
        }
    }
}
