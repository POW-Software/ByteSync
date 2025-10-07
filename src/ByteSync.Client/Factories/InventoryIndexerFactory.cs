using ByteSync.Interfaces.Factories;
using ByteSync.Services.Inventories;

namespace ByteSync.Factories;

public class InventoryIndexerFactory : IInventoryIndexerFactory
{
    public InventoryIndexer Create()
    {
        return new InventoryIndexer();
    }
}