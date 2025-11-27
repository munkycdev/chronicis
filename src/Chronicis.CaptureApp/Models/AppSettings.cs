using Whisper.net.Ggml;

namespace Chronicis.CaptureApp.Models;

public class AppSettings
{
    public GgmlType SelectedModel { get; set; } = GgmlType.Base;
    public int ChunkDurationSeconds { get; set; } = 5;
    public string LastAudioSource { get; set; } = "System Audio (All Sounds)";
    public bool MinimizeToTray { get; set; } = true;
    public bool EnableSpeakerDetection { get; set; } = true; // NEW
    public Dictionary<int, string> SpeakerNames { get; set; } = new(); // NEW
}