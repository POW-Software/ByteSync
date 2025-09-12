using ByteSync.ServerCommon.Interfaces.Repositories;
using ByteSync.ServerCommon.Interfaces.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ByteSync.ServerCommon.Commands.FileTransfers;

public class AssertDownloadIsFinishedCommandHandler : IRequestHandler<AssertDownloadIsFinishedRequest>
{
    private readonly ICloudSessionsRepository _cloudSessionsRepository;
    private readonly ITrackingActionRepository _trackingActionRepository;
    private readonly ISynchronizationProgressService _synchronizationProgressService;
    private readonly ISynchronizationStatusCheckerService _synchronizationStatusCheckerService;
    private readonly ISynchronizationService _synchronizationService;
    private readonly ITransferLocationService _transferLocationService;
    private readonly ILogger<AssertDownloadIsFinishedCommandHandler> _logger;

    public AssertDownloadIsFinishedCommandHandler(ICloudSessionsRepository cloudSessionsRepository,
        ITrackingActionRepository trackingActionRepository,
        ISynchronizationProgressService synchronizationProgressService,
        ISynchronizationStatusCheckerService synchronizationStatusCheckerService,
        ISynchronizationService synchronizationService,
        ITransferLocationService transferLocationService,
        ILogger<AssertDownloadIsFinishedCommandHandler> logger)
    {
        _cloudSessionsRepository = cloudSessionsRepository;
        _trackingActionRepository = trackingActionRepository;
        _synchronizationProgressService = synchronizationProgressService;
        _synchronizationStatusCheckerService = synchronizationStatusCheckerService;
        _synchronizationService = synchronizationService;
        _transferLocationService = transferLocationService;
        _logger = logger;
    }

    public async Task Handle(AssertDownloadIsFinishedRequest request, CancellationToken cancellationToken)
    {
        var sessionMemberData = await _cloudSessionsRepository.GetSessionMember(request.SessionId, request.Client);
        var sharedFileDefinition = request.TransferParameters.SharedFileDefinition;

        if (_transferLocationService.IsSharedFileDefinitionAllowed(sessionMemberData, sharedFileDefinition))
        {
            _logger.LogInformation("AssertDownloadIsFinished: {cloudSession} {sharedFileDefinition}",
                sessionMemberData!.CloudSessionData.SessionId,
                sharedFileDefinition.Id);

            if (sharedFileDefinition.IsSynchronization)
            {
                var actionsGroupsIds = sharedFileDefinition.ActionsGroupIds;

                var needSendSynchronizationUpdated = false;

                var result = await _trackingActionRepository.AddOrUpdate(
                    sharedFileDefinition.SessionId,
                    actionsGroupsIds!,
                    (trackingAction, synchronization) =>
                    {
                        if (!_synchronizationStatusCheckerService.CheckSynchronizationCanBeUpdated(synchronization))
                        {
                            return false;
                        }

                        var wasTrackingActionFinished = trackingAction.IsFinished;

                        var targetsForClient = trackingAction.TargetClientInstanceAndNodeIds
                            .Where(x => x.ClientInstanceId == request.Client.ClientInstanceId)
                            .ToList();
                        foreach (var target in targetsForClient)
                        {
                            trackingAction.AddSuccessOnTarget(target);
                            synchronization.Progress.FinishedAtomicActionsCount += 1;
                        }

                        if (!wasTrackingActionFinished && trackingAction.IsFinished)
                        {
                            var volumeToAdd = trackingAction.Size ?? 0;

                            // New tracking
                            synchronization.Progress.SynchronizedVolume += volumeToAdd;

                            // Keep for compatibility
                            synchronization.Progress.ProcessedVolume += volumeToAdd;
                        }

                        // Legacy: Keep ExchangedVolume logic for compatibility
                        if (sharedFileDefinition.IsMultiFileZip)
                        {
                            synchronization.Progress.ExchangedVolume += trackingAction.Size ?? 0;
                        }
                        else
                        {
                            synchronization.Progress.ExchangedVolume += sharedFileDefinition.UploadedFileLength;
                        }

                        needSendSynchronizationUpdated = _synchronizationService.CheckSynchronizationIsFinished(synchronization);

                        return true;
                    });

                if (result.IsSuccess)
                {
                    await _synchronizationProgressService.UpdateSynchronizationProgress(result, needSendSynchronizationUpdated);
                }
            }
        }

        _logger.LogDebug("Download finished asserted for session {SessionId}, file {FileId}",
            request.SessionId, sharedFileDefinition.Id);
    }
}