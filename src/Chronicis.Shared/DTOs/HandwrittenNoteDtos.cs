using System.Diagnostics.CodeAnalysis;

namespace Chronicis.Shared.DTOs;

/// <summary>
/// Response DTO for saving a handwritten note image.
/// </summary>
[ExcludeFromCodeCoverage]
public class HandwrittenNoteSaveResultDto
{
    public Guid DocumentId { get; set; }
    public string DownloadUrl { get; set; } = string.Empty;
}

/// <summary>
/// Response DTO for saving and transcribing a handwritten note.
/// </summary>
[ExcludeFromCodeCoverage]
public class HandwrittenNoteTranscribeResultDto
{
    public Guid DocumentId { get; set; }
    public string DownloadUrl { get; set; } = string.Empty;
    public string TranscribedText { get; set; } = string.Empty;
}

/// <summary>
/// Result DTO from the transcription service.
/// </summary>
[ExcludeFromCodeCoverage]
public class TranscriptionResultDto
{
    public bool Success { get; set; }
    public string Text { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
}
