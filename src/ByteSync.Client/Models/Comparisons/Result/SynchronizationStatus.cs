namespace ByteSync.Models.Comparisons.Result;

public class SynchronizationStatus
{
    public bool IsPendingStatus { get; set; }
    
    public bool IsSuccessStatus { get; set; }
    
    public bool IsErrorStatus { get; set; }
    
    // public bool IsOK
    // {
    //     get
    //     {
    //         if (PathIdentity.FileSystemType == FileSystemTypes.File)
    //         {
    //             return FingerPrintGroups.Count == 1 && LastWriteTimeGroups.Count == 1 && MissingInventories.Count == 0
    //                    && MissingInventoryParts.Count(ip => ip.InventoryPartType == FileSystemTypes.Directory) == 0;
    //         }
    //         else
    //         {
    //             return MissingInventories.Count == 0
    //                    && MissingInventoryParts.Count(ip => ip.InventoryPartType == FileSystemTypes.Directory) == 0;
    //         }
    //     }
    // }
}