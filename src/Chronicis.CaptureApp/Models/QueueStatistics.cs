namespace Chronicis.CaptureApp.Models;

public class QueueStatistics
{
    public int PendingChunks { get; set; }
    public int ProcessedChunks { get; set; }
    public int SkippedChunks { get; set; }

    public void Reset()
    {
        PendingChunks = 0;
        ProcessedChunks = 0;
        SkippedChunks = 0;
    }
}
