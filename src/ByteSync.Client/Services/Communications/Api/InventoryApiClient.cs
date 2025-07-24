using ByteSync.Common.Business.Inventories;
using ByteSync.Common.Business.Sessions;
using ByteSync.Interfaces.Controls.Communications.Http;

namespace ByteSync.Services.Communications.Api;

public class InventoryApiClient : IInventoryApiClient
{
    private readonly IApiInvoker _apiInvoker;
    private readonly ILogger<InventoryApiClient> _logger;
    
    public InventoryApiClient(IApiInvoker apiInvoker, ILogger<InventoryApiClient> logger)
    {
        _apiInvoker = apiInvoker;
        _logger = logger;
    }
    
    public async Task<StartInventoryResult> StartInventory(string sessionId, EncryptedSessionSettings encryptedSessionSettings)
    {
        try
        {
            var result = await _apiInvoker.PostAsync<StartInventoryResult>($"session/{sessionId}/inventory/start", null);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while starting inventory with sessionId: {sessionId}", sessionId);
                
            throw;
        }
    }

    public async Task<List<EncryptedDataSource>?> GetDataSources(string sessionId, string clientInstanceId, string dataNodeId)
    {
        try
        {
            var result = await _apiInvoker.GetAsync<List<EncryptedDataSource>?>($"session/{sessionId}/inventory/{clientInstanceId}/dataNode/{dataNodeId}/dataSource");

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while getting dataSources from an inventory with sessionId: {sessionId}", sessionId);
                
            throw;
        }
    }
    
    public async Task<bool> AddDataSource(string sessionId, string clientInstanceId, string dataNodeId, EncryptedDataSource encryptedDataSource)
    {
        try
        {
            var result = await _apiInvoker.PostAsync<bool>($"session/{sessionId}/inventory/{clientInstanceId}/dataNode/{dataNodeId}/dataSource", encryptedDataSource);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while adding dataSource to an inventory with sessionId: {sessionId}", sessionId);
                
            throw;
        }
    }
    
    public async Task<bool> RemoveDataSource(string sessionId, string clientInstanceId, string dataNodeId, EncryptedDataSource encryptedDataSource)
    {
        try
        {
            var result = await _apiInvoker.DeleteAsync<bool>($"session/{sessionId}/inventory/{clientInstanceId}/dataNode/{dataNodeId}/dataSource", encryptedDataSource);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while removing dataSource from an inventory with sessionId: {sessionId}", sessionId);
                
            throw;
        }
    }

    public async Task<List<EncryptedDataNode>?> GetDataNodes(string sessionId, string clientInstanceId)
    {
        try
        {
            var result = await _apiInvoker.GetAsync<List<EncryptedDataNode>?>($"session/{sessionId}/inventory/{clientInstanceId}/dataNode");

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while getting dataNodes from an inventory with sessionId: {sessionId}", sessionId);
                
            throw;
        }
    }

    public async Task<bool> AddDataNode(string sessionId, string clientInstanceId, EncryptedDataNode encryptedDataNode)
    {
        try
        {
            var result = await _apiInvoker.PostAsync<bool>($"session/{sessionId}/inventory/{clientInstanceId}/dataNode", encryptedDataNode);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while adding dataNode to an inventory with sessionId: {sessionId}", sessionId);

            throw;
        }
    }

    public async Task<bool> RemoveDataNode(string sessionId, string clientInstanceId, EncryptedDataNode encryptedDataNode)
    {
        try
        {
            var result = await _apiInvoker.DeleteAsync<bool>($"session/{sessionId}/inventory/{clientInstanceId}/dataNode", encryptedDataNode);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while removing dataNode from an inventory with sessionId: {sessionId}", sessionId);

            throw;
        }
    }
}