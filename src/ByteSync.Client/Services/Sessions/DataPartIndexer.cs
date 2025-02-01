using System.Collections.ObjectModel;
using ByteSync.Business.Actions.Local;
using ByteSync.Business.Comparisons;
using ByteSync.Interfaces.Controls.Sessions;
using ByteSync.Models.Inventories;

namespace ByteSync.Services.Sessions;

public class DataPartIndexer : IDataPartIndexer
{
    public DataPartIndexer()
    {
        Inventories = new List<Inventory>();

        DataPartsByNames = new Dictionary<string, DataPart>();
    }

    private List<Inventory> Inventories { get; }
    
    private Dictionary<string, DataPart> DataPartsByNames { get; set; }

    public void BuildMap(List<Inventory> inventories)
    {
        Inventories.Clear();
        Inventories.AddAll(inventories);
        
        DataPartsByNames.Clear();
        
        var cptInventory = 0;
        foreach (var inventory in Inventories)
        {
            var inventoryLetter = ((char)('A' + cptInventory)).ToString();
            
            if (inventory.InventoryParts.Count == 1)
            {
                var dataPart = new DataPart(inventoryLetter, inventory);
                DataPartsByNames.Add(dataPart.Name, dataPart);
            }

            if (inventory.InventoryParts.Count > 1)
            {
                var cptPart = 1;
                foreach (var inventoryPart in inventory.InventoryParts)
                {
                    var name = $"{inventoryLetter}{cptPart}";

                    var dataPart = new DataPart(name, inventoryPart);
                    DataPartsByNames.Add(dataPart.Name, dataPart);
                    
                    cptPart += 1;
                }
            }

            cptInventory += 1;
        }
    }

    public ReadOnlyCollection<DataPart> GetAllDataParts()
    {
        return DataPartsByNames.Values.ToList().AsReadOnly();
    }

    public DataPart? GetDataPart(DataPart? dataPart)
    {
        return GetDataPart(dataPart?.Name);
    }
    
    public DataPart? GetDataPart(string? dataPartName)
    {
        if (dataPartName == null)
        {
            return null;
        }
        
        if (DataPartsByNames.TryGetValue(dataPartName, out var dataPart))
        {
            return dataPart;
        }
        else
        {
            return null;
        }
    }
    
    public void Remap(ICollection<SynchronizationRule> synchronizationRules)
    {
        foreach (var synchronizationRule in synchronizationRules)
        {
            foreach (var action in synchronizationRule.Actions)
            {
                action.Source = GetDataPart(action.Source);
                    
                action.Destination = GetDataPart(action.Destination);
            }
                
            foreach (var condition in synchronizationRule.Conditions)
            {
                condition.Source = GetDataPart(condition.Source)!;
                    
                condition.Destination = GetDataPart(condition.Destination);
            }
        }
    }
}