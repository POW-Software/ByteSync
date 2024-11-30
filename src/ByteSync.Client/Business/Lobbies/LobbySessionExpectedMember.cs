using ByteSync.Common.Business.Lobbies;

namespace ByteSync.Business.Lobbies;

public class LobbySessionExpectedMember
{
    public LobbySessionExpectedMember(LobbyMember lobbyMember, LobbyMemberInfo lobbyMemberInfo, string lobbyId, string sessionId)
    {
        LobbyMember = lobbyMember;
        LobbyId = lobbyId;
        SessionId = sessionId;

        ProfileClientId = LobbyMember.ProfileClientId;

        ClientInstanceId = lobbyMemberInfo.ClientInstanceId;
    }
    
    public LobbyMember LobbyMember { get; }

    public string LobbyId { get; }

    public string SessionId { get; }
    
    public string ProfileClientId { get; }
    
    public string ClientInstanceId { get; }
}