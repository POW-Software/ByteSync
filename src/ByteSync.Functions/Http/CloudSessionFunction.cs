using System.Net;
using ByteSync.Common.Business.Sessions;
using ByteSync.Common.Business.Sessions.Cloud.Connections;
using ByteSync.Functions.Constants;
using ByteSync.Functions.Helpers;
using ByteSync.ServerCommon.Interfaces.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace ByteSync.Functions.Http;

public class CloudSessionFunction
{
    private readonly ICloudSessionsService _cloudSessionsService;
    private readonly ILogger<CloudSessionFunction> _logger;

    public CloudSessionFunction(ICloudSessionsService cloudSessionsService, ILoggerFactory loggerFactory)
    {
        _cloudSessionsService = cloudSessionsService;
        _logger = loggerFactory.CreateLogger<CloudSessionFunction>();
    }
        
    [Function("CreateCloudSessionFunction")]
    public async Task<HttpResponseData> Create([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "session")] HttpRequestData req,
        FunctionContext executionContext)
    {
        var response = req.CreateResponse();
        try
        {
            var client = FunctionHelper.GetClientFromContext(executionContext);
            var createCloudSessionParameters = await FunctionHelper.DeserializeRequestBody<CreateCloudSessionParameters>(req);
            
            var cloudSessionResult = await _cloudSessionsService.CreateCloudSession(createCloudSessionParameters, client);
            
            await response.WriteAsJsonAsync(cloudSessionResult, HttpStatusCode.OK);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while creating session");
            
            await response.WriteAsJsonAsync(new { error = ErrorConstants.INTERNAL_SERVER_ERROR }, HttpStatusCode.InternalServerError);
        }
        
        return response;
    }
    
    [Function("AskPasswordExchangeKeyFunction")]
    public async Task<HttpResponseData> AskPasswordExchangeKey(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "session/{sessionId}/askPasswordExchangeKey")] HttpRequestData req,
        FunctionContext executionContext, string sessionId)
    {
        var response = req.CreateResponse();
        try
        {
            var client = FunctionHelper.GetClientFromContext(executionContext);
            var parameters = await FunctionHelper.DeserializeRequestBody<AskCloudSessionPasswordExchangeKeyParameters>(req);
            
            var result = await _cloudSessionsService.AskCloudSessionPasswordExchangeKey(client, parameters);
            
            await response.WriteAsJsonAsync(result, HttpStatusCode.OK);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while asking PasswordExchangeKey for session {sessionId}", sessionId);
            
            await response.WriteAsJsonAsync(new { error = ErrorConstants.INTERNAL_SERVER_ERROR }, HttpStatusCode.InternalServerError);
        }
        
        return response;
    }
    
    [Function("GetMembersInstanceIdsFunction")]
    public async Task<HttpResponseData> GetMembersInstanceIds(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "session/{sessionId}/membersInstanceIds")] HttpRequestData req,
        FunctionContext executionContext, string sessionId)
    {
        var response = req.CreateResponse();
        try
        {
            var membersInstanceIds = await _cloudSessionsService.GetMembersInstanceIds(sessionId);
            
            await response.WriteAsJsonAsync(membersInstanceIds, HttpStatusCode.OK);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while getting members ids for session {sessionId}", sessionId);
            
            await response.WriteAsJsonAsync(new { error = ErrorConstants.INTERNAL_SERVER_ERROR }, HttpStatusCode.InternalServerError);
        }
        
        return response;
    }
    
    [Function("GetMembersFunction")]
    public async Task<HttpResponseData> GetMembers(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "session/{sessionId}/members")] HttpRequestData req,
        FunctionContext executionContext, string sessionId)
    {
        var response = req.CreateResponse();
        try
        {
            var members = await _cloudSessionsService.GetSessionMembersInfosAsync(sessionId);
            
            await response.WriteAsJsonAsync(members, HttpStatusCode.OK);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while getting members for session {sessionId}", sessionId);
            
            await response.WriteAsJsonAsync(new { error = ErrorConstants.INTERNAL_SERVER_ERROR }, HttpStatusCode.InternalServerError);
        }
        
        return response;
    }
    
    [Function("ValidateJoinCloudSessionFunction")]
    public async Task<HttpResponseData> ValidateJoinCloudSession(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "session/{sessionId}/validateJoin")] HttpRequestData req,
        FunctionContext executionContext, string sessionId)
    {
        HttpResponseData response;
        try
        {
            var parameters = await FunctionHelper.DeserializeRequestBody<ValidateJoinCloudSessionParameters>(req);

            await _cloudSessionsService.ValidateJoinCloudSession(parameters).ConfigureAwait(false);
            
            response = req.CreateResponse(HttpStatusCode.OK);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while validating joining session {sessionId}", sessionId);
            
            response = req.CreateResponse(HttpStatusCode.InternalServerError);
            await response.WriteAsJsonAsync(new { error = ErrorConstants.INTERNAL_SERVER_ERROR }, HttpStatusCode.InternalServerError);
        }
        
        return response;
    }
    
    [Function("FinalizeJoinCloudSessionFunction")]
    public async Task<HttpResponseData> FinalizeJoinCloudSession(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "session/{sessionId}/finalizeJoin")] HttpRequestData req,
        FunctionContext executionContext, string sessionId)
    {
        var response = req.CreateResponse();
        try
        {
            var client = FunctionHelper.GetClientFromContext(executionContext);
            var parameters = await FunctionHelper.DeserializeRequestBody<FinalizeJoinCloudSessionParameters>(req);

            var result = await _cloudSessionsService.FinalizeJoinCloudSession(client, parameters).ConfigureAwait(false);
            
            await response.WriteAsJsonAsync(result, HttpStatusCode.OK);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while finalizing joining session {sessionId}", sessionId);
            
            await response.WriteAsJsonAsync(new { error = ErrorConstants.INTERNAL_SERVER_ERROR }, HttpStatusCode.InternalServerError);
        }
        
        return response;
    }
    
    [Function("AskJoinCloudSessionFunction")]
    public async Task<HttpResponseData> AskJoinCloudSession(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "session/{sessionId}/askJoin")] HttpRequestData req,
        FunctionContext executionContext, string sessionId)
    {
        var response = req.CreateResponse();
        try
        {
            var client = FunctionHelper.GetClientFromContext(executionContext);
            var parameters = await FunctionHelper.DeserializeRequestBody<AskJoinCloudSessionParameters>(req);

            var result = await _cloudSessionsService.AskJoinCloudSession(client, parameters).ConfigureAwait(false);
            
            await response.WriteAsJsonAsync(result, HttpStatusCode.OK);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while asking for joining session {sessionId}", sessionId);
            
            await response.WriteAsJsonAsync(new { error = ErrorConstants.INTERNAL_SERVER_ERROR }, HttpStatusCode.InternalServerError);
        }
        
        return response;
    }
    
    [Function("GiveCloudSessionPasswordExchangeKeyFunction")]
    public async Task<HttpResponseData> GiveCloudSessionPasswordExchangeKey(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "session/{sessionId}/givePassworkExchangeKey")] HttpRequestData req,
        FunctionContext executionContext, string sessionId)
    {
        HttpResponseData response;
        try
        {
            var client = FunctionHelper.GetClientFromContext(executionContext);
            var parameters = await FunctionHelper.DeserializeRequestBody<GiveCloudSessionPasswordExchangeKeyParameters>(req);

            await _cloudSessionsService.GiveCloudSessionPasswordExchangeKey(client, parameters).ConfigureAwait(false);
            
            response = req.CreateResponse(HttpStatusCode.OK);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while giving password exchange key for session {sessionId}", sessionId);
            
            response = req.CreateResponse(HttpStatusCode.InternalServerError);
            await response.WriteAsJsonAsync(new { error = ErrorConstants.INTERNAL_SERVER_ERROR });
        }
        
        return response;
    }
    
    [Function("InformPasswordIsWrongFunction")]
    public async Task<HttpResponseData> InformPasswordIsWrong(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "session/{sessionId}/informPasswordIsWrong")] HttpRequestData req,
        FunctionContext executionContext, string sessionId)
    {
        HttpResponseData response;
        try
        {
            var client = FunctionHelper.GetClientFromContext(executionContext);
            var clientInstanceId = await FunctionHelper.DeserializeRequestBody<string>(req);

            await _cloudSessionsService.InformPasswordIsWrong(client, sessionId, clientInstanceId).ConfigureAwait(false);
            
            response = req.CreateResponse(HttpStatusCode.OK);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while informing that password is wrong for session {sessionId}", sessionId);
            
            response = req.CreateResponse(HttpStatusCode.InternalServerError);
            await response.WriteAsJsonAsync(new { error = ErrorConstants.INTERNAL_SERVER_ERROR });
        }
        
        return response;
    }
    
    [Function("UpdateSettingsFunction")]
    public async Task<HttpResponseData> UpdateSettings(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "session/{sessionId}/updateSettings")] HttpRequestData req,
        FunctionContext executionContext, string sessionId)
    {
        HttpResponseData response;
        try
        {
            var client = FunctionHelper.GetClientFromContext(executionContext);
            var settings = await FunctionHelper.DeserializeRequestBody<EncryptedSessionSettings>(req);

            await _cloudSessionsService.UpdateSessionSettings(client, sessionId, settings).ConfigureAwait(false);
            
            response = req.CreateResponse(HttpStatusCode.OK);
        }
        catch (Exception ex)
        {
           _logger.LogError(ex, "Error while updating settings for session {sessionId}", sessionId);
            
            response = req.CreateResponse(HttpStatusCode.InternalServerError);
            await response.WriteAsJsonAsync(new { error = ErrorConstants.INTERNAL_SERVER_ERROR });
        }
        
        return response;
    }
    
    [Function("QuitFunction")]
    public async Task<HttpResponseData> Quit(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "session/{sessionId}/quit")] HttpRequestData req,
        FunctionContext executionContext, string sessionId)
    {
        HttpResponseData response;
        try
        {
            var client = FunctionHelper.GetClientFromContext(executionContext);

            await _cloudSessionsService.QuitCloudSession(client, sessionId).ConfigureAwait(false);
            
            response = req.CreateResponse(HttpStatusCode.OK);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while quitting session {sessionId}", sessionId);
            
            response = req.CreateResponse(HttpStatusCode.InternalServerError);
            await response.WriteAsJsonAsync(new { error = ErrorConstants.INTERNAL_SERVER_ERROR });
        }
        
        return response;
    }
        
    [Function("ResetFunction")]
    public async Task<HttpResponseData> Reset(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "session/{sessionId}/reset")] HttpRequestData req,
        FunctionContext executionContext, string sessionId)
    {
        HttpResponseData response;
        try
        {
            var client = FunctionHelper.GetClientFromContext(executionContext);

            await _cloudSessionsService.ResetSession(sessionId, client).ConfigureAwait(false);
            
            response = req.CreateResponse(HttpStatusCode.OK);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while resetting session {sessionId}", sessionId);
            
            response = req.CreateResponse(HttpStatusCode.InternalServerError);
            await response.WriteAsJsonAsync(new { error = ErrorConstants.INTERNAL_SERVER_ERROR });
        }
        
        return response;
    }
}