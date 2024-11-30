using System.Collections.ObjectModel;
using ByteSync.Common.Business.EndPoints;
using ByteSync.Common.Business.Lobbies;
using ByteSync.ServerCommon.Business.Auth;

namespace ByteSync.ServerCommon.Entities;

public class Lobby
{
    public Lobby()
    {
        LobbyMemberCells = new List<LobbyMemberCell>();
    }

    public string LobbyId { get; set; }

    public string ProfileDetailsPassword { get; set; }
    
    public List<LobbyMemberCell> LobbyMemberCells { get; set; }
    
    public string CloudSessionProfileId { get; set; }

    public ReadOnlyCollection<LobbyMember> ConnectedLobbyMembers
    {
        get
        {
            return LobbyMemberCells
                .Where(cell => cell.IsMemberSet)
                .Select(cell => cell.LobbyMember!)
                .ToList()
                .AsReadOnly();
        }
    }

    public bool ConnectLobbyMember(string profileClientId, PublicKeyInfo publicKeyInfo, JoinLobbyModes joinLobbyModes,
        Client byteSyncEndpoint)
    {
        var cell = GetLobbyMemberCellByProfileClientId(profileClientId);
        
        if (cell.IsMemberSet)
        {
            return false;
        }
        else
        {
            LobbyMember lobbyMember = new LobbyMember(profileClientId, publicKeyInfo, joinLobbyModes, byteSyncEndpoint);

            cell.LobbyMember = lobbyMember;

            return true;
        }
    }

    public void RemoveLobbyMember(LobbyMember lobbyMember)
    {
        var cell = GetLobbyMemberCellByProfileClientId(lobbyMember.ProfileClientId);

        cell.LobbyMember = null;
    }

    public LobbyMember? GetLobbyMemberByClientInstanceId(string clientInstanceId)
    {
        var lobbyMemberCell = GetLobbyMemberCellByClientInstanceId(clientInstanceId);
        
        return lobbyMemberCell?.LobbyMember;
    }

    public LobbyInfo BuildLobbyInfo()
    {
        LobbyInfo lobbyInfo = new LobbyInfo();
        
        lobbyInfo.LobbyId = LobbyId;
        lobbyInfo.CloudSessionProfileId = CloudSessionProfileId;
        lobbyInfo.ProfileDetailsPassword = ProfileDetailsPassword;

        lobbyInfo.ConnectedMembers = new List<LobbyMemberInfo>();
        foreach (var lobbyMember in ConnectedLobbyMembers.Where(cm => cm != null))
        {
            LobbyMemberInfo lobbyMemberInfo = lobbyMember.GetLobbyMemberInfo();
            
            lobbyInfo.ConnectedMembers.Add(lobbyMemberInfo);
        }
        
        return lobbyInfo;
    }
    
    private LobbyMemberCell GetLobbyMemberCellByProfileClientId(string profileClientId)
    {
        return LobbyMemberCells.Single(cell => cell.ProfileClientId.Equals(profileClientId));
    }
    
    private LobbyMemberCell? GetLobbyMemberCellByClientInstanceId(string clientInstanceId)
    {
        return LobbyMemberCells
            .SingleOrDefault(cell => cell.IsMemberSet &&
                                     cell.LobbyMember.ClientInstanceId.Equals(clientInstanceId));
    }

    protected bool Equals(Lobby other)
    {
        return LobbyId == other.LobbyId;
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((Lobby)obj);
    }

    public override int GetHashCode()
    {
        return (LobbyId != null ? LobbyId.GetHashCode() : 0);
    }
}