using ByteSync.Common.Business.EndPoints;

namespace ByteSync.Common.Business.Sessions.Cloud.Connections;

public class AskCloudSessionPasswordExchangeKeyParameters
{
    public AskCloudSessionPasswordExchangeKeyParameters()
    {

    }
        
    public AskCloudSessionPasswordExchangeKeyParameters(string sessionId, PublicKeyInfo publicKeyInfo)
    {
        SessionId = sessionId;
        PublicKeyInfo = publicKeyInfo;
    }

    public string SessionId { get; set; }
        
    public PublicKeyInfo PublicKeyInfo { get; set; }
    
    // Todo 0512 LobbyId doit être contrôlé par le serveur
    public string? LobbyId { get; set; }
    
    public string? ProfileClientId { get; set; }
}