using ByteSync.ServerCommon.Interfaces.Repositories;
using ByteSync.ServerCommon.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace ByteSync.ServerCommon.Commands.Synchronizations;

public class SynchronizationErrorsCommandHandler : ErrorOccurredHandlerBase<SynchronizationErrorsRequest>
{
    protected override string EmptyIdsLog => "SynchronizationError: no action group IDs were provided";
    protected override string DoneLogTemplate => "Synchronization errors reported for session {SessionId} with {ActionCount} actions";

    public SynchronizationErrorsCommandHandler(
        ITrackingActionRepository trackingActionRepository,
        ISynchronizationStatusCheckerService synchronizationStatusCheckerService,
        ISynchronizationProgressService synchronizationProgressService,
        ISynchronizationService synchronizationService,
        ILogger<SynchronizationErrorsCommandHandler> logger)
        : base(trackingActionRepository, synchronizationStatusCheckerService, synchronizationProgressService, synchronizationService, logger)
    {
    }

    protected override List<string>? GetActionsGroupIds(SynchronizationErrorsRequest request)
    {
        return request.ActionsGroupIds;
    }
}