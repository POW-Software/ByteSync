using System.Collections.ObjectModel;
using ByteSync.Business.Actions.Local;
using ByteSync.Business.Comparisons;
using ByteSync.Interfaces.Services.Sessions;
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
        
        foreach (var inventory in Inventories)
        {
            if (inventory.InventoryParts.Count == 1)
            {
                // Single part inventory: use inventory code directly
                var dataPart = new DataPart(inventory.Code, inventory);
                DataPartsByNames.Add(dataPart.Name, dataPart);
            }
            else
            {
                // Multi-part inventory: use individual part codes
                foreach (var inventoryPart in inventory.InventoryParts)
                {
                    var dataPart = new DataPart(inventoryPart.Code, inventoryPart);
                    DataPartsByNames.Add(dataPart.Name, dataPart);
                }
            }
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
        
        if (DataPartsByNames.Count == 0)
        {
            return null;
        }
        
        var result = DataPartsByNames.GetValueOrDefault(dataPartName);

        if (result == null)
        {
            bool areAllSinglePartInventories = Inventories.All(i => i.InventoryParts.Count == 1);

            if (areAllSinglePartInventories)
            {
                if (IsSinglePartDataPartName(dataPartName))
                {
                    result = DataPartsByNames.GetValueOrDefault(dataPartName[0].ToString());
                }
            }
        }
        
        return result;
    }

    private bool IsSinglePartDataPartName(string dataPartName)
    {
        return dataPartName.Length == 2 && char.IsLetter(dataPartName[0]) && dataPartName[1] == '1';
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