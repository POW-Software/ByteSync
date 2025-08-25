using ByteSync.ServerCommon.Interfaces.Repositories;
using ByteSync.ServerCommon.Interfaces.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ByteSync.ServerCommon.Commands.Synchronizations;

public class RequestSynchronizationAbortCommandHandler : IRequestHandler<RequestSynchronizationAbortRequest>
{
    private readonly ISynchronizationRepository _synchronizationRepository;
    private readonly ISynchronizationStatusCheckerService _synchronizationStatusCheckerService;
    private readonly ISynchronizationProgressService _synchronizationProgressService;
    private readonly ILogger<RequestSynchronizationAbortCommandHandler> _logger;

    public RequestSynchronizationAbortCommandHandler(
        ISynchronizationRepository synchronizationRepository,
        ISynchronizationStatusCheckerService synchronizationStatusCheckerService,
        ISynchronizationProgressService synchronizationProgressService,
        ILogger<RequestSynchronizationAbortCommandHandler> logger)
    {
        _synchronizationRepository = synchronizationRepository;
        _synchronizationStatusCheckerService = synchronizationStatusCheckerService;
        _synchronizationProgressService = synchronizationProgressService;
        _logger = logger;
    }
    
    public async Task Handle(RequestSynchronizationAbortRequest request, CancellationToken cancellationToken)
    {
        var result = await _synchronizationRepository.UpdateIfExists(request.SessionId, synchronizationEntity =>
        {
            if (!_synchronizationStatusCheckerService.CheckSynchronizationCanBeAborted(synchronizationEntity))
            {
                return false;
            }

            var isDateUpdated = false;
            var isRequesterAdded = false;

            if (synchronizationEntity.AbortRequestedOn == null)
            {
                synchronizationEntity.AbortRequestedOn = DateTimeOffset.UtcNow;
                isDateUpdated = true;
            }
            
            if (!synchronizationEntity.AbortRequestedBy.Contains(request.Client.ClientInstanceId))
            {
                synchronizationEntity.AbortRequestedBy.Add(request.Client.ClientInstanceId);
                isRequesterAdded = true;
            }
            
            return isDateUpdated || isRequesterAdded;
        });

        if (result.IsSaved)
        {
            await _synchronizationProgressService.UpdateSynchronizationProgress(result.Element!, true);
        }
        
        _logger.LogInformation("Synchronization abort requested for session {SessionId}", request.SessionId);
    }
}