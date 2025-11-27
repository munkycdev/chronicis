namespace Chronicis.CaptureApp.Models;

public class SpeakerSegment
{
    public int SpeakerId { get; set; }
    public string SpeakerName { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public double AveragePitch { get; set; }
    public double AverageVolume { get; set; }
}