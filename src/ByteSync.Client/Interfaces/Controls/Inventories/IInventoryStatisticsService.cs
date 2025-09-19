using ByteSync.Business.Inventories;

namespace ByteSync.Interfaces.Controls.Inventories;

public interface IInventoryStatisticsService
{
    IObservable<InventoryStatistics> Statistics { get; }
    
    Task Compute();
}