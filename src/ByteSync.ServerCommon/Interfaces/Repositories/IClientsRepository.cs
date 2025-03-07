using ByteSync.Common.Business.EndPoints;
using ByteSync.ServerCommon.Business.Auth;
using StackExchange.Redis;

namespace ByteSync.ServerCommon.Interfaces.Repositories;

public interface IClientsRepository : IRepository<Client>
{
    public Task<Client?> Get(ByteSyncEndpoint byteSyncEndpoint);
    
    public Task<HashSet<Client>> GetClientsWithoutConnectionId();
    
    public Task RemoveClient(Client client);
}