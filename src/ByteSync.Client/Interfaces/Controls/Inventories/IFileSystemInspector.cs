using System.IO;
using ByteSync.Common.Business.Misc;

namespace ByteSync.Interfaces.Controls.Inventories;

public interface IFileSystemInspector
{
    bool IsHidden(FileSystemInfo fsi, OSPlatforms os);
    bool IsSystem(FileInfo fileInfo);
    bool IsReparsePoint(FileSystemInfo fsi);
    bool Exists(FileInfo fileInfo);
    bool IsOffline(FileInfo fileInfo);
    bool IsRecallOnDataAccess(FileInfo fileInfo);
}