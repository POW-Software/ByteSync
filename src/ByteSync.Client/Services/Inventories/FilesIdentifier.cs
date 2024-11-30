using ByteSync.Business.Inventories;
using ByteSync.Business.Sessions;
using ByteSync.Common.Helpers;
using ByteSync.Models.Comparisons.Result;
using ByteSync.Models.Inventories;

namespace ByteSync.Services.Inventories;

public class FilesIdentifier
{
    public FilesIdentifier(Inventory inventory, SessionSettings sessionSettings, InventoryIndexer inventoryIndexer)
    {
        Inventory = inventory;
        SessionSettings = sessionSettings;
        InventoryIndexer = inventoryIndexer;
    }
    
    internal Inventory Inventory { get; }
    
    internal SessionSettings SessionSettings { get; }
    
    internal InventoryIndexer InventoryIndexer { get; }

    public HashSet<IndexedItem> Identify(ComparisonResult comparisonResult)
    {
        // On récupère tous les ComparisonItems qui sont en lien avec l'inventaire
        var comparisonItems = comparisonResult.ComparisonItems
            .Where(item => item.ContentIdentities
                .Any(ident => ident.InventoryPartsByLastWriteTimes.Any(
                    pair => pair.Value.Any(
                        ip => ip.Inventory.Equals(Inventory)))));

        HashSet<ComparisonItem> comparisonItemsToAnalyse = new HashSet<ComparisonItem>();

        // On doit trouver tous les comparisonItems pour lesquels il y a plusieurs valeurs de date et/ou de taille
        foreach (var comparisonItem in comparisonItems)
        {
            // Ceci est commun au mode "standard" et au mode "checksum"
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
                // Spécifique au mode CheckSum
                
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
        // On dispose désormais des comparisonsItems
        foreach (var comparisonItem in comparisonItemsToAnalyse)
        {
            var items = InventoryIndexer.GetItemsBy(comparisonItem.PathIdentity)!;

            result.AddAll(items);
        }

        return result;
    }
}