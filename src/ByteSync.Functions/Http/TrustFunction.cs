using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using ByteSync.Common.Business.Sessions.Cloud.Connections;
using ByteSync.Common.Business.Trust.Connections;
using ByteSync.Functions.Constants;
using ByteSync.Functions.Helpers;
using ByteSync.ServerCommon.Interfaces.Services;

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
    public async Task<HttpResponseData> StartTrustCheck(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "trust/startTrustCheck")] 
        HttpRequestData req, 
        FunctionContext executionContext)
    {
        var client = FunctionHelper.GetClientFromContext(executionContext);
        var parameters = await FunctionHelper.DeserializeRequestBody<TrustCheckParameters>(req);

        var result = await _trustService.StartTrustCheck(client, parameters);
        
        var response = req.CreateResponse();
        await response.WriteAsJsonAsync(result, HttpStatusCode.OK);
        
        return response;
    }
    
    [Function("GiveMemberPublicKeyCheckDataFunction")]
    public async Task<HttpResponseData> GiveMemberPublicKeyCheckData(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "trust/giveMemberPublicKeyCheckData")] 
        HttpRequestData req, 
        FunctionContext executionContext)
    {
        var client = FunctionHelper.GetClientFromContext(executionContext);
        var parameters = await FunctionHelper.DeserializeRequestBody<GiveMemberPublicKeyCheckDataParameters>(req);

        await _trustService.GiveMemberPublicKeyCheckData(client, parameters);
        var response = req.CreateResponse();
        
        response.StatusCode = HttpStatusCode.OK;
        
        return response;
    }
    
    [Function("SendDigitalSignaturesFunction")]
    public async Task<HttpResponseData> SendDigitalSignatures(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "trust/sendDigitalSignatures")] 
        HttpRequestData req, 
        FunctionContext executionContext)
    {
        var client = FunctionHelper.GetClientFromContext(executionContext);
        var parameters = await FunctionHelper.DeserializeRequestBody<SendDigitalSignaturesParameters>(req);

        await _trustService.SendDigitalSignatures(client, parameters);
           
        var response = req.CreateResponse();
        response.StatusCode = HttpStatusCode.OK;
        
        return response;
    }
    
    [Function("SetAuthCheckedFunction")]
    public async Task<HttpResponseData> SetAuthChecked(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "trust/setAuthChecked")] 
        HttpRequestData req, 
        FunctionContext executionContext)
    {
        var client = FunctionHelper.GetClientFromContext(executionContext);
        var parameters = await FunctionHelper.DeserializeRequestBody<SetAuthCheckedParameters>(req);

        await _trustService.SetAuthChecked(client, parameters);
           
        var response = req.CreateResponse();
        response.StatusCode = HttpStatusCode.OK;
        
        return response;
    }
    
    [Function("RequestTrustPublicKeyFunction")]
    public async Task<HttpResponseData> RequestTrustPublicKey(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "trust/requestTrustPublicKey")] 
        HttpRequestData req, 
        FunctionContext executionContext)
    {
        var client = FunctionHelper.GetClientFromContext(executionContext);
        var parameters = await FunctionHelper.DeserializeRequestBody<RequestTrustProcessParameters>(req);

        await _trustService.RequestTrustPublicKey(client, parameters);
          
        var response = req.CreateResponse();
        response.StatusCode = HttpStatusCode.OK;
        
        return response;
    }
        
    [Function("InformPublicKeyValidationIsFinishedFunction")]
    public async Task<HttpResponseData> InformPublicKeyValidationIsFinished(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "trust/informPublicKeyValidationIsFinished")] 
        HttpRequestData req, 
        FunctionContext executionContext)
    {
        var client = FunctionHelper.GetClientFromContext(executionContext);
        var parameters = await FunctionHelper.DeserializeRequestBody<PublicKeyValidationParameters>(req);

        await _trustService.InformPublicKeyValidationIsFinished(client, parameters);
            
        var response = req.CreateResponse();
        response.StatusCode = HttpStatusCode.OK;
        
        return response;
    }
}