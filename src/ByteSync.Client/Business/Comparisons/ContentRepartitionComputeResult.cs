using ByteSync.Common.Business.Inventories;

namespace ByteSync.Business.Comparisons;

public class ContentRepartitionComputeResult
{
    public ContentRepartitionComputeResult(FileSystemTypes fileSystemType)
    {
        FileSystemType = fileSystemType;
    }

    public FileSystemTypes FileSystemType { get; set; }

    public int FingerPrintGroups { get; set; }
    
    public int LastWriteTimeGroups { get; set; }
    
    public int PresenceGroups { get; set; }
}