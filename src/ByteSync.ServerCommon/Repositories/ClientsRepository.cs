using ByteSync.Common.Business.EndPoints;
using ByteSync.ServerCommon.Business.Auth;
using ByteSync.ServerCommon.Interfaces.Repositories;
using ByteSync.ServerCommon.Interfaces.Services;

namespace ByteSync.ServerCommon.Repositories;

public class ClientsRepository : BaseRepository<Client>, IClientsRepository
{
    public ClientsRepository(ICacheService cacheService) : base(cacheService)
    {

    }
    
    public override string ElementName => "Client";

    public Task<Client?> Get(ByteSyncEndpoint byteSyncEndpoint)
    {
        return Get(byteSyncEndpoint.ClientInstanceId);
    }

    public async Task<HashSet<Client>> GetClientsWithoutConnectionId()
    {
        return new HashSet<Client>();
    }

    public async Task RemoveClient(Client client)
    {
        await Delete(client.ClientInstanceId);
    }
}