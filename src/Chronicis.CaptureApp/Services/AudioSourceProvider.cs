using Chronicis.CaptureApp.Models;
using NAudio.CoreAudioApi;
using System;
using System.Collections.Generic;

namespace Chronicis.CaptureApp.Services;

public class AudioSourceProvider : IAudioSourceProvider
{
    public List<AudioSource> GetAvailableAudioSources()
    {
        var sources = new List<AudioSource>
        {
            new AudioSource { DisplayName = "System Audio (All Sounds)", ProcessId = null }
        };

        try
        {
            var enumerator = new MMDeviceEnumerator();
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
            Console.WriteLine($"Error loading audio sources: {ex.Message}");
        }

        return sources;
    }
}