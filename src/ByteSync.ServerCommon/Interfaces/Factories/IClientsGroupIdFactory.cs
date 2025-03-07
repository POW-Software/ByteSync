namespace ByteSync.ServerCommon.Interfaces.Hubs;

public interface IClientsGroupIdFactory
{
    string GetClientGroupId(string clientInstanceId);
    
    string GetSessionGroupId(string sessionId);
    
    string GetLobbyGroupId(string lobbyId);
}