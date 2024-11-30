/*using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using ByteSync.Business;
using ByteSync.Business.Actions.Shared;
using ByteSync.Business.Communications;
using ByteSync.Business.Inventories;
using ByteSync.Business.PathItems;
using ByteSync.Business.Sessions;
using ByteSync.Business.Sessions.RunSessionInfos;
using ByteSync.Business.Synchronizations;
using ByteSync.Controls.Inventories;
using ByteSync.Controls.Sessions;
using ByteSync.Models.Comparisons.Result;
using ByteSync.ViewModels.Sessions.Comparisons.Results;
using ByteSync.ViewModels.Sessions.Comparisons.Results.Misc;
using ByteSync.ViewModels.Sessions.Inventories;
using ByteSyncCommon.Business.Actions;
using ByteSyncCommon.Business.EndPoints;
using ByteSyncCommon.Business.Sessions;
using ByteSyncCommon.Business.Sessions.Cloud;
using ByteSyncCommon.Business.Sessions.Cloud.Connections;
using ByteSyncCommon.Business.Sessions.Local;
using ByteSyncCommon.Business.SharedFiles;
using ByteSyncCommon.Business.Synchronizations;
using ByteSyncCommon.Business.Synchronizations.Light;
using DynamicData.Binding;

namespace ByteSync.Interfaces.Controls.Sessions
{
    public interface ISessionDataHolder
    {
        AbstractSession? Session { get; }
        
        AbstractRunSessionProfileInfo? RunSessionProfileInfo { get;  }
        
        SessionSettings? SessionSettings { get; }

        // ComparisonResult? ComparisonResult { get; }

        // DataPartMapper? DataPartMapper { get; }

        string? SessionId { get; }
        
        string? CloudSessionPassword { get; }

        // ObservableCollectionExtended<ComparisonItemViewModel> ComparisonItems { get; }
        
        // ObservableCollectionExtended<PathItemViewModel>? PathItems { get; }
        
        ObservableCollectionExtended<PathItemViewModel>? GetPathItems(SessionMemberInfo sessionMemberInfo);

        ObservableCollectionExtended<PathItemViewModel>? GetMyPathItems();
        
        ObservableCollection<SynchronizationRuleSummaryViewModel>? SynchronizationRules { get; }
        
        ObservableCollection<SharedAtomicAction>? SharedAtomicActions { get; }
        
        List<SharedActionsGroup>? SharedActionsGroups { get; }
        
        // todo 040423 : nullable ???? => On enlève
        // InventoryProcessData InventoryProcessData { get; }
        
        // SynchronizationProcessData SynchronizationProcessData { get; }
        
        SessionFullDetails SessionFullDetails { get; }

        bool IsSessionActivated { get; }
        
        // bool HasSynchronizationStarted { get; }
        
        // bool IsSynchronizationRunning { get; }
        
        // bool IsSynchronizationEnded { get; }
        
        bool IsCloudSessionOnFatalError { get; }
        
        // bool IsInventoriesComparisonDone { get; }
        
        // bool IsSynchronizationAbortRequested { get; }
        
        bool IsProfileSession { get; }
        
        Task SetCloudSession(CloudSession cloudSession, RunCloudSessionProfileInfo? runCloudSessionProfileInfo, SessionSettings sessionSettings, List<SessionMemberInfo> sessionMemberInfos);
        
        void ClearCloudSession();
        bool CheckCloudSession(CloudSession cloudSession);
        bool CheckCloudSession(string cloudSessionId);
        
        Task SetLocalSession(LocalSession localSession, RunLocalSessionProfileInfo? runLocalSessionProfileInfo, SessionSettings sessionSettings);

        List<SessionMemberInfo> GetAllSessionMembers();

        List<SessionMemberInfo>? GetOtherSessionMembers();

        SessionMemberInfo? GetCurrentSessionMember();

        SessionMemberInfo? GetSessionMember(string clientInstanceId);
        

        


        bool AddSessionMember(CloudSessionResult cloudSessionResult);
        
        Task RemoveSessionMember(CloudSessionResult cloudSessionResult);
        
        Task AddPathItem(string clientInstanceId, PathItem pathItem);
        
        Task RemovePathItem(string clientInstanceId, PathItem pathItem);

        bool IsCurrentInstanceId(string synchronizationStartStartedBy);
        
        bool IsCloudSession { get; }
        
        bool IsLocalSession { get; }
        
        bool IsCloudSessionCreatedByMe { get; }
        
        bool IsSessionCreatedByMe { get; }
        
        bool IsLobbyCloudSessionCreatedByMe { get; }
        
        // bool AreBaseInventoriesComplete { get; }
        //
        // bool AreFullInventoriesComplete { get; }
        
        int ProgressActionsCount { get; }
        
        int ProgressActionsErrorsCount { get; }

        void GeneratePassword();
        
        Task SetSessionSettings(string sessionId, SessionSettings tupleSessionSettings);
        
        void SetCloudSessionDetails(CloudSessionDetails cloudSessionDetails);
        
        Task SetSessionActivated(string sessionId);

        Task SetSessionOnFatalError(CloudSessionFatalError cloudSessionFatalError);
        
        void SetPassword(string sessionPassword);

        // Task SetLocalInventoryGlobalStatus(string sessionId, LocalInventoryGlobalStatus localInventoryGlobalStatus);
        
        // bool HandleLocalInventoryGlobalStatusChanged(LocalInventoryGlobalStatusChangedParameters parameters);
        
        // void SetLocalInventoryStarted();

        // LocalInventoryGlobalStatus? GetLocalInventoryStatus();
        
        // void SetLocalInventoryFinished(LocalInventoryStatuses convertFinishInventory);

        // Task OnFileIsFullyDownloaded(LocalSharedFile localSharedFile);
        
        // Task SetSynchronizationStarted(SynchronizationStart synchronizationStart);
        
        // void SetSynchronizationAbortRequest(SynchronizationAbortRequest synchronizationAbortRequest);
        
        // void SetSynchronizationEnded(SynchronizationEnd synchronizationEnd);
        
        HashSet<ProgressActionInfo>? GetProgressActions(HashSet<string> actionIds);
        
        // void SetComparisonResult(ComparisonResult comparisonResult);
        
        SharedActionsGroup? GetSharedActionsGroup(ActionsGroupDefinition? actionsGroupDefinition);
        
        SharedActionsGroup? GetSharedActionsGroup(string actionsGroupId);
        
        // Task SetSynchronizationStartData(SharedSynchronizationStartData synchronizationStartData);
        
        HashSet<string> GetAtomicActionsIds(string actionsGroupId);
        
        HashSet<string> GetActionsGroupIds(ICollection<string> atomicActionsIds);
        
        List<string>? GetActionsGroupIds(SharedFileDefinition sharedFileDefinition);
        
        // void SetInventoryTransferError();
        
        // Task<bool> WaitForInventoryStartedAsync();
        
        // Task<bool> WaitForComparisonResultSetAsync();
        
        // Task<bool> WaitForSynchronizationEndedAsync();
        
        Task<bool> WaitForSynchronizationDataReadyAsync();
        
        // WaitHandle GetSessionEndedEvent();
        
        // WaitHandle GetInventoryTransferErrorEvent();

        void OnSynchronizationProgressChanged(SynchronizationProgressInfo synchronizationProgressInfo);
        
        void OnSynchronizationProgressChanged(List<SynchronizationProgressInfo> synchronizationProgressInfos);
        
        Task ResetSession();
        
        // Task ApplySynchronizationRules();
        
        Task UpdateSharedSynchronizationActions();

        void SetActionsGroupIds(SharedFileDefinition sharedFileDefinition, List<string> actionsGroupsIds);
        
        // void SetInventoryProcessData(InventoryProcessData inventoryProcessData);
        
        // Task InitializeComparisonItems();
    }
}
*/