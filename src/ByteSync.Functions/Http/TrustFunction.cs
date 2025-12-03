using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using ByteSync.Common.Business.Sessions.Cloud.Connections;
using ByteSync.Common.Business.Trust.Connections;
using ByteSync.Functions.Helpers.Misc;
using ByteSync.ServerCommon.Commands.Trusts;
using ByteSync.ServerCommon.Interfaces.Services;
using MediatR;

namespace ByteSync.Functions.Http;

public class TrustFunction
{
    private readonly IMediator _mediator;

    public TrustFunction(IMediator mediator)
    {
        _mediator = mediator;
    }
    
    [Function("StartTrustCheckFunction")]
    public async Task<HttpResponseData> StartTrustCheck(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "trust/startTrustCheck")] 
        HttpRequestData req, 
        FunctionContext executionContext)
    {
        var client = FunctionHelper.GetClientFromContext(executionContext);
        var parameters = await FunctionHelper.DeserializeRequestBody<TrustCheckParameters>(req);

        var request = new StartTrustCheckRequest(parameters, client);
        var result = await _mediator.Send(request);
        
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

        var request = new GiveMemberPublicKeyCheckDataRequest(parameters, client);
        await _mediator.Send(request);
        
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

        var request = new SendDigitalSignaturesRequest(parameters, client);
        await _mediator.Send(request);
           
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

        var request = new SetAuthCheckedRequest(parameters, client);
        await _mediator.Send(request);
        
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

        var request = new RequestTrustPublicKeyRequest(parameters, client);
        await _mediator.Send(request);
          
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

        var request = new InformPublicKeyValidationIsFinishedRequest(parameters, client);
        await _mediator.Send(request);
            
        var response = req.CreateResponse();
        response.StatusCode = HttpStatusCode.OK;
        
        return response;
    }
    
    [Function("InformProtocolVersionIncompatibleFunction")]
    public async Task<HttpResponseData> InformProtocolVersionIncompatible(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "trust/informProtocolVersionIncompatible")] 
        HttpRequestData req, 
        FunctionContext executionContext)
    {
        var client = FunctionHelper.GetClientFromContext(executionContext);
        var parameters = await FunctionHelper.DeserializeRequestBody<InformProtocolVersionIncompatibleParameters>(req);

        var request = new InformProtocolVersionIncompatibleRequest(parameters, client);
        await _mediator.Send(request);
            
        var response = req.CreateResponse();
        response.StatusCode = HttpStatusCode.OK;
        
        return response;
    }
}