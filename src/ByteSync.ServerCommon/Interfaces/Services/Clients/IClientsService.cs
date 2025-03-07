using ByteSync.Common.Business.EndPoints;
using ByteSync.ServerCommon.Business.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;

namespace ByteSync.ServerCommon.Interfaces.Services.Clients;

public interface IClientsService
{
    Task<Client?> OnClientConnected(string clientInstanceId, string connectionId);
    
    Task OnClientDisconnected(HubCallerContext? context, Exception? exception);

    Task<ByteSyncEndpoint?> BuildByteSyncEndpoint(HubCallerContext context);

    Task<ByteSyncEndpoint?> BuildByteSyncEndpoint(HttpContext httpContext);
    
    Task<Client?> GetClient(HubCallerContext context);

    Task<Client?> GetClient(HttpContext httpContext);
}