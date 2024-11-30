using ByteSync.Common.Business.Actions;
using ByteSync.ServerCommon.Entities;

namespace ByteSync.ServerCommon.Interfaces.Repositories;

public interface IActionsGroupDefinitionsRepository
{
    Task AddOrUpdateActionsGroupDefinitions(string sessionId, List<ActionsGroupDefinition> synchronizationActionsDefinitions);

    Task<ActionsGroupDefinitionEntity> GetActionGroupDefinition(string actionsGroupId, string sessionId);
    
    Task DeleteActionsGroupDefinitions(string sessionId);
}