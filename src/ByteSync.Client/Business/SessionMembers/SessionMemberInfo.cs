using ByteSync.Common.Business.EndPoints;
using ByteSync.Common.Business.Sessions;
using ByteSync.Common.Business.Sessions.Cloud;

namespace ByteSync.Business.SessionMembers;

public class SessionMemberInfo
{
    public ByteSyncEndpoint Endpoint { get; set; }
    
    public SessionMemberPrivateData PrivateData { get; set; }
    
    public string SessionId { get; set; }

    public DateTimeOffset JoinedSessionOn { get; set; }
    
    public int PositionInList { get; set; }
    
    public DateTimeOffset? LastLocalInventoryGlobalStatusUpdate { get; set; }

    public SessionMemberGeneralStatus SessionMemberGeneralStatus { get; set; }
    
    public string? LobbyId { get; set; }
    
    public string? ProfileClientId { get; set; }
    
    public string ClientInstanceId => Endpoint.ClientInstanceId;
    
    public string IpAddress => Endpoint.IpAddress;
    
    public string MachineName => PrivateData.MachineName;
    
    public bool HasClientInstanceId(string clientInstanceId)
    {
        return Equals(ClientInstanceId, clientInstanceId);
    }
    
    protected bool Equals(SessionMemberInfoDTO other)
    {
        return ClientInstanceId == other.ClientInstanceId;
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
        return ClientInstanceId.GetHashCode();
    }
}