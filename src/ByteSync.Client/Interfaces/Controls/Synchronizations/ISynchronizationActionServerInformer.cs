using System.Threading.Tasks;
using ByteSync.Business.Actions.Shared;

namespace ByteSync.Interfaces.Controls.Synchronizations;

public interface ISynchronizationActionServerInformer
{
    public delegate Task CloudActionCaller(string sessionId, List<string> actionsGroupIds, string? nodeId);
    
    Task HandleCloudActionDone(SharedActionsGroup sharedActionsGroup, CloudActionCaller cloudActionCaller);

    Task HandleCloudActionError(SharedActionsGroup sharedActionsGroup);
    
    Task HandleCloudActionError(List<string> actionsGroupIds);
    
    Task HandlePendingActions();
    
    Task ClearPendingActions();
}