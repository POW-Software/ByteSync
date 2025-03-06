using ByteSync.ServerCommon.Business.Auth;

namespace ByteSync.ServerCommon.Interfaces.Hubs;

public interface IClientsGroupsManager
{
    Task AddToSessionGroup(Client client, string sessionId);
    
    Task AddToLobbyGroup(Client client, string lobby);
    
    Task RemoveFromSessionGroup(Client client, string sessionId);
    
    Task RemoveFromLobbyGroup(Client client, string sessionId);
    
    Task AddClientGroup(string connectionId, Client client);
    
    Task RemoveClientGroup(string connectionId, Client client);
}