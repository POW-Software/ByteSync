using ByteSync.Common.Helpers;

namespace ByteSync.Common.Business.Lobbies.Connections;

public enum JoinLobbyStatuses
{
    LobbyJoinedSucessfully = 1,
    UnknownCloudSessionProfile = 2,
    UnknownProfileClientId = 3,
    LobbyPreviouslyJoined = 4,
    UnexpectedLobbyJoinMode = 5,
    Unknown_6 = 6,
    Unknown_7 = 7,
}

public class JoinLobbyResult
{
    public JoinLobbyStatuses Status { get; set; }
    
    public LobbyInfo? LobbyInfo { get; set; }

    // public string? CloudSessionProfileId { get; set; }

    public bool IsOK
    {
        get
        {
            return Status.In(JoinLobbyStatuses.LobbyJoinedSucessfully, JoinLobbyStatuses.LobbyPreviouslyJoined);
        }
    }
    
    public static JoinLobbyResult BuildFrom(LobbyInfo lobbyInfo, JoinLobbyStatuses status)
    {
        JoinLobbyResult joinLobbyResult = new JoinLobbyResult();

        joinLobbyResult.LobbyInfo = lobbyInfo;
        joinLobbyResult.Status = status;

        return joinLobbyResult;
    }

    public static JoinLobbyResult BuildFrom(JoinLobbyStatuses status)
    {
        JoinLobbyResult joinLobbyResult = new JoinLobbyResult();

        joinLobbyResult.Status = status;

        return joinLobbyResult;
    }
}