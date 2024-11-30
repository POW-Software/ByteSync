using System.Threading.Tasks;
using ByteSync.Business.Actions.Shared;
using ByteSync.Business.Communications;
using ByteSync.Interfaces.Controls.Actions;
using ByteSync.Interfaces.Controls.Sessions;
using ByteSync.Interfaces.Controls.Synchronizations;
using ByteSync.Interfaces.Repositories;
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



        /* todo 050523
        await RunAsync(synchronizationStartData.SessionId, cloudSessionLocalDetails =>
        {
            if (cloudSessionLocalDetails is { HasSynchronizationStarted: false })
            {
                cloudSessionLocalDetails.SharedActionsGroups.Clear();
                cloudSessionLocalDetails.SharedActionsGroups.AddAll(synchronizationStartData.SharedActionsGroups);

                _uiHelper.ClearAndAddOnUI(cloudSessionLocalDetails.SharedAtomicActions, synchronizationStartData.SharedAtomicActions)
                    .ContinueWith(_ =>
                    {
                        cloudSessionLocalDetails.RegisterActionsGroupsAndAtomicActionsLinks();
                    })
                    .ContinueWith(_ =>
                    {
                        cloudSessionLocalDetails.SynchronizationDataReady.Set();
                    });

                Log.Information("The Data Synchronization actions have been set:");
                if (synchronizationStartData.SharedAtomicActions.Count == 0)
                {
                    Log.Information(" - No action to perform");
                }
                else
                {
                    Log.Information(" - {Count} action(s) to perform", synchronizationStartData.SharedAtomicActions.Count);
                }
                foreach (var synchronizationRule in synchronizationStartData.LooseSynchronizationRules)
                {
                    var descriptionBuilder = new SynchronizationRuleDescriptionBuilder(synchronizationRule);
                    descriptionBuilder.BuildDescription(" | ");
                    var description = $"{descriptionBuilder.Mode} [{descriptionBuilder.Conditions}] {descriptionBuilder.Then} " +
                                      $"[{descriptionBuilder.Actions}]";

                    Log.Information(" - Synchronization Rule: {Description}", description);
                }
                foreach (var sharedAtomicAction in synchronizationStartData.SharedAtomicActions.Where(a => !a.IsFromSynchronizationRule))
                {
                    var descriptionBuilder = new SharedAtomicActionDescriptionBuilder();
                    var description = $"{sharedAtomicAction.PathIdentity.LinkingData} ({sharedAtomicAction.PathIdentity.FileSystemType}) - " +
                                      $"{descriptionBuilder.GetDescription(sharedAtomicAction)}";

                    Log.Information(" - Targeted Action: {LinkingData} ({FileSystemType}) - {Description}",
                        sharedAtomicAction.PathIdentity.LinkingData, sharedAtomicAction.PathIdentity.FileSystemType, description);
                }
            }
        });

        await WaitForSynchronizationDataReadyAsync();

        */
    }
}