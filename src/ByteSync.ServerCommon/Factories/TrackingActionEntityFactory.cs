using ByteSync.Common.Business.Actions;
using ByteSync.ServerCommon.Entities;
using ByteSync.ServerCommon.Interfaces.Factories;
using ByteSync.ServerCommon.Interfaces.Repositories;

namespace ByteSync.ServerCommon.Factories;

public class TrackingActionEntityFactory : ITrackingActionEntityFactory
{
    private readonly IActionsGroupDefinitionsRepository _actionsGroupDefinitionsRepository;
    
    public TrackingActionEntityFactory()
    {

    }
    
    public async Task<TrackingActionEntity> Create(string sessionId, string actionsGroupId)
    {
        return new TrackingActionEntity();
        
        // var actionGroupDefinition = await _actionsGroupDefinitionsRepository.GetActionGroupDefinition(actionsGroupId, sessionId);
        //     
        // string? source = actionGroupDefinition.Source;
        // if (actionGroupDefinition.Operator == ActionOperatorTypes.SynchronizeDate
        //     || (actionGroupDefinition.Operator == ActionOperatorTypes.SynchronizeContentAndDate && actionGroupDefinition.AppliesOnlySynchronizeDate))
        // {
        //     source = null;
        // }
        //     
        // var trackingActionEntity = new TrackingActionEntity
        // {
        //     ActionsGroupId = actionsGroupId,
        //     SourceClientInstanceId = source,
        //     TargetClientInstanceIds = [..actionGroupDefinition.Targets],
        //     Size = actionGroupDefinition.Size,
        // };
        //
        // return trackingActionEntity;
    }
}