using System.IO;
using ByteSync.Business.Inventories;
using ByteSync.Common.Business.Misc;

namespace ByteSync.Interfaces.Controls.Inventories;

public interface IFileSystemInspector
{
    FileSystemEntryKind ClassifyEntry(FileSystemInfo fsi);
    
    bool IsHidden(FileSystemInfo fsi, OSPlatforms os);
    
    bool IsSystemAttribute(FileInfo fileInfo);
    
    bool IsNoiseEntryName(string? entryName, OSPlatforms os);
    
    bool IsNoiseFileName(FileInfo fileInfo, OSPlatforms os);
    
    bool IsReparsePoint(FileSystemInfo fsi);
    
    bool Exists(FileInfo fileInfo);
    
    bool IsOffline(FileInfo fileInfo);
    
    bool IsRecallOnDataAccess(FileInfo fileInfo);
}
