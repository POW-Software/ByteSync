﻿using System.Reactive.Linq;
using ByteSync.Business.Inventories;
using ByteSync.Common.Helpers;
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

    public IObservable<(long IdentifiedSize, long ProcessedSize)> GetDataObservable()
    {
        var inventoryProcessData = _inventoryService.InventoryProcessData;
                
        var source = inventoryProcessData.InventoryMonitorObservable.CombineLatest(inventoryProcessData.IdentificationStatus);

        Func<(InventoryMonitorData, LocalInventoryPartStatus), bool> canSkip =
            tuple =>
            {
                var inventoryMonitorData = tuple.Item1;
                var localInventoryPartStatus = tuple.Item2;

                return inventoryMonitorData.HasNonZeroProperty() &&
                       localInventoryPartStatus.In(LocalInventoryPartStatus.Running);
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
            (tuple.Item1.IdentifiedSize, tuple.Item1.ProcessedSize));
    }
}