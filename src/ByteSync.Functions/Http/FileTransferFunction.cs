using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using ByteSync.Common.Business.SharedFiles;
using ByteSync.Functions.Helpers.Misc;
using ByteSync.ServerCommon.Interfaces.Services;

namespace ByteSync.Functions.Http;

public class FileTransferFunction
{
    private readonly ITransferLocationService _transferLocationService;

    public FileTransferFunction(ITransferLocationService transferLocationService)
    {
        _transferLocationService = transferLocationService;
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

        var url = await _transferLocationService.GetUploadFileUrl(
            sessionId,
            client,
            transferParameters.SharedFileDefinition,
            transferParameters.PartNumber!.Value
        );
           
        var response = req.CreateResponse();
        await response.WriteAsJsonAsync(url, HttpStatusCode.OK);

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

        var url = await _transferLocationService.GetDownloadFileUrl(
            sessionId,
            client,
            transferParameters.SharedFileDefinition,
            transferParameters.PartNumber!.Value
        );
           
        var response = req.CreateResponse();
        await response.WriteAsJsonAsync(url, HttpStatusCode.OK);

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

        await _transferLocationService.AssertFilePartIsUploaded(sessionId, client, transferParameters);

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

        await _transferLocationService.AssertFilePartIsDownloaded(
            sessionId,
            client,
            transferParameters.SharedFileDefinition,
            transferParameters.PartNumber!.Value
        );

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

        await _transferLocationService.AssertUploadIsFinished(sessionId, client, transferParameters);

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

        await _transferLocationService.AssertDownloadIsFinished(
            sessionId,
            client,
            transferParameters.SharedFileDefinition
        );

        var response = req.CreateResponse();
        response.StatusCode = HttpStatusCode.OK;

        return response;
    }
}