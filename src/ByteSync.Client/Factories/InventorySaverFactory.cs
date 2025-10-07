using ByteSync.Interfaces.Controls.Inventories;
using ByteSync.Interfaces.Factories;
using ByteSync.Services.Inventories;

namespace ByteSync.Factories;

public class InventorySaverFactory : IInventorySaverFactory
{
    public IInventorySaver Create(InventoryBuilder builder)
    {
        return new InventorySaver(builder);
    }
}