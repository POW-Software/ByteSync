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
    
    [Function("InventoryAddDataSourceFunction")]
    public async Task<HttpResponseData> AddDataSource(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "session/{sessionId}/inventory/dataSource")] 
        HttpRequestData req,
        FunctionContext executionContext,
        string sessionId)
    {
        var client = FunctionHelper.GetClientFromContext(executionContext);
        var encryptedDataSource = await FunctionHelper.DeserializeRequestBody<EncryptedDataSource>(req);

        var request = new AddDataSourceRequest(sessionId, client, client.ClientInstanceId, encryptedDataSource);
        var result = await _mediator.Send(request);

        var response = req.CreateResponse();
        await response.WriteAsJsonAsync(result, HttpStatusCode.OK);

        return response;
    }
    
    [Function("InventoryRemoveDataSourceFunction")]
    public async Task<HttpResponseData> RemoveDataSource(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "session/{sessionId}/inventory/dataSource")] 
        HttpRequestData req,
        FunctionContext executionContext,
        string sessionId)
    {
        var client = FunctionHelper.GetClientFromContext(executionContext);
        var encryptedDataSource = await FunctionHelper.DeserializeRequestBody<EncryptedDataSource>(req);

        var request = new RemoveDataSourceRequest(sessionId, client, client.ClientInstanceId, encryptedDataSource);
        var result = await _mediator.Send(request);
        
        var response = req.CreateResponse();
        await response.WriteAsJsonAsync(result, HttpStatusCode.OK);
        
        return response;
    }
    
    [Function("InventoryGetDataSourcesFunction")]
    public async Task<HttpResponseData> GetDataSources(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "session/{sessionId}/inventory/dataSource/{clientInstanceId}")]
        HttpRequestData req,
        FunctionContext executionContext,
        string sessionId,
        string clientInstanceId)
    {
        var request = new GetDataSourcesRequest(sessionId, clientInstanceId);
        var result = await _mediator.Send(request);

        var response = req.CreateResponse();
        await response.WriteAsJsonAsync(result, HttpStatusCode.OK);

        return response;
    }

    [Function("InventoryAddDataNodeFunction")]
    public async Task<HttpResponseData> AddDataNode(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "session/{sessionId}/inventory/dataNode/{nodeId}")]
        HttpRequestData req,
        FunctionContext executionContext,
        string sessionId,
        string nodeId)
    {
        var client = FunctionHelper.GetClientFromContext(executionContext);

        var request = new AddDataNodeRequest(sessionId, client, nodeId);
        var result = await _mediator.Send(request);

        var response = req.CreateResponse();
        await response.WriteAsJsonAsync(result, HttpStatusCode.OK);

        return response;
    }

    [Function("InventoryRemoveDataNodeFunction")]
    public async Task<HttpResponseData> RemoveDataNode(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "session/{sessionId}/inventory/dataNode/{nodeId}")]
        HttpRequestData req,
        FunctionContext executionContext,
        string sessionId,
        string nodeId)
    {
        var client = FunctionHelper.GetClientFromContext(executionContext);

        var request = new RemoveDataNodeRequest(sessionId, client, nodeId);
        var result = await _mediator.Send(request);

        var response = req.CreateResponse();
        await response.WriteAsJsonAsync(result, HttpStatusCode.OK);

        return response;
    }
    
    [Function("InventoryGetDataNodesFunction")]
    public async Task<HttpResponseData> GetDataNodes(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "session/{sessionId}/inventory/dataNode/{clientInstanceId}")]
        HttpRequestData req,
        FunctionContext executionContext,
        string sessionId,
        string clientInstanceId)
    {
        var request = new GetDataNodesRequest(sessionId, clientInstanceId);
        var result = await _mediator.Send(request);

        var response = req.CreateResponse();
        await response.WriteAsJsonAsync(result, HttpStatusCode.OK);

        return response;
    }
}