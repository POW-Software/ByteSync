using ByteSync.Common.Business.Sessions.Cloud.Connections;
using ByteSync.Common.Business.Trust.Connections;
using ByteSync.Functions.Helpers;
using ByteSync.ServerCommon.Interfaces.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace ByteSync.Functions.Http;

public class TrustFunction
{
    private readonly ITrustService _trustService;
    private readonly ILogger<TrustFunction> _logger;

    public TrustFunction(ITrustService trustService, ILoggerFactory loggerFactory)
    {
        _trustService = trustService;
        _logger = loggerFactory.CreateLogger<TrustFunction>();
    }
    
    [Function("StartTrustCheckFunction")]
    public async Task<IActionResult> StartTrustCheck(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "trust/startTrustCheck")] HttpRequestData req, FunctionContext executionContext)
    {
        try
        {
            var client = FunctionHelper.GetClientFromContext(executionContext);
            var parameters = await FunctionHelper.DeserializeRequestBody<TrustCheckParameters>(req);

            var result = await _trustService.StartTrustCheck(client, parameters);

            return new OkObjectResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while starting trust check");
            
            return new ObjectResult(new { error = "An internal server error occurred." })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }
    
    [Function("GiveMemberPublicKeyCheckDataFunction")]
    public async Task<IActionResult> GiveMemberPublicKeyCheckData(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "trust/giveMemberPublicKeyCheckData")] HttpRequestData req, FunctionContext executionContext)
    {
        try
        {
            var client = FunctionHelper.GetClientFromContext(executionContext);
            var parameters = await FunctionHelper.DeserializeRequestBody<GiveMemberPublicKeyCheckDataParameters>(req);

            await _trustService.GiveMemberPublicKeyCheckData(client, parameters);
            
            return new OkResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while giving pulick key check data");
            
            return new ObjectResult(new { error = "An internal server error occurred." })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }
    
    [Function("SendDigitalSignaturesFunction")]
    public async Task<IActionResult> SendDigitalSignatures(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "trust/sendDigitalSignatures")] HttpRequestData req, FunctionContext executionContext)
    {
        try
        {
            var client = FunctionHelper.GetClientFromContext(executionContext);
            var parameters = await FunctionHelper.DeserializeRequestBody<SendDigitalSignaturesParameters>(req);

            await _trustService.SendDigitalSignatures(client, parameters);
            
            return new OkResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while sending digital signatures");
            
            return new ObjectResult(new { error = "An internal server error occurred." })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }
    
    [Function("SetAuthCheckedFunction")]
    public async Task<IActionResult> SetAuthChecked(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "trust/setAuthChecked")] HttpRequestData req, FunctionContext executionContext)
    {
        try
        {
            var client = FunctionHelper.GetClientFromContext(executionContext);
            var parameters = await FunctionHelper.DeserializeRequestBody<SetAuthCheckedParameters>(req);

            await _trustService.SetAuthChecked(client, parameters);
            
            return new OkResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while setting auth checked");
            
            return new ObjectResult(new { error = "An internal server error occurred." })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }
    
    [Function("RequestTrustPublicKeyFunction")]
    public async Task<IActionResult> RequestTrustPublicKey(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "trust/requestTrustPublicKey")] HttpRequestData req, FunctionContext executionContext)
    {
        try
        {
            var client = FunctionHelper.GetClientFromContext(executionContext);
            var parameters = await FunctionHelper.DeserializeRequestBody<RequestTrustProcessParameters>(req);

            await _trustService.RequestTrustPublicKey(client, parameters);
            
            return new OkResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while requesting trust public key");
            
            return new ObjectResult(new { error = "An internal server error occurred." })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }
    
        
    [Function("InformPublicKeyValidationIsFinishedFunction")]
    public async Task<IActionResult> InformPublicKeyValidationIsFinished(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "trust/informPublicKeyValidationIsFinished")] HttpRequestData req, FunctionContext executionContext)
    {
        try
        {
            var client = FunctionHelper.GetClientFromContext(executionContext);
            var parameters = await FunctionHelper.DeserializeRequestBody<PublicKeyValidationParameters>(req);

            await _trustService.InformPublicKeyValidationIsFinished(client, parameters);
            
            return new OkResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while informing public key validation is finished");
            
            return new ObjectResult(new { error = "An internal server error occurred." })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }
}