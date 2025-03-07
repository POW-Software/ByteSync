using ByteSync.ServerCommon.Business.Auth;
using ByteSync.ServerCommon.Interfaces.Hubs;
using ByteSync.ServerCommon.Interfaces.Repositories;
using ByteSync.ServerCommon.Interfaces.Services.Clients;
using StackExchange.Redis;

namespace ByteSync.ServerCommon.Services.Clients;

public class ClientsGroupsPersistenceService : IClientsGroupsPersistenceService
{
    private readonly IClientsRepository _clientsRepository;
    private readonly IClientsGroupIdFactory _clientsGroupIdFactory;

    public ClientsGroupsPersistenceService(IClientsRepository clientsRepository, IClientsGroupIdFactory clientsGroupIdFactory)
    {
        _clientsRepository = clientsRepository;
        _clientsGroupIdFactory = clientsGroupIdFactory;
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
        var updateResult = await _clientsRepository.AddOrUpdate(client.ClientInstanceId, innerClient =>
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
        var updateResult = await _clientsRepository.AddOrUpdate(client.ClientInstanceId, innerClient =>
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