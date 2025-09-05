using ByteSync.Common.Business.Actions;
using ByteSync.ServerCommon.Entities;
using ByteSync.ServerCommon.Interfaces.Repositories;
using ByteSync.ServerCommon.Interfaces.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ByteSync.ServerCommon.Commands.Synchronizations;

public class StartSynchronizationCommandHandler : IRequestHandler<StartSynchronizationRequest>
{
    private readonly ICloudSessionsRepository _cloudSessionsRepository;
    private readonly ISynchronizationRepository _synchronizationRepository;
    private readonly ISynchronizationProgressService _synchronizationProgressService;
    private readonly ILogger<StartSynchronizationCommandHandler> _logger;

    public StartSynchronizationCommandHandler(
        ICloudSessionsRepository cloudSessionsRepository,
        ISynchronizationRepository synchronizationRepository,
        ISynchronizationProgressService synchronizationProgressService,
        ILogger<StartSynchronizationCommandHandler> logger)
    {
        _cloudSessionsRepository = cloudSessionsRepository;
        _synchronizationRepository = synchronizationRepository;
        _synchronizationProgressService = synchronizationProgressService;
        _logger = logger;
    }
    
    public async Task Handle(StartSynchronizationRequest request, CancellationToken cancellationToken)
    {
        var synchronizationEntity = await _synchronizationRepository.Get(request.SessionId);
        
        if (synchronizationEntity == null)
        {
            var session = await _cloudSessionsRepository.Get(request.SessionId);
            
            synchronizationEntity = new SynchronizationEntity
            {
                SessionId = request.SessionId,
                Progress = new SynchronizationProgressEntity
                {
                    TotalAtomicActionsCount = GetTotalAtomicActionsCount(request.ActionsGroupDefinitions),
                    Members = session!.SessionMembers.Select(m => m.ClientInstanceId).ToList(),
                },
                StartedOn = DateTimeOffset.UtcNow,
                StartedBy = request.Client.ClientInstanceId
            };
            
            await _synchronizationRepository.AddSynchronization(synchronizationEntity, request.ActionsGroupDefinitions);
            
            await _synchronizationProgressService.InformSynchronizationStarted(synchronizationEntity, request.Client);
        }
        
        _logger.LogInformation("Synchronization started for session {SessionId}", request.SessionId);
    }

    private long GetTotalAtomicActionsCount(List<ActionsGroupDefinition> actionsGroupDefinitions)
    {
        long total = 0;

        foreach (var actionsGroupDefinition in actionsGroupDefinitions)
        {
            if (!actionsGroupDefinition.IsDoNothing)
            {
                total += actionsGroupDefinition.TargetClientInstanceAndNodeIds.Distinct().Count();
            }
        }

        return total;
    }
}