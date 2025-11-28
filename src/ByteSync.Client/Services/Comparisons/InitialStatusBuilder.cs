using ByteSync.Common.Business.Inventories;
using ByteSync.Interfaces.Controls.Inventories;
using ByteSync.Models.Comparisons.Result;
using ByteSync.Models.Inventories;

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
    
    public void BuildStatus(ComparisonItem comparisonItem, IEnumerable<Inventory> inventories)
    {
        NonFoundInventories = new HashSet<Inventory>();
        NonFoundInventoryParts = new HashSet<InventoryPart>();
        
        foreach (var inventory in inventories.OrderBy(i => i.Code))
        {
            NonFoundInventories.Add(inventory);
            foreach (var inventoryPart in inventory.InventoryParts)
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
        
        comparisonItem.ContentRepartition.MissingInventories.AddAll(NonFoundInventories);
        comparisonItem.ContentRepartition.MissingInventoryParts.AddAll(NonFoundInventoryParts);
    }

    private void BuildInitialStatusForFile(ComparisonItem comparisonItem)
    {
        foreach (var contentIdentity in comparisonItem.ContentIdentities)
        {
            if (comparisonItem.FileSystemType == FileSystemTypes.File)
            {
                if (!comparisonItem.ContentRepartition.FingerPrintGroups.ContainsKey(contentIdentity.Core!))
                {
                    comparisonItem.ContentRepartition.FingerPrintGroups.Add(contentIdentity.Core!, new HashSet<InventoryPart>());
                }
            }

            foreach (var pair in contentIdentity.InventoryPartsByLastWriteTimes)
            {
                comparisonItem.ContentRepartition.FingerPrintGroups[contentIdentity.Core!].AddAll(pair.Value);

                foreach (var inventoryPart in pair.Value)
                {
                    NonFoundInventories.Remove(inventoryPart.Inventory);
                    NonFoundInventoryParts.Remove(inventoryPart);
                }
            }
            
            foreach (var pair in contentIdentity.InventoryPartsByLastWriteTimes)
            {
                if (!comparisonItem.ContentRepartition.LastWriteTimeGroups.ContainsKey(pair.Key))
                {
                    comparisonItem.ContentRepartition.LastWriteTimeGroups.Add(pair.Key, new HashSet<InventoryPart>());
                }

                comparisonItem.ContentRepartition.LastWriteTimeGroups[pair.Key].AddAll(pair.Value);

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
        var inventoriesOK = new HashSet<Inventory>();
        var inventoryPartsOK = new HashSet<InventoryPart>();
        
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