using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using ByteSync.Common.Business.Lobbies;
using ByteSync.Common.Business.Lobbies.Connections;
using ByteSync.Functions.Helpers.Misc;
using ByteSync.ServerCommon.Commands.Lobbies;
using ByteSync.ServerCommon.Interfaces.Services;
using MediatR;

namespace ByteSync.Functions.Http;

public class LobbyFunction
{
    private readonly ILobbyService _lobbyService;
    private readonly IMediator _mediator;

    public LobbyFunction(ILobbyService lobbyService, IMediator mediator)
    {
        _lobbyService = lobbyService;
        _mediator = mediator;
    }
    
    [Function("JoinLobbyFunction")]
    public async Task<HttpResponseData> JoinLobby(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "lobby/join/{cloudSessionProfileId}")] 
        HttpRequestData req,
        FunctionContext executionContext, 
        string cloudSessionProfileId)
    {
        var client = FunctionHelper.GetClientFromContext(executionContext);
        var parameters = await FunctionHelper.DeserializeRequestBody<JoinLobbyParameters>(req);
            
        var request = new TryJoinLobbyRequest(parameters, client);
        var result = await _mediator.Send(request);

        var response = req.CreateResponse();
        await response.WriteAsJsonAsync(result, HttpStatusCode.OK);
        
        return response;
    }
    
    [Function("SendLobbyCloudSessionCredentialsFunction")]
    public async Task<HttpResponseData> SendLobbyCloudSessionCredentials(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "lobby/{lobbyId}/sendCloudSessionCredentials")] 
        HttpRequestData req,
        FunctionContext executionContext, 
        string lobbyId)
    {
        var client = FunctionHelper.GetClientFromContext(executionContext);
        var lobbyCloudSessionCredentials = await FunctionHelper.DeserializeRequestBody<LobbyCloudSessionCredentials>(req);
            
        await _lobbyService.SendLobbyCloudSessionCredentials(lobbyCloudSessionCredentials, client);
          
        var response = req.CreateResponse();
        response.StatusCode = HttpStatusCode.OK;
        
        return response;
    }
    
    [Function("QuitLobbyFunction")]
    public async Task<HttpResponseData> QuitLobby(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "lobby/{lobbyId}/quit")] 
        HttpRequestData req,
        FunctionContext executionContext, 
        string lobbyId)
    {
        var client = FunctionHelper.GetClientFromContext(executionContext);
            
        var request = new QuitLobbyRequest(lobbyId, client);
        await _mediator.Send(request);
           
        var response = req.CreateResponse();
        response.StatusCode = HttpStatusCode.OK;
        
        return response;
    }
    
    [Function("SendLobbyCheckInfosFunction")]
    public async Task<HttpResponseData> SendLobbyCheckInfos(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "lobby/{lobbyId}/checkInfos")] 
        HttpRequestData req,
        FunctionContext executionContext, 
        string lobbyId)
    {
        var client = FunctionHelper.GetClientFromContext(executionContext);
        var lobbyCheckInfo = await FunctionHelper.DeserializeRequestBody<LobbyCheckInfo>(req);
            
        await _lobbyService.SendLobbyCheckInfos(lobbyCheckInfo, client);
            
        var response = req.CreateResponse();
        response.StatusCode = HttpStatusCode.OK;
        
        return response;
    }
    
    [Function("UpdateLobbyMemberStatusFunction")]
    public async Task<HttpResponseData> UpdateLobbyMemberStatus(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "lobby/{lobbyId}/memberStatus")] 
        HttpRequestData req,
        FunctionContext executionContext, 
        string lobbyId)
    {
        var client = FunctionHelper.GetClientFromContext(executionContext);
        var lobbyMemberStatus = await FunctionHelper.DeserializeRequestBody<LobbyMemberStatuses>(req);
            
        await _lobbyService.UpdateLobbyMemberStatus(lobbyId, client, lobbyMemberStatus);
           
        var response = req.CreateResponse();
        response.StatusCode = HttpStatusCode.OK;
        
        return response;
    }
}