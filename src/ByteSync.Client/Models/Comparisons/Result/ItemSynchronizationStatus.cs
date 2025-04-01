using ByteSync.Business.Inventories;

namespace ByteSync.Models.Comparisons.Result;

public class ItemSynchronizationStatus
{
    public ItemSynchronizationStatus(PathIdentity pathIdentity)
    {
        PathIdentity = pathIdentity;
    }
        
    public PathIdentity PathIdentity { get; }
    
    public bool IsSuccessStatus { get; set; }
    
    public bool IsErrorStatus { get; set; }
}