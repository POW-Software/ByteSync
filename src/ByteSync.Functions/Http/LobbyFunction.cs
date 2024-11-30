using ByteSync.Common.Business.Lobbies;
using ByteSync.Common.Business.Lobbies.Connections;
using ByteSync.Functions.Helpers;
using ByteSync.ServerCommon.Interfaces.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

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
    public async Task<IActionResult> JoinLobby([HttpTrigger(
            AuthorizationLevel.Anonymous, "post", Route = "lobby/join/{cloudSessionProfileId}")] HttpRequestData req,
        FunctionContext executionContext, string cloudSessionProfileId)
    {
        try
        {
            var client = FunctionHelper.GetClientFromContext(executionContext);
            var parameters = await FunctionHelper.DeserializeRequestBody<JoinLobbyParameters>(req);
            
            var result = await _lobbyService.TryJoinLobby(parameters, client);
            return new OkObjectResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while joining lobby with cloudSessionProfileId: {cloudSessionProfileId}", cloudSessionProfileId);
            
            return new ObjectResult(new { error = "An internal server error occurred." })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }
    
    [Function("SendLobbyCloudSessionCredentialsFunction")]
    public async Task<IActionResult> SendLobbyCloudSessionCredentials([HttpTrigger(
            AuthorizationLevel.Anonymous, "post", Route = "lobby/{lobbyId}/sendCloudSessionCredentials")] HttpRequestData req,
        FunctionContext executionContext, string lobbyId)
    {
        try
        {
            var client = FunctionHelper.GetClientFromContext(executionContext);
            var lobbyCloudSessionCredentials = await FunctionHelper.DeserializeRequestBody<LobbyCloudSessionCredentials>(req);
            
            await _lobbyService.SendLobbyCloudSessionCredentials(lobbyCloudSessionCredentials, client);
            
            return new OkResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending cloud session credentials for lobby: {lobbyId}", lobbyId);
            
            return new ObjectResult(new { error = "An internal server error occurred." })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }
    
    [Function("QuitLobbyFunction")]
    public async Task<IActionResult> QuitLobby([HttpTrigger(
            AuthorizationLevel.Anonymous, "post", Route = "lobby/{lobbyId}/quit")] HttpRequestData req,
        FunctionContext executionContext, string lobbyId)
    {
        try
        {
            var client = FunctionHelper.GetClientFromContext(executionContext);
            
            await _lobbyService.QuitLobby(lobbyId, client);
            
            return new OkResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "message");
            
            return new ObjectResult(new { error = "An internal server error occurred." })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }
    
    [Function("SendLobbyCheckInfosFunction")]
    public async Task<IActionResult> SendLobbyCheckInfos([HttpTrigger(
            AuthorizationLevel.Anonymous, "post", Route = "lobby/{lobbyId}/checkInfos")] HttpRequestData req,
        FunctionContext executionContext, string lobbyId)
    {
        try
        {
            var client = FunctionHelper.GetClientFromContext(executionContext);
            var lobbyCheckInfo = await FunctionHelper.DeserializeRequestBody<LobbyCheckInfo>(req);
            
            await _lobbyService.SendLobbyCheckInfos(lobbyCheckInfo, client);
            
            return new OkResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "message");
            
            return new ObjectResult(new { error = "An internal server error occurred." })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }
    
    [Function("UpdateLobbyMemberStatusFunction")]
    public async Task<IActionResult> UpdateLobbyMemberStatus([HttpTrigger(
            AuthorizationLevel.Anonymous, "post", Route = "lobby/{lobbyId}/memberStatus")] HttpRequestData req,
        FunctionContext executionContext, string lobbyId)
    {
        try
        {
            var client = FunctionHelper.GetClientFromContext(executionContext);
            var lobbyMemberStatus = await FunctionHelper.DeserializeRequestBody<LobbyMemberStatuses>(req);
            
            await _lobbyService.UpdateLobbyMemberStatus(lobbyId, client, lobbyMemberStatus);
            
            return new OkResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "message");
            
            return new ObjectResult(new { error = "An internal server error occurred." })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }
}