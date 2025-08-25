using ByteSync.ServerCommon.Interfaces.Repositories;
using ByteSync.ServerCommon.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace ByteSync.ServerCommon.Commands.Synchronizations;

public class FileOrDirectoryIsDeletedCommandHandler : ActionCompletedHandlerBase<FileOrDirectoryIsDeletedRequest>
{
    protected override string EmptyIdsLog => "FileOrDirectoryIsDeleted: no action group IDs were provided for deletion operation";
    protected override string DoneLogTemplate => "File or directory is deleted for session {SessionId} with {ActionCount} actions";

    public FileOrDirectoryIsDeletedCommandHandler(
        ITrackingActionRepository trackingActionRepository,
        ISynchronizationStatusCheckerService synchronizationStatusCheckerService,
        ISynchronizationProgressService synchronizationProgressService,
        ISynchronizationService synchronizationService,
        ILogger<FileOrDirectoryIsDeletedCommandHandler> logger)
        : base(trackingActionRepository, synchronizationStatusCheckerService, synchronizationProgressService, synchronizationService, logger)
    {
    }
}