using ByteSync.ServerCommon.Interfaces.Factories;

namespace ByteSync.ServerCommon.Factories;

public class ClientsGroupIdFactory : IClientsGroupIdFactory
{
    private const string SESSION_PREFIX = "Session_";
    private const string LOBBY_PREFIX = "Lobby_";
    private const string CLIENT_PREFIX = "CGID_";
    
    public string GetClientGroupId(string clientInstanceId)
    {
        return $"Auth{ByteSync.Common.Business.Auth.AuthenticationProtocol.Epoch}_" + CLIENT_PREFIX + clientInstanceId;
    }
    
    public string GetSessionGroupId(string sessionId)
    {
        return $"Auth{ByteSync.Common.Business.Auth.AuthenticationProtocol.Epoch}_" + SESSION_PREFIX + sessionId;
    }
    
    public string GetLobbyGroupId(string lobbyId)
    {
        return $"Auth{ByteSync.Common.Business.Auth.AuthenticationProtocol.Epoch}_" + LOBBY_PREFIX + lobbyId;
    }
}