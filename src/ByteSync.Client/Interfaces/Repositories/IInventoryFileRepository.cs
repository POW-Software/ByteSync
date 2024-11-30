using ByteSync.Business;
using ByteSync.Business.Inventories;

namespace ByteSync.Interfaces.Repositories;

public interface IInventoryFileRepository : IBaseSourceCacheRepository<InventoryFile, string>
{
    List<InventoryFile> GetAllInventoriesFiles(LocalInventoryModes localInventoryMode);
}