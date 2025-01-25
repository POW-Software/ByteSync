using ByteSync.Common.Business.Inventories;
using ByteSync.Common.Business.Sessions.Cloud;
using ByteSync.ServerCommon.Business.Auth;

namespace ByteSync.ServerCommon.Interfaces.Services;

public interface IInventoryService
{
    Task<StartInventoryResult> StartInventory(string sessionId, Client client);
    
    Task<bool> AddPathItem(string sessionId, Client client, EncryptedPathItem encryptedPathItem);

    Task<bool> RemovePathItem(string sessionId, Client client, EncryptedPathItem encryptedPathItem);

    Task<List<EncryptedPathItem>> GetPathItems(string sessionId, string clientInstanceId);
    
    Task<bool> SetLocalInventoryStatus(Client byteSyncEndpoint, UpdateSessionMemberGeneralStatusParameters parameters);
    
    Task ResetSession(string sessionId);
}