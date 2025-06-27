using ByteSync.Interfaces.Controls.Inventories;

namespace ByteSync.Interfaces.Factories;

public interface IInventoryBuilderFactory
{
    IInventoryBuilder CreateInventoryBuilder();
}