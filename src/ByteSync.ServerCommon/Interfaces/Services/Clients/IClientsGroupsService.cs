using ByteSync.ServerCommon.Business.Auth;
using StackExchange.Redis;

namespace ByteSync.ServerCommon.Interfaces.Services.Clients;

public interface IClientsGroupsService
{
    Task AddToSessionGroup(Client client, string sessionId);
    
    Task AddToLobbyGroup(Client client, string lobby);
    
    Task RemoveFromSessionGroup(Client client, string sessionId);
    
    Task RemoveFromLobbyGroup(Client client, string lobbyId);
    
    Task<Client> AddSessionSubscription(Client client, string sessionId, ITransaction transaction);
    
    Task<Client> AddLobbySubscription(Client client, string lobbyId, ITransaction transaction);
    
    Task<Client> RemoveSessionSubscription(Client client, string requestSessionId, ITransaction transaction);
    
    Task<Client> RemoveLobbySubscription(Client client, string lobbyId, ITransaction transaction);
}