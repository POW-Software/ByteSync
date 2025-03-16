using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using ByteSync.Common.Business.Inventories;
using ByteSync.Common.Business.Sessions.Cloud;
using ByteSync.Functions.Helpers.Misc;
using ByteSync.ServerCommon.Commands.Inventories;
using MediatR;

namespace ByteSync.Functions.Http;

public class InventoryFunction
{
    private readonly IMediator _mediator;

    public InventoryFunction(IMediator mediator)
    {
        _mediator = mediator;
    }
    
    [Function("InventoryStartFunction")]
    public async Task<HttpResponseData> Start(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "session/{sessionId}/inventory/start")] 
        HttpRequestData req,
        FunctionContext executionContext,
        string sessionId)
    {
        var client = FunctionHelper.GetClientFromContext(executionContext);
            
        var request = new StartInventoryRequest(sessionId, client);
        var result = await _mediator.Send(request);

        var response = req.CreateResponse();
        await response.WriteAsJsonAsync(result, HttpStatusCode.OK);
        
        return response;
    }
    
    [Function("InventorySetLocalInventoryStatusFunction")]
    public async Task<HttpResponseData> SetLocalInventoryStatus(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "session/{sessionId}/inventory/localStatus")] 
        HttpRequestData req,
        FunctionContext executionContext,
        string sessionId)
    {
        var client = FunctionHelper.GetClientFromContext(executionContext);
        var localInventoryStatusParameters = await FunctionHelper.DeserializeRequestBody<UpdateSessionMemberGeneralStatusParameters>(req);
            
        var request = new SetLocalInventoryStatusRequest(client, localInventoryStatusParameters);
        var result = await _mediator.Send(request);

        var response = req.CreateResponse();
        await response.WriteAsJsonAsync(result, HttpStatusCode.OK);
        
        return response;
    }
    
    [Function("InventoryAddPathItemFunction")]
    public async Task<HttpResponseData> AddPathItem(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "session/{sessionId}/inventory/pathItem")] 
        HttpRequestData req,
        FunctionContext executionContext,
        string sessionId)
    {
        var client = FunctionHelper.GetClientFromContext(executionContext);
        var encryptedPathItem = await FunctionHelper.DeserializeRequestBody<EncryptedPathItem>(req);

        var request = new AddPathItemRequest(sessionId, client, encryptedPathItem);
        var result = await _mediator.Send(request);

        var response = req.CreateResponse();
        await response.WriteAsJsonAsync(result, HttpStatusCode.OK);

        return response;
    }
    
    [Function("InventoryRemovePathItemFunction")]
    public async Task<HttpResponseData> RemovePathItem(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "session/{sessionId}/inventory/pathItem")] 
        HttpRequestData req,
        FunctionContext executionContext,
        string sessionId)
    {
        var client = FunctionHelper.GetClientFromContext(executionContext);
        var encryptedPathItem = await FunctionHelper.DeserializeRequestBody<EncryptedPathItem>(req);

        var request = new RemovePathItemRequest(sessionId, client, encryptedPathItem);
        var result = await _mediator.Send(request);
        
        var response = req.CreateResponse();
        await response.WriteAsJsonAsync(result, HttpStatusCode.OK);
        
        return response;
    }
    
    [Function("InventoryGetPathItemsFunction")]
    public async Task<HttpResponseData> GetPathItems(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "session/{sessionId}/inventory/pathItem/{clientInstanceId}")] 
        HttpRequestData req,
        FunctionContext executionContext,
        string sessionId,
        string clientInstanceId)
    {
        var request = new GetPathItemsRequest(sessionId, clientInstanceId);
        var result = await _mediator.Send(request);
          
        var response = req.CreateResponse();
        await response.WriteAsJsonAsync(result, HttpStatusCode.OK);
        
        return response;
    }
}