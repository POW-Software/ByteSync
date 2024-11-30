using System.IO;
using ByteSync.Models.FileSystems;

namespace ByteSync.Business.Inventories;

public class IndexedItem
{
    public IndexedItem(FileSystemDescription fileSystemDescription, FileSystemInfo fileSystemInfo)
    {
        FileSystemDescription = fileSystemDescription;
        FileSystemInfo = fileSystemInfo;
    }

    public FileSystemDescription FileSystemDescription { get; set; }
        
    public FileSystemInfo FileSystemInfo { get; set; }
}