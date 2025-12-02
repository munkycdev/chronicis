using Whisper.net;
using Whisper.net.Ggml;

namespace Chronicis.CaptureApp.Services;

public class WhisperTranscriptionService : ITranscriptionService, IDisposable
{
    private WhisperProcessor? _processor;
    private GgmlType _currentModel;

    public bool IsInitialized { get; private set; }

    public async Task InitializeAsync(GgmlType model)
    {
        _currentModel = model;

        string modelFileName = model switch
        {
            GgmlType.Tiny => "ggml-tiny.bin",
            GgmlType.Small => "ggml-small.bin",
            _ => "ggml-base.bin"
        };

        string modelPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Models", modelFileName);

        if (!File.Exists(modelPath))
        {
            var modelDir = Path.GetDirectoryName(modelPath);
            if (!Directory.Exists(modelDir))
            {
                Directory.CreateDirectory(modelDir!);
            }

            using var modelStream = await WhisperGgmlDownloader.GetGgmlModelAsync(model);
            using var fileWriter = File.OpenWrite(modelPath);
            await modelStream.CopyToAsync(fileWriter);
        }

        await Task.Run(() =>
        {
            var factory = WhisperFactory.FromPath(modelPath);
            _processor = factory.CreateBuilder()
                .WithLanguage("auto")
                .Build();
        });

        IsInitialized = true;
    }

    public async Task<string> TranscribeAsync(string audioFilePath)
    {
        if (!IsInitialized || _processor == null)
            throw new InvalidOperationException("Transcription service not initialized");

        using var fileStream = File.OpenRead(audioFilePath);
        var segments = _processor.ProcessAsync(fileStream).ConfigureAwait(false);
        var transcription = string.Empty;

        await foreach (var segment in segments)
        {
            transcription += segment.Text;
        }

        // Cleanup temp file after transcription
        try { File.Delete(audioFilePath); } catch { }

        return transcription.Trim();
    }

    public void Dispose()
    {
        _processor?.Dispose();
    }
}