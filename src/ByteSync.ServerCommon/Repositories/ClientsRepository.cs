using ByteSync.Common.Business.EndPoints;
using ByteSync.ServerCommon.Business.Auth;
using ByteSync.ServerCommon.Interfaces.Hubs;
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

    public async Task<Client> AddSessionSubscription(Client client, string sessionId, ITransaction transaction)
    {
        var groupName = _clientsGroupIdFactory.GetSessionGroupId(sessionId);

        return await AddSubscription(client, transaction, groupName);
    }

    public async Task<Client> AddLobbySubscription(Client client, string lobbyId, ITransaction transaction)
    {
        var groupName = _clientsGroupIdFactory.GetLobbyGroupId(lobbyId);

        return await AddSubscription(client, transaction, groupName);
    }

    public async Task<Client> RemoveSessionSubscription(Client client, string sessionId, ITransaction transaction)
    {
        var groupName = _clientsGroupIdFactory.GetSessionGroupId(sessionId);

        return await RemoveSubscription(client, transaction, groupName);
    }
    
    public async Task<Client> RemoveLobbySubscription(Client client, string lobbyId, ITransaction transaction)
    {
        var groupName = _clientsGroupIdFactory.GetLobbyGroupId(lobbyId);

        return await RemoveSubscription(client, transaction, groupName);
    }

    private async Task<Client> AddSubscription(Client client, ITransaction transaction, string groupName)
    {
        var updateResult = await AddOrUpdate(client.ClientInstanceId, innerClient =>
        {
            if (innerClient != null)
            {
                innerClient.SubscribedGroups.Add(groupName);
            }

            return innerClient;
        }, transaction).ConfigureAwait(false);

        return updateResult.Element!;
    }
    
    private async Task<Client> RemoveSubscription(Client client, ITransaction transaction, string groupName)
    {
        var updateResult = await AddOrUpdate(client.ClientInstanceId, innerClient =>
        {
            if (innerClient != null)
            {
                innerClient.SubscribedGroups.Remove(groupName);
            }

            return innerClient;
        }, transaction).ConfigureAwait(false);

        return updateResult.Element!;
    }
}