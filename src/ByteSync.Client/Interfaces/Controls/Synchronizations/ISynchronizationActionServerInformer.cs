using System.Threading.Tasks;
using ByteSync.Business.Actions.Shared;
using ByteSync.Common.Business.Synchronizations;

namespace ByteSync.Interfaces.Controls.Synchronizations;

public interface ISynchronizationActionServerInformer
{
    public delegate Task CloudActionCaller(string sessionId, SynchronizationActionRequest synchronizationActionRequests);
    
    Task HandleCloudActionDone(SharedActionsGroup sharedActionsGroup, SharedDataPart localTarget, CloudActionCaller cloudActionCaller);

    Task HandleCloudActionDone(SharedActionsGroup sharedActionsGroup, SharedDataPart localTarget, CloudActionCaller cloudActionCaller,
        Dictionary<string, SynchronizationActionMetrics>? actionMetricsByActionId);

    Task HandleCloudActionError(SharedActionsGroup sharedActionsGroup, SharedDataPart localTarget);

    Task HandleCloudActionError(SharedActionsGroup sharedActionsGroup);
    
    Task HandleCloudActionError(List<string> actionsGroupIds);
    
    Task HandlePendingActions();
    
    Task ClearPendingActions();
}
