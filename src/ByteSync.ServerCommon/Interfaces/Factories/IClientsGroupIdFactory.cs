namespace ByteSync.ServerCommon.Interfaces.Factories;

public interface IClientsGroupIdFactory
{
    string GetClientGroupId(string clientInstanceId);
    
    string GetSessionGroupId(string sessionId);
    
    string GetLobbyGroupId(string lobbyId);
}