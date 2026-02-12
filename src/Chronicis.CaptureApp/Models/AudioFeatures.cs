namespace Chronicis.CaptureApp.Models;

public class AudioFeatures
{
    public double AveragePitch { get; set; }
    public double AverageVolume { get; set; }
    public double PitchVariance { get; set; }
    public TimeSpan Duration { get; set; }
}
