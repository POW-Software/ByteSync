using ByteSync.Common.Business.Inventories;
using ByteSync.Common.Business.Sessions;

namespace ByteSync.Interfaces.Controls.Communications.Http;

public interface IInventoryApiClient
{
    Task<StartInventoryResult> StartInventory(string sessionId, EncryptedSessionSettings encryptedSessionSettings);
    
    Task<List<EncryptedDataSource>?> GetDataSources(string sessionId, string clientInstanceId, string dataNodeId);
    
    Task<bool> AddDataSource(string sessionId, string clientInstanceId, string dataNodeId, EncryptedDataSource encryptedDataSource);

    Task<bool> RemoveDataSource(string sessionId, string clientInstanceId, string dataNodeId, EncryptedDataSource encryptedDataSource);
    
    Task<List<EncryptedDataNode>?> GetDataNodes(string sessionId, string clientInstanceId);

    Task<bool> AddDataNode(string sessionId, string clientInstanceId, EncryptedDataNode encryptedDataNode);

    Task<bool> RemoveDataNode(string sessionId, string clientInstanceId, EncryptedDataNode encryptedDataNode);
}