using ByteSync.ServerCommon.Business.Auth;
using StackExchange.Redis;

namespace ByteSync.ServerCommon.Interfaces.Services.Clients;

public interface IClientsGroupsPersistenceService
{
    Task<Client> AddSessionSubscription(Client client, string sessionId, ITransaction transaction);
    
    Task<Client> AddLobbySubscription(Client client, string lobbyId, ITransaction transaction);
    
    Task<Client> RemoveSessionSubscription(Client client, string requestSessionId, ITransaction transaction);
    
    Task<Client> RemoveLobbySubscription(Client client, string lobbyId, ITransaction transaction);
}