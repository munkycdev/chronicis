using Chronicis.CaptureApp.Models;
using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace Chronicis.CaptureApp.Services;

public class AudioCaptureService : IAudioCaptureService, IDisposable
{
    private WasapiLoopbackCapture? _captureDevice;
    private MemoryStream? _currentChunkStream;
    private WaveFileWriter? _currentChunkWriter;
    private WaveFormat? _captureFormat;

    // NEW: Full session audio writer
    private WaveFileWriter? _sessionAudioWriter;
    private string? _sessionAudioPath;

    private int _chunkDurationSeconds;
    private int _bytesPerChunk;
    private int _currentChunkBytes;
    private DateTime _recordingStartTime;
    private DateTime _currentChunkStartTime;

    private Queue<(string path, TimeSpan timestamp)> _pendingChunks = new();
    private bool _isProcessing;

    public event EventHandler<(string audioPath, TimeSpan timestamp)>? ChunkReady;
    public event EventHandler<QueueStatistics>? QueueStatsUpdated;
    public event EventHandler? RecordingStopped;
    public event EventHandler<string>? SessionAudioReady; // NEW

    public bool IsRecording { get; private set; }
    public QueueStatistics QueueStats { get; private set; } = new();

    public void StartRecording(TranscriptionSettings settings)
    {
        if (IsRecording)
            return;

        _chunkDurationSeconds = settings.ChunkDurationSeconds;
        _recordingStartTime = DateTime.Now;
        _currentChunkStartTime = _recordingStartTime;
        QueueStats.Reset();

        var enumerator = new MMDeviceEnumerator();
        var device = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);

        _captureDevice = new WasapiLoopbackCapture(device);
        _captureFormat = _captureDevice.WaveFormat;
        _bytesPerChunk = _captureFormat.AverageBytesPerSecond * _chunkDurationSeconds;
        _currentChunkBytes = 0;

        // NEW: Create full session audio file
        CreateSessionAudioFile();

        CreateNewChunk();

        _captureDevice.DataAvailable += OnDataAvailable;
        _captureDevice.RecordingStopped += OnRecordingStopped;

        _captureDevice.StartRecording();
        IsRecording = true;
    }

    public void StopRecording()
    {
        if (!IsRecording)
            return;
        _captureDevice?.StopRecording();
    }

    // NEW: Create session audio file
    private void CreateSessionAudioFile()
    {
        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        _sessionAudioPath = Path.Combine(
            Path.GetTempPath(),
            $"session_audio_{timestamp}.wav"
        );

        _sessionAudioWriter = new WaveFileWriter(_sessionAudioPath, _captureFormat!);
    }

    private void OnDataAvailable(object? sender, WaveInEventArgs e)
    {
        if (_currentChunkWriter == null)
            return;

        // Write to chunk
        _currentChunkWriter.Write(e.Buffer, 0, e.BytesRecorded);
        _currentChunkBytes += e.BytesRecorded;

        // NEW: Also write to full session audio
        _sessionAudioWriter?.Write(e.Buffer, 0, e.BytesRecorded);

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

        // NEW: Finalize session audio
        FinalizeSessionAudio();

        RecordingStopped?.Invoke(this, EventArgs.Empty);
    }

    // NEW: Finalize and compress session audio
    private void FinalizeSessionAudio()
    {
        _sessionAudioWriter?.Dispose();
        _sessionAudioWriter = null;

        if (!string.IsNullOrEmpty(_sessionAudioPath) && File.Exists(_sessionAudioPath))
        {
            // Compress to MP3 for smaller file size
            string compressedPath = CompressToMp3(_sessionAudioPath);

            // Delete original WAV
            try
            { File.Delete(_sessionAudioPath); }
            catch { }

            // Notify that audio is ready
            SessionAudioReady?.Invoke(this, compressedPath);
        }
    }

    // NEW: Compress WAV to MP3
    private string CompressToMp3(string wavPath)
    {
        string mp3Path = Path.ChangeExtension(wavPath, ".mp3");

        try
        {
            using var reader = new WaveFileReader(wavPath);

            // Convert to 16kHz mono for smaller size (good enough for voice)
            var outFormat = new WaveFormat(16000, 1);
            using var resampler = new MediaFoundationResampler(reader, outFormat);
            resampler.ResamplerQuality = 30; // Lower quality = smaller file

            // Encode to MP3 (64kbps - good for voice)
            MediaFoundationEncoder.EncodeToMp3(resampler, mp3Path, 64000);

            return mp3Path;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error compressing audio: {ex.Message}");
            // If compression fails, return original WAV
            return wavPath;
        }
    }

    private void CreateNewChunk()
    {
        _currentChunkStream = new MemoryStream();
        _currentChunkWriter = new WaveFileWriter(_currentChunkStream, _captureFormat!);
        _currentChunkBytes = 0;
        _currentChunkStartTime = DateTime.Now;
    }

    private void ProcessCurrentChunk()
    {
        _currentChunkWriter?.Flush();

        string chunkPath = Path.Combine(Path.GetTempPath(), $"chunk_{Guid.NewGuid()}.wav");
        File.WriteAllBytes(chunkPath, _currentChunkStream!.ToArray());

        var timestamp = _currentChunkStartTime - _recordingStartTime;

        _currentChunkWriter?.Dispose();
        _currentChunkStream?.Dispose();

        if (_pendingChunks.Count > 3)
        {
            QueueStats.SkippedChunks++;
            NotifyQueueStats();
            return;
        }

        _pendingChunks.Enqueue((chunkPath, timestamp));
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
            var (chunkPath, timestamp) = _pendingChunks.Dequeue();
            QueueStats.PendingChunks = _pendingChunks.Count;
            NotifyQueueStats();

            try
            {
                var whisperPath = ConvertToWhisperFormat(chunkPath);
                ChunkReady?.Invoke(this, (whisperPath, timestamp));

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
        try
        { if (File.Exists(path)) File.Delete(path); }
        catch { }
    }

    public void Dispose()
    {
        _captureDevice?.Dispose();
        _currentChunkWriter?.Dispose();
        _currentChunkStream?.Dispose();
        _sessionAudioWriter?.Dispose(); // NEW
    }
}
