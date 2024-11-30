using ByteSync.Business;
using ByteSync.Common.Business.Sessions;

namespace ByteSync.Services.Misc;

public static class LocalInventoryStatusHelper
{
    public static SessionMemberGeneralStatus ConvertStartInventory(this LocalInventoryModes localInventoryMode)
    {
        SessionMemberGeneralStatus localInventoryStatus;
        
        if (localInventoryMode == LocalInventoryModes.Base)
        {
            localInventoryStatus = SessionMemberGeneralStatus.InventoryRunningIdentification;
        }
        else
        {
            localInventoryStatus = SessionMemberGeneralStatus.InventoryRunningAnalysis;
        }

        return localInventoryStatus;
    }
    
    public static SessionMemberGeneralStatus ConvertFinishInventory(this LocalInventoryModes localInventoryMode)
    {
        SessionMemberGeneralStatus localInventoryStatus;
        
        if (localInventoryMode == LocalInventoryModes.Base)
        {
            localInventoryStatus = SessionMemberGeneralStatus.InventoryWaitingForAnalysis;
        }
        else
        {
            localInventoryStatus = SessionMemberGeneralStatus.InventoryFinished;
        }

        return localInventoryStatus;
    }
}