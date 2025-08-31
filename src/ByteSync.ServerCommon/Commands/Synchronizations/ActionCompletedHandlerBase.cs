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

    /// <summary>
    /// Permet aux classes dérivées de traiter la logique spécifique à la source (ex: IsSourceSuccess = true)
    /// </summary>
    protected virtual void ProcessSourceAction(TrackingActionEntity trackingAction, TRequest request)
    {
        // Implémentation par défaut : rien à faire
    }

    /// <summary>
    /// Permet aux classes dérivées de mettre à jour la progression (ex: ProcessedVolume)
    /// </summary>
    protected virtual void UpdateProgress(SynchronizationEntity synchronization, TrackingActionEntity trackingAction, TRequest request)
    {
        // Implémentation par défaut : rien à faire
    }

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

            // Permettre aux classes dérivées de traiter la logique source (ex: IsSourceSuccess = true)
            ProcessSourceAction(trackingAction, request);

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
                    trackingAction.AddSuccessOnTarget(targetClientInstanceAndNodeId);
                }
                else
                {
                    throw new InvalidOperationException($"Client {request.Client.ClientInstanceId} with NodeId {request.NodeId} is not a target of the action");
                }
            }
            else
            {
                // NodeId null - traitement de tous les targets correspondant au ClientInstanceId
                var targetClientInstanceAndNodeIds = trackingAction.TargetClientInstanceAndNodeIds
                    .Where(x => x.ClientInstanceId == request.Client.ClientInstanceId)
                    .ToList();

                foreach (var targetId in targetClientInstanceAndNodeIds)
                {
                    trackingAction.AddSuccessOnTarget(targetId);
                }
            }

            if (!wasTrackingActionFinished && trackingAction.IsFinished)
            {
                synchronization.Progress.FinishedActionsCount += 1;
            }

            // Permettre aux classes dérivées de traiter la logique de progression (ex: ProcessedVolume)
            UpdateProgress(synchronization, trackingAction, request);

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


