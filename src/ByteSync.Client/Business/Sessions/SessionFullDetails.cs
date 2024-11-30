/*
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Subjects;
using System.Threading;
using ByteSync.Business.Actions;
using ByteSync.Business.Actions.Shared;
using ByteSync.Business.Communications;
using ByteSync.Business.Inventories;
using ByteSync.Business.Lobbies;
using ByteSync.Business.PathItems;
using ByteSync.Business.Sessions.RunSessionInfos;
using ByteSync.Business.Synchronizations;
using ByteSync.Controls.Inventories;
using ByteSync.Controls.Sessions;
using ByteSync.Interfaces.Controls.Encryptions;
using ByteSync.Models.Comparisons.Result;
using ByteSync.ViewModels.Sessions.Cloud.Members;
using ByteSync.ViewModels.Sessions.Comparisons.Results;
using ByteSync.ViewModels.Sessions.Comparisons.Results.Misc;
using ByteSync.ViewModels.Sessions.Inventories;
using ByteSyncCommon.Business.Actions;
using ByteSyncCommon.Business.Sessions;
using ByteSyncCommon.Business.Sessions.Cloud;
using ByteSyncCommon.Business.SharedFiles;
using ByteSyncCommon.Business.Synchronizations;
using ByteSyncCommon.Business.Synchronizations.Light;
using DynamicData.Binding;
using PowSoftware.Common.Helpers;
using Splat;

namespace ByteSync.Business.Sessions;

public class SessionFullDetails
{
    public SessionFullDetails()
    {
        // OtherMembersPathItems = new Dictionary<SessionMemberInfo, ObservableCollectionExtended<PathItemViewModel>>();


        // SynchronizationProcessData = new SynchronizationProcessData();
        
        /* todo 040423 : Gestion dans InventoriesService ???
        OtherMembersInventories = new HashSet<LocalSharedFile>();
        
        OtherMembersInventories.Clear();
        LocalBaseInventories = null;
        LocalFullInventories = null;
        LocalInventoryGlobalStatus = LocalInventoryGlobalStatus.WaitingForStart;
        */

/*
        // ComparisonItems = new ObservableCollectionExtended<ComparisonItemViewModel>();
        SharedAtomicActions = new ObservableCollection<SharedAtomicAction>();
        SharedActionsGroups = new List<SharedActionsGroup>();
        ProgressActions = new HashSet<ProgressActionInfo>();
        
        PathItems = new Dictionary<SessionMemberInfo, ObservableCollectionExtended<PathItemViewModel>>();
        SynchronizationRules = new ObservableCollection<SynchronizationRuleSummaryViewModel>();

        AtomicActionsIdsPerActionGroupsId = new Dictionary<string, HashSet<string>>();

        ActionsGroupIdPerAtomicActionId = new Dictionary<string, HashSet<string>>();

        ActionsGroupIdsPerSharedFileDefinition = new Dictionary<SharedFileDefinition, List<string>>();
        
        // InventoryStarted = new ManualResetEvent(false);
        // InventoryTransferError = new ManualResetEvent(false);
        // ComparisonResultSet = new ManualResetEvent(false);
        SynchronizationDataReady = new ManualResetEvent(false);
        SynchronizationStarted = new ManualResetEvent(false);
        SynchronizationEnded = new ManualResetEvent(false);
        
        SessionEnded = new ReplaySubject<bool>(1);
        SessionEnded.OnNext(false);

        HasSessionBeenRestarted = false;
    }

    // public ManualResetEvent InventoryStarted { get; }

    // todo 040423
    // public ManualResetEvent InventoryTransferError { get; }
    
    public SessionModes SessionMode { get; set; }
    
    // public ManualResetEvent ComparisonResultSet { get; }
    
    public ManualResetEvent SynchronizationDataReady { get; }
    
    public ManualResetEvent SynchronizationStarted { get; }
    
    public ManualResetEvent SynchronizationEnded { get; }
    
    public ISubject<bool> SessionEnded { get; }

    public AbstractSession? Session { get; set; }
    
    public AbstractRunSessionProfileInfo? RunSessionProfileInfo { get; set; }
    
    public SessionSettings? SessionSettings { get; set; }
    
    public List<SessionMemberInfo>? SessionMembers { get; set; }
    
    public bool IsSessionActivated { get; set; }

    public string? SessionPassword { get; set; }
    
    public Dictionary<SessionMemberInfo, ObservableCollectionExtended<PathItemViewModel>> PathItems { get; }
    
    public ObservableCollection<SynchronizationRuleSummaryViewModel> SynchronizationRules { get; }
    
    // internal ComparisonResult? ComparisonResult { get; set; }

    // internal ObservableCollectionExtended<ComparisonItemViewModel> ComparisonItems { get; }
    
    public ObservableCollection<SharedAtomicAction> SharedAtomicActions { get; }
    
    public List<SharedActionsGroup> SharedActionsGroups { get; }


    
    // public bool AreBaseInventoriesComplete { get; set; }
    //
    // public bool AreFullInventoriesComplete { get; set; }

    // public SynchronizationStart? SynchronizationStart { get; set; }
    //
    // public SynchronizationAbortRequest? SynchronizationAbortRequest { get; set; }
    //
    // public SynchronizationEnd? SynchronizationEnd { get; set; }
    
    public CloudSessionFatalError? CloudSessionFatalError { get; set; }
    
    public HashSet<ProgressActionInfo> ProgressActions { get; set; }
    
    // public bool IsInventoriesComparisonDone { get; set; }
    

    
    // public SynchronizationProcessData SynchronizationProcessData { get; }
    
    public Dictionary<string, HashSet<string>> AtomicActionsIdsPerActionGroupsId { get; set; }
    
    public Dictionary<string, HashSet<string>> ActionsGroupIdPerAtomicActionId { get; set; }
    
    public Dictionary<SharedFileDefinition, List<string>> ActionsGroupIdsPerSharedFileDefinition { get; set; }

    // public bool HasSynchronizationStarted
    // {
    //     get
    //     {
    //         return SynchronizationStart != null;
    //     }
    // }
    //
    // public bool IsSynchronizationAbortRequested
    // {
    //     get
    //     {
    //         return SynchronizationAbortRequest != null;
    //     }
    // }
    //
    // public bool IsSynchronizationEnded
    // {
    //     get
    //     {
    //         return SynchronizationEnd != null;
    //     }
    // }

    public bool IsCloudSessionFatalError
    {
        get
        {
            return CloudSessionFatalError != null;
        }
    }

    // public DataPartMapper DataPartMapper { get; set; }
    
    public bool HasSessionBeenRestarted { get; set; }

    public void Fill(CloudSessionDetails cloudSessionDetails)
    {
        Session = cloudSessionDetails.CloudSession;
        SessionMembers = cloudSessionDetails.Members;
        
        var dataEncrypter = Locator.Current.GetService<IDataEncrypter>()!;
        var sessionSettings = dataEncrypter.DecryptSessionSettings(cloudSessionDetails.SessionSettings);
        
        SessionSettings = sessionSettings;
        IsSessionActivated = cloudSessionDetails.IsActivated;
    }

    public HashSet<string> GetAtomicActionsIds(string actionsGroupId)
    {
        HashSet<string>? result;
        if (AtomicActionsIdsPerActionGroupsId.TryGetValue(actionsGroupId, out result))
        {
            return result;
        }
        else
        {
            return new HashSet<string>();
        }
    }

    public HashSet<string> GetActionsGroupIds(ICollection<string> atomicActionsIds)
    {
        HashSet<string> result = new HashSet<string>();

        foreach (var atomicActionId in atomicActionsIds)
        {
            if (ActionsGroupIdPerAtomicActionId.TryGetValue(atomicActionId, out HashSet<string>? actionsGroupId))
            {
                result.AddAll(actionsGroupId);
            }
        }

        return result;
    }
    
    public List<string>? GetActionsGroupIds(SharedFileDefinition sharedFileDefinition)
    {
        if (ActionsGroupIdsPerSharedFileDefinition.TryGetValue(sharedFileDefinition, out List<string>? actionsGroupId))
        {
            return actionsGroupId;
        }
        else
        {
            return null;
        }
    }
    
    
    public void SetActionsGroupIds(SharedFileDefinition sharedFileDefinition, List<string> actionsGroupsIds)
    {
        if (!ActionsGroupIdsPerSharedFileDefinition.ContainsKey(sharedFileDefinition))
        {
            ActionsGroupIdsPerSharedFileDefinition.Add(sharedFileDefinition, actionsGroupsIds);
        }
    }
    
    // public void SetInventoryProcessData(InventoryProcessData inventoryProcessData)
    // {
    //     InventoryProcessData = inventoryProcessData;
    //
    //     InventoryStarted.Set();
    // }
    
    public void RegisterActionsGroupsAndAtomicActionsLinks()
    {
        foreach (var sharedAtomicAction in SharedAtomicActions)
        {
            string atomicActionId = sharedAtomicAction.AtomicActionId;
            string actionsGroupId = sharedAtomicAction.ActionsGroupId!;

            if (!ActionsGroupIdPerAtomicActionId.ContainsKey(atomicActionId))
            {
                ActionsGroupIdPerAtomicActionId.Add(atomicActionId, new HashSet<string>());
            }
            ActionsGroupIdPerAtomicActionId[atomicActionId].Add(actionsGroupId);

            if (!AtomicActionsIdsPerActionGroupsId.ContainsKey(actionsGroupId))
            {
                AtomicActionsIdsPerActionGroupsId.Add(actionsGroupId, new HashSet<string>());
            }

            AtomicActionsIdsPerActionGroupsId[actionsGroupId].Add(atomicActionId);
        }
    }

    public void OnSessionEnd()
    {
        // todo 040423 : gérer le dispose
        // foreach (var comparisonItemViewModel in ComparisonItems)
        // {
        //     comparisonItemViewModel.Dispose();
        // }

        SessionEnded.OnNext(true);
    }

    public void ResetSession()
    {
        // todo 040423 : gérer le dispose
        // foreach (var comparisonItemViewModel in ComparisonItems)
        // {
        //     comparisonItemViewModel.Dispose();
        // }
        
        // ComparisonItems.Clear();
        
        /* todo 040423 : Reset à traiter dans InventoriesService ???
        OtherMembersInventories.Clear();
        LocalBaseInventories = null;
        LocalFullInventories = null;
        LocalInventoryGlobalStatus = LocalInventoryGlobalStatus.WaitingForStart;
        */

        /*
        SharedAtomicActions.Clear();
        SharedActionsGroups.Clear();
        ProgressActions.Clear();
        
        AtomicActionsIdsPerActionGroupsId.Clear();
        ActionsGroupIdPerAtomicActionId.Clear(); 
        ActionsGroupIdsPerSharedFileDefinition.Clear();
        
        // InventoryStarted.Reset();
        // InventoryTransferError.Reset();
        // ComparisonResultSet.Reset();
        SynchronizationDataReady.Reset(); 
        SynchronizationStarted.Reset();
        SynchronizationEnded.Reset();
        SessionEnded.OnNext(false);

        // AreBaseInventoriesComplete = false;
        // AreFullInventoriesComplete = false;

        // SynchronizationStart = null;
        // SynchronizationAbortRequest = null;
        // SynchronizationEnd = null;
        CloudSessionFatalError = null;

        // IsInventoriesComparisonDone = false;
        IsSessionActivated = false;

        // todo 040423 : gestion du reset dans InventoriesService
        // InventoryProcessData.Reset();
        
        HasSessionBeenRestarted = true;
    }
}
*/