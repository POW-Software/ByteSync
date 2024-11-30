using ByteSync.Common.Business.EndPoints;

namespace ByteSync.Common.Business.Sessions.Cloud.Connections;

public class CreateCloudSessionParameters
{
    public CreateCloudSessionParameters()
    {
        
    }
    
    public string? LobbyId { get; set; }
    
    public string? CreatorProfileClientId { get; set; }
    
    public EncryptedSessionSettings SessionSettings { get; set; } = null!;

    public PublicKeyInfo CreatorPublicKeyInfo { get; set; } = null!;
    
    public EncryptedSessionMemberPrivateData CreatorPrivateData { get; set; } = null!;
}