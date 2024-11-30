using ByteSync.Common.Business.EndPoints;
using ByteSync.Common.Business.Lobbies;
using ByteSync.ServerCommon.Business.Auth;

namespace ByteSync.ServerCommon.Entities;

public class LobbyMember
{ 
    public LobbyMember(string profileClientId, PublicKeyInfo publicKeyInfo, JoinLobbyModes joinLobbyMode, Client client)
    {
        ProfileClientId = profileClientId;
        PublicKeyInfo = publicKeyInfo;
        JoinLobbyMode = joinLobbyMode;
        ClientInstanceId = client.ClientInstanceId;
        ClientIpAddress = client.IpAddress;
        Status = LobbyMemberStatuses.Joined;
    }
    
    public string ProfileClientId { get; set; }
    
    public PublicKeyInfo PublicKeyInfo { get; set; }
    
    public JoinLobbyModes JoinLobbyMode { get; set; }
    
    public string ClientInstanceId { get; set; }
    
    public string ClientIpAddress { get; set; }
    
    public LobbyMemberStatuses Status { get; set; }

    // public void QuitLobby()
    // {
    //     Lobby.RemoveLobbyMember(this);
    //
    //     Lobby = null;
    // }
    
    public LobbyMemberInfo GetLobbyMemberInfo()
    {
        LobbyMemberInfo lobbyMemberInfo = new LobbyMemberInfo();

        lobbyMemberInfo.ProfileClientId = ProfileClientId;
        lobbyMemberInfo.ClientInstanceId = ClientInstanceId;
        lobbyMemberInfo.PublicKeyInfo = PublicKeyInfo;
        lobbyMemberInfo.JoinLobbyMode = JoinLobbyMode;
        lobbyMemberInfo.Status = Status;
        lobbyMemberInfo.IpAddress = ClientIpAddress;

        return lobbyMemberInfo;
    }

    protected bool Equals(LobbyMember other)
    {
        return ProfileClientId == other.ProfileClientId && Equals(ClientInstanceId, other.ClientInstanceId);
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((LobbyMember)obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(ProfileClientId, ClientInstanceId);
    }
}