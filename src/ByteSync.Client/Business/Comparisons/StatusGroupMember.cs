using ByteSync.Models.Inventories;

namespace ByteSync.Business.Comparisons;

internal class StatusGroupMember
{
    public string Letter { get; set; }
    public bool IsMissing { get; set; }
    public Inventory Inventory { get; set; }
    public InventoryPart InventoryPart { get; set; }
    public object Link { get; set; }
}