using ByteSync.Common.Business.Sessions;
using ByteSync.Common.Business.Sessions.Cloud.Connections;
using ByteSync.Functions.Helpers;
using ByteSync.ServerCommon.Interfaces.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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
    public async Task<IActionResult> Create([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "session")] HttpRequestData req,
        FunctionContext executionContext)
    {
        try
        {
            var client = FunctionHelper.GetClientFromContext(executionContext);
            var createCloudSessionParameters = await FunctionHelper.DeserializeRequestBody<CreateCloudSessionParameters>(req);
            
            var cloudSessionResult = await _cloudSessionsService.CreateCloudSession(createCloudSessionParameters, client);
            
            return new OkObjectResult(cloudSessionResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while creating session");
            
            return new ObjectResult(new { error = "An internal server error occurred." })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }
    
    [Function("AskPasswordExchangeKeyFunction")]
    public async Task<IActionResult> AskPasswordExchangeKey(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "session/{sessionId}/askPasswordExchangeKey")] HttpRequestData req,
        FunctionContext executionContext, string sessionId)
    {
        try
        {
            var client = FunctionHelper.GetClientFromContext(executionContext);
            var parameters = await FunctionHelper.DeserializeRequestBody<AskCloudSessionPasswordExchangeKeyParameters>(req);
            
            var result = await _cloudSessionsService.AskCloudSessionPasswordExchangeKey(client, parameters);
            
            return new OkObjectResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while asking PasswordExchangeKey for session {sessionId}", sessionId);
            
            return new ObjectResult(new { error = "An internal server error occurred." })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }
    
    [Function("GetMembersInstanceIdsFunction")]
    public async Task<IActionResult> GetMembersInstanceIds(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "session/{sessionId}/membersInstanceIds")] HttpRequestData req,
        FunctionContext executionContext, string sessionId)
    {
        try
        {
            var membersInstanceIds = await _cloudSessionsService.GetMembersInstanceIds(sessionId);
            
            return new OkObjectResult(membersInstanceIds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while getting members ids for session {sessionId}", sessionId);
            
            return new ObjectResult(new { error = "An internal server error occurred." })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }
    
    [Function("GetMembersFunction")]
    public async Task<IActionResult> GetMembers(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "session/{sessionId}/members")] HttpRequestData req,
        FunctionContext executionContext, string sessionId)
    {
        try
        {
            var members = await _cloudSessionsService.GetSessionMembersInfosAsync(sessionId);
            
            return new OkObjectResult(members);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while getting members for session {sessionId}", sessionId);
            
            return new ObjectResult(new { error = "An internal server error occurred." })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }
    
    [Function("ValidateJoinCloudSessionFunction")]
    public async Task<IActionResult> ValidateJoinCloudSession(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "session/{sessionId}/validateJoin")] HttpRequestData req,
        FunctionContext executionContext, string sessionId)
    {
        try
        {
            var parameters = await FunctionHelper.DeserializeRequestBody<ValidateJoinCloudSessionParameters>(req);

            await _cloudSessionsService.ValidateJoinCloudSession(parameters).ConfigureAwait(false);
            
            return new OkResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while validating joining session {sessionId}", sessionId);
            
            return new ObjectResult(new { error = "An internal server error occurred." })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }
    
    [Function("FinalizeJoinCloudSessionFunction")]
    public async Task<IActionResult> FinalizeJoinCloudSession(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "session/{sessionId}/finalizeJoin")] HttpRequestData req,
        FunctionContext executionContext, string sessionId)
    {
        try
        {
            var client = FunctionHelper.GetClientFromContext(executionContext);
            var parameters = await FunctionHelper.DeserializeRequestBody<FinalizeJoinCloudSessionParameters>(req);

            var result = await _cloudSessionsService.FinalizeJoinCloudSession(client, parameters).ConfigureAwait(false);
            
            return new OkObjectResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while finalizing joining session {sessionId}", sessionId);
            
            return new ObjectResult(new { error = "An internal server error occurred." })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }
    
    [Function("AskJoinCloudSessionFunction")]
    public async Task<IActionResult> AskJoinCloudSession(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "session/{sessionId}/askJoin")] HttpRequestData req,
        FunctionContext executionContext, string sessionId)
    {
        try
        {
            var client = FunctionHelper.GetClientFromContext(executionContext);
            var parameters = await FunctionHelper.DeserializeRequestBody<AskJoinCloudSessionParameters>(req);

            var result = await _cloudSessionsService.AskJoinCloudSession(client, parameters).ConfigureAwait(false);
            
            return new OkObjectResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while asking for joining session {sessionId}", sessionId);
            
            return new ObjectResult(new { error = "An internal server error occurred." })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }
    
    [Function("GiveCloudSessionPasswordExchangeKeyFunction")]
    public async Task<IActionResult> GiveCloudSessionPasswordExchangeKey(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "session/{sessionId}/givePassworkExchangeKey")] HttpRequestData req,
        FunctionContext executionContext, string sessionId)
    {
        try
        {
            var client = FunctionHelper.GetClientFromContext(executionContext);
            var parameters = await FunctionHelper.DeserializeRequestBody<GiveCloudSessionPasswordExchangeKeyParameters>(req);

            await _cloudSessionsService.GiveCloudSessionPasswordExchangeKey(client, parameters).ConfigureAwait(false);
            
            return new OkResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while giving password exchange key for session {sessionId}", sessionId);
            
            return new ObjectResult(new { error = "An internal server error occurred." })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }
    
    [Function("InformPasswordIsWrongFunction")]
    public async Task<IActionResult> InformPasswordIsWrong(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "session/{sessionId}/informPasswordIsWrong")] HttpRequestData req,
        FunctionContext executionContext, string sessionId)
    {
        try
        {
            var client = FunctionHelper.GetClientFromContext(executionContext);
            var clientInstanceId = await FunctionHelper.DeserializeRequestBody<string>(req);

            await _cloudSessionsService.InformPasswordIsWrong(client, sessionId, clientInstanceId).ConfigureAwait(false);
            
            return new OkResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while informing that password is wrong for session {sessionId}", sessionId);
            
            return new ObjectResult(new { error = "An internal server error occurred." })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }
    
    [Function("UpdateSettingsFunction")]
    public async Task<IActionResult> UpdateSettings(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "session/{sessionId}/updateSettings")] HttpRequestData req,
        FunctionContext executionContext, string sessionId)
    {
        try
        {
            var client = FunctionHelper.GetClientFromContext(executionContext);
            var settings = await FunctionHelper.DeserializeRequestBody<EncryptedSessionSettings>(req);

            await _cloudSessionsService.UpdateSessionSettings(client, sessionId, settings).ConfigureAwait(false);
            
            return new OkResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while quitting session {sessionId}", sessionId);
            
            return new ObjectResult(new { error = "An internal server error occurred." })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }
    
    [Function("QuitFunction")]
    public async Task<IActionResult> Quit(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "session/{sessionId}/quit")] HttpRequestData req,
        FunctionContext executionContext, string sessionId)
    {
        try
        {
            var client = FunctionHelper.GetClientFromContext(executionContext);

            await _cloudSessionsService.QuitCloudSession(client, sessionId).ConfigureAwait(false);
            
            return new OkResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while quitting session {sessionId}", sessionId);
            
            return new ObjectResult(new { error = "An internal server error occurred." })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }
        
    [Function("ResetFunction")]
    public async Task<IActionResult> Reset(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "session/{sessionId}/reset")] HttpRequestData req,
        FunctionContext executionContext, string sessionId)
    {
        try
        {
            var client = FunctionHelper.GetClientFromContext(executionContext);

            await _cloudSessionsService.ResetSession(sessionId, client).ConfigureAwait(false);
            
            return new OkResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while resetting session {sessionId}", sessionId);
            
            return new ObjectResult(new { error = "An internal server error occurred." })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }
}