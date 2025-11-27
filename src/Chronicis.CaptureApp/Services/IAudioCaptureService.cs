using Chronicis.CaptureApp.Models;

namespace Chronicis.CaptureApp.Services;

public interface IAudioCaptureService
{
    event EventHandler<(string audioPath, TimeSpan timestamp)>? ChunkReady; // UPDATED: Added timestamp
    event EventHandler<QueueStatistics>? QueueStatsUpdated;
    event EventHandler? RecordingStopped;

    bool IsRecording { get; }
    QueueStatistics QueueStats { get; }

    void StartRecording(TranscriptionSettings settings);
    void StopRecording();
}