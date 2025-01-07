﻿using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using ByteSync.Common.Business.Inventories;
using ByteSync.Common.Business.Sessions.Cloud;
using ByteSync.Functions.Constants;
using ByteSync.Functions.Helpers;
using ByteSync.ServerCommon.Interfaces.Services;

namespace ByteSync.Functions.Http;

public class InventoryFunction
{
    private readonly IInventoryService _inventoryService;
    private readonly ILogger<InventoryFunction> _logger;

    public InventoryFunction(IInventoryService inventoryService, ILoggerFactory loggerFactory)
    {
        _inventoryService = inventoryService;
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
            
            var result = await _inventoryService.StartInventory(sessionId, client);

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
            
            var result = await _inventoryService.SetLocalInventoryStatus(client, localInventoryStatusParameters);

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
        var response = req.CreateResponse();
        try
        {
            var client = FunctionHelper.GetClientFromContext(executionContext);
            var encryptedPathItem = await FunctionHelper.DeserializeRequestBody<EncryptedPathItem>(req);
            
            var result = await _inventoryService.AddPathItem(sessionId, client, encryptedPathItem);

            await response.WriteAsJsonAsync(result, HttpStatusCode.OK);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while adding pathItem to an inventory with sessionId: {sessionId}", sessionId);
            
            await response.WriteAsJsonAsync(new { error = ErrorConstants.INTERNAL_SERVER_ERROR }, HttpStatusCode.InternalServerError);
        }
        
        return response;
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

            var result = await _inventoryService.RemovePathItem(sessionId, client, encryptedPathItem);

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
            var result = await _inventoryService.GetPathItems(sessionId, clientInstanceId);
            
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