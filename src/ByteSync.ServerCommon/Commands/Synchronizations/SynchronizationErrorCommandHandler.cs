using ByteSync.Common.Business.Synchronizations;
using ByteSync.ServerCommon.Entities;
using ByteSync.ServerCommon.Interfaces.Repositories;
using ByteSync.ServerCommon.Interfaces.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ByteSync.ServerCommon.Commands.Synchronizations;

public class SynchronizationErrorCommandHandler : IRequestHandler<SynchronizationErrorRequest>
{
    private readonly ITrackingActionRepository _trackingActionRepository;
    private readonly ISynchronizationStatusCheckerService _synchronizationStatusCheckerService;
    private readonly ISynchronizationProgressService _synchronizationProgressService;
    private readonly ILogger<SynchronizationErrorCommandHandler> _logger;

    public SynchronizationErrorCommandHandler(
        ITrackingActionRepository trackingActionRepository,
        ISynchronizationStatusCheckerService synchronizationStatusCheckerService,
        ISynchronizationProgressService synchronizationProgressService,
        ILogger<SynchronizationErrorCommandHandler> logger)
    {
        _trackingActionRepository = trackingActionRepository;
        _synchronizationStatusCheckerService = synchronizationStatusCheckerService;
        _synchronizationProgressService = synchronizationProgressService;
        _logger = logger;
    }
    
    public async Task Handle(SynchronizationErrorRequest request, CancellationToken cancellationToken)
    {
        if (request.SharedFileDefinition.ActionsGroupIds?.Count == 0)
        {
            _logger.LogInformation("OnSuccessOnTarget: no action group id provided");
            return;
        }
        
        bool needSendSynchronizationUpdated = false;
        
        var result = await _trackingActionRepository.AddOrUpdate(request.SessionId, request.SharedFileDefinition.ActionsGroupIds!, (trackingAction, synchronization) =>
        {
            if (!_synchronizationStatusCheckerService.CheckSynchronizationCanBeUpdated(synchronization))
            {
                return false;
            }
            
            bool wasTrackingActionFinished = trackingAction.IsFinished;
            bool isNewError = !trackingAction.IsError;
            
            if (trackingAction.SourceClientInstanceId == request.Client.ClientInstanceId)
            {
                trackingAction.IsSourceSuccess = false;
            }
            else
            {
                if (trackingAction.TargetClientInstanceIds.Contains(request.Client.ClientInstanceId))
                {
                    trackingAction.AddErrorOnTarget(request.Client.ClientInstanceId);
                }
                else
                {
                    throw new InvalidOperationException("Client is not a target of the action");
                }
            }
            
            if (isNewError)
            {
                synchronization.Progress.ErrorsCount += 1;
            }
            if (!wasTrackingActionFinished && trackingAction.IsFinished)
            {
                synchronization.Progress.FinishedActionsCount += 1;
            }
            
            needSendSynchronizationUpdated = CheckSynchronizationIsFinished(synchronization);

            return true;
        });

        if (result.IsSuccess)
        {
            await _synchronizationProgressService.UpdateSynchronizationProgress(result, needSendSynchronizationUpdated);
        }
        
        _logger.LogInformation("Synchronization error reported for session {SessionId} with {ActionCount} actions", 
            request.SessionId, request.SharedFileDefinition.ActionsGroupIds?.Count ?? 0);
    }
    
    private bool CheckSynchronizationIsFinished(SynchronizationEntity synchronizationEntity)
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
}