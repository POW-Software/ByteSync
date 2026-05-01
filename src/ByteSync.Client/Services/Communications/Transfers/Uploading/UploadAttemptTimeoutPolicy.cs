using System;

namespace ByteSync.Services.Communications.Transfers.Uploading;

public static class UploadAttemptTimeoutPolicy
{
    private const int AttemptTimeoutFloorSeconds = 60;
    private const int AttemptTimeoutCeilingSeconds = 180;
    private const int ExtendedOversizedSliceTimeoutCeilingSeconds = 300;
    private const int SecondsPerMegabyteHeuristic = 3;
    private const int RetryGrowthSeconds = 15;
    private const int OversizedSliceSecondsPerCurrentChunk = 30;
    private const int ExtendedOversizedSliceThresholdBytes = 1024 * 1024;
    private const int ExtendedOversizedSliceChunkRatioThreshold = 8;
    
    public static int ComputeTimeoutSeconds(long sliceLengthBytes, int attempt, int currentChunkSizeBytes)
    {
        var timeoutCeilingSeconds = ComputeTimeoutCeilingSeconds(sliceLengthBytes, currentChunkSizeBytes);
        var timeoutSec = Math.Max(
            (long)ComputeBaseTimeoutSeconds(sliceLengthBytes),
            ComputeOversizedSliceTimeoutSeconds(sliceLengthBytes, currentChunkSizeBytes, timeoutCeilingSeconds));

        if (attempt > 1)
        {
            timeoutSec += (long)(attempt - 1) * RetryGrowthSeconds;
        }
        
        return (int)Math.Clamp(timeoutSec, AttemptTimeoutFloorSeconds, timeoutCeilingSeconds);
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
    
    private static int ComputeTimeoutCeilingSeconds(long sliceLengthBytes, int currentChunkSizeBytes)
    {
        if (currentChunkSizeBytes <= 0 || sliceLengthBytes <= currentChunkSizeBytes)
        {
            return AttemptTimeoutCeilingSeconds;
        }

        var chunkRatio = Math.Ceiling(sliceLengthBytes / (double)currentChunkSizeBytes);
        if (sliceLengthBytes >= ExtendedOversizedSliceThresholdBytes
            && chunkRatio >= ExtendedOversizedSliceChunkRatioThreshold)
        {
            return ExtendedOversizedSliceTimeoutCeilingSeconds;
        }

        return AttemptTimeoutCeilingSeconds;
    }

    private static long ComputeOversizedSliceTimeoutSeconds(long sliceLengthBytes, int currentChunkSizeBytes, int timeoutCeilingSeconds)
    {
        if (currentChunkSizeBytes <= 0 || sliceLengthBytes <= currentChunkSizeBytes)
        {
            return 0;
        }
        
        var chunkRatio = Math.Ceiling(sliceLengthBytes / (double)currentChunkSizeBytes);
        if (chunkRatio >= timeoutCeilingSeconds / (double)OversizedSliceSecondsPerCurrentChunk)
        {
            return timeoutCeilingSeconds;
        }
        
        return (long)chunkRatio * OversizedSliceSecondsPerCurrentChunk;
    }
}
