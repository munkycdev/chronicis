using Chronicis.CaptureApp.Models;
using NAudio.Wave;

namespace Chronicis.CaptureApp.Services;

public class SpeakerDetectionService : ISpeakerDetectionService
{
    private List<AudioFeatures> _knownSpeakers = new();
    private const double SIMILARITY_THRESHOLD = 0.3; // Lower = more strict matching

    public SpeakerSegment AnalyzeAudioSegment(string audioFilePath, string transcribedText, TimeSpan startTime)
    {
        var features = ExtractAudioFeatures(audioFilePath);
        var speakerId = IdentifyOrCreateSpeaker(features);

        return new SpeakerSegment
        {
            SpeakerId = speakerId,
            SpeakerName = $"Speaker {speakerId}",
            Text = transcribedText,
            StartTime = startTime,
            EndTime = startTime + features.Duration,
            AveragePitch = features.AveragePitch,
            AverageVolume = features.AverageVolume
        };
    }

    public void Reset()
    {
        _knownSpeakers.Clear();
    }

    private AudioFeatures ExtractAudioFeatures(string audioFilePath)
    {
        using var reader = new WaveFileReader(audioFilePath);

        var samples = new List<float>();
        var buffer = new byte[reader.WaveFormat.AverageBytesPerSecond]; // byte buffer
        int bytesRead;

        while ((bytesRead = reader.Read(buffer, 0, buffer.Length)) > 0)
        {
            // Convert bytes to floats based on format
            int bytesPerSample = reader.WaveFormat.BitsPerSample / 8;

            for (int i = 0; i < bytesRead; i += bytesPerSample)
            {
                float sample = 0f;

                if (reader.WaveFormat.BitsPerSample == 16)
                {
                    // 16-bit PCM
                    if (i + 1 < bytesRead)
                    {
                        short sampleValue = (short)(buffer[i] | (buffer[i + 1] << 8));
                        sample = sampleValue / 32768f; // Normalize to -1.0 to 1.0
                    }
                }
                else if (reader.WaveFormat.BitsPerSample == 32 && reader.WaveFormat.Encoding == WaveFormatEncoding.IeeeFloat)
                {
                    // 32-bit float
                    if (i + 3 < bytesRead)
                    {
                        sample = BitConverter.ToSingle(buffer, i);
                    }
                }
                else if (reader.WaveFormat.BitsPerSample == 8)
                {
                    // 8-bit PCM
                    sample = (buffer[i] - 128) / 128f; // Normalize to -1.0 to 1.0
                }

                samples.Add(sample);
            }
        }

        if (samples.Count == 0)
        {
            return new AudioFeatures
            {
                AveragePitch = 0,
                AverageVolume = 0,
                PitchVariance = 0,
                Duration = TimeSpan.Zero
            };
        }

        // Calculate average volume (RMS)
        double sumSquares = 0;
        foreach (var sample in samples)
        {
            sumSquares += sample * sample;
        }
        double rms = Math.Sqrt(sumSquares / samples.Count);

        // Estimate pitch using zero-crossing rate (simplified)
        int zeroCrossings = 0;
        for (int i = 1; i < samples.Count; i++)
        {
            if ((samples[i] >= 0 && samples[i - 1] < 0) || (samples[i] < 0 && samples[i - 1] >= 0))
            {
                zeroCrossings++;
            }
        }

        double zeroCrossingRate = (double)zeroCrossings / samples.Count;
        double estimatedPitch = zeroCrossingRate * reader.WaveFormat.SampleRate / 2;

        // Calculate pitch variance (how much pitch changes)
        var pitchVariance = CalculateVariance(samples);

        var duration = TimeSpan.FromSeconds((double)samples.Count / reader.WaveFormat.SampleRate);

        return new AudioFeatures
        {
            AveragePitch = estimatedPitch,
            AverageVolume = rms,
            PitchVariance = pitchVariance,
            Duration = duration
        };
    }

    private double CalculateVariance(List<float> samples)
    {
        if (samples.Count < 2)
            return 0;

        double mean = samples.Average();
        double sumSquaredDiffs = samples.Sum(sample => Math.Pow(sample - mean, 2));
        return sumSquaredDiffs / samples.Count;
    }

    private int IdentifyOrCreateSpeaker(AudioFeatures features)
    {
        // Find most similar known speaker
        int bestMatchId = -1;
        double bestSimilarity = double.MaxValue;

        for (int i = 0; i < _knownSpeakers.Count; i++)
        {
            double similarity = CalculateSimilarity(features, _knownSpeakers[i]);

            if (similarity < bestSimilarity)
            {
                bestSimilarity = similarity;
                bestMatchId = i;
            }
        }

        // If similar enough to existing speaker, use that ID
        if (bestMatchId >= 0 && bestSimilarity < SIMILARITY_THRESHOLD)
        {
            return bestMatchId + 1; // 1-based speaker IDs
        }

        // Otherwise, create new speaker
        _knownSpeakers.Add(features);
        return _knownSpeakers.Count;
    }

    private double CalculateSimilarity(AudioFeatures a, AudioFeatures b)
    {
        // Normalize and calculate Euclidean distance
        double pitchDiff = Math.Abs(a.AveragePitch - b.AveragePitch) / 1000.0; // Normalize pitch
        double volumeDiff = Math.Abs(a.AverageVolume - b.AverageVolume);
        double varianceDiff = Math.Abs(a.PitchVariance - b.PitchVariance);

        return Math.Sqrt(
            pitchDiff * pitchDiff +
            volumeDiff * volumeDiff * 10 + // Weight volume more heavily
            varianceDiff * varianceDiff
        );
    }
}