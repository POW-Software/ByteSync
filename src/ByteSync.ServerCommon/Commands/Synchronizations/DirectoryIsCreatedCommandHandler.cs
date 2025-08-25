using ByteSync.ServerCommon.Interfaces.Repositories;
using ByteSync.ServerCommon.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace ByteSync.ServerCommon.Commands.Synchronizations;

public class DirectoryIsCreatedCommandHandler : ActionCompletedHandlerBase<DirectoryIsCreatedRequest>
{
    protected override string EmptyIdsLog => "DirectoryIsCreated: no action group IDs provided";
    protected override string DoneLogTemplate => "Directory is created for session {SessionId} with {ActionCount} actions";

    public DirectoryIsCreatedCommandHandler(
        ITrackingActionRepository trackingActionRepository,
        ISynchronizationStatusCheckerService synchronizationStatusCheckerService,
        ISynchronizationProgressService synchronizationProgressService,
        ISynchronizationService synchronizationService,
        ILogger<DirectoryIsCreatedCommandHandler> logger)
        : base(trackingActionRepository, synchronizationStatusCheckerService, synchronizationProgressService, synchronizationService, logger)
    {
    }
}