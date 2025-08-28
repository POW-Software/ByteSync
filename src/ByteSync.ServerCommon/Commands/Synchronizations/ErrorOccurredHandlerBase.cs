using ByteSync.ServerCommon.Interfaces.Repositories;
using ByteSync.ServerCommon.Interfaces.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ByteSync.ServerCommon.Commands.Synchronizations;

public abstract class ErrorOccurredHandlerBase<TRequest> : IRequestHandler<TRequest>
    where TRequest : IActionErrorRequest
{
    protected readonly ITrackingActionRepository _trackingActionRepository;
    protected readonly ISynchronizationStatusCheckerService _synchronizationStatusCheckerService;
    protected readonly ISynchronizationProgressService _synchronizationProgressService;
    protected readonly ISynchronizationService _synchronizationService;
    protected readonly ILogger _logger;

    protected ErrorOccurredHandlerBase(
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

    protected abstract List<string>? GetActionsGroupIds(TRequest request);

    public async Task Handle(TRequest request, CancellationToken cancellationToken)
    {
        var actionGroupIds = GetActionsGroupIds(request);
        if (actionGroupIds == null || actionGroupIds.Count == 0)
        {
            _logger.LogInformation(EmptyIdsLog);
            return;
        }

        var needSendSynchronizationUpdated = false;

        var result = await _trackingActionRepository.AddOrUpdate(request.SessionId, actionGroupIds, (trackingAction, synchronization) =>
        {
            if (!_synchronizationStatusCheckerService.CheckSynchronizationCanBeUpdated(synchronization))
            {
                return false;
            }

            var wasTrackingActionFinished = trackingAction.IsFinished;
            var isNewError = !trackingAction.IsError;

            if (trackingAction.SourceClientInstanceId == request.Client.ClientInstanceId)
            {
                trackingAction.IsSourceSuccess = false;
            }
            else
            {
                var targetClientInstanceAndNodeId = trackingAction.TargetClientInstanceAndNodeIds
                    .FirstOrDefault(id => id.StartsWith($"{request.Client.ClientInstanceId}_"));
                    
                if (targetClientInstanceAndNodeId != null)
                {
                    trackingAction.AddErrorOnTarget(targetClientInstanceAndNodeId);
                }
                else
                {
                    throw new InvalidOperationException("Client is not a source or target of the action");
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

            needSendSynchronizationUpdated = _synchronizationService.CheckSynchronizationIsFinished(synchronization);

            return true;
        });

        if (result.IsSuccess)
        {
            await _synchronizationProgressService.UpdateSynchronizationProgress(result, needSendSynchronizationUpdated);
        }

        _logger.LogInformation(DoneLogTemplate, request.SessionId, actionGroupIds.Count);
    }
}


