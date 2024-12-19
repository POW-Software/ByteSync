using ByteSync.Business.Actions.Shared;

namespace ByteSync.Business.Communications.Downloading;

public class DownloadTargetDates
{
    public DownloadTargetDates()
    {

    }

    public DateTime CreationTimeUtc { get; set; }
    
    public DateTime LastWriteTimeUtc { get; set; }
    
    public static DownloadTargetDates FromSharedActionsGroup(SharedActionsGroup sharedActionsGroup)
    {
        var downloadTargetDates = new DownloadTargetDates
        {
            CreationTimeUtc =  sharedActionsGroup.CreationTimeUtc!.Value,
            LastWriteTimeUtc = sharedActionsGroup.LastWriteTimeUtc!.Value
        };

        return downloadTargetDates;
    }
}