using ByteSync.Common.Business.EndPoints;
using ByteSync.ServerCommon.Business.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;

namespace ByteSync.ServerCommon.Interfaces.Services;

public interface IClientsService
{

    
    // Task<RegisterClientResponse> RegisterClient(LoginData loginData, string ipAddress);
    
    // Task<Client?> GetClientBy(ByteSyncEndpoint byteSyncEndpoint);

    Task<Client?> OnClientConnected(HubCallerContext context);
    
    Task<Client?> OnClientConnected(string clientInstanceId, string connectionId, string? ipAddress);
    
    Task OnClientDisconnected(HubCallerContext? context, Exception? exception);

    // Task OnClientsDisconnected(HashSet<Client> disconnectedClients);

    // Task OnSerialValidityEnd(ProductSerial productSerial);

    // Task GetClientMandatoryVersionAsync(bool isStartup);

    // Task<bool> IsClientVersionAllowed(string version);

    Task<ByteSyncEndpoint?> BuildByteSyncEndpoint(HubCallerContext context);

    Task<ByteSyncEndpoint?> BuildByteSyncEndpoint(HttpContext httpContext);
    
    Task<Client?> GetClient(HubCallerContext context);

    Task<Client?> GetClient(HttpContext httpContext);

    // Task<ByteSyncEndpoint> BuildByteSyncEndpoint(Client httpContext);

    // Task PingAll();
}