using ByteSync.Business.Actions.Shared;
using ByteSync.Business.Communications;
using ByteSync.Interfaces.Controls.Actions;
using ByteSync.Interfaces.Controls.Synchronizations;
using ByteSync.Interfaces.Repositories;
using ByteSync.Interfaces.Services.Sessions;
using ByteSync.Services.Actions;

namespace ByteSync.Services.Synchronizations;

public class SynchronizationDataReceiver : ISynchronizationDataReceiver
{
    private readonly ICloudSessionLocalDataManager _cloudSessionLocalDataManager;
    private readonly ISharedAtomicActionRepository _sharedAtomicActionRepository;
    private readonly ISharedActionsGroupRepository _sharedActionsGroupRepository;
    private readonly ISynchronizationService _synchronizationService;
    private readonly ISynchronizationDataLogger _synchronizationDataLogger;
    private readonly ISharedActionsGroupOrganizer _sharedActionsGroupOrganizer;

    public SynchronizationDataReceiver(ICloudSessionLocalDataManager cloudSessionLocalDataManager, ISharedAtomicActionRepository sharedAtomicActionRepository,
        ISharedActionsGroupRepository sharedActionsGroupRepository, ISynchronizationService synchronizationService, 
        ISharedActionsGroupOrganizer sharedActionsGroupOrganizer, ISynchronizationDataLogger synchronizationDataLogger)
    {
        _cloudSessionLocalDataManager = cloudSessionLocalDataManager;
        _sharedAtomicActionRepository = sharedAtomicActionRepository;
        _sharedActionsGroupRepository = sharedActionsGroupRepository;
        _synchronizationService = synchronizationService;
        _sharedActionsGroupOrganizer = sharedActionsGroupOrganizer;
        _synchronizationDataLogger = synchronizationDataLogger;
    }
    
    public async Task OnSynchronizationDataFileDownloaded(LocalSharedFile downloadTargetLocalSharedFile)
    {
        var synchronizationDataPath = _cloudSessionLocalDataManager.GetSynchronizationStartDataPath();
        var synchronizationDataSaver = new SynchronizationDataSaver();
        var synchronizationData = synchronizationDataSaver.Load(synchronizationDataPath);

        await SetSynchronizationStartData(synchronizationData);
    }
    
    private async Task SetSynchronizationStartData(SharedSynchronizationStartData sharedSynchronizationStartData)
    {
        _sharedAtomicActionRepository.SetSharedAtomicActions(sharedSynchronizationStartData.SharedAtomicActions);
        _sharedActionsGroupRepository.SetSharedActionsGroups(sharedSynchronizationStartData.SharedActionsGroups);
        
        await _sharedActionsGroupOrganizer.OrganizeSharedActionGroups();
        
        await _synchronizationDataLogger.LogReceivedSynchronizationData(sharedSynchronizationStartData);

        await _synchronizationService.OnSynchronizationDataTransmitted(sharedSynchronizationStartData);
    }
}