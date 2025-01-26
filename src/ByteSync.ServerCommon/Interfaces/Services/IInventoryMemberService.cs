using ByteSync.ServerCommon.Business.Auth;
using ByteSync.ServerCommon.Business.Sessions;

namespace ByteSync.ServerCommon.Interfaces.Services;

public interface IInventoryMemberService
{
    InventoryMemberData GetOrCreateInventoryMember(InventoryData inventoryData, string sessionId, Client client);
}