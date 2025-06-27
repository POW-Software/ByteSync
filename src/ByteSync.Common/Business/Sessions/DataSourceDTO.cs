using ByteSync.Common.Business.Inventories;

namespace ByteSync.Common.Business.Sessions;

public class DataSourceDTO : BaseSessionDto
{
    public DataSourceDTO()
    {
        
    }
    
    public DataSourceDTO(string sessionId, string clientInstanceId, EncryptedPathItem encryptedPathItem)
        : base(sessionId, clientInstanceId)
    {
        EncryptedPathItem = encryptedPathItem;
    }
    
    public EncryptedPathItem EncryptedPathItem { get; set; } = null!;
}