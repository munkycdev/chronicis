using Chronicis.Shared.DTOs;

namespace Chronicis.Api.Services;

/// <summary>
/// Service for transcribing handwritten note images to text using an external AI/OCR API.
/// </summary>
public interface ITranscriptionService
{
    /// <summary>
    /// Transcribe an image to text using the external AI/OCR service.
    /// </summary>
    /// <param name="imageBytes">The PNG image bytes to transcribe.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A result indicating success/failure and the transcribed text.</returns>
    Task<TranscriptionResultDto> TranscribeImageAsync(byte[] imageBytes, CancellationToken cancellationToken = default);
}
