using Chronicis.CaptureApp.Models;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using NAudio.MediaFoundation;

namespace Chronicis.CaptureApp.Services;

public class AudioCaptureService : IAudioCaptureService, IDisposable
{
    private WasapiLoopbackCapture? _captureDevice;
    private MemoryStream? _currentChunkStream;
    private WaveFileWriter? _currentChunkWriter;
    private WaveFormat? _captureFormat;

    private int _chunkDurationSeconds;
    private int _bytesPerChunk;
    private int _currentChunkBytes;
    private DateTime _recordingStartTime; // NEW
    private DateTime _currentChunkStartTime; // NEW

    private Queue<(string path, TimeSpan timestamp)> _pendingChunks = new(); // UPDATED
    private bool _isProcessing;

    public event EventHandler<(string audioPath, TimeSpan timestamp)>? ChunkReady; // UPDATED
    public event EventHandler<QueueStatistics>? QueueStatsUpdated;
    public event EventHandler? RecordingStopped;

    public bool IsRecording { get; private set; }
    public QueueStatistics QueueStats { get; private set; } = new();

    public void StartRecording(TranscriptionSettings settings)
    {
        if (IsRecording) return;

        _chunkDurationSeconds = settings.ChunkDurationSeconds;
        _recordingStartTime = DateTime.Now; // NEW
        _currentChunkStartTime = _recordingStartTime; // NEW
        QueueStats.Reset();

        var enumerator = new MMDeviceEnumerator();
        var device = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);

        _captureDevice = new WasapiLoopbackCapture(device);
        _captureFormat = _captureDevice.WaveFormat;
        _bytesPerChunk = _captureFormat.AverageBytesPerSecond * _chunkDurationSeconds;
        _currentChunkBytes = 0;

        CreateNewChunk();

        _captureDevice.DataAvailable += OnDataAvailable;
        _captureDevice.RecordingStopped += OnRecordingStopped;

        _captureDevice.StartRecording();
        IsRecording = true;
    }

    public void StopRecording()
    {
        if (!IsRecording) return;
        _captureDevice?.StopRecording();
    }

    private void OnDataAvailable(object? sender, WaveInEventArgs e)
    {
        if (_currentChunkWriter == null) return;

        _currentChunkWriter.Write(e.Buffer, 0, e.BytesRecorded);
        _currentChunkBytes += e.BytesRecorded;

        if (_currentChunkBytes >= _bytesPerChunk)
        {
            ProcessCurrentChunk();
            CreateNewChunk();
        }
    }

    private void OnRecordingStopped(object? sender, StoppedEventArgs e)
    {
        IsRecording = false;

        if (_currentChunkBytes > 0)
        {
            ProcessCurrentChunk();
        }

        RecordingStopped?.Invoke(this, EventArgs.Empty);
    }

    private void CreateNewChunk()
    {
        _currentChunkStream = new MemoryStream();
        _currentChunkWriter = new WaveFileWriter(_currentChunkStream, _captureFormat!);
        _currentChunkBytes = 0;
        _currentChunkStartTime = DateTime.Now; // NEW
    }

    private void ProcessCurrentChunk()
    {
        _currentChunkWriter?.Flush();

        string chunkPath = Path.Combine(Path.GetTempPath(), $"chunk_{Guid.NewGuid()}.wav");
        File.WriteAllBytes(chunkPath, _currentChunkStream!.ToArray());

        var timestamp = _currentChunkStartTime - _recordingStartTime; // NEW

        _currentChunkWriter?.Dispose();
        _currentChunkStream?.Dispose();

        // Skip chunks if queue is backing up
        if (_pendingChunks.Count > 3)
        {
            QueueStats.SkippedChunks++;
            NotifyQueueStats();
            return;
        }

        _pendingChunks.Enqueue((chunkPath, timestamp)); // UPDATED
        QueueStats.PendingChunks = _pendingChunks.Count;
        NotifyQueueStats();

        if (!_isProcessing)
        {
            Task.Run(ProcessQueueAsync);
        }
    }

    private async Task ProcessQueueAsync()
    {
        _isProcessing = true;

        while (_pendingChunks.Count > 0)
        {
            var (chunkPath, timestamp) = _pendingChunks.Dequeue(); // UPDATED
            QueueStats.PendingChunks = _pendingChunks.Count;
            NotifyQueueStats();

            try
            {
                var whisperPath = ConvertToWhisperFormat(chunkPath);
                ChunkReady?.Invoke(this, (whisperPath, timestamp)); // UPDATED

                QueueStats.ProcessedChunks++;
                NotifyQueueStats();
            }
            finally
            {
                CleanupFile(chunkPath);
            }
        }

        _isProcessing = false;
    }

    private string ConvertToWhisperFormat(string inputWavPath)
    {
        string outputPath = Path.Combine(Path.GetTempPath(),
            $"whisper_{Path.GetFileNameWithoutExtension(inputWavPath)}.wav");

        using (var reader = new WaveFileReader(inputWavPath))
        {
            var outFormat = new WaveFormat(16000, 1);
            using (var resampler = new MediaFoundationResampler(reader, outFormat))
            {
                WaveFileWriter.CreateWaveFile(outputPath, resampler);
            }
        }

        return outputPath;
    }

    private void NotifyQueueStats()
    {
        QueueStatsUpdated?.Invoke(this, QueueStats);
    }

    private void CleanupFile(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); }
        catch { /* Ignore cleanup errors */ }
    }

    public void Dispose()
    {
        _captureDevice?.Dispose();
        _currentChunkWriter?.Dispose();
        _currentChunkStream?.Dispose();
    }
}