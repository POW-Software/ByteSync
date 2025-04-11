namespace ByteSync.ServerCommon.Interfaces.Services;

public interface IInventoryService
{
    // Task<bool> AddPathItem(string sessionId, Client client, EncryptedPathItem encryptedPathItem);

    // Task<bool> RemovePathItem(string sessionId, Client client, EncryptedPathItem encryptedPathItem);
    //
    // Task<List<EncryptedPathItem>> GetPathItems(string sessionId, string clientInstanceId);
    
    // Task<bool> SetLocalInventoryStatus(Client byteSyncEndpoint, UpdateSessionMemberGeneralStatusParameters parameters);
    
    Task ResetSession(string sessionId);
}