using Whisper.net.Ggml;

namespace Chronicis.CaptureApp.Models;

public class TranscriptionSettings
{
    public GgmlType Model { get; set; }
    public int ChunkDurationSeconds { get; set; }
    public string AudioSourceName { get; set; } = string.Empty;
}
