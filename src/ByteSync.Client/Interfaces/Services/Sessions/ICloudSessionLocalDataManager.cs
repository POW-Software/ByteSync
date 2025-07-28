using ByteSync.Business;
using ByteSync.Business.Actions.Shared;
using ByteSync.Common.Business.SharedFiles;
using ByteSync.Models.Inventories;

namespace ByteSync.Interfaces.Services.Sessions;

public interface ICloudSessionLocalDataManager
{
    string GetCurrentMachineInventoryPath(Inventory inventory, LocalInventoryModes localInventoryMode);

    string GetInventoryPath(SharedFileDefinition sharedFileDefinition);
        
    string GetInventoryPath(string clientInstanceId, string inventoryCodeAndId, LocalInventoryModes localInventoryMode);

    string GetTempDeltaFullName(SharedDataPart source, SharedDataPart target);
        
    string GetSynchronizationStartDataPath();
        
    string GetSynchronizationTempZipPath(SharedFileDefinition sharedFileDefinition);
        
    Task BackupCurrentSessionFiles();
}