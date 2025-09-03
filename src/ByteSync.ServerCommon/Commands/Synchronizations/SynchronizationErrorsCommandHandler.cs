using ByteSync.ServerCommon.Interfaces.Repositories;
using ByteSync.ServerCommon.Interfaces.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ByteSync.ServerCommon.Commands.Synchronizations;

public class SynchronizationErrorsCommandHandler : IRequestHandler<SynchronizationErrorsRequest>
{
    private readonly ITrackingActionRepository _trackingActionRepository;
    private readonly ISynchronizationStatusCheckerService _synchronizationStatusCheckerService;
    private readonly ISynchronizationProgressService _synchronizationProgressService;
    private readonly ISynchronizationService _synchronizationService;
    private readonly ILogger<SynchronizationErrorsCommandHandler> _logger;

    public SynchronizationErrorsCommandHandler(
        ITrackingActionRepository trackingActionRepository,
        ISynchronizationStatusCheckerService synchronizationStatusCheckerService,
        ISynchronizationProgressService synchronizationProgressService,
        ISynchronizationService synchronizationService,
        ILogger<SynchronizationErrorsCommandHandler> logger)
    {
        _trackingActionRepository = trackingActionRepository;
        _synchronizationStatusCheckerService = synchronizationStatusCheckerService;
        _synchronizationProgressService = synchronizationProgressService;
        _synchronizationService = synchronizationService;
        _logger = logger;
    }

    public async Task Handle(SynchronizationErrorsRequest request, CancellationToken cancellationToken)
    {
        var actionGroupIds = request.ActionsGroupIds;
        if (actionGroupIds.Count == 0)
        {
            _logger.LogInformation("SynchronizationError: no action group IDs were provided");
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
                if (request.NodeId != null)
                {
                    // NodeId spécifique fourni - traitement précis
                    var targetClientInstanceAndNodeId = new ByteSync.Common.Business.Actions.ClientInstanceIdAndNodeId
                    {
                        ClientInstanceId = request.Client.ClientInstanceId,
                        NodeId = request.NodeId
                    };
                    
                    if (trackingAction.TargetClientInstanceAndNodeIds.Contains(targetClientInstanceAndNodeId))
                    {
                        trackingAction.AddErrorOnTarget(targetClientInstanceAndNodeId);
                    }
                    else
                    {
                        throw new InvalidOperationException($"Client {request.Client.ClientInstanceId} with NodeId {request.NodeId} is not a target of the action");
                    }
                }
                else
                {
                    // NodeId null - traitement d'erreur sur tous les targets correspondant au ClientInstanceId
                    var targetClientInstanceAndNodeIds = trackingAction.TargetClientInstanceAndNodeIds
                        .Where(x => x.ClientInstanceId == request.Client.ClientInstanceId)
                        .ToList();

                    foreach (var targetId in targetClientInstanceAndNodeIds)
                    {
                        trackingAction.AddErrorOnTarget(targetId);
                    }
                }
            }

            if (isNewError)
            {
                synchronization.Progress.ErrorsCount += 1;
            }
            if (!wasTrackingActionFinished && trackingAction.IsFinished)
            {
                synchronization.Progress.FinishedAtomicActionsCount += 1;
            }

            needSendSynchronizationUpdated = _synchronizationService.CheckSynchronizationIsFinished(synchronization);

            return true;
        });

        if (result.IsSuccess)
        {
            await _synchronizationProgressService.UpdateSynchronizationProgress(result, needSendSynchronizationUpdated);
        }

        _logger.LogInformation("Synchronization errors reported for session {SessionId} with {ActionCount} actions", request.SessionId, actionGroupIds.Count);
    }
}