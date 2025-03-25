using ByteSync.Business.Inventories;
using ByteSync.Models.Comparisons.Result;

namespace ByteSync.Interfaces.Controls.Inventories;

public interface IInventoryComparer : IDisposable
{
    void AddInventories(ICollection<InventoryFile> inventoriesFiles);

    ComparisonResult Compare();
}