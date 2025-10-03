using ByteSync.Business.Actions.Shared;
using ByteSync.Common.Business.EndPoints;
using ByteSync.Common.Business.Inventories;
using ByteSync.Models.Inventories;

namespace ByteSync.Client.UnitTests.TestUtilities.Helpers;

public static class SharedDataPartTestFactory
{
    public static SharedDataPart Create(
        string name,
        FileSystemTypes inventoryPartType,
        string clientInstanceId,
        string inventoryCode,
        string rootPath,
        string? relativePath,
        string? signatureGuid,
        string? signatureHash,
        bool hasAnalysisError)
    {
        var endpoint = new ByteSyncEndpoint
        {
            ClientInstanceId = clientInstanceId
        };
        
        var inventory = new Inventory
        {
            InventoryId = $"INV_{inventoryCode}",
            Endpoint = endpoint,
            Code = inventoryCode,
            NodeId = $"N_{inventoryCode}",
            MachineName = "TestMachine"
        };
        
        var inventoryPart = new InventoryPart(inventory, rootPath, inventoryPartType)
        {
            Code = inventoryCode + "1"
        };
        
        return new SharedDataPart(name, inventory, inventoryPart, relativePath, signatureGuid, signatureHash, hasAnalysisError);
    }
}