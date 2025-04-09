using ByteSync.Common.Business.Actions;
using ByteSync.ServerCommon.Entities;
using ByteSync.ServerCommon.Interfaces.Repositories;
using ByteSync.ServerCommon.Interfaces.Services;

namespace ByteSync.ServerCommon.Repositories;

public class SynchronizationRepository : BaseRepository<SynchronizationEntity>, ISynchronizationRepository
{
    private readonly IActionsGroupDefinitionsRepository _actionsGroupDefinitionsRepository;

    public SynchronizationRepository(ICacheService cacheService, IActionsGroupDefinitionsRepository actionsGroupDefinitionsRepository) : base(cacheService)
    {
        _actionsGroupDefinitionsRepository = actionsGroupDefinitionsRepository;
    }

    public override EntityType EntityType => EntityType.Synchronization;

    public async Task AddSynchronization(SynchronizationEntity synchronizationEntity, List<ActionsGroupDefinition> actionsGroupDefinitions)
    {
        await Save(synchronizationEntity.SessionId, synchronizationEntity);
        
        await _actionsGroupDefinitionsRepository.AddOrUpdateActionsGroupDefinitions(synchronizationEntity.SessionId, actionsGroupDefinitions);
    }

    public async Task ResetSession(string sessionId)
    {
        await Delete(sessionId);
        
        await _actionsGroupDefinitionsRepository.DeleteActionsGroupDefinitions(sessionId);
    }
}