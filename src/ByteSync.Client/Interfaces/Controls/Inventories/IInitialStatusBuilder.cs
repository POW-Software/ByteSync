using ByteSync.Models.Comparisons.Result;
using ByteSync.Services.Inventories;

namespace ByteSync.Interfaces.Controls.Inventories;

public interface IInitialStatusBuilder : IDisposable
{
    void BuildStatus(ComparisonItem comparisonItem, List<InventoryLoader> inventoryLoaders);
}