using ByteSync.Business.Inventories;
using ByteSync.Models.Inventories;

namespace ByteSync.Models.Comparisons.Result;

public class ComparisonResult
{
    public ComparisonResult()
    {
        ComparisonItems = new HashSet<ComparisonItem>();
        ComparisonItemByPathIdentity = new Dictionary<PathIdentity, ComparisonItem>();

        Inventories = new List<Inventory>();
    }
        
    public HashSet<ComparisonItem> ComparisonItems { get; }
        
    private Dictionary<PathIdentity, ComparisonItem> ComparisonItemByPathIdentity { get; set; }

    public List<Inventory> Inventories { get; }
        
    public ComparisonItem? GetItemBy(PathIdentity pathIdentity)
    {
        ComparisonItem? comparisonItem;
        ComparisonItemByPathIdentity.TryGetValue(pathIdentity, out comparisonItem);

        return comparisonItem;
    }

    public void AddItem(ComparisonItem comparisonItem)
    {
        ComparisonItems.Add(comparisonItem);
        ComparisonItemByPathIdentity.Add(comparisonItem.PathIdentity, comparisonItem);

        comparisonItem.ComparisonResult = this;
    }

    public void AddInventory(Inventory inventory)
    {
        Inventories.Add(inventory);
    }

    public void Clear()
    {
        ComparisonItems.Clear();
        ComparisonItemByPathIdentity.Clear();
        Inventories.Clear();
    }
}