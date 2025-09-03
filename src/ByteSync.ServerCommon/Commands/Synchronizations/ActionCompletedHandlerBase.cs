using ByteSync.Common.Business.Actions;
using ByteSync.ServerCommon.Entities;
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
    
    protected virtual void ProcessSourceAction(TrackingActionEntity trackingAction, TRequest request)
    {

    }
    
    protected virtual void UpdateProgress(SynchronizationEntity synchronization, TrackingActionEntity trackingAction, TRequest request)
    {
        
    }

    public async Task Handle(TRequest request, CancellationToken cancellationToken)
    {
        if (request.ActionsGroupIds.Count == 0)
        {
            // ReSharper disable once TemplateIsNotCompileTimeConstantProblem
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
            
            ProcessSourceAction(trackingAction, request);

            if (request.NodeId != null)
            {
                var targetClientInstanceAndNodeId = new ByteSync.Common.Business.Actions.ClientInstanceIdAndNodeId
                {
                    ClientInstanceId = request.Client.ClientInstanceId,
                    NodeId = request.NodeId
                };
                
                HandleTarget(request, trackingAction, targetClientInstanceAndNodeId, synchronization);
            }
            else
            {
                var targetClientInstanceAndNodeIds = trackingAction.TargetClientInstanceAndNodeIds
                    .Where(x => x.ClientInstanceId == request.Client.ClientInstanceId)
                    .ToList();

                foreach (var targetId in targetClientInstanceAndNodeIds)
                {
                    HandleTarget(request, trackingAction, targetId, synchronization);
                }
            }
            
            UpdateProgress(synchronization, trackingAction, request);

            needSendSynchronizationUpdated = _synchronizationService.CheckSynchronizationIsFinished(synchronization);

            return true;
        });

        if (result.IsSuccess)
        {
            await _synchronizationProgressService.UpdateSynchronizationProgress(result, needSendSynchronizationUpdated);
        }

        // ReSharper disable once TemplateIsNotCompileTimeConstantProblem
        _logger.LogInformation(DoneLogTemplate, request.SessionId, request.ActionsGroupIds.Count);
    }

    private void HandleTarget(TRequest request, TrackingActionEntity trackingAction,
        ClientInstanceIdAndNodeId targetClientInstanceAndNodeId, SynchronizationEntity synchronization)
    {
        if (trackingAction.TargetClientInstanceAndNodeIds.Contains(targetClientInstanceAndNodeId))
        {
            if (trackingAction.ErrorTargetClientInstanceAndNodeIds.Contains(targetClientInstanceAndNodeId))
            {
                _logger.LogWarning("Client {ClientInstanceId} with NodeId {NodeId} reported action {ActionGroupId} as completed, but it was already marked as error",
                    request.Client.ClientInstanceId, request.NodeId, trackingAction.ActionsGroupId);
            }
            else
            {
                trackingAction.AddSuccessOnTarget(targetClientInstanceAndNodeId);
                synchronization.Progress.FinishedAtomicActionsCount += 1;
            }
        }
        else
        {
            throw new InvalidOperationException($"Client {request.Client.ClientInstanceId} with NodeId {request.NodeId} is not a target of the action");
        }
    }
}


