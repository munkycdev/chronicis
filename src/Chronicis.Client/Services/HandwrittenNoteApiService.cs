using System.Net.Http.Json;
using Chronicis.Shared.DTOs;

namespace Chronicis.Client.Services;

/// <summary>
/// Service for handwritten note API operations.
/// Uses HttpClientExtensions for consistent error handling and logging.
/// </summary>
public class HandwrittenNoteApiService : IHandwrittenNoteApiService
{
    private readonly HttpClient _http;
    private readonly ILogger<HandwrittenNoteApiService> _logger;

    public HandwrittenNoteApiService(HttpClient http, ILogger<HandwrittenNoteApiService> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<HandwrittenNoteSaveResultDto?> SaveHandwrittenNoteAsync(Guid articleId, byte[] imageBytes)
    {
        return await _http.PostEntityAsync<HandwrittenNoteSaveResultDto>(
            $"articles/{articleId}/handwritten-note",
            new { ImageBytes = imageBytes },
            _logger,
            $"handwritten note for article {articleId}");
    }

    public async Task<HandwrittenNoteTranscribeResultDto?> TranscribeHandwrittenNoteAsync(Guid articleId, byte[] imageBytes)
    {
        return await _http.PostEntityAsync<HandwrittenNoteTranscribeResultDto>(
            $"articles/{articleId}/handwritten-note/transcribe",
            new { ImageBytes = imageBytes },
            _logger,
            $"handwritten note transcription for article {articleId}");
    }

    public async Task<string?> GetHandwrittenNoteUrlAsync(Guid articleId)
    {
        try
        {
            var response = await _http.GetAsync($"articles/{articleId}/handwritten-note");

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound ||
                response.StatusCode == System.Net.HttpStatusCode.NoContent)
            {
                return null;
            }

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<HandwrittenNoteDownloadUrlDto>();
                return result?.DownloadUrl;
            }

            _logger.LogWarning("Failed to get handwritten note URL for article {ArticleId}: {StatusCode}",
                articleId, response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting handwritten note URL for article {ArticleId}", articleId);
            return null;
        }
    }

    public async Task<HandwrittenNoteTranscribeResultDto?> TranscribeExistingAsync(Guid articleId)
    {
        return await _http.PostEntityAsync<HandwrittenNoteTranscribeResultDto>(
            $"articles/{articleId}/handwritten-note/transcribe-existing",
            new { },
            _logger,
            $"transcribe existing handwritten note for article {articleId}");
    }

    public async Task<bool> DeleteHandwrittenNoteAsync(Guid articleId)
    {
        return await _http.DeleteEntityAsync(
            $"articles/{articleId}/handwritten-note",
            _logger,
            $"handwritten note for article {articleId}");
    }
}

/// <summary>
/// Internal DTO for deserializing the download URL response from the GET endpoint.
/// </summary>
internal class HandwrittenNoteDownloadUrlDto
{
    public string DownloadUrl { get; set; } = string.Empty;
}
