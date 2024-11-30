using ByteSync.Models.Inventories;

namespace ByteSync.Business.Comparisons
{
    public class DataPart
    {
        public DataPart(string name)
        {
            Name = name;
            IsVirtual = true;
        }

        public DataPart(string name, Inventory inventory)
        {
            Name = name;
            Inventory = inventory;
            IsVirtual = false;
        }

        public DataPart(string name, InventoryPart inventoryPart)
        {
            Name = name;
            InventoryPart = inventoryPart;
            IsVirtual = false;
        }

        public string Name { get; }

        /// <summary>
        /// Inventory, peut être null
        /// </summary>
        public Inventory? Inventory { get; }

        /// <summary>
        /// InventoryPart, peut être null
        /// </summary>
        public InventoryPart? InventoryPart { get; }
        
        /// <summary>
        /// Indique si le DataPart est Virtual, ce qui est un état transitoire pour les AtomicConditions personnalisées
        /// </summary>
        public bool IsVirtual { get; }

        public Inventory GetAppliableInventory()
        {
            if (Inventory != null)
            {
                return Inventory;
            }
            else
            {
                return InventoryPart?.Inventory!;
            }
        }

        public InventoryPart GetAppliableInventoryPart()
        {
            if (InventoryPart != null)
            {
                return InventoryPart;
            }
            else
            {
                return Inventory?.InventoryParts.SingleOrDefault()!;
            }
        }

        protected bool Equals(DataPart other)
        {
            return Name == other.Name && Equals(Inventory, other.Inventory) && Equals(InventoryPart, other.InventoryPart);
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((DataPart) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Name.GetHashCode();
                hashCode = (hashCode * 397) ^ (Inventory != null ? Inventory.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (InventoryPart != null ? InventoryPart.GetHashCode() : 0);
                return hashCode;
            }
        }
    }
}
