using System.Text.Json;
using Azure.Core.Serialization;
using ByteSync.Common.Interfaces.Hub;
using ByteSync.Functions.Helpers;
using ByteSync.ServerCommon.Interfaces.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.SignalR.Management;
using Microsoft.Extensions.Logging;

namespace ByteSync.Functions.SignalR;

public class SignalREntryFunction
{
    private readonly ServiceHubContext<IHubByteSyncPush> _hubContext;
    private readonly IClientsService _clientsService;
    private readonly ILogger<SignalREntryFunction> _logger;
    
    private const string HUB_NAME = "ByteSync";

    public SignalREntryFunction(ServiceHubContext<IHubByteSyncPush> hubContext, IClientsService clientsService,
        ILogger<SignalREntryFunction> logger)
    {
        _hubContext = hubContext;
        _clientsService = clientsService;
        _logger = logger;
    }
    
    [Function("negotiate")]
    public async Task<HttpResponseData> Negotiate([HttpTrigger(AuthorizationLevel.Anonymous, "post", "get", Route = "auth/negotiate")] HttpRequestData req, 
        FunctionContext executionContext)
    {
        try
        {
            _logger.LogInformation("C# HTTP trigger function processed a request");
            
            var client = FunctionHelper.GetClientFromContext(executionContext);
            
            var negotiateResponse = await _hubContext.NegotiateAsync(new() { UserId = client.ClientInstanceId });
            
            if (negotiateResponse == null || string.IsNullOrEmpty(negotiateResponse.Url) || string.IsNullOrEmpty(negotiateResponse.AccessToken))
            {
                _logger.LogWarning("Negotiate response is null or invalid {@negotiateResponse}", negotiateResponse);
                return req.CreateResponse(System.Net.HttpStatusCode.InternalServerError);
            }
            
            ObjectSerializer objectSerializer = new JsonObjectSerializer(new(JsonSerializerDefaults.Web));
            var serialized = await objectSerializer.SerializeAsync(new SignalRConnectionInfo()
            {
                Url = negotiateResponse.Url,
                AccessToken = negotiateResponse.AccessToken,
            });
            
            var response = req.CreateResponse();
            await response.WriteBytesAsync(serialized.ToArray());

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while negotiating signalr connection");
            
            return req.CreateResponse(System.Net.HttpStatusCode.InternalServerError);
        }
    }
    
    [Function("OnConnected")]
    public async Task OnConnected([SignalRTrigger(HUB_NAME, "connections", "connected")] SignalRInvocationContext invocationContext)
    {
        try
        {
            var xForwardedFor = invocationContext.Headers.Keys
                .FirstOrDefault(k => k.Equals("X-Forwarded-For", StringComparison.OrdinalIgnoreCase));

            string? ipAddress = null;
            if (xForwardedFor != null)
            {
                ipAddress = invocationContext.Headers[xForwardedFor];
            }
            
            await _clientsService.OnClientConnected(invocationContext.UserId, invocationContext.ConnectionId, ipAddress);
        
            invocationContext.Headers.TryGetValue("Authorization", out var auth);
            _logger.LogInformation("{InvocationContextConnectionId} has connected", invocationContext.ConnectionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during OnConnected method");
        }
    }
    
    [Function("OnDisconnected")]
    public void OnDisconnected([SignalRTrigger(HUB_NAME, "connections", "disconnected")] SignalRInvocationContext invocationContext)
    {
        _logger.LogInformation("{ConnectionId} has disconnected, UserId:{UserId}, Reason:{Reason}", 
            invocationContext.ConnectionId, invocationContext.UserId, invocationContext.Error);
    }
}