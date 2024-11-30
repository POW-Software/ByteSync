using ByteSync.Common.Business.EndPoints;

namespace ByteSync.Common.Business.Sessions.Cloud.Connections;

public class RequestTrustProcessParameters
{
    public RequestTrustProcessParameters()
    {
    }

    public RequestTrustProcessParameters(string sessionId, PublicKeyCheckData joinerPublicKeyCheckData, string sessionMemberInstanceId)
    {
        SessionId = sessionId;
        JoinerPublicKeyCheckData = joinerPublicKeyCheckData;
        SessionMemberInstanceId = sessionMemberInstanceId;
    }
    
    public string SessionId { get; set; } = null!;

    public PublicKeyCheckData JoinerPublicKeyCheckData { get; set; } = null!;
    
    public string SessionMemberInstanceId { get; set; } = null!;

    public string JoinerClientInstanceId
    {
        get { return JoinerPublicKeyCheckData.IssuerClientInstanceId; }
    }
}