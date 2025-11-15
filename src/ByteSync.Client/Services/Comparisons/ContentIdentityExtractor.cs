using ByteSync.Business.Comparisons;
using ByteSync.Models.Comparisons.Result;

namespace ByteSync.Services.Comparisons;

public class ContentIdentityExtractor
{
    public ContentIdentity? ExtractContentIdentity(DataPart? dataPart, ComparisonItem comparisonItem)
    {
        if (dataPart == null)
        {
            return null;
        }
        
        return LocalizeContentIdentity(dataPart, comparisonItem);
    }
    
    public long? ExtractSize(DataPart dataPart, ComparisonItem comparisonItem)
    {
        var contentIdentity = LocalizeContentIdentity(dataPart, comparisonItem);
        
        return contentIdentity?.Core?.Size;
    }
    
    public DateTime? ExtractDate(DataPart dataPart, ComparisonItem comparisonItem)
    {
        var contentIdentity = LocalizeContentIdentity(dataPart, comparisonItem);
        
        if (contentIdentity != null)
        {
            foreach (var pair in contentIdentity.InventoryPartsByLastWriteTimes)
            {
                if (pair.Value.Contains(dataPart.GetApplicableInventoryPart()))
                {
                    return pair.Key;
                }
            }
        }
        
        return null;
    }
    
    public ContentIdentity? LocalizeContentIdentity(DataPart dataPart, ComparisonItem comparisonItem)
    {
        if (dataPart.Inventory != null)
        {
            foreach (var contentIdentity in comparisonItem.ContentIdentities
                .Where(ci => ci.GetInventories().Contains(dataPart.Inventory)))
            {
                return contentIdentity;
            }
        }
        else if (dataPart.InventoryPart != null)
        {
            foreach (var contentIdentity in comparisonItem.ContentIdentities)
            {
                var inventoryParts = contentIdentity.GetInventoryParts();
                
                if (inventoryParts.Contains(dataPart.InventoryPart))
                {
                    return contentIdentity;
                }
            }
        }
        
        return null;
    }
    
    public bool ExistsOn(DataPart? dataPart, ComparisonItem comparisonItem)
    {
        if (dataPart == null)
        {
            return false;
        }
        
        var contentIdentity = LocalizeContentIdentity(dataPart, comparisonItem);
        
        return contentIdentity != null;
    }
}