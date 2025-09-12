namespace ByteSync.Business.Synchronizations;

public class SynchronizationProgress
{
    public SynchronizationProgress()
    {
        Version = 0;
        ActualUploadedVolume = 0;
        ActualDownloadedVolume = 0;
        LocalCopyTransferredVolume = 0;
        SynchronizedVolume = 0;
    }

    public string SessionId { get; set; } = null!;

    public long Version { get; set; }

    public long ActualUploadedVolume { get; set; }

    public long ActualDownloadedVolume { get; set; }

    public long LocalCopyTransferredVolume { get; set; }

    public long SynchronizedVolume { get; set; }

    public long FinishedActionsCount { get; set; }

    public long ErrorActionsCount { get; set; }

    public long TotalVolumeToProcess { get; set; }

    public bool HasNonZeroProperty()
    {
        return SynchronizedVolume != 0
               || ActualUploadedVolume != 0
               || ActualDownloadedVolume != 0
               || LocalCopyTransferredVolume != 0
               || FinishedActionsCount != 0
               || ErrorActionsCount != 0;
    }
}