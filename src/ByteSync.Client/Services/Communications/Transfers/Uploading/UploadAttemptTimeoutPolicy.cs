using System;

namespace ByteSync.Services.Communications.Transfers.Uploading;

public static class UploadAttemptTimeoutPolicy
{
    private const int AttemptTimeoutFloorSeconds = 60;
    private const int AttemptTimeoutCeilingSeconds = 180;
    private const int SecondsPerMegabyteHeuristic = 3;
    private const int RetryGrowthSeconds = 15;
    private const int OversizedSliceSecondsPerCurrentChunk = 30;
    
    public static int ComputeTimeoutSeconds(long sliceLengthBytes, int attempt, int currentChunkSizeBytes)
    {
        var timeoutSec = Math.Max(
            (long)ComputeBaseTimeoutSeconds(sliceLengthBytes),
            ComputeOversizedSliceTimeoutSeconds(sliceLengthBytes, currentChunkSizeBytes));

        if (attempt > 1)
        {
            timeoutSec += (long)(attempt - 1) * RetryGrowthSeconds;
        }
        
        return (int)Math.Clamp(timeoutSec, AttemptTimeoutFloorSeconds, AttemptTimeoutCeilingSeconds);
    }
    
    private static int ComputeBaseTimeoutSeconds(long sliceLengthBytes)
    {
        var sizeMb = Math.Max(1d, Math.Ceiling(sliceLengthBytes / (1024d * 1024d)));
        var ceilingSizeMb = Math.Ceiling(AttemptTimeoutCeilingSeconds / (double)SecondsPerMegabyteHeuristic);
        if (sizeMb >= ceilingSizeMb)
        {
            return AttemptTimeoutCeilingSeconds;
        }
        
        return Math.Clamp(
            SecondsPerMegabyteHeuristic * (int)sizeMb,
            AttemptTimeoutFloorSeconds,
            AttemptTimeoutCeilingSeconds);
    }
    
    private static long ComputeOversizedSliceTimeoutSeconds(long sliceLengthBytes, int currentChunkSizeBytes)
    {
        if (currentChunkSizeBytes <= 0 || sliceLengthBytes <= currentChunkSizeBytes)
        {
            return 0;
        }
        
        var chunkRatio = Math.Ceiling(sliceLengthBytes / (double)currentChunkSizeBytes);
        if (chunkRatio >= AttemptTimeoutCeilingSeconds / (double)OversizedSliceSecondsPerCurrentChunk)
        {
            return AttemptTimeoutCeilingSeconds;
        }
        
        return (long)chunkRatio * OversizedSliceSecondsPerCurrentChunk;
    }
}
