using ByteSync.Common.Business.Actions;
using ByteSync.ServerCommon.Entities;

namespace ByteSync.ServerCommon.Interfaces.Repositories;

public interface ISynchronizationRepository : IRepository<SynchronizationEntity>
{
    Task AddSynchronization(SynchronizationEntity synchronizationEntity, List<ActionsGroupDefinition> actionsGroupDefinitions);
    
    Task ResetSession(string sessionId);
}