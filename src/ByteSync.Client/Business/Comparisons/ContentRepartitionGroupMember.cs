using ByteSync.Models.Inventories;

namespace ByteSync.Business.Comparisons;

internal class ContentRepartitionGroupMember
{
    public required string Letter { get; init; }
    
    public bool IsMissing { get; init; }
    
    public Inventory? Inventory { get; set; }
    
    public InventoryPart? InventoryPart { get; set; }
    
    public object? Link { get; init; }
}