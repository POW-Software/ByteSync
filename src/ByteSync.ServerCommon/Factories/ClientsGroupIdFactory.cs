using ByteSync.ServerCommon.Interfaces.Factories;

namespace ByteSync.ServerCommon.Factories;

public class ClientsGroupIdFactory : IClientsGroupIdFactory
{
    private const string SESSION_PREFIX = "Session_";
    private const string LOBBY_PREFIX = "Lobby_";
    private const string CLIENT_PREFIX = "CGID_";
    
    public string GetClientGroupId(string clientInstanceId)
    {
        return CLIENT_PREFIX + clientInstanceId;
    }
    
    public string GetSessionGroupId(string sessionId)
    {
        return SESSION_PREFIX + sessionId;
    }
    
    public string GetLobbyGroupId(string lobbyId)
    {
        return LOBBY_PREFIX + lobbyId;
    }
}