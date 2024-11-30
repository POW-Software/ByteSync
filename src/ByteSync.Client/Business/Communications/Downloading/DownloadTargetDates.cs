using ByteSync.Business.Actions.Shared;

namespace ByteSync.Business.Communications.Downloading;

public class DownloadTargetDates
{
    public DownloadTargetDates(SharedActionsGroup sharedActionsGroup)
    {
        CreationTimeUtc = sharedActionsGroup.CreationTimeUtc!.Value;
        LastWriteTimeUtc = sharedActionsGroup.LastWriteTimeUtc!.Value;
    }

    public DateTime CreationTimeUtc { get; set; }
    
    public DateTime LastWriteTimeUtc { get; set; }
}