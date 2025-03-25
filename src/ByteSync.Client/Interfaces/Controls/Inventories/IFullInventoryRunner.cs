namespace ByteSync.Interfaces.Controls.Inventories;

public interface IFullInventoryRunner
{
    Task<bool> RunFullInventory();
}