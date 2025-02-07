using ByteSync.Business;
using ByteSync.Business.Actions.Shared;
using ByteSync.Common.Business.SharedFiles;

namespace ByteSync.Interfaces.Services.Sessions
{
    public interface ICloudSessionLocalDataManager
    {
        string GetCurrentMachineInventoryPath(string letter, LocalInventoryModes localInventoryMode);

        string GetInventoryPath(SharedFileDefinition sharedFileDefinition);
        
        string GetInventoryPath(string clientInstanceId, string letter, LocalInventoryModes localInventoryMode);

        string GetTempDeltaFullName(SharedDataPart source, SharedDataPart target);
        
        string GetSynchronizationStartDataPath();
        
        string GetSynchronizationTempZipPath(SharedFileDefinition sharedFileDefinition);
        
        Task BackupCurrentSessionFiles();
    }
}