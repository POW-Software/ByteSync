using ByteSync.Common.Business.Inventories;
using ByteSync.Interfaces.Controls.Inventories;
using ByteSync.Models.Comparisons.Result;
using ByteSync.Models.Inventories;
using ByteSync.Services.Inventories;

namespace ByteSync.Services.Comparisons;

public class InitialStatusBuilder : IInitialStatusBuilder
{
    public InitialStatusBuilder()
    {
        NonFoundInventories = new HashSet<Inventory>();
        NonFoundInventoryParts = new HashSet<InventoryPart>();
    }
    
    HashSet<Inventory> NonFoundInventories { get; set; }
    HashSet<InventoryPart> NonFoundInventoryParts { get; set; }
    
    public void BuildStatus(ComparisonItem comparisonItem, List<InventoryLoader> inventoryLoaders)
    {
        NonFoundInventories = new HashSet<Inventory>();
        NonFoundInventoryParts = new HashSet<InventoryPart>();
        
        foreach (var inventoryLoader in inventoryLoaders.OrderBy(il => il.Inventory.Letter))
        {
            NonFoundInventories.Add(inventoryLoader.Inventory);
            foreach (var inventoryPart in inventoryLoader.Inventory.InventoryParts)
            {
                NonFoundInventoryParts.Add(inventoryPart);
            }
        }

        if (comparisonItem.FileSystemType == FileSystemTypes.File)
        {
            BuildInitialStatusForFile(comparisonItem);
        }
        else
        {
            BuildInitialStatusForDirectory(comparisonItem);
        }
        
        // NonFoundInventoryParts.Clear();
        
        comparisonItem.Status.MissingInventories.AddAll(NonFoundInventories);
        comparisonItem.Status.MissingInventoryParts.AddAll(NonFoundInventoryParts);
    }

    private void BuildInitialStatusForFile(ComparisonItem comparisonItem)
    {
        foreach (var contentIdentity in comparisonItem.ContentIdentities)
        {
            if (comparisonItem.FileSystemType == FileSystemTypes.File)
            {
                if (!comparisonItem.Status.FingerPrintGroups.ContainsKey(contentIdentity.Core))
                {
                    comparisonItem.Status.FingerPrintGroups.Add(contentIdentity.Core, new HashSet<InventoryPart>());
                }
            }

            foreach (KeyValuePair<DateTime, HashSet<InventoryPart>> pair in contentIdentity.InventoryPartsByLastWriteTimes)
            {
                comparisonItem.Status.FingerPrintGroups[contentIdentity.Core].AddAll(pair.Value);

                foreach (var inventoryPart in pair.Value)
                {
                    NonFoundInventories.Remove(inventoryPart.Inventory);
                    NonFoundInventoryParts.Remove(inventoryPart);
                }
            }
            
            foreach (var pair in contentIdentity.InventoryPartsByLastWriteTimes)
            {
                if (!comparisonItem.Status.LastWriteTimeGroups.ContainsKey(pair.Key))
                {
                    comparisonItem.Status.LastWriteTimeGroups.Add(pair.Key, new HashSet<InventoryPart>());
                }

                comparisonItem.Status.LastWriteTimeGroups[pair.Key].AddAll(pair.Value);

                foreach (var inventoryPart in pair.Value)
                {
                    NonFoundInventories.Remove(inventoryPart.Inventory);
                    NonFoundInventoryParts.Remove(inventoryPart);
                }
            }
        }
    }

    private void BuildInitialStatusForDirectory(ComparisonItem comparisonItem)
    {
        HashSet<Inventory> inventoriesOK = new HashSet<Inventory>();
        HashSet<InventoryPart> inventoryPartsOK = new HashSet<InventoryPart>();
        
        foreach (var contentIdentity in comparisonItem.ContentIdentities)
        {
            inventoriesOK.AddAll(contentIdentity.GetInventories());
            inventoryPartsOK.AddAll(contentIdentity.GetInventoryParts());
        }

        NonFoundInventories.RemoveAll(inventoriesOK);
        NonFoundInventoryParts.RemoveAll(inventoryPartsOK);
    }

    public void Dispose()
    {
        NonFoundInventories = new HashSet<Inventory>();
        NonFoundInventoryParts = new HashSet<InventoryPart>();
    }
}