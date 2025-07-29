using ByteSync.Common.Business.Synchronizations;
using ByteSync.ServerCommon.Entities;
using ByteSync.ServerCommon.Interfaces.Repositories;
using ByteSync.ServerCommon.Interfaces.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ByteSync.ServerCommon.Commands.Synchronizations;

public class DateIsCopiedCommandHandler : IRequestHandler<DateIsCopiedRequest>
{
    private readonly ITrackingActionRepository _trackingActionRepository;
    private readonly ISynchronizationStatusCheckerService _synchronizationStatusCheckerService;
    private readonly ISynchronizationProgressService _synchronizationProgressService;
    private readonly ILogger<DateIsCopiedCommandHandler> _logger;

    public DateIsCopiedCommandHandler(
        ITrackingActionRepository trackingActionRepository,
        ISynchronizationStatusCheckerService synchronizationStatusCheckerService,
        ISynchronizationProgressService synchronizationProgressService,
        ILogger<DateIsCopiedCommandHandler> logger)
    {
        _trackingActionRepository = trackingActionRepository;
        _synchronizationStatusCheckerService = synchronizationStatusCheckerService;
        _synchronizationProgressService = synchronizationProgressService;
        _logger = logger;
    }
    
    public async Task Handle(DateIsCopiedRequest request, CancellationToken cancellationToken)
    {
        if (request.ActionsGroupIds.Count == 0)
        {
            _logger.LogInformation("OnSuccessOnTarget: no action group id provided");
            return;
        }
        
        bool needSendSynchronizationUpdated = false;
        
        var result = await _trackingActionRepository.AddOrUpdate(request.SessionId, request.ActionsGroupIds, (trackingAction, synchronization) =>
        {
            if (!_synchronizationStatusCheckerService.CheckSynchronizationCanBeUpdated(synchronization))
            {
                return false;
            }
            
            bool wasTrackingActionFinished = trackingAction.IsFinished;
            
            trackingAction.AddSuccessOnTarget(request.Client.ClientInstanceId);
            
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
        
        _logger.LogInformation("Date is copied for session {SessionId} with {ActionCount} actions", 
            request.SessionId, request.ActionsGroupIds.Count);
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