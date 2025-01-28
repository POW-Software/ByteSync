using ByteSync.Common.Business.EndPoints;
using ByteSync.Common.Business.Sessions.Cloud.Connections;
using MediatR;

namespace ByteSync.Commands.Sessions.Connecting;

public class OnCloudSessionPasswordExchangeKeyGivenRequest : IRequest
{
    public OnCloudSessionPasswordExchangeKeyGivenRequest(string sessionId, string joinerInstanceId, string validatorInstanceId, PublicKeyInfo publicKeyInfo)
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

    public GiveCloudSessionPasswordExchangeKeyParameters ToGiveCloudSessionPasswordExchangeKeyParameters()
    {
        return new GiveCloudSessionPasswordExchangeKeyParameters(SessionId, JoinerInstanceId, ValidatorInstanceId, PublicKeyInfo);
    }
}