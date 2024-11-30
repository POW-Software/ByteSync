using ByteSync.Common.Business.Inventories;

namespace ByteSync.Common.Business.Sessions;

public class PathItemDTO : BaseSessionDto
{
    public PathItemDTO()
    {
        
    }
    
    public PathItemDTO(string sessionId, string clientInstanceId, EncryptedPathItem encryptedPathItem)
        : base(sessionId, clientInstanceId)
    {
        EncryptedPathItem = encryptedPathItem;
    }
    
    public EncryptedPathItem EncryptedPathItem { get; set; } = null!;
}