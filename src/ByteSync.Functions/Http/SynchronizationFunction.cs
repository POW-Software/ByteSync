using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using ByteSync.Common.Business.SharedFiles;
using ByteSync.Common.Business.Synchronizations;
using ByteSync.Functions.Helpers.Misc;
using ByteSync.ServerCommon.Commands.Synchronizations;
using MediatR;

namespace ByteSync.Functions.Http;

public class SynchronizationFunction
{
    private readonly IMediator _mediator;

    public SynchronizationFunction(IMediator mediator)
    {
        _mediator = mediator;
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
            
        var request = new StartSynchronizationRequest(sessionId, client, synchronizationStartRequest.ActionsGroupDefinitions);
        await _mediator.Send(request);

        var response = req.CreateResponse();
        response.StatusCode = HttpStatusCode.OK;
        
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
        var synchronizationActionRequest = await FunctionHelper.DeserializeRequestBody<SynchronizationActionRequest>(req);

        var request = new LocalCopyIsDoneRequest(sessionId, client, synchronizationActionRequest.ActionsGroupIds, synchronizationActionRequest.NodeId);
        await _mediator.Send(request);
            
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
        var synchronizationActionRequest = await FunctionHelper.DeserializeRequestBody<SynchronizationActionRequest>(req);

        var request = new DateIsCopiedRequest(sessionId, client, synchronizationActionRequest.ActionsGroupIds, synchronizationActionRequest.NodeId);
        await _mediator.Send(request);
        
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
        var synchronizationActionRequest = await FunctionHelper.DeserializeRequestBody<SynchronizationActionRequest>(req);

        var request = new FileOrDirectoryIsDeletedRequest(sessionId, client, synchronizationActionRequest.ActionsGroupIds, synchronizationActionRequest.NodeId);
        await _mediator.Send(request);
            
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
        var synchronizationActionRequest = await FunctionHelper.DeserializeRequestBody<SynchronizationActionRequest>(req);

        var request = new DirectoryIsCreatedRequest(sessionId, client, synchronizationActionRequest.ActionsGroupIds, synchronizationActionRequest.NodeId);
        await _mediator.Send(request);
            
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

        var request = new MemberHasFinishedRequest(sessionId, client);
        await _mediator.Send(request);
            
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

        var request = new RequestSynchronizationAbortRequest(sessionId, client);
        await _mediator.Send(request);
      
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
        var synchronizationActionRequest = await FunctionHelper.DeserializeRequestBody<SynchronizationActionRequest>(req);

        var request = new SynchronizationErrorsRequest(sessionId, client, synchronizationActionRequest.ActionsGroupIds, 
            synchronizationActionRequest.NodeId);
        await _mediator.Send(request);
            
        var response = req.CreateResponse();
        response.StatusCode = HttpStatusCode.OK;

        return response;
    }
}