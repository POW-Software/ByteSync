using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using ByteSync.Common.Business.SharedFiles;
using ByteSync.Functions.Constants;
using ByteSync.Functions.Helpers;
using ByteSync.ServerCommon.Interfaces.Services;

namespace ByteSync.Functions.Http;

public class FileTransferFunction
{
    private readonly ITransferLocationService _transferLocationService;
    private readonly ILogger<FileTransferFunction> _logger;

    public FileTransferFunction(
        ITransferLocationService transferLocationService, 
        ILoggerFactory loggerFactory)
    {
        _transferLocationService = transferLocationService;
        _logger = loggerFactory.CreateLogger<FileTransferFunction>();
    }
    
    [Function("GetUploadFileUrlFunction")]
    public async Task<HttpResponseData> GetUploadFileUrl(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "session/{sessionId}/file/getUploadUrl")]
        HttpRequestData req,
        FunctionContext executionContext,
        string sessionId)
    {
        var response = req.CreateResponse();
        try
        {
            var client = FunctionHelper.GetClientFromContext(executionContext);
            var transferParameters = await FunctionHelper.DeserializeRequestBody<TransferParameters>(req);

            var url = await _transferLocationService.GetUploadFileUrl(
                sessionId,
                client,
                transferParameters.SharedFileDefinition,
                transferParameters.PartNumber!.Value
            );

            response.StatusCode = HttpStatusCode.OK;
            await response.WriteAsJsonAsync(url);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while getting upload file url for sessionId {sessionId}", sessionId);
            
            response.StatusCode = HttpStatusCode.InternalServerError;
            await response.WriteAsJsonAsync(new { error = ErrorConstants.INTERNAL_SERVER_ERROR });
        }

        return response;
    }
    
    [Function("GetDownloadFileUrlFunction")]
    public async Task<HttpResponseData> GetDownloadFileUrl(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "session/{sessionId}/file/getDownloadUrl")]
        HttpRequestData req,
        FunctionContext executionContext,
        string sessionId)
    {
        var response = req.CreateResponse();
        try
        {
            var client = FunctionHelper.GetClientFromContext(executionContext);
            var transferParameters = await FunctionHelper.DeserializeRequestBody<TransferParameters>(req);

            var url = await _transferLocationService.GetDownloadFileUrl(
                sessionId,
                client,
                transferParameters.SharedFileDefinition,
                transferParameters.PartNumber!.Value
            );

            response.StatusCode = HttpStatusCode.OK;
            await response.WriteAsJsonAsync(url);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while getting download file url for sessionId {sessionId}", sessionId);
            
            response.StatusCode = HttpStatusCode.InternalServerError;
            await response.WriteAsJsonAsync(new { error = ErrorConstants.INTERNAL_SERVER_ERROR });
        }

        return response;
    }
    
    [Function("AssertFilePartIsUploadedFunction")]
    public async Task<HttpResponseData> AssertFilePartIsUploaded(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "session/{sessionId}/file/partUploaded")]
        HttpRequestData req,
        FunctionContext executionContext,
        string sessionId)
    {
        var response = req.CreateResponse();
        try
        {
            var client = FunctionHelper.GetClientFromContext(executionContext);
            var transferParameters = await FunctionHelper.DeserializeRequestBody<TransferParameters>(req);

            await _transferLocationService.AssertFilePartIsUploaded(sessionId, client, transferParameters);

            response.StatusCode = HttpStatusCode.OK;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while asserting file part is uploaded for sessionId {sessionId}", sessionId);
            
            response.StatusCode = HttpStatusCode.InternalServerError;
            await response.WriteAsJsonAsync(new { error = ErrorConstants.INTERNAL_SERVER_ERROR });
        }

        return response;
    }
    
    [Function("AssertFilePartIsDownloadedFunction")]
    public async Task<HttpResponseData> AssertFilePartIsDownloaded(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "session/{sessionId}/file/partDownloaded")]
        HttpRequestData req,
        FunctionContext executionContext,
        string sessionId)
    {
        var response = req.CreateResponse();
        try
        {
            var client = FunctionHelper.GetClientFromContext(executionContext);
            var transferParameters = await FunctionHelper.DeserializeRequestBody<TransferParameters>(req);

            await _transferLocationService.AssertFilePartIsDownloaded(
                sessionId,
                client,
                transferParameters.SharedFileDefinition,
                transferParameters.PartNumber!.Value
            );

            response.StatusCode = HttpStatusCode.OK;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while asserting file part is downloaded for sessionId {sessionId}", sessionId);
            
            response.StatusCode = HttpStatusCode.InternalServerError;
            await response.WriteAsJsonAsync(new { error = ErrorConstants.INTERNAL_SERVER_ERROR });
        }

        return response;
    }
    
    [Function("AssertUploadIsFinishedFunction")]
    public async Task<HttpResponseData> AssertUploadIsFinished(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "session/{sessionId}/file/uploadFinished")]
        HttpRequestData req,
        FunctionContext executionContext,
        string sessionId)
    {
        var response = req.CreateResponse();
        try
        {
            var client = FunctionHelper.GetClientFromContext(executionContext);
            var transferParameters = await FunctionHelper.DeserializeRequestBody<TransferParameters>(req);

            await _transferLocationService.AssertUploadIsFinished(sessionId, client, transferParameters);

            response.StatusCode = HttpStatusCode.OK;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while asserting upload is finished for sessionId {sessionId}", sessionId);
            
            response.StatusCode = HttpStatusCode.InternalServerError;
            await response.WriteAsJsonAsync(new { error = ErrorConstants.INTERNAL_SERVER_ERROR });
        }

        return response;
    }
    
    [Function("AssertDownloadIsFinishedFunction")]
    public async Task<HttpResponseData> AssertDownloadIsFinished(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "session/{sessionId}/file/downloadFinished")]
        HttpRequestData req,
        FunctionContext executionContext,
        string sessionId)
    {
        var response = req.CreateResponse();
        try
        {
            var client = FunctionHelper.GetClientFromContext(executionContext);
            var transferParameters = await FunctionHelper.DeserializeRequestBody<TransferParameters>(req);

            await _transferLocationService.AssertDownloadIsFinished(
                sessionId,
                client,
                transferParameters.SharedFileDefinition
            );

            response.StatusCode = HttpStatusCode.OK;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while asserting download is finished for sessionId {sessionId}", sessionId);
            
            response.StatusCode = HttpStatusCode.InternalServerError;
            await response.WriteAsJsonAsync(new { error = ErrorConstants.INTERNAL_SERVER_ERROR });
        }

        return response;
    }
}