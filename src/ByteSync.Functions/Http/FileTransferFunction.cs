using ByteSync.Common.Business.SharedFiles;
using ByteSync.Functions.Helpers;
using ByteSync.ServerCommon.Interfaces.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace ByteSync.Functions.Http;

public class FileTransferFunction
{
    private readonly ITransferLocationService _transferLocationService;
    private readonly ILogger<FileTransferFunction> _logger;

    public FileTransferFunction(ITransferLocationService transferLocationService, ILoggerFactory loggerFactory)
    {
        _transferLocationService = transferLocationService;
        _logger = loggerFactory.CreateLogger<FileTransferFunction>();
    }
    
    [Function("GetUploadFileUrlFunction")]
    public async Task<IActionResult> GetUploadFileUrl(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "session/{sessionId}/file/getUploadUrl")] HttpRequestData req,
        FunctionContext executionContext, string sessionId)
    {
        try
        {
            var client = FunctionHelper.GetClientFromContext(executionContext);
            var transferParameters = await FunctionHelper.DeserializeRequestBody<TransferParameters>(req);
            
            string url = await _transferLocationService.GetUploadFileUrl(sessionId, client, transferParameters.SharedFileDefinition,
                transferParameters.PartNumber!.Value);
            
            return new OkObjectResult(url);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while getting upload file url");
            
            return new ObjectResult(new { error = "An internal server error occurred." })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }
    
    [Function("GetDownloadFileUrlFunction")]
    public async Task<IActionResult> GetDownloadFileUrl(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "session/{sessionId}/file/getDownloadUrl")] HttpRequestData req,
        FunctionContext executionContext, string sessionId)
    {
        try
        {
            var client = FunctionHelper.GetClientFromContext(executionContext);
            var transferParameters = await FunctionHelper.DeserializeRequestBody<TransferParameters>(req);
            
            string url = await _transferLocationService.GetDownloadFileUrl(sessionId, client, transferParameters.SharedFileDefinition,
                transferParameters.PartNumber!.Value);
            
            return new OkObjectResult(url);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while getting download file url");
            
            return new ObjectResult(new { error = "An internal server error occurred." })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }
    
    [Function("AssertFilePartIsUploadedFunction")]
    public async Task<IActionResult> AssertFilePartIsUploaded(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "session/{sessionId}/file/partUploaded")] HttpRequestData req,
        FunctionContext executionContext, string sessionId)
    {
        try
        {
            var client = FunctionHelper.GetClientFromContext(executionContext);
            var transferParameters = await FunctionHelper.DeserializeRequestBody<TransferParameters>(req);
            
            await _transferLocationService.AssertFilePartIsUploaded(sessionId, client, transferParameters);
            
            return new OkResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while asserting file part is uploaded");
            
            return new ObjectResult(new { error = "An internal server error occurred." })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }
    
    [Function("AssertFilePartIsDownloadedFunction")]
    public async Task<IActionResult> AssertFilePartIsDownloaded(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "session/{sessionId}/file/partDownloaded")] HttpRequestData req,
        FunctionContext executionContext, string sessionId)
    {
        try
        {
            var client = FunctionHelper.GetClientFromContext(executionContext);
            var transferParameters = await FunctionHelper.DeserializeRequestBody<TransferParameters>(req);
            
            await _transferLocationService.AssertFilePartIsDownloaded(sessionId, client, transferParameters.SharedFileDefinition,
                transferParameters.PartNumber!.Value);
            
            return new OkResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while asserting file part is downloaded");
            
            return new ObjectResult(new { error = "An internal server error occurred." })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }
    
    [Function("AssertUploadIsFinishedFunction")]
    public async Task<IActionResult> AssertUploadIsFinished(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "session/{sessionId}/file/uploadFinished")] HttpRequestData req,
        FunctionContext executionContext, string sessionId)
    {
        try
        {
            var client = FunctionHelper.GetClientFromContext(executionContext);
            var transferParameters = await FunctionHelper.DeserializeRequestBody<TransferParameters>(req);
            
            await _transferLocationService.AssertUploadIsFinished(sessionId, client, transferParameters);
            
            return new OkResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while asserting upload is finished");
            
            return new ObjectResult(new { error = "An internal server error occurred." })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }
    
    [Function("AssertDownloadIsFinishedFunction")]
    public async Task<IActionResult> AssertDownloadIsFinished(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "session/{sessionId}/file/downloadFinished")] HttpRequestData req,
        FunctionContext executionContext, string sessionId)
    {
        try
        {
            var client = FunctionHelper.GetClientFromContext(executionContext);
            var transferParameters = await FunctionHelper.DeserializeRequestBody<TransferParameters>(req);
            
            await _transferLocationService.AssertDownloadIsFinished(sessionId, client, transferParameters.SharedFileDefinition);
            
            return new OkResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while asserting download is finished");
            
            return new ObjectResult(new { error = "An internal server error occurred." })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }
}