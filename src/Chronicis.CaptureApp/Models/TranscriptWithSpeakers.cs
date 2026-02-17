namespace Chronicis.CaptureApp.Models;

public class TranscriptWithSpeakers
{
    public List<SpeakerSegment> Segments { get; set; } = new();
    public Dictionary<int, string> SpeakerNames { get; set; } = new();

    public string GetFormattedTranscript()
    {
        var sb = new System.Text.StringBuilder();

        foreach (var segment in Segments)
        {
            var speakerName = SpeakerNames.ContainsKey(segment.SpeakerId)
                ? SpeakerNames[segment.SpeakerId]
                : $"Speaker {segment.SpeakerId}";

            sb.AppendLine($"[{speakerName}] {segment.Text}");
        }

        return sb.ToString();
    }
}
