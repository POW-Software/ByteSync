using System;
using ByteSync.Common.Business.EndPoints;

namespace ByteSync.Common.Business.Sessions.Cloud;

public class SessionMemberInfoDTO
{
    public SessionMemberInfoDTO()
    {

    }

    public ByteSyncEndpoint Endpoint { get; set; }
    
    public EncryptedSessionMemberPrivateData EncryptedPrivateData { get; set; }
        
    public string SessionId { get; set; }
        

    public DateTimeOffset JoinedSessionOn { get; set; }

    public int PositionInList { get; set; }

    public string Letter
    {
        get
        {
            return ((char) ('A' + PositionInList)).ToString();
        }
    }
    
    public DateTimeOffset? LastLocalInventoryGlobalStatusUpdate { get; set; }

    public SessionMemberGeneralStatus SessionMemberGeneralStatus { get; set; }
    
    public string? LobbyId { get; set; }
    
    public string? ProfileClientId { get; set; }
    
    public string ClientId => Endpoint.ClientId;

    public string ClientInstanceId => Endpoint.ClientInstanceId;

    public bool HasClientInstanceId(string clientInstanceId)
    {
        return Equals(Endpoint.ClientInstanceId, clientInstanceId);
    }

    protected bool Equals(SessionMemberInfoDTO other)
    {
        return Endpoint.ClientInstanceId == other.Endpoint.ClientInstanceId;
    }

    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((SessionMemberInfoDTO)obj);
    }

    public override int GetHashCode()
    {
        return Endpoint.ClientInstanceId.GetHashCode();
    }
}