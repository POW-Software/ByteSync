using ByteSync.Common.Business.EndPoints;
using ByteSync.Common.Helpers;
using ByteSync.ServerCommon.Business.Auth;
using ByteSync.ServerCommon.Helpers;
using ByteSync.ServerCommon.Interfaces.Factories;
using ByteSync.ServerCommon.Interfaces.Repositories;
using ByteSync.ServerCommon.Interfaces.Services.Clients;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace ByteSync.ServerCommon.Services.Clients;

public class ClientsService : IClientsService
{
    private readonly IByteSyncEndpointFactory _byteSyncEndpointFactory;
    private readonly IClientsRepository _clientsRepository;
    private readonly IClientsGroupsHubService _clientsGroupsHubService;
    private readonly ILogger<ClientsService> _logger;

    public ClientsService(IByteSyncEndpointFactory byteSyncEndpointFactory,
        IClientsRepository clientsRepository, IClientsGroupsHubService clientsGroupsHubService,
        ILogger<ClientsService> logger)
    {
        _byteSyncEndpointFactory = byteSyncEndpointFactory;
        _clientsRepository = clientsRepository;
        _clientsGroupsHubService = clientsGroupsHubService;
        _logger = logger;
    }

    public async Task<Client?> OnClientConnected(string clientInstanceId, string connectionId)
    {
        var result = await _clientsRepository.AddOrUpdate(clientInstanceId, client =>
        {
            if (client != null)
            {
                client.SetConnected(connectionId);
            }
            
            return client;
        });

        
        if (result.IsSaved)
        {
            var client = result.Element!;
            
            await _clientsGroupsHubService.AddClientGroup(connectionId, client);

            _logger.LogInformation("ClientsService.OnClientConnected: client:{@client}, connectionId:{connectionId}", 
                client.BuildLog(), connectionId);
        }
        else
        {
            _logger.LogInformation("ClientsService.OnClientConnected: unknown client with connectionId {connectionId}", connectionId);
        }

        return result.Element;
    }

    public async Task OnClientDisconnected(HubCallerContext? context, Exception? exception)
    {
        if (context == null)
        {
            return;
        }
        
        var clientInstanceId = context.User?.Claims.FirstOrDefault(c => c.Type.Equals(AuthConstants.CLAIM_CLIENT_INSTANCE_ID))?.Value;

        if (clientInstanceId == null)
        {
            _logger.LogInformation(exception, "ClientsManager.OnConnectionLost: clientInstanceId ({clientInstanceId}) is null", clientInstanceId);
            return;
        }
            
        var lostConnectionClient = await _clientsRepository.Get(clientInstanceId); 
        
        if (lostConnectionClient != null)
        {
            lostConnectionClient.OnConnectionLost(context.ConnectionId);
            
            _logger.LogInformation(exception,
                "ClientsManager.OnConnectionLost: client:{@clientToRemove}, connectionId:{connectionId}",
                lostConnectionClient.BuildLog(), context.ConnectionId);
        }
        else
        {
            _logger.LogInformation(exception, 
                "ClientsManager.OnConnectionLost: can not find client by clientInstanceId ({clientInstanceId}) " +
                "for connectionId {connectionId}",
                clientInstanceId, context.ConnectionId);
        }
    }

    public async Task<ByteSyncEndpoint?> BuildByteSyncEndpoint(HubCallerContext hubCallerContext)
    {
        var byteSyncEndpoint = _byteSyncEndpointFactory.BuildByteSyncEndpoint(hubCallerContext.User?.Claims.ToList());
        if (byteSyncEndpoint == null)
        {
            return null;
        }

        bool checkClient = await CheckClient(byteSyncEndpoint);
        if (checkClient)
        {
            return byteSyncEndpoint;
        }
        else
        {
            return null;
        }
    }
        
    public async Task<ByteSyncEndpoint?> BuildByteSyncEndpoint(HttpContext httpContext)
    {
        var byteSyncEndpoint = _byteSyncEndpointFactory.BuildByteSyncEndpoint(httpContext.User.Claims.ToList());
        if (byteSyncEndpoint == null)
        {
            return null;
        }
        
        bool checkClient = await CheckClient(byteSyncEndpoint);
        if (checkClient)
        {
            return byteSyncEndpoint;
        }
        else
        {
            return null;
        }
    }
    
    public async Task<Client?> GetClient(HubCallerContext hubCallerContext)
    {
        var byteSyncEndpoint = _byteSyncEndpointFactory.BuildByteSyncEndpoint(hubCallerContext.User?.Claims.ToList());
        if (byteSyncEndpoint == null)
        {
            return null;
        }

        var client = await _clientsRepository.Get(byteSyncEndpoint);
        return client;
    }
        
    public async Task<Client?> GetClient(HttpContext httpContext)
    {
        var byteSyncEndpoint = _byteSyncEndpointFactory.BuildByteSyncEndpoint(httpContext.User.Claims.ToList());
        if (byteSyncEndpoint == null)
        {
            return null;
        }
        
        var client = await _clientsRepository.Get(byteSyncEndpoint);
        return client;
    }

    private async Task<bool> CheckClient(ByteSyncEndpoint? byteSyncEndpoint)
    {
        if (byteSyncEndpoint == null)
        {
            return false;
        }

        var client = await _clientsRepository.Get(byteSyncEndpoint);
        if (client == null)
        {
            return false;
        }
        
        bool result = client.Status.In(ClientStatuses.Created, ClientStatuses.Connected);
        
        return result;
    }
}