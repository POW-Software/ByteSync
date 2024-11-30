namespace ByteSync.Common.Business.Sessions;

public class SessionSettingsUpdatedDTO : BaseSessionDto
{
    public SessionSettingsUpdatedDTO()
    {
        
    }
    
    public SessionSettingsUpdatedDTO(string sessionId, string clientInstanceId, EncryptedSessionSettings encryptedSessionSettings)
        : base(sessionId, clientInstanceId)
    {
        EncryptedSessionSettings = encryptedSessionSettings;
    }

    public EncryptedSessionSettings EncryptedSessionSettings { get; set; } = null!;
}