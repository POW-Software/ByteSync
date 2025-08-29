using ByteSync.ServerCommon.Interfaces.Repositories;
using ByteSync.ServerCommon.Interfaces.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ByteSync.ServerCommon.Commands.Synchronizations;

public abstract class ActionCompletedHandlerBase<TRequest> : IRequestHandler<TRequest>
    where TRequest : IActionCompletedRequest
{
    protected readonly ITrackingActionRepository _trackingActionRepository;
    protected readonly ISynchronizationStatusCheckerService _synchronizationStatusCheckerService;
    protected readonly ISynchronizationProgressService _synchronizationProgressService;
    protected readonly ISynchronizationService _synchronizationService;
    protected readonly ILogger _logger;

    protected ActionCompletedHandlerBase(
        ITrackingActionRepository trackingActionRepository,
        ISynchronizationStatusCheckerService synchronizationStatusCheckerService,
        ISynchronizationProgressService synchronizationProgressService,
        ISynchronizationService synchronizationService,
        ILogger logger)
    {
        _trackingActionRepository = trackingActionRepository;
        _synchronizationStatusCheckerService = synchronizationStatusCheckerService;
        _synchronizationProgressService = synchronizationProgressService;
        _synchronizationService = synchronizationService;
        _logger = logger;
    }

    protected abstract string EmptyIdsLog { get; }
    protected abstract string DoneLogTemplate { get; }

    public async Task Handle(TRequest request, CancellationToken cancellationToken)
    {
        if (request.ActionsGroupIds.Count == 0)
        {
            _logger.LogInformation(EmptyIdsLog);
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

            if (request.NodeId != null)
            {
                var targetClientInstanceAndNodeId = $"{request.Client.ClientInstanceId}_{request.NodeId}";
                
                if (trackingAction.TargetClientInstanceAndNodeIds.Contains(targetClientInstanceAndNodeId))
                {
                    trackingAction.AddSuccessOnTarget(targetClientInstanceAndNodeId);
                }
                else
                {
                    throw new InvalidOperationException($"Client {request.Client.ClientInstanceId} with NodeId {request.NodeId} is not a target of the action");
                }
            }
            else
            {
                throw new InvalidOperationException("Client NodeId is required to identify the target");
            }

            if (!wasTrackingActionFinished && trackingAction.IsFinished)
            {
                synchronization.Progress.FinishedActionsCount += 1;
            }

            needSendSynchronizationUpdated = _synchronizationService.CheckSynchronizationIsFinished(synchronization);

            return true;
        });

        if (result.IsSuccess)
        {
            await _synchronizationProgressService.UpdateSynchronizationProgress(result, needSendSynchronizationUpdated);
        }

        _logger.LogInformation(DoneLogTemplate, request.SessionId, request.ActionsGroupIds.Count);
    }
}


