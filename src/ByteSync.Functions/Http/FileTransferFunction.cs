using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using ByteSync.Common.Business.SharedFiles;
using ByteSync.Functions.Helpers.Misc;
using ByteSync.ServerCommon.Commands.FileTransfer;
using MediatR;

namespace ByteSync.Functions.Http;

public class FileTransferFunction
{
    private readonly IMediator _mediator;

    public FileTransferFunction(IMediator mediator)
    {
        _mediator = mediator;
    }
    
    [Function("GetUploadFileUrlFunction")]
    public async Task<HttpResponseData> GetUploadFileUrl(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "session/{sessionId}/file/getUploadUrl")]
        HttpRequestData req,
        FunctionContext executionContext,
        string sessionId)
    {
        var client = FunctionHelper.GetClientFromContext(executionContext);
        var transferParameters = await FunctionHelper.DeserializeRequestBody<TransferParameters>(req);
        
        var request = new GetUploadFileUrlRequest(sessionId, client, transferParameters.SharedFileDefinition, transferParameters.PartNumber!.Value);
        var url = await _mediator.Send(request);
           
        var response = req.CreateResponse();
        await response.WriteAsJsonAsync(url, HttpStatusCode.OK);

        return response;
    }
    
    [Function("GetUploadFileStorageLocationFunction")]
    public async Task<HttpResponseData> GetUploadFileStorageLocation(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "session/{sessionId}/file/getUploadStorageLocation")]
        HttpRequestData req,
        FunctionContext executionContext,
        string sessionId)
    {
        var client = FunctionHelper.GetClientFromContext(executionContext);
        var transferParameters = await FunctionHelper.DeserializeRequestBody<TransferParameters>(req);

        var request = new GetUploadFileStorageLocationRequest(sessionId, client, transferParameters);
        var responseObject = await _mediator.Send(request);
        var response = req.CreateResponse();
        await response.WriteAsJsonAsync(responseObject, HttpStatusCode.OK);

        return response;
    }

    [Function("GetDownloadFileUrlFunction")]
    public async Task<HttpResponseData> GetDownloadFileUrl(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "session/{sessionId}/file/getDownloadUrl")]
        HttpRequestData req,
        FunctionContext executionContext,
        string sessionId)
    {
        var client = FunctionHelper.GetClientFromContext(executionContext);
        var transferParameters = await FunctionHelper.DeserializeRequestBody<TransferParameters>(req);

        var request = new GetDownloadFileUrlRequest(sessionId, client, transferParameters.SharedFileDefinition, transferParameters.PartNumber!.Value);
        var url = await _mediator.Send(request);
           
        var response = req.CreateResponse();
        await response.WriteAsJsonAsync(url, HttpStatusCode.OK);

        return response;
    }
    
    [Function("GetDownloadFileStorageLocationFunction")]
    public async Task<HttpResponseData> GetDownloadFileStorageLocation(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "session/{sessionId}/file/getDownloadStorageLocation")]
        HttpRequestData req,
        FunctionContext executionContext,
        string sessionId)
    {
        var client = FunctionHelper.GetClientFromContext(executionContext);
        var transferParameters = await FunctionHelper.DeserializeRequestBody<TransferParameters>(req);

        var request = new GetDownloadFileStorageLocationRequest(sessionId, client, transferParameters);
        var responseObject = await _mediator.Send(request);
        var response = req.CreateResponse();
        await response.WriteAsJsonAsync(responseObject, HttpStatusCode.OK);

        return response;
    }
    
    [Function("AssertFilePartIsUploadedFunction")]
    public async Task<HttpResponseData> AssertFilePartIsUploaded(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "session/{sessionId}/file/partUploaded")]
        HttpRequestData req,
        FunctionContext executionContext,
        string sessionId)
    {
        var client = FunctionHelper.GetClientFromContext(executionContext);
        var transferParameters = await FunctionHelper.DeserializeRequestBody<TransferParameters>(req);

        var request = new AssertFilePartIsUploadedRequest(sessionId, client, transferParameters);
        await _mediator.Send(request);

        var response = req.CreateResponse();
        response.StatusCode = HttpStatusCode.OK;

        return response;
    }
    
    [Function("AssertFilePartIsDownloadedFunction")]
    public async Task<HttpResponseData> AssertFilePartIsDownloaded(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "session/{sessionId}/file/partDownloaded")]
        HttpRequestData req,
        FunctionContext executionContext,
        string sessionId)
    {
        var client = FunctionHelper.GetClientFromContext(executionContext);
        var transferParameters = await FunctionHelper.DeserializeRequestBody<TransferParameters>(req);

        var request = new AssertFilePartIsDownloadedRequest(sessionId, client, transferParameters.SharedFileDefinition, transferParameters.PartNumber!.Value);
        await _mediator.Send(request);

        var response = req.CreateResponse();
        response.StatusCode = HttpStatusCode.OK;

        return response;
    }
    
    [Function("AssertUploadIsFinishedFunction")]
    public async Task<HttpResponseData> AssertUploadIsFinished(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "session/{sessionId}/file/uploadFinished")]
        HttpRequestData req,
        FunctionContext executionContext,
        string sessionId)
    {
        var client = FunctionHelper.GetClientFromContext(executionContext);
        var transferParameters = await FunctionHelper.DeserializeRequestBody<TransferParameters>(req);

        var request = new AssertUploadIsFinishedRequest(sessionId, client, transferParameters);
        await _mediator.Send(request);

        var response = req.CreateResponse();
        response.StatusCode = HttpStatusCode.OK;

        return response;
    }
    
    [Function("AssertDownloadIsFinishedFunction")]
    public async Task<HttpResponseData> AssertDownloadIsFinished(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "session/{sessionId}/file/downloadFinished")]
        HttpRequestData req,
        FunctionContext executionContext,
        string sessionId)
    {
        var client = FunctionHelper.GetClientFromContext(executionContext);
        var transferParameters = await FunctionHelper.DeserializeRequestBody<TransferParameters>(req);

        var request = new AssertDownloadIsFinishedRequest(sessionId, client, transferParameters.SharedFileDefinition);
        await _mediator.Send(request);

        var response = req.CreateResponse();
        response.StatusCode = HttpStatusCode.OK;

        return response;
    }
}