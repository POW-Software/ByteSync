using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using ByteSync.Common.Business.SharedFiles;
using ByteSync.Common.Business.Synchronizations;
using ByteSync.Functions.Constants;
using ByteSync.Functions.Helpers;
using ByteSync.ServerCommon.Interfaces.Services;

namespace ByteSync.Functions.Http;

public class SynchronizationFunction
{
    private readonly ISynchronizationService _synchronizationService;
    private readonly ILogger<SynchronizationFunction> _logger;

    public SynchronizationFunction(ISynchronizationService synchronizationService, ILoggerFactory loggerFactory)
    {
        _synchronizationService = synchronizationService;
        _logger = loggerFactory.CreateLogger<SynchronizationFunction>();
    }
    
    [Function("StartSynchronizationFunction")]
    public async Task<HttpResponseData> StartSynchronization(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "session/{sessionId}/synchronization/start")] 
        HttpRequestData req,
        FunctionContext executionContext, 
        string sessionId)
    {
        var response = req.CreateResponse();
        try
        {
            var client = FunctionHelper.GetClientFromContext(executionContext);
            var synchronizationStartRequest = await FunctionHelper.DeserializeRequestBody<SynchronizationStartRequest>(req);
            
            var result = await _synchronizationService.StartSynchronization(sessionId, client, synchronizationStartRequest.ActionsGroupDefinitions);

            await response.WriteAsJsonAsync(result, HttpStatusCode.OK);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while starting synchronization with sessionId: {sessionId}", sessionId);
            
            await response.WriteAsJsonAsync(new { error = ErrorConstants.INTERNAL_SERVER_ERROR }, HttpStatusCode.InternalServerError);
        }
        
        return response;
    }

    [Function("LocalCopyIsDoneFunction")]
    public async Task<HttpResponseData> LocalCopyIsDone(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "session/{sessionId}/synchronization/localCopyIsDone")] 
        HttpRequestData req,
        FunctionContext executionContext, 
        string sessionId)
    {
        var response = req.CreateResponse();
        try
        {
            var client = FunctionHelper.GetClientFromContext(executionContext);
            List<string> actionsGroupIds = await FunctionHelper.DeserializeRequestBody<List<string>>(req);

            await _synchronizationService.OnLocalCopyIsDoneAsync(sessionId, actionsGroupIds, client);
            
            response.StatusCode = HttpStatusCode.OK;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while handling information that local copy is done with sessionId: {sessionId}", sessionId);
            
            await response.WriteAsJsonAsync(new { error = ErrorConstants.INTERNAL_SERVER_ERROR }, HttpStatusCode.InternalServerError);
        }

        return response;
    }
    
    [Function("DateIsCopiedFunction")]
    public async Task<HttpResponseData> DateIsCopied(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "session/{sessionId}/synchronization/dateIsCopied")] 
        HttpRequestData req,
        FunctionContext executionContext, 
        string sessionId)
    {
        var response = req.CreateResponse();
        try
        {
            var client = FunctionHelper.GetClientFromContext(executionContext);
            List<string> actionsGroupIds = await FunctionHelper.DeserializeRequestBody<List<string>>(req);

            await _synchronizationService.OnDateIsCopied(sessionId, actionsGroupIds, client);
            
            response.StatusCode = HttpStatusCode.OK;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while handling information that date is copied with sessionId: {sessionId}", sessionId);
            
            await response.WriteAsJsonAsync(new { error = ErrorConstants.INTERNAL_SERVER_ERROR }, HttpStatusCode.InternalServerError);
        }

        return response;
    }
    
    [Function("FileOrDirectoryIsDeletedFunction")]
    public async Task<HttpResponseData> FileOrDirectoryIsDeleted(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "session/{sessionId}/synchronization/fileOrDirectoryIsDeleted")] 
        HttpRequestData req,
        FunctionContext executionContext, 
        string sessionId)
    {
        var response = req.CreateResponse();
        try
        {
            var client = FunctionHelper.GetClientFromContext(executionContext);
            List<string> actionsGroupIds = await FunctionHelper.DeserializeRequestBody<List<string>>(req);

            await _synchronizationService.OnFileOrDirectoryIsDeletedAsync(sessionId, actionsGroupIds, client);
            
            response.StatusCode = HttpStatusCode.OK;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while handling information that file or directory is deleted with sessionId: {sessionId}", sessionId);
            
            await response.WriteAsJsonAsync(new { error = ErrorConstants.INTERNAL_SERVER_ERROR }, HttpStatusCode.InternalServerError);
        }

        return response;
    }
    
    [Function("DirectoryIsCreatedFunction")]
    public async Task<HttpResponseData> DirectoryIsCreated(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "session/{sessionId}/synchronization/directoryIsCreated")] 
        HttpRequestData req,
        FunctionContext executionContext, 
        string sessionId)
    {
        var response = req.CreateResponse();
        try
        {
            var client = FunctionHelper.GetClientFromContext(executionContext);
            List<string> actionsGroupIds = await FunctionHelper.DeserializeRequestBody<List<string>>(req);

            await _synchronizationService.OnDirectoryIsCreatedAsync(sessionId, actionsGroupIds, client);
            
            response.StatusCode = HttpStatusCode.OK;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while handling information that directory is created with sessionId: {sessionId}", sessionId);
            
            await response.WriteAsJsonAsync(new { error = ErrorConstants.INTERNAL_SERVER_ERROR }, HttpStatusCode.InternalServerError);
        }

        return response;
    }
    
    [Function("MemberHasFinishedFunction")]
    public async Task<HttpResponseData> MemberHasFinished(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "session/{sessionId}/synchronization/memberHasFinished")] 
        HttpRequestData req,
        FunctionContext executionContext, 
        string sessionId)
    {
        var response = req.CreateResponse();
        try
        {
            var client = FunctionHelper.GetClientFromContext(executionContext);

            await _synchronizationService.OnMemberHasFinished(sessionId, client);
            
            response.StatusCode = HttpStatusCode.OK;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while handling information that member has finished synchronization with sessionId: {sessionId}", sessionId);
            
            await response.WriteAsJsonAsync(new { error = ErrorConstants.INTERNAL_SERVER_ERROR }, HttpStatusCode.InternalServerError);
        }

        return response;
    }
    
    [Function("RequestSynchronizationAbortFunction")]
    public async Task<HttpResponseData> Abort(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "session/{sessionId}/synchronization/abort")] 
        HttpRequestData req,
        FunctionContext executionContext, 
        string sessionId)
    {
        var response = req.CreateResponse();
        try
        {
            var client = FunctionHelper.GetClientFromContext(executionContext);

            await _synchronizationService.RequestAbortSynchronization(sessionId, client);
            
            response.StatusCode = HttpStatusCode.OK;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while handling request to abort synchronization with sessionId: {sessionId}", sessionId);
            
            await response.WriteAsJsonAsync(new { error = ErrorConstants.INTERNAL_SERVER_ERROR }, HttpStatusCode.InternalServerError);
        }

        return response;
    }
    
    [Function("SynchronizationErrorFunction")]
    public async Task<HttpResponseData> SynchronizationError(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "session/{sessionId}/synchronization/error")] 
        HttpRequestData req,
        FunctionContext executionContext, 
        string sessionId)
    {
        var response = req.CreateResponse();
        try
        {
            var client = FunctionHelper.GetClientFromContext(executionContext);
            var sharedFileDefinition = await FunctionHelper.DeserializeRequestBody<SharedFileDefinition>(req);

            await _synchronizationService.AssertSynchronizationActionErrors(sessionId, sharedFileDefinition.ActionsGroupIds!, client);
            
            response.StatusCode = HttpStatusCode.OK;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while handling synchronization error with sessionId: {sessionId}", sessionId);
            
            await response.WriteAsJsonAsync(new { error = ErrorConstants.INTERNAL_SERVER_ERROR }, HttpStatusCode.InternalServerError);
        }

        return response;
    }
    
    [Function("SynchronizationErrorsFunction")]
    public async Task<HttpResponseData> SynchronizationErrors(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "session/{sessionId}/synchronization/errors")] 
        HttpRequestData req,
        FunctionContext executionContext, 
        string sessionId)
    {
        var response = req.CreateResponse();
        try
        {
            var client = FunctionHelper.GetClientFromContext(executionContext);
            var actionsGroupIds = await FunctionHelper.DeserializeRequestBody<List<string>>(req);

            await _synchronizationService.AssertSynchronizationActionErrors(sessionId, actionsGroupIds, client);
            
            response.StatusCode = HttpStatusCode.OK;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while handling synchronization errors with sessionId: {sessionId}", sessionId);
            
            response.StatusCode = HttpStatusCode.InternalServerError;
            await response.WriteAsJsonAsync(new { error = ErrorConstants.INTERNAL_SERVER_ERROR }, HttpStatusCode.InternalServerError);
        }

        return response;
    }
}