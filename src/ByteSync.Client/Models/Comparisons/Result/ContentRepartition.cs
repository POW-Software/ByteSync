using ByteSync.Business.Inventories;
using ByteSync.Models.Inventories;

namespace ByteSync.Models.Comparisons.Result;

public class ContentRepartition
{
    public ContentRepartition(PathIdentity pathIdentity)
    {
        PathIdentity = pathIdentity;
            
        FingerPrintGroups = new Dictionary<ContentIdentityCore, HashSet<InventoryPart>>();
        LastWriteTimeGroups = new Dictionary<DateTime, List<InventoryPart>>();

        MissingInventories = new HashSet<Inventory>();
        MissingInventoryParts = new HashSet<InventoryPart>();
    }
        
    public PathIdentity PathIdentity { get; }

    public Dictionary<ContentIdentityCore, HashSet<InventoryPart>> FingerPrintGroups { get; private set; }

    public Dictionary<DateTime, List<InventoryPart>> LastWriteTimeGroups { get; private set; }

    public HashSet<Inventory> MissingInventories { get; set; }
        
    public HashSet<InventoryPart> MissingInventoryParts { get; set; }
}
