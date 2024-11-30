namespace ByteSync.Common.Business.Sessions;

public class InventoryStartedDTO : BaseSessionDto
{
    public InventoryStartedDTO()
    {
        
    }
    
    public InventoryStartedDTO(string sessionId, string clientInstanceId, EncryptedSessionSettings encryptedSessionSettings)
        : base(sessionId, clientInstanceId)
    {
        EncryptedSessionSettings = encryptedSessionSettings;
    }
    
    public EncryptedSessionSettings EncryptedSessionSettings { get; set; } = null!;
}