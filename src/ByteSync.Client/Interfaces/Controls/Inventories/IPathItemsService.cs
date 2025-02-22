using ByteSync.Business.PathItems;
using ByteSync.Common.Business.Inventories;

namespace ByteSync.Interfaces.Controls.Inventories;

public interface IPathItemsService
{
    Task TryAddPathItem(PathItem pathItem);
    
    Task CreateAndTryAddPathItem(string path, FileSystemTypes fileSystemType);

    public void ApplyAddPathItemLocally(PathItem pathItem);
    
    Task TryRemovePathItem(PathItem pathItem);

    public void ApplyRemovePathItemLocally(PathItem pathItem);
}