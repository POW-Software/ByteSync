using ByteSync.Business.Actions.Loose;
using ByteSync.Common.Business.EndPoints;

namespace ByteSync.Business.Actions.Shared;

public class SharedSynchronizationStartData
{
    public SharedSynchronizationStartData()
    {
        
    }
    
    public SharedSynchronizationStartData(string sessionId, ByteSyncEndpoint endpoint, List<SharedAtomicAction> sharedAtomicActions,
        List<SharedActionsGroup> sharedActionsGroups, List<LooseSynchronizationRule> looseSynchronizationRules)
    {
        SessionId = sessionId;
        Endpoint = endpoint;
        SharedAtomicActions = sharedAtomicActions;
        SharedActionsGroups = sharedActionsGroups;
        LooseSynchronizationRules = looseSynchronizationRules;
    }

    public string SessionId { get; set; } = null!;
    
    public ByteSyncEndpoint Endpoint { get; set; } = null!;

    public List<SharedAtomicAction> SharedAtomicActions { get; set; } = null!;

    public List<SharedActionsGroup> SharedActionsGroups { get; set; } = null!;
    
    public List<LooseSynchronizationRule> LooseSynchronizationRules { get; set; } = null!;
    
    public long TotalVolumeToProcess { get; set; }
    
    public long TotalActionsToProcess { get; set; }
}