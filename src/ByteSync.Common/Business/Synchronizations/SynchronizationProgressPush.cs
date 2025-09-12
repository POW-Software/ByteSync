using System.Collections.Generic;

namespace ByteSync.Common.Business.Synchronizations;

public class SynchronizationProgressPush
{
    public List<TrackingActionSummary>? TrackingActionSummaries { get; init; }

    public string SessionId { get; init; } = null!;

    public long ActualUploadedVolume { get; init; }

    public long ActualDownloadedVolume { get; init; }

    public long LocalCopyTransferredVolume { get; init; }

    public long SynchronizedVolume { get; init; }

    public long FinishedActionsCount { get; init; }

    public long ErrorActionsCount { get; init; }

    public long Version { get; init; }
}