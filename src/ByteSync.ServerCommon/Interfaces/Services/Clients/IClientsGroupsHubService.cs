using ByteSync.ServerCommon.Business.Auth;

namespace ByteSync.ServerCommon.Interfaces.Hubs;

public interface IClientsGroupsHubService
{
    Task AddToSessionGroup(Client client, string sessionId);
    
    Task AddToLobbyGroup(Client client, string lobby);
    
    Task RemoveFromSessionGroup(Client client, string sessionId);
    
    Task RemoveFromLobbyGroup(Client client, string lobbyId);
    
    Task AddClientGroup(string connectionId, Client client);
    
    Task RemoveClientGroup(string connectionId, Client client);
}