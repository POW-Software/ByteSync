using ByteSync.Common.Interfaces.Hub;
using ByteSync.ServerCommon.Business.Auth;
using ByteSync.ServerCommon.Business.Sessions;

namespace ByteSync.ServerCommon.Interfaces.Hubs;

public interface IClientsGroupsManager
{
    
    
    Task AddToSessionGroup(Client client, string sessionId);
    
    Task AddToLobbyGroup(Client client, string lobby);
    
    Task RemoveFromGroup(Client client, string groupName);
    
    Task AddClientGroup(string connectionId, Client client);
    
    Task RemoveClientGroup(string connectionId, Client client);
}