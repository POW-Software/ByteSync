using ByteSync.Common.Business.Inventories;

namespace ByteSync.Common.Business.Sessions;

public class DataSourceDTO : BaseSessionDto
{
    public DataSourceDTO()
    {
        
    }
    
    public DataSourceDTO(string sessionId, string clientInstanceId, EncryptedDataSource encryptedDataSource)
        : base(sessionId, clientInstanceId)
    {
        EncryptedDataSource = encryptedDataSource;
    }
    
    public EncryptedDataSource EncryptedDataSource { get; set; } = null!;
}