using ByteSync.Services.Inventories;

namespace ByteSync.Interfaces.Factories;

public interface IInventoryIndexerFactory
{
    InventoryIndexer Create();
}