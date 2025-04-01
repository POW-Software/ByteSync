using ByteSync.Models.Comparisons.Result;
using ByteSync.Models.Inventories;

namespace ByteSync.Interfaces.Controls.Inventories;

public interface IInitialStatusBuilder : IDisposable
{
    void BuildStatus(ComparisonItem comparisonItem, IEnumerable<Inventory> inventories);
}