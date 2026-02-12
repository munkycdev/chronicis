using Whisper.net.Ggml;

namespace Chronicis.CaptureApp.Services;

public interface ITranscriptionService
{
    Task InitializeAsync(GgmlType model);
    Task<string> TranscribeAsync(string audioFilePath);
    bool IsInitialized { get; }
}
