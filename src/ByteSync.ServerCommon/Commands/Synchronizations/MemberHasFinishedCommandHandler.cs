using ByteSync.ServerCommon.Interfaces.Repositories;
using ByteSync.ServerCommon.Interfaces.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ByteSync.ServerCommon.Commands.Synchronizations;

public class MemberHasFinishedCommandHandler : IRequestHandler<MemberHasFinishedRequest>
{
    private readonly ISynchronizationRepository _synchronizationRepository;
    private readonly ISynchronizationProgressService _synchronizationProgressService;
    private readonly ISynchronizationService _synchronizationService;
    private readonly ILogger<MemberHasFinishedCommandHandler> _logger;

    public MemberHasFinishedCommandHandler(
        ISynchronizationRepository synchronizationRepository,
        ISynchronizationProgressService synchronizationProgressService,
        ISynchronizationService synchronizationService,
        ILogger<MemberHasFinishedCommandHandler> logger)
    {
        _synchronizationRepository = synchronizationRepository;
        _synchronizationProgressService = synchronizationProgressService;
        _synchronizationService = synchronizationService;
        _logger = logger;
    }
    
    public async Task Handle(MemberHasFinishedRequest request, CancellationToken cancellationToken)
    {
        var needSendSynchronizationUpdated = false;
        
        var result = await _synchronizationRepository.UpdateIfExists(request.SessionId, synchronizationEntity =>
        {
            if (synchronizationEntity.Progress.Members.Contains(request.Client.ClientInstanceId))
            {
                synchronizationEntity.Progress.CompletedMembers.Add(request.Client.ClientInstanceId);

                needSendSynchronizationUpdated = _synchronizationService.CheckSynchronizationIsFinished(synchronizationEntity);
                
                _logger.LogInformation("Member {ClientInstanceId} has finished synchronization", request.Client.ClientInstanceId);
                
                return true;
            }

            return false;
        });

        if (result.IsSaved)
        {
            await _synchronizationProgressService.UpdateSynchronizationProgress(result.Element!, needSendSynchronizationUpdated);
        }
        
        _logger.LogInformation("Member has finished for session {SessionId}", request.SessionId);
    }
}