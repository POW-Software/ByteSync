using ByteSync.Common.Business.Actions;
using ByteSync.ServerCommon.Entities;
using ByteSync.ServerCommon.Interfaces.Repositories;
using ByteSync.ServerCommon.Misc;
using Microsoft.EntityFrameworkCore;

namespace ByteSync.ServerCommon.Repositories;

public class ActionsGroupDefinitionsRepository : IActionsGroupDefinitionsRepository
{
    private readonly ByteSyncDbContext _dbContext;

    public ActionsGroupDefinitionsRepository(ByteSyncDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddOrUpdateActionsGroupDefinitions(string sessionId, List<ActionsGroupDefinition> synchronizationActionsDefinitions)
    {
        List<ActionsGroupDefinitionEntity> actionsGroupDefinitionEntities = synchronizationActionsDefinitions
            .Select(definition => new ActionsGroupDefinitionEntity
            {
                ActionsGroupDefinitionEntityId = definition.ActionsGroupId,
                Operator = definition.Operator,
                Size = definition.Size,
                CreationTimeUtc = definition.CreationTimeUtc,
                AppliesOnlySynchronizeDate = definition.AppliesOnlySynchronizeDate,
                LastWriteTimeUtc = definition.LastWriteTimeUtc,
                SessionId = sessionId,
                Source = definition.Source,
                Targets = definition.Targets,
                FileSystemType = definition.FileSystemType,
            }).ToList(); 
        
        await _dbContext.ActionsGroupDefinitions.AddRangeAsync(actionsGroupDefinitionEntities);
        await _dbContext.SaveChangesAsync();
    }

    public async Task<ActionsGroupDefinitionEntity> GetActionGroupDefinition(string actionsGroupId, string sessionId)
    {
        return await _dbContext.ActionsGroupDefinitions
            .FirstAsync(e => e.ActionsGroupDefinitionEntityId == actionsGroupId && 
                             e.SessionId == sessionId);
    }
    
    public async Task DeleteActionsGroupDefinitions(string sessionId)
    {
        var entitiesToDelete = await _dbContext.ActionsGroupDefinitions
            .Where(e => e.SessionId == sessionId)
            .ToListAsync();
        
        _dbContext.ActionsGroupDefinitions.RemoveRange(entitiesToDelete);
        
        await _dbContext.SaveChangesAsync();
    }
}