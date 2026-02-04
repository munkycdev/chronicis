using Chronicis.CaptureApp.Models;
using Microsoft.Extensions.Logging;
using NAudio.CoreAudioApi;

namespace Chronicis.CaptureApp.Services;

public class AudioSourceProvider : IAudioSourceProvider
{
    private readonly ILogger<AudioSourceProvider> _logger;

    public AudioSourceProvider(ILogger<AudioSourceProvider> logger)
    {
        _logger = logger;
    }

    public List<AudioSource> GetAvailableAudioSources()
    {
        var sources = new List<AudioSource>
        {
            new AudioSource { DisplayName = "System Audio (All Sounds)", ProcessId = null }
        };

        try
        {
            using var enumerator = new MMDeviceEnumerator();
            var devices = enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);

            foreach (var device in devices)
            {
                var sessionManager = device.AudioSessionManager;
                var sessions = sessionManager.Sessions;

                for (int i = 0; i < sessions.Count; i++)
                {
                    var session = sessions[i];
                    var processId = session.GetProcessID;

                    if (processId != 0)
                    {
                        try
                        {
                            var process = System.Diagnostics.Process.GetProcessById((int)processId);
                            string displayName = $"{process.ProcessName} (PID: {processId})";

                            if (!sources.Any(s => s.DisplayName == displayName))
                            {
                                sources.Add(new AudioSource
                                {
                                    DisplayName = displayName,
                                    ProcessId = (int)processId
                                });
                            }
                        }
                        catch { /* Process may have exited */ }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading audio sources: {Message}", ex.Message);
        }

        return sources;
    }
}