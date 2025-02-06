using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using ByteSync.Common.Business.SharedFiles;
using ByteSync.Common.Business.Synchronizations;
using ByteSync.Functions.Helpers;
using ByteSync.ServerCommon.Interfaces.Services;

namespace ByteSync.Functions.Http;

public class SynchronizationFunction
{
    private readonly ISynchronizationService _synchronizationService;

    public SynchronizationFunction(ISynchronizationService synchronizationService)
    {
        _synchronizationService = synchronizationService;
    }
    
    [Function("StartSynchronizationFunction")]
    public async Task<HttpResponseData> StartSynchronization(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "session/{sessionId}/synchronization/start")] 
        HttpRequestData req,
        FunctionContext executionContext, 
        string sessionId)
    {
        var client = FunctionHelper.GetClientFromContext(executionContext);
        var synchronizationStartRequest = await FunctionHelper.DeserializeRequestBody<SynchronizationStartRequest>(req);
            
        var result = await _synchronizationService.StartSynchronization(sessionId, client, synchronizationStartRequest.ActionsGroupDefinitions);

        var response = req.CreateResponse();
        await response.WriteAsJsonAsync(result, HttpStatusCode.OK);
        
        return response;
    }

    [Function("LocalCopyIsDoneFunction")]
    public async Task<HttpResponseData> LocalCopyIsDone(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "session/{sessionId}/synchronization/localCopyIsDone")] 
        HttpRequestData req,
        FunctionContext executionContext, 
        string sessionId)
    {
        var client = FunctionHelper.GetClientFromContext(executionContext);
        List<string> actionsGroupIds = await FunctionHelper.DeserializeRequestBody<List<string>>(req);

        await _synchronizationService.OnLocalCopyIsDoneAsync(sessionId, actionsGroupIds, client);
            
        var response = req.CreateResponse();
        response.StatusCode = HttpStatusCode.OK;

        return response;
    }
    
    [Function("DateIsCopiedFunction")]
    public async Task<HttpResponseData> DateIsCopied(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "session/{sessionId}/synchronization/dateIsCopied")] 
        HttpRequestData req,
        FunctionContext executionContext, 
        string sessionId)
    {
        var client = FunctionHelper.GetClientFromContext(executionContext);
        List<string> actionsGroupIds = await FunctionHelper.DeserializeRequestBody<List<string>>(req);

        await _synchronizationService.OnDateIsCopied(sessionId, actionsGroupIds, client);
        
        var response = req.CreateResponse();
        response.StatusCode = HttpStatusCode.OK;

        return response;
    }
    
    [Function("FileOrDirectoryIsDeletedFunction")]
    public async Task<HttpResponseData> FileOrDirectoryIsDeleted(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "session/{sessionId}/synchronization/fileOrDirectoryIsDeleted")] 
        HttpRequestData req,
        FunctionContext executionContext, 
        string sessionId)
    {
        var client = FunctionHelper.GetClientFromContext(executionContext);
        List<string> actionsGroupIds = await FunctionHelper.DeserializeRequestBody<List<string>>(req);

        await _synchronizationService.OnFileOrDirectoryIsDeletedAsync(sessionId, actionsGroupIds, client);
            
        var response = req.CreateResponse();
        response.StatusCode = HttpStatusCode.OK;

        return response;
    }
    
    [Function("DirectoryIsCreatedFunction")]
    public async Task<HttpResponseData> DirectoryIsCreated(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "session/{sessionId}/synchronization/directoryIsCreated")] 
        HttpRequestData req,
        FunctionContext executionContext, 
        string sessionId)
    {
        var client = FunctionHelper.GetClientFromContext(executionContext);
        List<string> actionsGroupIds = await FunctionHelper.DeserializeRequestBody<List<string>>(req);

        await _synchronizationService.OnDirectoryIsCreatedAsync(sessionId, actionsGroupIds, client);
            
        var response = req.CreateResponse();
        response.StatusCode = HttpStatusCode.OK;
        
        return response;
    }
    
    [Function("MemberHasFinishedFunction")]
    public async Task<HttpResponseData> MemberHasFinished(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "session/{sessionId}/synchronization/memberHasFinished")] 
        HttpRequestData req,
        FunctionContext executionContext, 
        string sessionId)
    {
        var client = FunctionHelper.GetClientFromContext(executionContext);

        await _synchronizationService.OnMemberHasFinished(sessionId, client);
            
        var response = req.CreateResponse();
        response.StatusCode = HttpStatusCode.OK;

        return response;
    }
    
    [Function("RequestSynchronizationAbortFunction")]
    public async Task<HttpResponseData> Abort(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "session/{sessionId}/synchronization/abort")] 
        HttpRequestData req,
        FunctionContext executionContext, 
        string sessionId)
    {
        var client = FunctionHelper.GetClientFromContext(executionContext);

        await _synchronizationService.RequestAbortSynchronization(sessionId, client);
      
        var response = req.CreateResponse();
        response.StatusCode = HttpStatusCode.OK;

        return response;
    }
    
    [Function("SynchronizationErrorFunction")]
    public async Task<HttpResponseData> SynchronizationError(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "session/{sessionId}/synchronization/error")] 
        HttpRequestData req,
        FunctionContext executionContext, 
        string sessionId)
    {
        var client = FunctionHelper.GetClientFromContext(executionContext);
        var sharedFileDefinition = await FunctionHelper.DeserializeRequestBody<SharedFileDefinition>(req);

        await _synchronizationService.AssertSynchronizationActionErrors(sessionId, sharedFileDefinition.ActionsGroupIds!, client);
            
        var response = req.CreateResponse();
        response.StatusCode = HttpStatusCode.OK;

        return response;
    }
    
    [Function("SynchronizationErrorsFunction")]
    public async Task<HttpResponseData> SynchronizationErrors(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "session/{sessionId}/synchronization/errors")] 
        HttpRequestData req,
        FunctionContext executionContext, 
        string sessionId)
    {
        var client = FunctionHelper.GetClientFromContext(executionContext);
        var actionsGroupIds = await FunctionHelper.DeserializeRequestBody<List<string>>(req);

        await _synchronizationService.AssertSynchronizationActionErrors(sessionId, actionsGroupIds, client);
            
        var response = req.CreateResponse();
        response.StatusCode = HttpStatusCode.OK;

        return response;
    }
}