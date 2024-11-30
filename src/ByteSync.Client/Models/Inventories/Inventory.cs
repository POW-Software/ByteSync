using ByteSync.Common.Business.EndPoints;
using Newtonsoft.Json;

namespace ByteSync.Models.Inventories
{
    [JsonObject(IsReference = true)] 
    public class Inventory
    {
        public Inventory()
        {
            InventoryParts = new List<InventoryPart>();
        }
        
        public string InventoryId { get; set; } = null!;
        
        public ByteSyncEndpoint Endpoint { get; set; } = null!;
        
        public string Letter { get; set; } = null!;

        public DateTimeOffset StartDateTime { get; set; }
        
        public DateTimeOffset EndDateTime { get; set; }

        public List<InventoryPart> InventoryParts { get; set; }

        public string MachineName { get; set; }

        public void Add(InventoryPart inventoryPart)
        {
            InventoryParts.Add(inventoryPart);
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

        public override string ToString()
        {
#if DEBUG
            return $"Inventory {MachineName}, Parts : {InventoryParts.Count}";
#endif

#pragma warning disable 162
            return base.ToString();
#pragma warning restore 162
        }
    }
}
