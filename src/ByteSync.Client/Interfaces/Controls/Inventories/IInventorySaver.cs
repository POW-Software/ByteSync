using System.IO;

namespace ByteSync.Interfaces.Controls.Inventories;

public interface IInventorySaver
{
    void Start(string inventoryFullName);
    void AddSignature(string guid, MemoryStream memoryStream);
    void WriteInventory();
    void Stop();
}