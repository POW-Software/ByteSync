using ByteSync.Common.Business.EndPoints;
using ByteSync.Common.Business.Sessions;
using ByteSync.ServerCommon.Business.Auth;

namespace ByteSync.ServerCommon.Business.Sessions;

public class SessionMemberData
{
    public SessionMemberData()
    {
        JoinedSessionOn = DateTimeOffset.UtcNow;

        AuthCheckClientInstanceIds = new List<string>();
    }
    
    public SessionMemberData(Client client, PublicKeyInfo publicKeyInfo, string? profileClientId,
        CloudSessionData cloudSessionData, EncryptedSessionMemberPrivateData? encryptedSessionMemberPrivateData = null)
        : this(client.ClientInstanceId, client.ClientId, publicKeyInfo, profileClientId, cloudSessionData, encryptedSessionMemberPrivateData)
    {
    }
    
    public SessionMemberData(string clientInstanceId, string clientId, PublicKeyInfo publicKeyInfo, string? profileClientId,
        CloudSessionData cloudSessionData, EncryptedSessionMemberPrivateData? encryptedSessionMemberPrivateData = null) : this()
    {
        ClientInstanceId = clientInstanceId;
        
        ClientId = clientId;
        
        PublicKeyInfo = publicKeyInfo;
            
        ProfileClientId = profileClientId;

        CloudSessionData = cloudSessionData;
        
        EncryptedPrivateData = encryptedSessionMemberPrivateData;
    }
    
    public string ClientInstanceId { get; set; }
    
    public string ClientId { get; set; }
        
    public PublicKeyInfo PublicKeyInfo { get; set; }
        
    public string? ProfileClientId { get; set; }

    public CloudSessionData CloudSessionData { get; set; }

    public DateTimeOffset JoinedSessionOn { get; set; }

    /// <summary>
    /// InstanceId du client qui doit valider le SessionMemberData
    /// </summary>
    public string ValidatorInstanceId { get; set; }

    public string FinalizationPassword { get; set; }
        
    public List<string> AuthCheckClientInstanceIds { get; set; }

    public int PositionInList
    {
        get
        {
            return CloudSessionData.SessionMembers.FindIndex(m => m.ClientInstanceId == ClientInstanceId);
        }
    }

    public EncryptedSessionMemberPrivateData? EncryptedPrivateData { get; set; }

    public override bool Equals(object? obj)
    {
        if (obj is SessionMemberData sessionMemberData)
        {
            return Equals(sessionMemberData);
        }
        else
        {
            return false;
        }
    }

    protected bool Equals(SessionMemberData other)
    {
        return ClientInstanceId == other.ClientInstanceId;
    }

    public override int GetHashCode()
    {
        return ClientInstanceId.GetHashCode();
    }

    public bool IsAuthCheckedFor(string joinerInstanceId)
    {
        return AuthCheckClientInstanceIds.Contains(joinerInstanceId);
    }
}