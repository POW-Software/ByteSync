using ByteSync.Business.PathItems;
using ByteSync.Common.Business.Inventories;

namespace ByteSync.Interfaces.Controls.Inventories;

public interface IPathItemsService
{
    Task AddPathItem(PathItem pathItem);
    
    Task CreateAndAddPathItem(string path, FileSystemTypes fileSystemType);
    
    Task RemovePathItem(PathItem pathItem);
}