using ByteSync.Common.Business.Inventories;

namespace ByteSync.ServerCommon.Entities.Inventories;

public class InventoryDataSourceEntity
{
    public InventoryDataSourceEntity()
    {

    }
    
    public InventoryDataSourceEntity(EncryptedDataSource encryptedDataSource)
    {
        Id = encryptedDataSource.Id;
        EncryptedDataSource = encryptedDataSource;
    }

    public string Id { get; set; } = null!;
    
    public EncryptedDataSource EncryptedDataSource { get; set; } = null!;
}