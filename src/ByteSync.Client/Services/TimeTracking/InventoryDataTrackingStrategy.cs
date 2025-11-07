using System.Reactive.Linq;
using ByteSync.Business.Inventories;
using ByteSync.Interfaces.Controls.Inventories;
using ByteSync.Interfaces.Controls.TimeTracking;

namespace ByteSync.Services.TimeTracking;

public class InventoryDataTrackingStrategy : IDataTrackingStrategy
{
    private readonly IInventoryService _inventoryService;
    
    public InventoryDataTrackingStrategy(IInventoryService inventoryService)
    {
        _inventoryService = inventoryService;
    }
    
    public IObservable<(long IdentifiedVolume, long ProcessedVolume)> GetDataObservable()
    {
        var inventoryProcessData = _inventoryService.InventoryProcessData;

        var source = inventoryProcessData.InventoryMonitorObservable.CombineLatest(inventoryProcessData.IdentificationStatus);
        
        Func<(InventoryMonitorData, InventoryTaskStatus), bool> canSkip =
            tuple =>
            {
                var inventoryMonitorData = tuple.Item1;
                var localInventoryPartStatus = tuple.Item2;
                
                return inventoryMonitorData.HasNonZeroProperty() &&
                       localInventoryPartStatus.In(InventoryTaskStatus.Running);
            };
        
        // Share the source so that it's not subscribed multiple times
        var sharedSource = source.Publish().RefCount();
        
        // Sample the source observable every 0.5 seconds, but only for values that can be skipped
        var sampled = sharedSource
            .Where(canSkip)
            .Sample(TimeSpan.FromSeconds(0.5));
        
        // Get the values from the shared source that can not be skipped
        var notSkipped = sharedSource
            .Where(value => !canSkip(value));
        
        // Merge the sampled and notSkipped sequences
        var merged = sampled.Merge(notSkipped);
        
        return merged.Select(tuple =>
        {
            var monitor = tuple.Item1;
            var total = monitor.AnalyzableVolume + monitor.UploadTotalVolume;
            var done = monitor.ProcessedVolume + monitor.UploadedVolume;
            return (total, done);
        });
    }
}
