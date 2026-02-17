using Chronicis.CaptureApp.Models;

namespace Chronicis.CaptureApp.Services;

public interface IAudioSourceProvider
{
    List<AudioSource> GetAvailableAudioSources();
}
