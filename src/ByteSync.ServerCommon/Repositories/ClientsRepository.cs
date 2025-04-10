using ByteSync.Common.Business.EndPoints;
using ByteSync.ServerCommon.Business.Auth;
using ByteSync.ServerCommon.Entities;
using ByteSync.ServerCommon.Interfaces.Factories;
using ByteSync.ServerCommon.Interfaces.Repositories;
using ByteSync.ServerCommon.Interfaces.Services;

namespace ByteSync.ServerCommon.Repositories;

public class ClientsRepository : BaseRepository<Client>, IClientsRepository
{
    private readonly IClientsGroupIdFactory _clientsGroupIdFactory;

    public ClientsRepository(IRedisInfrastructureService redisInfrastructureService, ICacheRepository<Client> cacheRepository, 
        IClientsGroupIdFactory clientsGroupIdFactory) : base(redisInfrastructureService, cacheRepository)
    {
        _clientsGroupIdFactory = clientsGroupIdFactory;
    }
    
    public override EntityType EntityType => EntityType.Client;

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