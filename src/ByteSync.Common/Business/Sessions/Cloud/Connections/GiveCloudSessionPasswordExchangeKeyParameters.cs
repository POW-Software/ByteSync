using ByteSync.Common.Business.EndPoints;

namespace ByteSync.Common.Business.Sessions.Cloud.Connections;

public class GiveCloudSessionPasswordExchangeKeyParameters
{
    public GiveCloudSessionPasswordExchangeKeyParameters()
    {
            
    }
    
    public GiveCloudSessionPasswordExchangeKeyParameters(string sessionId, string joinerInstanceId, string validatorInstanceId, PublicKeyInfo publicKeyInfo)
    {
        SessionId = sessionId;
        JoinerInstanceId = joinerInstanceId;
        ValidatorInstanceId = validatorInstanceId;
        PublicKeyInfo = publicKeyInfo;
    }

    public string SessionId { get; set; }
        
    public string JoinerInstanceId { get; set; }
        
    public string ValidatorInstanceId { get; set; }
    
    public PublicKeyInfo PublicKeyInfo { get; set; }
}