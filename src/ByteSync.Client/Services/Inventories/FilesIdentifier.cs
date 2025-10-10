using ByteSync.Business.Inventories;
using ByteSync.Business.Sessions;
using ByteSync.Interfaces.Controls.Inventories;
using ByteSync.Models.Comparisons.Result;
using ByteSync.Models.Inventories;

namespace ByteSync.Services.Inventories;

public class FilesIdentifier
{
    public FilesIdentifier(Inventory inventory, SessionSettings sessionSettings, IInventoryIndexer inventoryIndexer)
    {
        Inventory = inventory;
        SessionSettings = sessionSettings;
        InventoryIndexer = inventoryIndexer;
    }
    
    internal Inventory Inventory { get; }
    
    internal SessionSettings SessionSettings { get; }
    
    internal IInventoryIndexer InventoryIndexer { get; }
    
    public HashSet<IndexedItem> Identify(ComparisonResult comparisonResult)
    {
        var comparisonItems = comparisonResult.ComparisonItems
            .Where(item => item.ContentIdentities
                .Any(ident => ident.InventoryPartsByLastWriteTimes.Any(pair => pair.Value.Any(ip => ip.Inventory.Equals(Inventory)))));
        
        var comparisonItemsToAnalyse = new HashSet<ComparisonItem>();
        
        foreach (var comparisonItem in comparisonItems)
        {
            ContentIdentity? contentIdentity = null;
            if (comparisonItem.ContentIdentities.Count > 1)
            {
                comparisonItemsToAnalyse.Add(comparisonItem);
            }
            else if (comparisonItem.ContentIdentities.Count == 1)
            {
                contentIdentity = comparisonItem.ContentIdentities.Single();
                if (contentIdentity.InventoryPartsByLastWriteTimes.Count > 1)
                {
                    comparisonItemsToAnalyse.Add(comparisonItem);
                }
            }
            
            if (SessionSettings.AnalysisMode == AnalysisModes.Checksum)
            {
                if (contentIdentity != null)
                {
                    var sum = 0;
                    foreach (var pair in contentIdentity.InventoryPartsByLastWriteTimes)
                    {
                        sum += pair.Value.Count;
                    }
                    
                    if (sum > 1)
                    {
                        comparisonItemsToAnalyse.Add(comparisonItem);
                    }
                }
            }
        }
        
        var result = new HashSet<IndexedItem>();
        foreach (var comparisonItem in comparisonItemsToAnalyse)
        {
            var items = InventoryIndexer.GetItemsBy(comparisonItem.PathIdentity)!;
            
            result.AddAll(items);
        }
        
        return result;
    }
}