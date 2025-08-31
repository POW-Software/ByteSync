using ByteSync.Common.Business.SharedFiles;
using ByteSync.Common.Business.Synchronizations;
using ByteSync.ServerCommon.Business.Auth;
using ByteSync.ServerCommon.Entities;
using ByteSync.ServerCommon.Exceptions;
using ByteSync.ServerCommon.Interfaces.Repositories;
using ByteSync.ServerCommon.Interfaces.Services;

namespace ByteSync.ServerCommon.Services;

public class SynchronizationService : ISynchronizationService
{
    private readonly ISynchronizationRepository _synchronizationRepository;
    private readonly ITrackingActionRepository _trackingActionRepository;
    private readonly ISynchronizationProgressService _synchronizationProgressService;
    private readonly ISynchronizationStatusCheckerService _synchronizationStatusCheckerService;

    public SynchronizationService(ISynchronizationRepository synchronizationRepository,
        ITrackingActionRepository trackingActionRepository, ISynchronizationProgressService synchronizationProgressService, 
        ISynchronizationStatusCheckerService synchronizationStatusCheckerService)
    {
        _synchronizationRepository = synchronizationRepository;
        _trackingActionRepository = trackingActionRepository;
        _synchronizationProgressService = synchronizationProgressService;
        _synchronizationStatusCheckerService = synchronizationStatusCheckerService;
    }
    
    // OnFilePartIsUploadedAsync has been inlined into AssertFilePartIsUploadedCommandHandler

    public async Task OnDownloadIsFinishedAsync(SharedFileDefinition sharedFileDefinition, Client client)
    {
        var actionsGroupsIds = sharedFileDefinition.ActionsGroupIds;

        bool needSendSynchronizationUpdated = false;
                
        var result = await _trackingActionRepository.AddOrUpdate(sharedFileDefinition.SessionId, actionsGroupsIds!, (trackingAction, synchronization) =>
        {
            if (!_synchronizationStatusCheckerService.CheckSynchronizationCanBeUpdated(synchronization))
            {
                return false;
            }
            
            bool wasTrackingActionFinished = trackingAction.IsFinished;
            
            var targetsForClient = trackingAction.TargetClientInstanceAndNodeIds
                .Where(x => x.ClientInstanceId == client.ClientInstanceId)
                .ToList();
            foreach (var target in targetsForClient)
            {
                trackingAction.AddSuccessOnTarget(target);
            }

            if (!wasTrackingActionFinished && trackingAction.IsFinished)
            {
                synchronization.Progress.FinishedAtomicActionsCount += 1;
                synchronization.Progress.ProcessedVolume += trackingAction.Size ?? 0;
            }

            if (sharedFileDefinition.IsMultiFileZip)
            {
                synchronization.Progress.ExchangedVolume += trackingAction.Size ?? 0;
            }
            else
            {
                synchronization.Progress.ExchangedVolume += sharedFileDefinition.UploadedFileLength;
            }
            
            needSendSynchronizationUpdated = CheckSynchronizationIsFinished(synchronization);

            return true;
        });

        if (result.IsSuccess)
        {
            await _synchronizationProgressService.UpdateSynchronizationProgress(result, needSendSynchronizationUpdated);
        }
    }

    public bool CheckSynchronizationIsFinished(SynchronizationEntity synchronizationEntity)
    {
        bool isUpdated = false;
        
        if (!synchronizationEntity.IsEnded && 
            (synchronizationEntity.Progress.AllMembersCompleted && 
                (synchronizationEntity.Progress.AllActionsDone || synchronizationEntity.IsAbortRequested)))
        {
            synchronizationEntity.EndedOn = DateTimeOffset.UtcNow;
            
            if (synchronizationEntity.IsAbortRequested)
            {
                synchronizationEntity.EndStatus = SynchronizationEndStatuses.Abortion;
            }
            else
            {
                synchronizationEntity.EndStatus = SynchronizationEndStatuses.Regular;
            }
            
            isUpdated = true;
        }

        return isUpdated;
    }

    public async Task ResetSession(string sessionId)
    {
        await _synchronizationRepository.ResetSession(sessionId);
    }

}