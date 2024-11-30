using ByteSync.Business.Inventories;
using ByteSync.Common.Business.Inventories;
using ByteSync.Models.Inventories;

namespace ByteSync.Models.Comparisons.Result
{
    public class Status
    {
        public Status(PathIdentity pathIdentity)
        {
            PathIdentity = pathIdentity;
            
            FingerPrintGroups = new Dictionary<ContentIdentityCore, HashSet<InventoryPart>>();
            LastWriteTimeGroups = new Dictionary<DateTime, HashSet<InventoryPart>>();

            MissingInventories = new HashSet<Inventory>();
            MissingInventoryParts = new HashSet<InventoryPart>();
        }
        
        public PathIdentity PathIdentity { get; }

        public Dictionary<ContentIdentityCore, HashSet<InventoryPart>> FingerPrintGroups { get; private set; }

        public Dictionary<DateTime, HashSet<InventoryPart>> LastWriteTimeGroups { get; private set; }

        public HashSet<Inventory> MissingInventories { get; set; }
        
        public HashSet<InventoryPart> MissingInventoryParts { get; set; }
        
        public bool IsSuccessStatus { get; set; }
        
        public bool IsErrorStatus { get; set; }

        public bool IsOK
        {
            get
            {
                if (PathIdentity.FileSystemType == FileSystemTypes.File)
                {
                    return FingerPrintGroups.Count == 1 && LastWriteTimeGroups.Count == 1 && MissingInventories.Count == 0
                           && MissingInventoryParts.Count(ip => ip.InventoryPartType == FileSystemTypes.Directory) == 0;
                }
                else
                {
                    return MissingInventories.Count == 0
                           && MissingInventoryParts.Count(ip => ip.InventoryPartType == FileSystemTypes.Directory) == 0;
                }
            }
        }
    }
}
