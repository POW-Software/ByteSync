using System.Collections.Generic;

namespace ByteSync.Common.Business.Synchronizations;

public class SynchronizationProgressPush
{
    public List<TrackingActionSummary>? TrackingActionSummaries { get; set; }
    
    public string SessionId { get; set; } = null!;
    
    // New volume tracking properties
    public long ActualUploadedVolume { get; set; }
    public long ActualDownloadedVolume { get; set; }
    public long LocalCopyTransferredVolume { get; set; }
    public long SynchronizedVolume { get; set; }
    
    public long ProcessedVolume { get; set; }
    
    public long ExchangedVolume { get; set; }
    
    public long FinishedActionsCount { get; set; }
    
    public long ErrorActionsCount { get; set; }

    public long Version { get; set; }
}