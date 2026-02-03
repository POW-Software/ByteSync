using System.IO;
using ByteSync.Common.Business.Misc;

namespace ByteSync.Interfaces.Controls.Inventories;

public interface IFileSystemInspector
{
    bool IsHidden(FileSystemInfo fsi, OSPlatforms os);
    bool IsSystemAttribute(FileInfo fileInfo);
    bool IsNoiseFileName(FileInfo fileInfo, OSPlatforms os);
    bool IsReparsePoint(FileSystemInfo fsi);
    bool Exists(FileInfo fileInfo);
    bool IsOffline(FileInfo fileInfo);
    bool IsRecallOnDataAccess(FileInfo fileInfo);
}
