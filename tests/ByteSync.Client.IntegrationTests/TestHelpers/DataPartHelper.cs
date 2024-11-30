using ByteSync.Business.Comparisons;
using ByteSync.Client.IntegrationTests.TestHelpers.Business;
using ByteSync.Models.Inventories;

namespace ByteSync.Client.IntegrationTests.TestHelpers;

internal static class DataPartHelper
{
    internal static DataPart GetSingleDataPart(this InventoryData inventoryData)
    {
        var result = GetDataPart(inventoryData.InventoryParts.Single());
        
        return result;
    }
    
    internal static DataPart GetDataPart(this InventoryData inventoryData, string name)
    {
        var result = GetDataPart(inventoryData.InventoryParts.Single(ip => ip.RootPath.EndsWith(name)));
    
        return result;
    }

    internal static DataPart GetSingleDataPart(this Inventory inventory)
    {
        return GetDataPart(inventory.InventoryParts.Single());
    }

    internal static DataPart GetDataPart(this InventoryPart inventoryPart)
    {
        var dataPart = new DataPart(inventoryPart.Code, inventoryPart);

        return dataPart;
    }
}