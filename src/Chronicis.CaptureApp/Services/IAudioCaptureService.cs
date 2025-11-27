using Chronicis.CaptureApp.Models;
using System;

namespace Chronicis.CaptureApp.Services;

public interface IAudioCaptureService
{
    event EventHandler<string>? ChunkReady;
    event EventHandler<QueueStatistics>? QueueStatsUpdated;
    event EventHandler? RecordingStopped;

    bool IsRecording { get; }
    QueueStatistics QueueStats { get; }

    void StartRecording(TranscriptionSettings settings);
    void StopRecording();
}