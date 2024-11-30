using ByteSync.Common.Business.Inventories;
using ByteSync.Common.Business.Sessions.Cloud;
using ByteSync.Functions.Helpers;
using ByteSync.ServerCommon.Interfaces.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace ByteSync.Functions.Http;

public class InventoryFunction
{
    private readonly IInventoryService _inventoryService;
    private readonly ILogger _logger;

    public InventoryFunction(IInventoryService inventoryService, ILoggerFactory loggerFactory)
    {
        _inventoryService = inventoryService;
        _logger = loggerFactory.CreateLogger<InventoryFunction>();
    }
    
    [Function("InventoryStartFunction")]
    public async Task<IActionResult> Start([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "session/{sessionId}/inventory/start")] HttpRequestData req,
        FunctionContext executionContext, string sessionId)
    {
        try
        {
            var client = FunctionHelper.GetClientFromContext(executionContext);
            
            var result = await _inventoryService.StartInventory(sessionId, client);
            return new OkObjectResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while starting inventory with sessionId: {sessionId}", sessionId);
            
            return new ObjectResult(new { error = "An internal server error occurred." })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }
    
    [Function("InventoryLocalInventoryStatusFunction")]
    public async Task<IActionResult> SetLocalInventoryStatus([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "session/{sessionId}/inventory/localStatus")] HttpRequestData req,
        FunctionContext executionContext, string sessionId)
    {
        try
        {
            var client = FunctionHelper.GetClientFromContext(executionContext);
            var localInventoryStatusParameters = await FunctionHelper.DeserializeRequestBody<UpdateSessionMemberGeneralStatusParameters>(req);
            
            var result = await _inventoryService.SetLocalInventoryStatus(client, localInventoryStatusParameters);
            return new OkObjectResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while setting local inventory status: {sessionId}", sessionId);
            
            return new ObjectResult(new { error = "An internal server error occurred." })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }
    
    [Function("InventoryAddPathItemFunction")]
    public async Task<IActionResult> AddPathItem([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "session/{sessionId}/inventory/pathItem")] HttpRequestData req,
        FunctionContext executionContext, string sessionId)
    {
        try
        {
            var client = FunctionHelper.GetClientFromContext(executionContext);
            var encryptedPathItem = await FunctionHelper.DeserializeRequestBody<EncryptedPathItem>(req);
            
            var result = await _inventoryService.AddPathItem(sessionId, client, encryptedPathItem);
            return new OkObjectResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while adding pathItem to an inventory with sessionId: {sessionId}", sessionId);
            
            return new ObjectResult(new { error = "An internal server error occurred." })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }
    
    [Function("InventoryRemovePathItemFunction")]
    public async Task<IActionResult> RemovePathItem([HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "session/{sessionId}/inventory/pathItem")] HttpRequestData req,
        FunctionContext executionContext, string sessionId)
    {
        try
        {
            var client = FunctionHelper.GetClientFromContext(executionContext);
            var encryptedPathItem = await FunctionHelper.DeserializeRequestBody<EncryptedPathItem>(req);

            var result = await _inventoryService.RemovePathItem(sessionId, client, encryptedPathItem);
            return new OkObjectResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while removing pathItem from an inventory with sessionId: {sessionId}", sessionId);

            return new ObjectResult(new { error = "An internal server error occurred." })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }
    
    [Function("InventoryGetPathItemsFunction")]
    public async Task<IActionResult> GetPathItems([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "session/{sessionId}/inventory/pathItem/{clientInstanceId}")] HttpRequestData req,
        FunctionContext executionContext, string sessionId, string clientInstanceId)
    {
        try
        {
            var result = await _inventoryService.GetPathItems(sessionId, clientInstanceId);
            
            return new OkObjectResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while getting pathItems from an inventory with sessionId: {sessionId}", sessionId);

            return new ObjectResult(new { error = "An internal server error occurred." })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }
}