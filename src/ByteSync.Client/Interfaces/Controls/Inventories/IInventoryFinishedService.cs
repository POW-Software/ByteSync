using ByteSync.Business;
using ByteSync.Models.Inventories;

namespace ByteSync.Interfaces.Controls.Inventories;

public interface IInventoryFinishedService
{
    Task SetLocalInventoryFinished(List<Inventory> inventories, LocalInventoryModes localInventoryMode);
}