using System.Collections.Generic;

namespace ByteSync.Common.Business.Synchronizations;

public class SynchronizationProgressPush
{
    public List<TrackingActionSummary>? TrackingActionSummaries { get; set; }
    
    public string SessionId { get; set; } = null!;
    
    public long ProcessedVolume { get; set; }
    
    public long ExchangedVolume { get; set; }
    
    public long FinishedActionsCount { get; set; }
    
    public long ErrorActionsCount { get; set; }

    public long Version { get; set; }
}