using System.IO;
using ByteSync.Models.Inventories;

namespace ByteSync.Interfaces.Controls.Inventories;

public interface IInventorySaver
{
    void Start(string inventoryFullName);
    void AddSignature(string guid, MemoryStream memoryStream);
    void WriteInventory(Inventory inventory);
    void Stop();
}