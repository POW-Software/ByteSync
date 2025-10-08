using ByteSync.Interfaces.Controls.Inventories;
using ByteSync.Interfaces.Factories;
using ByteSync.Services.Inventories;

namespace ByteSync.Factories;

public class InventorySaverFactory : IInventorySaverFactory
{
    public IInventorySaver Create(InventoryBuilder builder)
    {
        var saver = new InventorySaver();
        saver.Initialize(() => builder.Inventory);
        
        return saver;
    }
}