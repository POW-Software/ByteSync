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
    
    public async Task OnUploadIsFinishedAsync(SharedFileDefinition sharedFileDefinition, int totalParts, Client client)
    {
        var actionsGroupsIds = sharedFileDefinition.ActionsGroupIds;

        HashSet<string> targetInstanceIds = new HashSet<string>();
                
        var result = await _trackingActionRepository.AddOrUpdate(sharedFileDefinition.SessionId, actionsGroupsIds!, (trackingAction, synchronization) =>
        {
            if (!_synchronizationStatusCheckerService.CheckSynchronizationCanBeUpdated(synchronization))
            {
                return false;
            }
            
            trackingAction.IsSourceSuccess = true;

            foreach (var targetClientInstanceId in trackingAction.TargetClientInstanceIds)
            {
                targetInstanceIds.Add(targetClientInstanceId);
            }

            return true;
        });

        if (result.IsSuccess)
        {
            await _synchronizationProgressService.UploadIsFinished(sharedFileDefinition, totalParts, targetInstanceIds);
        }
    }

    public async Task OnFilePartIsUploadedAsync(SharedFileDefinition sharedFileDefinition, int partNumber)
    {
        var synchronization = await _synchronizationRepository.Get(sharedFileDefinition.SessionId);

        if (!_synchronizationStatusCheckerService.CheckSynchronizationCanBeUpdated(synchronization))
        {
            return;
        }
        
        if (sharedFileDefinition.ActionsGroupIds == null || sharedFileDefinition.ActionsGroupIds.Count == 0)
        {
            throw new BadRequestException("sharedFileDefinition.ActionsGroupIds is null or empty");
        }
        
        var actionsGroupsId = sharedFileDefinition.ActionsGroupIds!.First();
        var trackingAction = await _trackingActionRepository.GetOrThrow(sharedFileDefinition.SessionId, actionsGroupsId);
        
        await _synchronizationProgressService.FilePartIsUploaded(sharedFileDefinition, partNumber, trackingAction.TargetClientInstanceIds);
    }

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
            
            trackingAction.AddSuccessOnTarget(client.ClientInstanceId);

            if (!wasTrackingActionFinished && trackingAction.IsFinished)
            {
                synchronization.Progress.FinishedActionsCount += 1;
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
            synchronizationEntity.EndedOn = DateTimeOffset.Now;
            
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