using ByteSync.ServerCommon.Interfaces.Repositories;
using ByteSync.ServerCommon.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace ByteSync.ServerCommon.Commands.Synchronizations;

public class SynchronizationErrorCommandHandler : ErrorOccurredHandlerBase<SynchronizationErrorRequest>
{
    protected override string EmptyIdsLog => "SynchronizationError: no action group IDs were provided";
    protected override string DoneLogTemplate => "Synchronization error reported for session {SessionId} with {ActionCount} actions";

    public SynchronizationErrorCommandHandler(
        ITrackingActionRepository trackingActionRepository,
        ISynchronizationStatusCheckerService synchronizationStatusCheckerService,
        ISynchronizationProgressService synchronizationProgressService,
        ISynchronizationService synchronizationService,
        ILogger<SynchronizationErrorCommandHandler> logger)
        : base(trackingActionRepository, synchronizationStatusCheckerService, synchronizationProgressService, synchronizationService, logger)
    {
    }

    protected override List<string>? GetActionsGroupIds(SynchronizationErrorRequest request)
    {
        return request.ActionsGroupIds;
    }
}