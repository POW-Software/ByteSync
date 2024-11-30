using System.Threading.Tasks;
using ByteSync.Common.Business.Inventories;
using ByteSync.Common.Business.Sessions;
using ByteSync.Common.Business.Sessions.Cloud;

namespace ByteSync.Interfaces.Controls.Communications.Http;

public interface IInventoryApiClient
{
    Task<StartInventoryResult> StartInventory(string sessionId, EncryptedSessionSettings encryptedSessionSettings);
    
    Task<List<EncryptedPathItem>?> GetPathItems(string sessionId, string clientInstanceId);
    
    Task<bool> AddPathItem(string sessionId, EncryptedPathItem encryptedPathItem);
    
    Task<bool> RemovePathItem(string sessionId, EncryptedPathItem encryptedPathItem);
}