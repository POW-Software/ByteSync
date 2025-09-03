using ByteSync.Common.Business.Actions;
using ByteSync.ServerCommon.Entities;
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
            
            var isErrorHandled = false;

            if (trackingAction.SourceClientInstanceId == request.Client.ClientInstanceId)
            {
                trackingAction.IsSourceSuccess = false;
                isErrorHandled = true;
            }
            else
            {
                if (request.NodeId != null)
                {
                    var targetClientInstanceAndNodeId = new ClientInstanceIdAndNodeId
                    {
                        ClientInstanceId = request.Client.ClientInstanceId,
                        NodeId = request.NodeId
                    };
                    
                    if (trackingAction.TargetClientInstanceAndNodeIds.Contains(targetClientInstanceAndNodeId))
                    {
                        isErrorHandled |= HandleTarget(request, trackingAction, targetClientInstanceAndNodeId, synchronization);
                    }
                    else
                    {
                        throw new InvalidOperationException($"Client {request.Client.ClientInstanceId} with NodeId {request.NodeId} is not a target of the action");
                    }
                }
                else
                {
                    var targetClientInstanceAndNodeIds = trackingAction.TargetClientInstanceAndNodeIds
                        .Where(x => x.ClientInstanceId == request.Client.ClientInstanceId)
                        .ToList();

                    foreach (var targetId in targetClientInstanceAndNodeIds)
                    {
                        if (trackingAction.TargetClientInstanceAndNodeIds.Contains(targetId))
                        {
                            isErrorHandled |= HandleTarget(request, trackingAction, targetId, synchronization);
                        }
                        else
                        {
                            throw new InvalidOperationException($"Client {request.Client.ClientInstanceId} with NodeId {request.NodeId} is not a target of the action");
                        }
                    }
                }
            }

            if (isErrorHandled)
            {
                needSendSynchronizationUpdated = _synchronizationService.CheckSynchronizationIsFinished(synchronization);
            }

            return isErrorHandled;
        });

        if (result.IsSuccess)
        {
            await _synchronizationProgressService.UpdateSynchronizationProgress(result, needSendSynchronizationUpdated);
        }

        _logger.LogInformation("Synchronization errors reported for session {SessionId} with {ActionCount} actions", request.SessionId, actionGroupIds.Count);
    }

    private bool HandleTarget(SynchronizationErrorsRequest request, TrackingActionEntity trackingAction,
        ClientInstanceIdAndNodeId targetId, SynchronizationEntity synchronization)
    {
        bool isErrorHandled = false;
        
        if (trackingAction.SuccessTargetClientInstanceAndNodeIds.Contains(targetId))
        {
            _logger.LogWarning(
                "Client {ClientInstanceId} with NodeId {NodeId} reported an error but was already marked as success for action {ActionGroupId}",
                request.Client.ClientInstanceId, request.NodeId, trackingAction.ActionsGroupId);
        }
        else
        {
            trackingAction.AddErrorOnTarget(targetId);
            synchronization.Progress.ErrorsCount += 1;
            synchronization.Progress.FinishedAtomicActionsCount += 1;

            isErrorHandled = true;
        }

        return isErrorHandled;
    }
}