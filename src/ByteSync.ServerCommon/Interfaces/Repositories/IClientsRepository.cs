using ByteSync.Common.Business.EndPoints;
using ByteSync.ServerCommon.Business.Auth;
using StackExchange.Redis;

namespace ByteSync.ServerCommon.Interfaces.Repositories;

public interface IClientsRepository : IRepository<Client>
{
    public Task<Client?> Get(ByteSyncEndpoint byteSyncEndpoint);
    
    public Task<HashSet<Client>> GetClientsWithoutConnectionId();
    
    public Task RemoveClient(Client client);
    
    Task<Client> AddSessionSubscription(Client client, string sessionId, ITransaction transaction);
    
    Task<Client> AddLobbySubscription(Client client, string lobbyId, ITransaction transaction);
    
    Task<Client> RemoveSessionSubscription(Client client, string requestSessionId, ITransaction transaction);
    
    Task<Client> RemoveLobbySubscription(Client client, string lobbyId, ITransaction transaction);
}