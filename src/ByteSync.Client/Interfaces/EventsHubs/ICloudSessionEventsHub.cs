using System.Threading.Tasks;
using ByteSync.Business.Events;
using ByteSync.Common.Business.Sessions.Cloud;
using ByteSync.Common.Business.Sessions.Cloud.Connections;
using ByteSync.ViewModels.Sessions.Comparisons.Results;

namespace ByteSync.Interfaces.EventsHubs;

public interface ICloudSessionEventsHub
{
    // public event EventHandler<InventoryReadyEventArgs>? AllInventoriesReady;
    
    // public event EventHandler<GenericEventArgs<ComparisonResult>>? InventoriesComparisonDone;
    
    // public event EventHandler<EventArgs>? CloudSessionQuitted;

    // public event EventHandler<InventoryStatusChangedEventArgs>? InventoryStatusChanged;
    
    // public event EventHandler<CloudSessionResultEventArgs>? MemberJoinedSession;
        
    // public event EventHandler<CloudSessionResultEventArgs>? MemberQuittedSession;

    // public event EventHandler? SessionActivated;
    
    // public event EventHandler<CloudSessionSettingsEventArgs>? SessionSettingsUpdated;
    
    // public event EventHandler? Reconnected;
    
    // public event EventHandler<SynchronizationStartedEventArgs>? SynchronizationStarted;
    
    // public event EventHandler<GenericEventArgs<SynchronizationAbortRequest>>? SynchronizationAbortRequested;
    
    // public event EventHandler<GenericEventArgs<SynchronizationEnd>>? SynchronizationEnded;
    
    // public event EventHandler<GenericEventArgs<SynchronizationProgressInfo>>? SynchronizationProgressChanged;
    
    // public event EventHandler<GenericEventArgs<CloudSessionFatalError>>? CloudSessionOnFatalError;
    
    // public event EventHandler<GenericEventArgs<SynchronizationRuleSummaryViewModel>>? SynchronizationRuleRemoved;
    
    // public event EventHandler<GenericEventArgs<SynchronizationActionViewModel>>? SynchronizationActionRemoved;

    // public event EventHandler? SessionResetted;
    
    // public event EventHandler<GenericEventArgs<JoinSessionResult>>? JoinCloudSessionFailed;
    
    // public event EventHandler? SynchronizationRulesApplied;
    
    // void RaiseAllInventoriesReady(LocalInventoryModes localInventoryModes);
    
    // Task RaiseInventoriesComparisonDone(ComparisonResult comparisonResult);
    
    // void RaiseCloudSessionQuitted();

    // void RaiseLocalInventoryGlobalStatusChanged(ByteSyncEndpoint endpoint, bool isLocalMachine, LocalInventoryGlobalStatus localInventoryStatus, 
    //     LocalInventoryGlobalStatus? previousStatus);
    //
    // void RaiseMemberJoinedSession(CloudSessionResult cloudSessionResult);
    //
    // void RaiseMemberQuittedSession(CloudSessionResult cloudSessionResult);

    // void RaiseCloudSessionJoined(CloudSession cloudSession);
    
    // void RaiseSessionActivated();
    
    // void RaiseSessionSettingsUpdated(SessionSettings sessionSettings);
    
    // void RaiseReconnected();

    // void RaiseSynchronizationStarted(SynchronizationStart synchronizationStart, 
    //     ICollection<SharedAtomicAction> sharedAtomicActions, ICollection<SharedActionsGroup> sharedActionsGroups);

    // void RaiseSynchronizationAbortRequested(SynchronizationAbortRequest synchronizationAbortRequest);
    
    // void RaiseSynchronizationEnded(SynchronizationEnd synchronizationEnd);
    
    // void RaiseSynchronizationProgressChanged(SynchronizationProgressInfo synchronizationProgressData);
    
    // void RaiseSessionOnFatalError(CloudSessionFatalError cloudSessionFatalError);
    
    // void RaiseSynchronizationRuleRemoved(SynchronizationRuleSummaryViewModel synchronizationRuleSummaryViewModel);
    
    // void RaiseSynchronizationActionRemoved(SynchronizationActionViewModel synchronizationActionViewModel);
    
    // void RaiseSessionResetted();
    
    // Task RaiseJoinCloudSessionFailed(JoinSessionResult joinSessionResult);
    
    // Task RaiseSynchronizationRulesApplied();
}