using ByteSync.Common.Business.Lobbies;

namespace ByteSync.Business.Lobbies;

public class CheckedOtherMember
{
    public CheckedOtherMember(LobbyMember lobbyMember, bool? isCheckSuccess)
    {
        LobbyMember = lobbyMember;
        IsCheckSuccess = isCheckSuccess;
    }

    public LobbyMember LobbyMember { get; set; }
    
    public bool? IsCheckSuccess { get; set; }

    public LobbyMemberInfo LobbyMemberInfo => LobbyMember.LobbyMemberInfo!;

    public string ClientInstanceId => LobbyMemberInfo.ClientInstanceId;
}