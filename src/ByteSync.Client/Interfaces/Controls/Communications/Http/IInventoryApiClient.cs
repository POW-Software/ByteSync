using System.Threading.Tasks;
using ByteSync.Common.Business.Inventories;
using ByteSync.Common.Business.Sessions;
using ByteSync.Common.Business.Sessions.Cloud;

namespace ByteSync.Interfaces.Controls.Communications.Http;

public interface IInventoryApiClient
{
    Task<StartInventoryResult> StartInventory(string sessionId, EncryptedSessionSettings encryptedSessionSettings);
    
    Task<List<EncryptedDataSource>?> GetPathItems(string sessionId, string clientInstanceId);
    
    Task<bool> AddDataSource(string sessionId, EncryptedDataSource encryptedDataSource);
    
    Task<bool> RemoveDataSource(string sessionId, EncryptedDataSource encryptedDataSource);
}