using ByteSync.ServerCommon.Interfaces.Repositories;
using ByteSync.ServerCommon.Interfaces.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ByteSync.ServerCommon.Commands.Synchronizations;

public class LocalCopyIsDoneCommandHandler : IRequestHandler<LocalCopyIsDoneRequest>
{
    private readonly ITrackingActionRepository _trackingActionRepository;
    private readonly ISynchronizationStatusCheckerService _synchronizationStatusCheckerService;
    private readonly ISynchronizationProgressService _synchronizationProgressService;
    private readonly ISynchronizationService _synchronizationService;
    private readonly ILogger<LocalCopyIsDoneCommandHandler> _logger;

    public LocalCopyIsDoneCommandHandler(
        ITrackingActionRepository trackingActionRepository,
        ISynchronizationStatusCheckerService synchronizationStatusCheckerService,
        ISynchronizationProgressService synchronizationProgressService,
        ISynchronizationService synchronizationService,
        ILogger<LocalCopyIsDoneCommandHandler> logger)
    {
        _trackingActionRepository = trackingActionRepository;
        _synchronizationStatusCheckerService = synchronizationStatusCheckerService;
        _synchronizationProgressService = synchronizationProgressService;
        _synchronizationService = synchronizationService;
        _logger = logger;
    }
    
    public async Task Handle(LocalCopyIsDoneRequest request, CancellationToken cancellationToken)
    {
        if (request.ActionsGroupIds.Count == 0)
        {
            _logger.LogInformation("LocalCopyIsDone: no action group IDs were provided");
            return;
        }
        
        var needSendSynchronizationUpdated = false;
                
        var result = await _trackingActionRepository.AddOrUpdate(request.SessionId, request.ActionsGroupIds, (trackingAction, synchronization) =>
        {
            if (!_synchronizationStatusCheckerService.CheckSynchronizationCanBeUpdated(synchronization))
            {
                return false;
            }
            
            var wasTrackingActionFinished = trackingAction.IsFinished;
            
            trackingAction.IsSourceSuccess = true;
            trackingAction.AddSuccessOnTarget(request.Client.ClientInstanceId);

            if (!wasTrackingActionFinished && trackingAction.IsFinished)
            {
                synchronization.Progress.FinishedActionsCount += 1;
            }
            
            synchronization.Progress.ProcessedVolume += trackingAction.Size ?? 0;
            
            needSendSynchronizationUpdated = _synchronizationService.CheckSynchronizationIsFinished(synchronization);

            return true;
        });

        if (result.IsSuccess)
        {
            await _synchronizationProgressService.UpdateSynchronizationProgress(result, needSendSynchronizationUpdated);
        }
        
        _logger.LogInformation("Local copy is done for session {SessionId} with {ActionCount} actions", 
            request.SessionId, request.ActionsGroupIds.Count);
    }
}