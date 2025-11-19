using System.IO;
using ByteSync.Common.Business.Misc;
using ByteSync.Interfaces.Controls.Inventories;

namespace ByteSync.Services.Inventories;

public class FileSystemInspector : IFileSystemInspector
{
    private const int FILE_ATTRIBUTE_RECALL_ON_DATA_ACCESS = 4194304;
    
    public bool IsHidden(FileSystemInfo fsi, OSPlatforms os)
    {
        var isHidden = (fsi.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden;
        var isDot = os == OSPlatforms.Linux && fsi.Name.StartsWith('.');
        
        return isHidden || isDot;
    }
    
    public bool IsSystem(FileInfo fileInfo)
    {
        var isCommon = fileInfo.Name.In("desktop.ini", "thumbs.db", ".desktop.ini", ".thumbs.db", ".DS_Store");
        var isSystem = (fileInfo.Attributes & FileAttributes.System) == FileAttributes.System;
        
        return isCommon || isSystem;
    }
    
    public bool IsReparsePoint(FileSystemInfo fsi)
    {
        return (fsi.Attributes & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint;
    }
    
    public bool Exists(FileInfo fileInfo)
    {
        return fileInfo.Exists;
    }
    
    public bool IsOffline(FileInfo fileInfo)
    {
        return (fileInfo.Attributes & FileAttributes.Offline) == FileAttributes.Offline;
    }
    
    public bool IsRecallOnDataAccess(FileInfo fileInfo)
    {
        return (((int)fileInfo.Attributes) & FILE_ATTRIBUTE_RECALL_ON_DATA_ACCESS) == FILE_ATTRIBUTE_RECALL_ON_DATA_ACCESS;
    }
}