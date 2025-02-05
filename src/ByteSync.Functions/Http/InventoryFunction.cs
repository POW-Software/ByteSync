using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using ByteSync.Common.Business.Inventories;
using ByteSync.Common.Business.Sessions.Cloud;
using ByteSync.Functions.Constants;
using ByteSync.Functions.Helpers;
using ByteSync.ServerCommon.Commands.Inventories;
using ByteSync.ServerCommon.Interfaces.Services;
using MediatR;

namespace ByteSync.Functions.Http;

public class InventoryFunction
{
    private readonly IMediator _mediator;
    private readonly ILogger<InventoryFunction> _logger;

    public InventoryFunction( IMediator mediator, ILoggerFactory loggerFactory)
    {
        _mediator = mediator;
        _logger = loggerFactory.CreateLogger<InventoryFunction>();
    }
    
    [Function("InventoryStartFunction")]
    public async Task<HttpResponseData> Start(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "session/{sessionId}/inventory/start")] 
        HttpRequestData req,
        FunctionContext executionContext,
        string sessionId)
    {
        var response = req.CreateResponse();
        try
        {
            var client = FunctionHelper.GetClientFromContext(executionContext);
            
            var request = new StartInventoryRequest(sessionId, client);
            var result = await _mediator.Send(request);

            await response.WriteAsJsonAsync(result, HttpStatusCode.OK);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while starting inventory with sessionId: {sessionId}", sessionId);
            
            await response.WriteAsJsonAsync(new { error = ErrorConstants.INTERNAL_SERVER_ERROR }, HttpStatusCode.InternalServerError);
        }
        
        return response;
    }
    
    [Function("InventoryLocalInventoryStatusFunction")]
    public async Task<HttpResponseData> SetLocalInventoryStatus(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "session/{sessionId}/inventory/localStatus")] 
        HttpRequestData req,
        FunctionContext executionContext,
        string sessionId)
    {
        var response = req.CreateResponse();
        try
        {
            var client = FunctionHelper.GetClientFromContext(executionContext);
            var localInventoryStatusParameters = await FunctionHelper.DeserializeRequestBody<UpdateSessionMemberGeneralStatusParameters>(req);
            
            var request = new SetLocalInventoryStatusRequest(client, localInventoryStatusParameters);
            var result = await _mediator.Send(request);

            await response.WriteAsJsonAsync(result, HttpStatusCode.OK);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while setting local inventory status: {sessionId}", sessionId);
            
            await response.WriteAsJsonAsync(new { error = ErrorConstants.INTERNAL_SERVER_ERROR }, HttpStatusCode.InternalServerError);
        }
        
        return response;
    }
    
    [Function("InventoryAddPathItemFunction")]
    public async Task<HttpResponseData> AddPathItem(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "session/{sessionId}/inventory/pathItem")] 
        HttpRequestData req,
        FunctionContext executionContext,
        string sessionId)
    {
        using (_logger.BeginScope(new Dictionary<string, object>
               {
                   ["MyMethod"] = "AddPathItem",
                   ["SessionId"] = sessionId,
               }))
        {
            var response = req.CreateResponse();
            try
            {
                var client = FunctionHelper.GetClientFromContext(executionContext);
                var encryptedPathItem = await FunctionHelper.DeserializeRequestBody<EncryptedPathItem>(req);

                var request = new AddPathItemRequest(sessionId, client, encryptedPathItem);
                var result = await _mediator.Send(request);

                await response.WriteAsJsonAsync(result, HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while adding pathItem to an inventory with sessionId: {sessionId}", sessionId);

                await response.WriteAsJsonAsync(new { error = ErrorConstants.INTERNAL_SERVER_ERROR }, HttpStatusCode.InternalServerError);
            }

            return response;
        }
    }
    
    [Function("InventoryRemovePathItemFunction")]
    public async Task<HttpResponseData> RemovePathItem(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "session/{sessionId}/inventory/pathItem")] 
        HttpRequestData req,
        FunctionContext executionContext,
        string sessionId)
    {
        var response = req.CreateResponse();
        try
        {
            var client = FunctionHelper.GetClientFromContext(executionContext);
            var encryptedPathItem = await FunctionHelper.DeserializeRequestBody<EncryptedPathItem>(req);

            var request = new RemovePathItemRequest(sessionId, client, encryptedPathItem);
            var result = await _mediator.Send(request);

            await response.WriteAsJsonAsync(result, HttpStatusCode.OK);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while removing pathItem from an inventory with sessionId: {sessionId}", sessionId);
            
            await response.WriteAsJsonAsync(new { error = ErrorConstants.INTERNAL_SERVER_ERROR }, HttpStatusCode.InternalServerError);
        }
        
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
        var response = req.CreateResponse();
        try
        {
            var request = new GetPathItemsRequest(sessionId, clientInstanceId);
            var result = await _mediator.Send(request);
            
            await response.WriteAsJsonAsync(result, HttpStatusCode.OK);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while getting pathItems from an inventory with sessionId: {sessionId}", sessionId);
            
            await response.WriteAsJsonAsync(new { error = ErrorConstants.INTERNAL_SERVER_ERROR }, HttpStatusCode.InternalServerError);
        }
        
        return response;
    }
}