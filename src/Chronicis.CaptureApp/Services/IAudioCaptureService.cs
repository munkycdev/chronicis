using Chronicis.CaptureApp.Models;

namespace Chronicis.CaptureApp.Services;

public interface IAudioCaptureService
{
    event EventHandler<(string audioPath, TimeSpan timestamp)>? ChunkReady;
    event EventHandler<QueueStatistics>? QueueStatsUpdated;
    event EventHandler? RecordingStopped;
    event EventHandler<string>? SessionAudioReady; // NEW

    bool IsRecording { get; }
    QueueStatistics QueueStats { get; }

    void StartRecording(TranscriptionSettings settings);
    void StopRecording();
}
