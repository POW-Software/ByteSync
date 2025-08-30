using System.Threading.Tasks;
using ByteSync.Business.Actions.Shared;
using ByteSync.Common.Business.Synchronizations;

namespace ByteSync.Interfaces.Controls.Synchronizations;

public interface ISynchronizationActionServerInformer
{
    public delegate Task CloudActionCaller(string sessionId, SynchronizationActionRequest synchronizationActionRequests);
    
    Task HandleCloudActionDone(SharedActionsGroup sharedActionsGroup, CloudActionCaller cloudActionCaller);

    Task HandleCloudActionError(SharedActionsGroup sharedActionsGroup);
    
    Task HandleCloudActionError(List<string> actionsGroupIds);
    
    Task HandlePendingActions();
    
    Task ClearPendingActions();
}