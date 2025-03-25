using ByteSync.Business;
using ByteSync.Interfaces.Controls.Inventories;
using ByteSync.Services.Inventories;

namespace ByteSync.Interfaces.Factories;

public interface IInventoryComparerFactory
{   
    IInventoryComparer CreateInventoryComparer(LocalInventoryModes localInventoryMode, InventoryIndexer? inventoryIndexer = null);
}