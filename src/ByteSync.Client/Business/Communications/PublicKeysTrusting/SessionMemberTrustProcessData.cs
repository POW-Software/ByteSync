using ByteSync.Common.Business.EndPoints;

namespace ByteSync.Business.Communications.PublicKeysTrusting;

public class SessionMemberTrustProcessData
{
    public SessionMemberTrustProcessData()
    {
        LocalPublicKeyCheckDatasPerJoiner = new Dictionary<string, PublicKeyCheckData>();
    }
    
    private Dictionary<string, PublicKeyCheckData> LocalPublicKeyCheckDatasPerJoiner { get; }
    
    public void StoreLocalPublicKeyCheckData(string clientInstanceId, PublicKeyCheckData localPublicKeyCheckData)
    {
        if (LocalPublicKeyCheckDatasPerJoiner.ContainsKey(clientInstanceId))
        {
            LocalPublicKeyCheckDatasPerJoiner.Remove(clientInstanceId);
        }
            
        LocalPublicKeyCheckDatasPerJoiner.Add(clientInstanceId, localPublicKeyCheckData);
    }

    public PublicKeyCheckData? GetLocalPublicKeyCheckData(string joinerClientInstanceId)
    {
        if (LocalPublicKeyCheckDatasPerJoiner.ContainsKey(joinerClientInstanceId))
        {
            return LocalPublicKeyCheckDatasPerJoiner[joinerClientInstanceId];
        }

        return null;
    }
}