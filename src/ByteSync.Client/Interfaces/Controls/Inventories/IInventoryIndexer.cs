using System.IO;
using ByteSync.Business.Inventories;
using ByteSync.Models.FileSystems;

namespace ByteSync.Interfaces.Controls.Inventories;

public interface IInventoryIndexer
{
    void Reset();
    
    void Register(FileDescription fileDescription, FileInfo fileInfo);
    
    void Register(DirectoryDescription directoryDescription, DirectoryInfo directoryInfo);
    
    void Register(FileSystemDescription fileSystemDescription, PathIdentity pathIdentity);
    
    HashSet<IndexedItem>? GetItemsBy(PathIdentity pathIdentity);
}