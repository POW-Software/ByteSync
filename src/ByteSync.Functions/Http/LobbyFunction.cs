using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using ByteSync.Common.Business.Lobbies;
using ByteSync.Common.Business.Lobbies.Connections;
using ByteSync.Functions.Constants;
using ByteSync.Functions.Helpers;
using ByteSync.ServerCommon.Interfaces.Services;

namespace ByteSync.Functions.Http;

public class LobbyFunction
{
    private readonly ILobbyService _lobbyService;
    private readonly ILogger<LobbyFunction> _logger;

    public LobbyFunction(ILobbyService lobbyService, ILoggerFactory loggerFactory)
    {
        _lobbyService = lobbyService;
        _logger = loggerFactory.CreateLogger<LobbyFunction>();
    }
    
    [Function("JoinLobbyFunction")]
    public async Task<HttpResponseData> JoinLobby(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "lobby/join/{cloudSessionProfileId}")] 
        HttpRequestData req,
        FunctionContext executionContext, 
        string cloudSessionProfileId)
    {
        var response = req.CreateResponse();
        try
        {
            var client = FunctionHelper.GetClientFromContext(executionContext);
            var parameters = await FunctionHelper.DeserializeRequestBody<JoinLobbyParameters>(req);
            
            var result = await _lobbyService.TryJoinLobby(parameters, client);

            await response.WriteAsJsonAsync(result, HttpStatusCode.OK);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while joining lobby with cloudSessionProfileId: {cloudSessionProfileId}", cloudSessionProfileId);
            
            await response.WriteAsJsonAsync(new { error = ErrorConstants.INTERNAL_SERVER_ERROR }, HttpStatusCode.InternalServerError);
        }
        
        return response;
    }
    
    [Function("SendLobbyCloudSessionCredentialsFunction")]
    public async Task<HttpResponseData> SendLobbyCloudSessionCredentials(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "lobby/{lobbyId}/sendCloudSessionCredentials")] 
        HttpRequestData req,
        FunctionContext executionContext, 
        string lobbyId)
    {
        var response = req.CreateResponse();
        try
        {
            var client = FunctionHelper.GetClientFromContext(executionContext);
            var lobbyCloudSessionCredentials = await FunctionHelper.DeserializeRequestBody<LobbyCloudSessionCredentials>(req);
            
            await _lobbyService.SendLobbyCloudSessionCredentials(lobbyCloudSessionCredentials, client);
            
            response.StatusCode = HttpStatusCode.OK;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending cloud session credentials for lobby: {lobbyId}", lobbyId);
            
            await response.WriteAsJsonAsync(new { error = ErrorConstants.INTERNAL_SERVER_ERROR }, HttpStatusCode.InternalServerError);
        }
        
        return response;
    }
    
    [Function("QuitLobbyFunction")]
    public async Task<HttpResponseData> QuitLobby(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "lobby/{lobbyId}/quit")] 
        HttpRequestData req,
        FunctionContext executionContext, 
        string lobbyId)
    {
        var response = req.CreateResponse();
        try
        {
            var client = FunctionHelper.GetClientFromContext(executionContext);
            
            await _lobbyService.QuitLobby(lobbyId, client);
            
            response.StatusCode = HttpStatusCode.OK;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while quitting lobby with lobbyId: {lobbyId}", lobbyId);
            
            await response.WriteAsJsonAsync(new { error = ErrorConstants.INTERNAL_SERVER_ERROR }, HttpStatusCode.InternalServerError);
        }
        
        return response;
    }
    
    [Function("SendLobbyCheckInfosFunction")]
    public async Task<HttpResponseData> SendLobbyCheckInfos(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "lobby/{lobbyId}/checkInfos")] 
        HttpRequestData req,
        FunctionContext executionContext, 
        string lobbyId)
    {
        var response = req.CreateResponse();
        try
        {
            var client = FunctionHelper.GetClientFromContext(executionContext);
            var lobbyCheckInfo = await FunctionHelper.DeserializeRequestBody<LobbyCheckInfo>(req);
            
            await _lobbyService.SendLobbyCheckInfos(lobbyCheckInfo, client);
            
            response.StatusCode = HttpStatusCode.OK;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while sending lobby check infos for lobbyId: {lobbyId}", lobbyId);
            
            await response.WriteAsJsonAsync(new { error = ErrorConstants.INTERNAL_SERVER_ERROR }, HttpStatusCode.InternalServerError);
        }
        
        return response;
    }
    
    [Function("UpdateLobbyMemberStatusFunction")]
    public async Task<HttpResponseData> UpdateLobbyMemberStatus(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "lobby/{lobbyId}/memberStatus")] 
        HttpRequestData req,
        FunctionContext executionContext, 
        string lobbyId)
    {
        var response = req.CreateResponse();
        try
        {
            var client = FunctionHelper.GetClientFromContext(executionContext);
            var lobbyMemberStatus = await FunctionHelper.DeserializeRequestBody<LobbyMemberStatuses>(req);
            
            await _lobbyService.UpdateLobbyMemberStatus(lobbyId, client, lobbyMemberStatus);
            
            response.StatusCode = HttpStatusCode.OK;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while updating lobby member status for lobbyId: {lobbyId}", lobbyId);
            
            await response.WriteAsJsonAsync(new { error = ErrorConstants.INTERNAL_SERVER_ERROR }, HttpStatusCode.InternalServerError);
        }
        
        return response;
    }
}