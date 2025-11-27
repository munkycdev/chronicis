using Chronicis.CaptureApp.Models;
using System.Collections.Generic;

namespace Chronicis.CaptureApp.Services;

public interface IAudioSourceProvider
{
    List<AudioSource> GetAvailableAudioSources();
}