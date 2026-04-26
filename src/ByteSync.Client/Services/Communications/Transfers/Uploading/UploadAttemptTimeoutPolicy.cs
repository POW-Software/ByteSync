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
        var timeoutSec = ComputeBaseTimeoutSeconds(sliceLengthBytes);
        if (attempt <= 1)
        {
            return timeoutSec;
        }
        
        var staleChunkPenalty = ComputeStaleChunkPenaltySeconds(sliceLengthBytes, currentChunkSizeBytes);
        timeoutSec += (attempt - 1) * RetryGrowthSeconds + staleChunkPenalty;
        
        return Math.Clamp(timeoutSec, AttemptTimeoutFloorSeconds, AttemptTimeoutCeilingSeconds);
    }
    
    private static int ComputeBaseTimeoutSeconds(long sliceLengthBytes)
    {
        var sizeMb = Math.Max(1, (int)Math.Ceiling(sliceLengthBytes / (1024d * 1024d)));
        
        return Math.Clamp(
            SecondsPerMegabyteHeuristic * sizeMb,
            AttemptTimeoutFloorSeconds,
            AttemptTimeoutCeilingSeconds);
    }
    
    private static int ComputeStaleChunkPenaltySeconds(long sliceLengthBytes, int currentChunkSizeBytes)
    {
        if (currentChunkSizeBytes <= 0 || sliceLengthBytes <= currentChunkSizeBytes)
        {
            return 0;
        }
        
        var chunkRatio = (int)Math.Ceiling(sliceLengthBytes / (double)currentChunkSizeBytes);
        
        return (chunkRatio - 1) * StaleChunkPenaltySeconds;
    }
}
