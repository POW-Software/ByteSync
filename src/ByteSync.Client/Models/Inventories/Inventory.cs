using ByteSync.Common.Business.EndPoints;

namespace ByteSync.Models.Inventories;

public class Inventory
{
    public Inventory()
    {
        InventoryParts = new List<InventoryPart>();
    }
        
    public string InventoryId { get; set; } = null!;
        
    public ByteSyncEndpoint Endpoint { get; set; } = null!;
        
    public string Code { get; set; } = null!;
    
    public string NodeId { get; set; }

    public DateTimeOffset StartDateTime { get; set; }
        
    public DateTimeOffset EndDateTime { get; set; }

    public List<InventoryPart> InventoryParts { get; set; }

    public string MachineName { get; set; }
    
    public string CodeAndNodeId => $"{Code}_{NodeId}";

    public void Add(InventoryPart inventoryPart)
    {
        InventoryParts.Add(inventoryPart);
        inventoryPart.Inventory = this;
    }

    protected bool Equals(Inventory other)
    {
        return Equals(Endpoint, other.Endpoint) && InventoryId == other.InventoryId;
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((Inventory) obj);
    }

    public override int GetHashCode()
    {
        return InventoryId.GetHashCode();
    }
}