using System.Threading.Tasks;
using ByteSync.Business.Events;
using ByteSync.Common.Business.Sessions.Cloud;
using ByteSync.Common.Business.Sessions.Cloud.Connections;
using ByteSync.Interfaces.EventsHubs;
using ByteSync.ViewModels.Sessions.Comparisons.Results;

namespace ByteSync.Services.EventsHubs;

internal class CloudSessionEventsHub : ICloudSessionEventsHub
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
    
    public event EventHandler<GenericEventArgs<CloudSessionFatalError>>? CloudSessionOnFatalError;
    
    // public event EventHandler<GenericEventArgs<SynchronizationRuleSummaryViewModel>>? SynchronizationRuleRemoved;
    
    // public event EventHandler<GenericEventArgs<SynchronizationActionViewModel>>? SynchronizationActionRemoved;
    
    // public event EventHandler? SessionResetted;
    
    public event EventHandler<GenericEventArgs<JoinSessionResult>>? JoinCloudSessionFailed;
    
    // public event EventHandler? SynchronizationRulesApplied;

    // public void RaiseAllInventoriesReady(LocalInventoryModes localInventoryMode)
    // {
    //     Task.Run(() => AllInventoriesReady?.Invoke(this, new InventoryReadyEventArgs(localInventoryMode)));
    // }

    // public Task RaiseInventoriesComparisonDone(ComparisonResult comparisonResult)
    // {
    //     return Task.Run(() => InventoriesComparisonDone?.Invoke(this, new GenericEventArgs<ComparisonResult>(comparisonResult)));
    // }

    // public void RaiseCloudSessionQuitted()
    // {
    //     Task.Run(() => CloudSessionQuitted?.Invoke(this, EventArgs.Empty));
    // }
    
    // public void RaiseLocalInventoryGlobalStatusChanged(ByteSyncEndpoint endpoint, bool isLocalMachine, LocalInventoryGlobalStatus localInventoryStatus, 
    //     LocalInventoryGlobalStatus? previousStatus)
    // {
    //     Task.Run(() => InventoryStatusChanged?.Invoke(this, new InventoryStatusChangedEventArgs(endpoint, isLocalMachine, localInventoryStatus, previousStatus)));
    // }
    //
    // public void RaiseMemberJoinedSession(CloudSessionResult cloudSessionResult)
    // {
    //     Task.Run(() => MemberJoinedSession?.Invoke(this, new CloudSessionResultEventArgs(cloudSessionResult)));
    // }
    //
    // public void RaiseMemberQuittedSession(CloudSessionResult cloudSessionResult)
    // {
    //     Task.Run(() => MemberQuittedSession?.Invoke(this, new CloudSessionResultEventArgs(cloudSessionResult)));
    // }

    // public void RaiseCloudSessionJoined(CloudSession cloudSession)
    // {
    //     Task.Run(() => CloudSessionJoined?.Invoke(this, new CloudSessionEventArgs(cloudSession)));
    // }

    // public void RaiseSessionActivated()
    // {
    //     Task.Run(() => SessionActivated?.Invoke(this, EventArgs.Empty));
    // }

    // public void RaiseSessionSettingsUpdated(SessionSettings sessionSettings)
    // {
    //     Task.Run(() => SessionSettingsUpdated?.Invoke(this, new CloudSessionSettingsEventArgs(sessionSettings)));
    // }
    
    // public void RaiseReconnected()
    // {
    //     Task.Run(() => Reconnected?.Invoke(this, EventArgs.Empty));
    // }
    
    // public void RaiseSynchronizationStarted(SynchronizationStart synchronizationStart,
    //     ICollection<SharedAtomicAction> sharedAtomicActions, ICollection<SharedActionsGroup> sharedActionsGroups)
    // {
    //     Task.Run(() => SynchronizationStarted?.Invoke(this, 
    //         new SynchronizationStartedEventArgs(synchronizationStart, sharedAtomicActions, sharedActionsGroups)));
    // }

    // public void RaiseSynchronizationAbortRequested(SynchronizationAbortRequest synchronizationAbortRequest)
    // {
    //     Task.Run(() => SynchronizationAbortRequested?.Invoke(this, new GenericEventArgs<SynchronizationAbortRequest>(synchronizationAbortRequest)));
    // }

    // public void RaiseSynchronizationEnded(SynchronizationEnd synchronizationEnd)
    // {
    //     Task.Run(() => SynchronizationEnded?.Invoke(this, new GenericEventArgs<SynchronizationEnd>(synchronizationEnd)));
    // }

    // public void RaiseSynchronizationProgressChanged(SynchronizationProgressInfo synchronizationProgressData)
    // {
    //     Task.Run(() => SynchronizationProgressChanged?.Invoke(this, new GenericEventArgs<SynchronizationProgressInfo>(synchronizationProgressData)));
    // }

    public void RaiseSessionOnFatalError(CloudSessionFatalError cloudSessionFatalError)
    {
        Task.Run(() => CloudSessionOnFatalError?.Invoke(this, new GenericEventArgs<CloudSessionFatalError>(cloudSessionFatalError)));
    }

    // public void RaiseSynchronizationRuleRemoved(SynchronizationRuleSummaryViewModel synchronizationRuleSummaryViewModel)
    // {
    //     Task.Run(() => SynchronizationRuleRemoved?.Invoke(this, new GenericEventArgs<SynchronizationRuleSummaryViewModel>(synchronizationRuleSummaryViewModel)));
    // }

    // public void RaiseSynchronizationActionRemoved(SynchronizationActionViewModel synchronizationActionViewModel)
    // {
    //     Task.Run(() => SynchronizationActionRemoved?.Invoke(this, new GenericEventArgs<SynchronizationActionViewModel>(synchronizationActionViewModel)));
    // }

    // public void RaiseSessionResetted()
    // {
    //     Task.Run(() => SessionResetted?.Invoke(this, EventArgs.Empty));
    // }
    
    public async Task RaiseJoinCloudSessionFailed(JoinSessionResult joinSessionResult)
    {
        await Task.Run(() => JoinCloudSessionFailed?.Invoke(this, new GenericEventArgs<JoinSessionResult>(joinSessionResult)));
    }

    // public async Task RaiseSynchronizationRulesApplied()
    // {
    //     await Task.Run(() => SynchronizationRulesApplied?.Invoke(this, EventArgs.Empty));
    // }
}