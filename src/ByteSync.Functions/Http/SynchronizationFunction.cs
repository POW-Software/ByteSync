using ByteSync.Common.Business.SharedFiles;
using ByteSync.Common.Business.Synchronizations;
using ByteSync.Functions.Helpers;
using ByteSync.ServerCommon.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace ByteSync.Functions.Http;

public class SynchronizationFunction
{
    private readonly ISynchronizationService _synchronizationService;
    private readonly ILogger _logger;

    public SynchronizationFunction(ISynchronizationService synchronizationService, ILoggerFactory loggerFactory)
    {
        _synchronizationService = synchronizationService;
        _logger = loggerFactory.CreateLogger<SynchronizationFunction>();
    }
    
    [Function("StartSynchronizationFunction")]
    public async Task<IActionResult> StartSynchronization(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "session/{sessionId}/synchronization/start")] HttpRequestData req,
        FunctionContext executionContext, string sessionId)
    {
        try
        {
            var client = FunctionHelper.GetClientFromContext(executionContext);
            var synchronizationStartRequest = await FunctionHelper.DeserializeRequestBody<SynchronizationStartRequest>(req);
            
            var result = await _synchronizationService.StartSynchronization(sessionId, client, synchronizationStartRequest.ActionsGroupDefinitions);
            return new OkObjectResult(result);
        }
        catch (Exception ex)
        {
           _logger.LogError(ex, "Error while starting synchronization with sessionId: {sessionId}", sessionId);
            
            return new ObjectResult(new { error = "An internal server error occurred." })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }

    [Function("LocalCopyIsDoneFunction")]
    public async Task<IActionResult> LocalCopyIsDone(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "session/{sessionId}/synchronization/localCopyIsDone")] HttpRequestData req,
        FunctionContext executionContext, string sessionId)
    {
        try
        {
            var client = FunctionHelper.GetClientFromContext(executionContext);
            List<string> actionsGroupIds = await FunctionHelper.DeserializeRequestBody<List<string>>(req);

            await _synchronizationService.OnLocalCopyIsDoneAsync(sessionId, actionsGroupIds, client);
            return new OkResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while handling information that local copy is done with sessionId: {sessionId}", sessionId);
            
            return new ObjectResult(new { error = "An internal server error occurred." })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }
    
    [Function("DateIsCopiedFunction")]
    public async Task<IActionResult> DateIsCopied(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "session/{sessionId}/synchronization/dateIsCopied")] HttpRequestData req,
        FunctionContext executionContext, string sessionId)
    {
        try
        {
            var client = FunctionHelper.GetClientFromContext(executionContext);
            List<string> actionsGroupIds = await FunctionHelper.DeserializeRequestBody<List<string>>(req);

            await _synchronizationService.OnDateIsCopied(sessionId, actionsGroupIds, client);
            return new OkResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while handling information that local copy is done with sessionId: {sessionId}", sessionId);
            
            return new ObjectResult(new { error = "An internal server error occurred." })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }
    
    [Function("FileOrDirectoryIsDeletedFunction")]
    public async Task<IActionResult> FileOrDirectoryIsDeleted(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "session/{sessionId}/synchronization/fileOrDirectoryIsDeleted")] HttpRequestData req,
        FunctionContext executionContext, string sessionId)
    {
        try
        {
            var client = FunctionHelper.GetClientFromContext(executionContext);
            List<string> actionsGroupIds = await FunctionHelper.DeserializeRequestBody<List<string>>(req);

            await _synchronizationService.OnFileOrDirectoryIsDeletedAsync(sessionId, actionsGroupIds, client);
            return new OkResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while handling information that file or directory is deleted with sessionId: {sessionId}", sessionId);
            
            return new ObjectResult(new { error = "An internal server error occurred." })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }
    
    [Function("DirectoryIsCreatedFunction")]
    public async Task<IActionResult> DirectoryIsCreated(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "session/{sessionId}/synchronization/directoryIsCreated")] HttpRequestData req,
        FunctionContext executionContext, string sessionId)
    {
        try
        {
            var client = FunctionHelper.GetClientFromContext(executionContext);
            List<string> actionsGroupIds = await FunctionHelper.DeserializeRequestBody<List<string>>(req);

            await _synchronizationService.OnDirectoryIsCreatedAsync(sessionId, actionsGroupIds, client);
            return new OkResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while handling information that directory is created with sessionId: {sessionId}", sessionId);
            
            return new ObjectResult(new { error = "An internal server error occurred." })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }
    
    [Function("MemberHasFinishedFunction")]
    public async Task<IActionResult> MemberHasFinished(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "session/{sessionId}/synchronization/memberHasFinished")] HttpRequestData req,
        FunctionContext executionContext, string sessionId)
    {
        try
        {
            var client = FunctionHelper.GetClientFromContext(executionContext);

            await _synchronizationService.OnMemberHasFinished(sessionId, client);
            return new OkResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while handling information that member has finished synchronization with sessionId: {sessionId}", sessionId);
            
            return new ObjectResult(new { error = "An internal server error occurred." })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }
    
    [Function("RequestSynchronizationAbortFunction")]
    public async Task<IActionResult> Abort(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "session/{sessionId}/synchronization/abort")] HttpRequestData req,
        FunctionContext executionContext, string sessionId)
    {
        try
        {
            var client = FunctionHelper.GetClientFromContext(executionContext);

            await _synchronizationService.RequestAbortSynchronization(sessionId, client);
            return new OkResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while handling request to abort synchronization with sessionId: {sessionId}", sessionId);
            
            return new ObjectResult(new { error = "An internal server error occurred." })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }
    
    [Function("SynchronizationErrorFunction")]
    public async Task<IActionResult> SynchronizationError(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "session/{sessionId}/synchronization/error")] HttpRequestData req,
        FunctionContext executionContext, string sessionId)
    {
        try
        {
            var client = FunctionHelper.GetClientFromContext(executionContext);
            var sharedFileDefinition = await FunctionHelper.DeserializeRequestBody<SharedFileDefinition>(req);

            await _synchronizationService.AssertSynchronizationActionErrors(sessionId, sharedFileDefinition.ActionsGroupIds!, client);
            return new OkResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while handling synchronization error with sessionId: {sessionId}", sessionId);
            
            return new ObjectResult(new { error = "An internal server error occurred." })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }
    
    [Function("SynchronizationErrorsFunction")]
    public async Task<IActionResult> SynchronizationErrors(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "session/{sessionId}/synchronization/errors")] HttpRequestData req,
        FunctionContext executionContext, string sessionId)
    {
        try
        {
            var client = FunctionHelper.GetClientFromContext(executionContext);
            var actionsGroupIds = await FunctionHelper.DeserializeRequestBody<List<string>>(req);

            await _synchronizationService.AssertSynchronizationActionErrors(sessionId, actionsGroupIds, client);
            return new OkResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while handling synchronization errors with sessionId: {sessionId}", sessionId);
            
            return new ObjectResult(new { error = "An internal server error occurred." })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }
}