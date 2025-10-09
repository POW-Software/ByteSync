using ByteSync.Business;
using ByteSync.Interfaces.Controls.Inventories;

namespace ByteSync.Interfaces.Factories;

public interface IInventoryComparerFactory
{
    IInventoryComparer CreateInventoryComparer(LocalInventoryModes localInventoryMode, IInventoryIndexer? inventoryIndexer = null);
}