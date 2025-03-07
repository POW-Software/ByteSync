using ByteSync.Common.Business.EndPoints;
using ByteSync.ServerCommon.Business.Auth;
using ByteSync.ServerCommon.Interfaces.Factories;
using ByteSync.ServerCommon.Interfaces.Repositories;
using ByteSync.ServerCommon.Interfaces.Services;
using StackExchange.Redis;

namespace ByteSync.ServerCommon.Repositories;

public class ClientsRepository : BaseRepository<Client>, IClientsRepository
{
    private readonly IClientsGroupIdFactory _clientsGroupIdFactory;

    public ClientsRepository(ICacheService cacheService, IClientsGroupIdFactory clientsGroupIdFactory) : base(cacheService)
    {
        _clientsGroupIdFactory = clientsGroupIdFactory;
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