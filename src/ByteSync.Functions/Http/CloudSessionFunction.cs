using System.Net;
using ByteSync.Common.Business.Sessions;
using ByteSync.Common.Business.Sessions.Cloud.Connections;
using ByteSync.Functions.Helpers.Misc;
using ByteSync.ServerCommon.Commands.CloudSessions;
using ByteSync.ServerCommon.Interfaces.Services;
using MediatR;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace ByteSync.Functions.Http;

public class CloudSessionFunction
{
    private readonly ICloudSessionsService _cloudSessionsService;
    private readonly IMediator _mediator;

    public CloudSessionFunction(ICloudSessionsService cloudSessionsService, IMediator mediator)
    {
        _cloudSessionsService = cloudSessionsService;
        _mediator = mediator;
    }
        
    [Function("CreateCloudSessionFunction")]
    public async Task<HttpResponseData> Create([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "session")] HttpRequestData req,
        FunctionContext executionContext)
    {
        var client = FunctionHelper.GetClientFromContext(executionContext);
        var createCloudSessionParameters = await FunctionHelper.DeserializeRequestBody<CreateCloudSessionParameters>(req);
            
        var cloudSessionResult = await _cloudSessionsService.CreateCloudSession(createCloudSessionParameters, client);
            
        var response = req.CreateResponse();
        await response.WriteAsJsonAsync(cloudSessionResult, HttpStatusCode.OK);
        
        return response;
    }
    
    [Function("AskPasswordExchangeKeyFunction")]
    public async Task<HttpResponseData> AskPasswordExchangeKey(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "session/{sessionId}/askPasswordExchangeKey")] HttpRequestData req,
        FunctionContext executionContext, string sessionId)
    {
        var client = FunctionHelper.GetClientFromContext(executionContext);
        var parameters = await FunctionHelper.DeserializeRequestBody<AskCloudSessionPasswordExchangeKeyParameters>(req);

        var result = await _cloudSessionsService.AskCloudSessionPasswordExchangeKey(client, parameters);

        var response = req.CreateResponse();
        await response.WriteAsJsonAsync(result, HttpStatusCode.OK);

        return response;
    }
    
    [Function("GetMembersInstanceIdsFunction")]
    public async Task<HttpResponseData> GetMembersInstanceIds(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "session/{sessionId}/membersInstanceIds")] HttpRequestData req,
        FunctionContext executionContext, string sessionId)
    {
        var membersInstanceIds = await _cloudSessionsService.GetMembersInstanceIds(sessionId);

        var response = req.CreateResponse();
        await response.WriteAsJsonAsync(membersInstanceIds, HttpStatusCode.OK);

        return response;
    }
    
    [Function("GetMembersFunction")]
    public async Task<HttpResponseData> GetMembers(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "session/{sessionId}/members")] HttpRequestData req,
        FunctionContext executionContext, string sessionId)
    {
        var members = await _cloudSessionsService.GetSessionMembersInfosAsync(sessionId);
            
        var response = req.CreateResponse();
        await response.WriteAsJsonAsync(members, HttpStatusCode.OK);
        
        return response;
    }
    
    [Function("ValidateJoinCloudSessionFunction")]
    public async Task<HttpResponseData> ValidateJoinCloudSession(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "session/{sessionId}/validateJoin")] HttpRequestData req,
        FunctionContext executionContext, string sessionId)
    {
        var parameters = await FunctionHelper.DeserializeRequestBody<ValidateJoinCloudSessionParameters>(req);

        await _cloudSessionsService.ValidateJoinCloudSession(parameters).ConfigureAwait(false);
        
        var response = req.CreateResponse(HttpStatusCode.OK);
        
        return response;
    }
    
    [Function("FinalizeJoinCloudSessionFunction")]
    public async Task<HttpResponseData> FinalizeJoinCloudSession(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "session/{sessionId}/finalizeJoin")] HttpRequestData req,
        FunctionContext executionContext, string sessionId)
    {
       
        var client = FunctionHelper.GetClientFromContext(executionContext);
        var parameters = await FunctionHelper.DeserializeRequestBody<FinalizeJoinCloudSessionParameters>(req);

        var result = await _cloudSessionsService.FinalizeJoinCloudSession(client, parameters).ConfigureAwait(false);
            
        var response = req.CreateResponse();
        await response.WriteAsJsonAsync(result, HttpStatusCode.OK);
        
        return response;
    }
    
    [Function("AskJoinCloudSessionFunction")]
    public async Task<HttpResponseData> AskJoinCloudSession(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "session/{sessionId}/askJoin")] HttpRequestData req,
        FunctionContext executionContext, string sessionId)
    {
        var client = FunctionHelper.GetClientFromContext(executionContext);
        var parameters = await FunctionHelper.DeserializeRequestBody<AskJoinCloudSessionParameters>(req);

        var result = await _cloudSessionsService.AskJoinCloudSession(client, parameters).ConfigureAwait(false);
            
        var response = req.CreateResponse();
        await response.WriteAsJsonAsync(result, HttpStatusCode.OK);
        
        return response;
    }
    
    [Function("GiveCloudSessionPasswordExchangeKeyFunction")]
    public async Task<HttpResponseData> GiveCloudSessionPasswordExchangeKey(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "session/{sessionId}/givePassworkExchangeKey")] HttpRequestData req,
        FunctionContext executionContext, string sessionId)
    {
        var client = FunctionHelper.GetClientFromContext(executionContext);
        var parameters = await FunctionHelper.DeserializeRequestBody<GiveCloudSessionPasswordExchangeKeyParameters>(req);

        await _cloudSessionsService.GiveCloudSessionPasswordExchangeKey(client, parameters).ConfigureAwait(false);
            
        var response = req.CreateResponse(HttpStatusCode.OK);
        
        return response;
    }
    
    [Function("InformPasswordIsWrongFunction")]
    public async Task<HttpResponseData> InformPasswordIsWrong(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "session/{sessionId}/informPasswordIsWrong")] HttpRequestData req,
        FunctionContext executionContext, string sessionId)
    {
        var client = FunctionHelper.GetClientFromContext(executionContext);
        var clientInstanceId = await FunctionHelper.DeserializeRequestBody<string>(req);

        await _cloudSessionsService.InformPasswordIsWrong(client, sessionId, clientInstanceId).ConfigureAwait(false);
            
        var response = req.CreateResponse(HttpStatusCode.OK);
        
        return response;
    }
    
    [Function("UpdateSettingsFunction")]
    public async Task<HttpResponseData> UpdateSettings(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "session/{sessionId}/updateSettings")] HttpRequestData req,
        FunctionContext executionContext, string sessionId)
    {
        var client = FunctionHelper.GetClientFromContext(executionContext);
        var settings = await FunctionHelper.DeserializeRequestBody<EncryptedSessionSettings>(req);

        var request = new UpdateSessionSettingsRequest(sessionId, client, settings);
        await _mediator.Send(request);
            
        var response = req.CreateResponse(HttpStatusCode.OK);
        
        return response;
    }
    
    [Function("QuitFunction")]
    public async Task<HttpResponseData> Quit(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "session/{sessionId}/quit")] HttpRequestData req,
        FunctionContext executionContext, string sessionId)
    {
        var client = FunctionHelper.GetClientFromContext(executionContext);

        var request = new QuitSessionRequest(sessionId, client);
        await _mediator.Send(request);
            
        var response = req.CreateResponse(HttpStatusCode.OK);
        
        return response;
    }
        
    [Function("ResetFunction")]
    public async Task<HttpResponseData> Reset(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "session/{sessionId}/reset")] HttpRequestData req,
        FunctionContext executionContext, string sessionId)
    {
        var client = FunctionHelper.GetClientFromContext(executionContext);

        await _cloudSessionsService.ResetSession(sessionId, client).ConfigureAwait(false);
            
        var response = req.CreateResponse(HttpStatusCode.OK);
        
        return response;
    }
}