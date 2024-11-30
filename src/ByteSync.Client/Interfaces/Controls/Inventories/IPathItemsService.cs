using System.Threading.Tasks;
using ByteSync.Business.PathItems;
using ByteSync.Common.Business.Inventories;

namespace ByteSync.Interfaces.Controls.Inventories;

public interface IPathItemsService
{
    // public IObservableCache<PathItem, string> AllPathItems { get; }
    
    // public IObservableCache<PathItem, string> CurrentMemberPathItems { get; }

    Task AddPathItem(PathItem pathItem);
    
    Task CreateAndAddPathItem(string path, FileSystemTypes fileSystemType);
    
    Task RemovePathItem(PathItem pathItem);
}