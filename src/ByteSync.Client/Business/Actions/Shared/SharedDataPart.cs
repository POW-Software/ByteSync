using ByteSync.Common.Business.Inventories;
using ByteSync.Models.Inventories;

namespace ByteSync.Business.Actions.Shared;

public class SharedDataPart
{
    // Keep it for serialization needs
    public SharedDataPart()
    {

    }

    public SharedDataPart(string name, Inventory inventory, InventoryPart inventoryPart,
        string? relativePath, string? signatureGuid, string? signatureHash, bool hasAnalysisError)
    {
        Name = name;
        InventoryPartType = inventoryPart.InventoryPartType;
        ClientInstanceId = inventory.Endpoint.ClientInstanceId;
        InventoryCodeAndId = inventory.CodeAndId;
        NodeId = inventory.NodeId;
        RootPath = inventoryPart.RootPath;
        RelativePath = relativePath;
        SignatureGuid = signatureGuid;
        SignatureHash = signatureHash;
        HasAnalysisError = hasAnalysisError;
    }

    public string Name { get; set; } = null!;
        
    public FileSystemTypes InventoryPartType { get; set; }

    public string ClientInstanceId { get; set; } = null!;
        
    public string InventoryCodeAndId { get; set; }  = null!;

    public string? NodeId { get; set; }

    public string RootPath { get; set; } = null!;
        
    public string? RelativePath { get; }

    public string? SignatureGuid { get; } 
        
    public string? SignatureHash { get; } 

    public bool HasAnalysisError { get; set; }

    protected bool Equals(SharedDataPart other)
    {
        return Equals(ClientInstanceId, other.ClientInstanceId) && 
               RootPath.Equals(other.RootPath, StringComparison.InvariantCultureIgnoreCase) &&
               Equals (RelativePath, other.RelativePath);
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((SharedDataPart) obj);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return (ClientInstanceId.GetHashCode() * 397) ^ RootPath.GetHashCode();
        }
    }
}