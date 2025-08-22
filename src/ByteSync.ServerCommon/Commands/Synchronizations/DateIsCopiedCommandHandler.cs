using ByteSync.ServerCommon.Interfaces.Repositories;
using ByteSync.ServerCommon.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace ByteSync.ServerCommon.Commands.Synchronizations;

public class DateIsCopiedCommandHandler : ActionCompletedHandlerBase<DateIsCopiedRequest>
{
    protected override string EmptyIdsLog => "DateIsCopied: no action group IDs were provided for the operation";
    protected override string DoneLogTemplate => "Date is copied for session {SessionId} with {ActionCount} actions";

    public DateIsCopiedCommandHandler(
        ITrackingActionRepository trackingActionRepository,
        ISynchronizationStatusCheckerService synchronizationStatusCheckerService,
        ISynchronizationProgressService synchronizationProgressService,
        ISynchronizationService synchronizationService,
        ILogger<DateIsCopiedCommandHandler> logger)
        : base(trackingActionRepository, synchronizationStatusCheckerService, synchronizationProgressService, synchronizationService, logger)
    {
    }
}