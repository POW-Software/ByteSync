using ByteSync.ServerCommon.Business.Auth;
using ByteSync.ServerCommon.Interfaces.Services.Clients;
using StackExchange.Redis;

namespace ByteSync.ServerCommon.Services.Clients;

public class ClientsGroupsService : IClientsGroupsService
{
    private readonly IClientsGroupsPersistenceService _clientsGroupsPersistenceService;
    private readonly IClientsGroupsHubService _clientsGroupsHubService;

    public ClientsGroupsService(IClientsGroupsPersistenceService clientsGroupsPersistenceService, IClientsGroupsHubService clientsGroupsHubService)
    {
        _clientsGroupsPersistenceService = clientsGroupsPersistenceService;
        _clientsGroupsHubService = clientsGroupsHubService;
    }
    
    public Task AddToSessionGroup(Client client, string sessionId)
    {
        return _clientsGroupsHubService.AddToSessionGroup(client, sessionId);
    }

    public Task AddToLobbyGroup(Client client, string lobby)
    {
        return _clientsGroupsHubService.AddToLobbyGroup(client, lobby);
    }

    public Task RemoveFromSessionGroup(Client client, string sessionId)
    {
        return _clientsGroupsHubService.RemoveFromSessionGroup(client, sessionId);
    }

    public Task RemoveFromLobbyGroup(Client client, string lobbyId)
    {
        return _clientsGroupsHubService.RemoveFromLobbyGroup(client, lobbyId);
    }

    public Task<Client> AddSessionSubscription(Client client, string sessionId, ITransaction transaction)
    {
        return _clientsGroupsPersistenceService.AddSessionSubscription(client, sessionId, transaction);
    }

    public Task<Client> AddLobbySubscription(Client client, string lobbyId, ITransaction transaction)
    {
        return _clientsGroupsPersistenceService.AddLobbySubscription(client, lobbyId, transaction);
    }

    public Task<Client> RemoveSessionSubscription(Client client, string requestSessionId, ITransaction transaction)
    {
        return _clientsGroupsPersistenceService.RemoveSessionSubscription(client, requestSessionId, transaction);
    }

    public Task<Client> RemoveLobbySubscription(Client client, string lobbyId, ITransaction transaction)
    {
        return _clientsGroupsPersistenceService.RemoveLobbySubscription(client, lobbyId, transaction);
    }
}