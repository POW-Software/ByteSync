using System;

namespace ByteSync.Services.Communications.Transfers.Uploading;

public static class UploadAttemptTimeoutPolicy
{
    private const int AttemptTimeoutFloorSeconds = 60;
    private const int AttemptTimeoutCeilingSeconds = 120;
    private const int SecondsPerMegabyteHeuristic = 3;
    private const int RetryGrowthSeconds = 15;
    private const int StaleChunkPenaltySeconds = 5;
    
    public static int ComputeTimeoutSeconds(long sliceLengthBytes, int attempt, int currentChunkSizeBytes)
    {
        var timeoutSec = (long)ComputeBaseTimeoutSeconds(sliceLengthBytes);
        if (attempt <= 1)
        {
            return (int)timeoutSec;
        }
        
        var staleChunkPenalty = ComputeStaleChunkPenaltySeconds(sliceLengthBytes, currentChunkSizeBytes);
        timeoutSec += (long)(attempt - 1) * RetryGrowthSeconds + staleChunkPenalty;
        
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
    
    private static long ComputeStaleChunkPenaltySeconds(long sliceLengthBytes, int currentChunkSizeBytes)
    {
        if (currentChunkSizeBytes <= 0 || sliceLengthBytes <= currentChunkSizeBytes)
        {
            return 0;
        }
        
        var chunkRatio = Math.Ceiling(sliceLengthBytes / (double)currentChunkSizeBytes);
        if (chunkRatio >= AttemptTimeoutCeilingSeconds / (double)StaleChunkPenaltySeconds + 1)
        {
            return AttemptTimeoutCeilingSeconds;
        }
        
        return (long)(chunkRatio - 1) * StaleChunkPenaltySeconds;
    }
}
