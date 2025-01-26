using ByteSync.Common.Business.Sessions;
using ByteSync.ServerCommon.Business.Auth;
using ByteSync.ServerCommon.Business.Sessions;
using ByteSync.ServerCommon.Interfaces.Services;

namespace ByteSync.ServerCommon.Services;

public class InventoryMemberService : IInventoryMemberService
{
    public InventoryMemberData GetOrCreateInventoryMember(InventoryData inventoryData, string sessionId, Client client)
    {
        var inventoryMember = inventoryData.InventoryMembers.SingleOrDefault(imd => imd.ClientInstanceId == client.ClientInstanceId);

        if (inventoryMember == null)
        {
            inventoryMember = new InventoryMemberData
            {
                SessionId = sessionId,
                ClientInstanceId = client.ClientInstanceId,
                SessionMemberGeneralStatus = SessionMemberGeneralStatus.InventoryWaitingForStart,
            };

            inventoryData.InventoryMembers.Add(inventoryMember);
        }

        return inventoryMember;
    }
}