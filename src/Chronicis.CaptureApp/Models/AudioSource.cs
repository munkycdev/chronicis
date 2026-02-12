namespace Chronicis.CaptureApp.Models;

public class AudioSource
{
    public string DisplayName { get; set; } = string.Empty;
    public int? ProcessId { get; set; }
    public bool IsSystemAudio => ProcessId == null;
}
