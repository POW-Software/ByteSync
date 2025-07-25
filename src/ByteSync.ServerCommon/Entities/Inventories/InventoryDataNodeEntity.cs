using ByteSync.Common.Business.Sessions;

namespace ByteSync.ServerCommon.Entities.Inventories;

public class InventoryDataNodeEntity
{
    public InventoryDataNodeEntity()
    {
        DataSources = new List<InventoryDataSourceEntity>();
    }

    public InventoryDataNodeEntity(EncryptedDataNode encryptedDataNode) : this ()
    {
        Id = encryptedDataNode.Id;
        EncryptedDataNode = encryptedDataNode;
    }

    public string Id { get; set; } = null!;
    
    public EncryptedDataNode EncryptedDataNode { get; set; } = null!;
    
    public List<InventoryDataSourceEntity> DataSources { get; set; }
}