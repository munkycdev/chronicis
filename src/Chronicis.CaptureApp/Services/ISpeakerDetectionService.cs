using Chronicis.CaptureApp.Models;

namespace Chronicis.CaptureApp.Services;

public interface ISpeakerDetectionService
{
    SpeakerSegment AnalyzeAudioSegment(string audioFilePath, string transcribedText, TimeSpan startTime);
    void Reset();
}