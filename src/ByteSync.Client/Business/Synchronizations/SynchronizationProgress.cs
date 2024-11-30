namespace ByteSync.Business.Synchronizations;

public class SynchronizationProgress
{
    public SynchronizationProgress()
    {
        Version = 0;
        ProcessedVolume = 0;
        ExchangedVolume = 0;
    }

    public string CloudSessionId { get; set; }

    public long Version { get; set; }
        
    public long ProcessedVolume { get; set; }
    
    public long ExchangedVolume { get; set; }
    
    public long FinishedActionsCount { get; set; }
    
    public long ErrorActionsCount { get; set; }
    
    public long TotalVolumeToProcess { get; set; }

    public bool HasNonZeroProperty()
    {
        return ProcessedVolume != 0
               || ExchangedVolume != 0
               || FinishedActionsCount != 0
               || ErrorActionsCount != 0;
    }
}