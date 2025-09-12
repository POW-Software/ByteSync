using ByteSync.ServerCommon.Entities;
using ByteSync.ServerCommon.Interfaces.Repositories;
using ByteSync.ServerCommon.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace ByteSync.ServerCommon.Commands.Synchronizations;

public class LocalCopyIsDoneCommandHandler : ActionCompletedHandlerBase<LocalCopyIsDoneRequest>
{
    protected override string EmptyIdsLog => "LocalCopyIsDone: no action group IDs were provided";

    protected override string DoneLogTemplate => "Local copy is done for session {SessionId} with {ActionCount} actions";

    public LocalCopyIsDoneCommandHandler(
        ITrackingActionRepository trackingActionRepository,
        ISynchronizationStatusCheckerService synchronizationStatusCheckerService,
        ISynchronizationProgressService synchronizationProgressService,
        ISynchronizationService synchronizationService,
        ILogger<LocalCopyIsDoneCommandHandler> logger)
        : base(trackingActionRepository, synchronizationStatusCheckerService, synchronizationProgressService, synchronizationService,
            logger)
    {
    }

    protected override void ProcessSourceAction(TrackingActionEntity trackingAction, LocalCopyIsDoneRequest request)
    {
        trackingAction.IsSourceSuccess = true;
    }

    protected override void UpdateProgress(SynchronizationEntity synchronization, TrackingActionEntity trackingAction,
        LocalCopyIsDoneRequest request)
    {
        var volumeToAdd = trackingAction.Size ?? 0;

        synchronization.Progress.LocalCopyTransferredVolume += volumeToAdd;
        synchronization.Progress.SynchronizedVolume += volumeToAdd;
    }
}