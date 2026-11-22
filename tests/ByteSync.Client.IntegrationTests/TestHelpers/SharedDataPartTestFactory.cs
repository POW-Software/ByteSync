using ByteSync.Business.Actions.Shared;
using ByteSync.Common.Business.EndPoints;
using ByteSync.Common.Business.Inventories;
using ByteSync.Models.Inventories;

namespace ByteSync.Client.IntegrationTests.TestHelpers;

public static class SharedDataPartTestFactory
{
    private static string? Normalize(string? path) => path == null ? null : "/" + string.Join("/", path.Replace('\\', '/').Split('/', StringSplitOptions.RemoveEmptyEntries));
    public static SharedDataPart Create(
        string name,
        Inventory inventory,
        string overrideClientInstanceId,
        FileSystemTypes inventoryPartType,
        string rootPath,
        string? relativePath,
        string? signatureGuid,
        string? signatureHash,
        bool hasAnalysisError)
    {
        var inventoryPart = new InventoryPart(inventory, rootPath, inventoryPartType)
        {
            SourceId = inventory.InventoryParts.FirstOrDefault(p => p.RootPath == rootPath)?.SourceId ?? Guid.NewGuid().ToString(),
            Code = inventory.Code + "1"
        };

        var part = new SharedDataPart(name, inventory, inventoryPart, Normalize(relativePath), signatureGuid, signatureHash, hasAnalysisError);
        part.ClientInstanceId = overrideClientInstanceId;
        return part;
    }

    public static SharedDataPart Create(
        string name,
        Inventory inventory,
        FileSystemTypes inventoryPartType,
        string rootPath,
        string? relativePath,
        string? signatureGuid,
        string? signatureHash,
        bool hasAnalysisError)
    {
        var inventoryPart = new InventoryPart(inventory, rootPath, inventoryPartType)
        {
            SourceId = inventory.InventoryParts.FirstOrDefault(p => p.RootPath == rootPath)?.SourceId ?? Guid.NewGuid().ToString(),
            Code = inventory.Code + "1"
        };

        return new SharedDataPart(name, inventory, inventoryPart, Normalize(relativePath), signatureGuid, signatureHash, hasAnalysisError);
    }

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
            SourceId = Guid.NewGuid().ToString(),
            Code = inventoryCode + "1"
        };

        return new SharedDataPart(name, inventory, inventoryPart, Normalize(relativePath), signatureGuid, signatureHash, hasAnalysisError);
    }
}


