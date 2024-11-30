/*
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ByteSync.Business;
using ByteSync.Business.Actions.Shared;
using ByteSync.Business.Communications;
using ByteSync.Business.Events;
using ByteSync.Business.Inventories;
using ByteSync.Business.PathItems;
using ByteSync.Business.Sessions;
using ByteSync.Business.Sessions.RunSessionInfos;
using ByteSync.Business.Synchronizations;
using ByteSync.Controls.Comparisons;
using ByteSync.Controls.Comparisons.DescriptionBuilders;
using ByteSync.Controls.Inventories;
using ByteSync.Controls.Profiles;
using ByteSync.Interfaces;
using ByteSync.Interfaces.Communications;
using ByteSync.Interfaces.Controls.Actions;
using ByteSync.Interfaces.Controls.Sessions;
using ByteSync.Interfaces.EventsHubs;
using ByteSync.Models.Comparisons.Result;
using ByteSync.ViewModels.Sessions.Comparisons.Results;
using ByteSync.ViewModels.Sessions.Comparisons.Results.Misc;
using ByteSync.ViewModels.Sessions.Inventories;
using ByteSyncCommon.Business.Actions;
using ByteSyncCommon.Business.Sessions;
using ByteSyncCommon.Business.Sessions.Cloud;
using ByteSyncCommon.Business.Sessions.Cloud.Connections;
using ByteSyncCommon.Business.Sessions.Local;
using ByteSyncCommon.Business.SharedFiles;
using ByteSyncCommon.Business.Synchronizations;
using ByteSyncCommon.Business.Synchronizations.Light;
using DynamicData.Binding;
using PowSoftware.Common.Controls;
using PowSoftware.Common.Helpers;
using Prism.Events;
using Serilog;
using Splat;

namespace ByteSync.Controls.Sessions;

class SessionDataHolder : AbstractDataHolder<SessionFullDetails>, ISessionDataHolder
{
    private readonly IEventAggregator _eventAggregator;
    private readonly ICloudSessionEventsHub _cloudSessionEventsHub;
    private readonly IConnectionManager _connectionManager;
    private readonly IUIHelper _uiHelper;

    public SessionDataHolder(IEventAggregator? eventAggregator = null, ICloudSessionEventsHub? cloudSessionEventsHub = null,
        IConnectionManager? connectionManager = null, IUIHelper? uiHelper = null)
    {
        _eventAggregator = eventAggregator ?? Locator.Current.GetService<IEventAggregator>()!;
        _cloudSessionEventsHub = cloudSessionEventsHub ?? Locator.Current.GetService<ICloudSessionEventsHub>()!;
        _connectionManager = connectionManager ?? Locator.Current.GetService<IConnectionManager>()!;
        _uiHelper = uiHelper ?? Locator.Current.GetService<IUIHelper>()!;

        SessionFullDetails = new SessionFullDetails();

        InitializeData(SessionFullDetails);

        _eventAggregator.GetEvent<OnServerSynchronizationProgressChanged>().Subscribe(OnSynchronizationProgressChanged);

        // InventoryProcessData.AreFullInventoriesComplete
        //     .DistinctUntilChanged()
        //     .Where(b => b is true)
        //     .Subscribe(_ =>
        //     {
        //         ComputeComparisonResult().ConfigureAwait(false).GetAwaiter().GetResult();
        //     });
    }

    public SessionFullDetails SessionFullDetails { get; set; }

    public AbstractSession? Session => RunLocked(details => details.Session);
    
    public AbstractRunSessionProfileInfo? RunSessionProfileInfo  => RunLocked(details => details.RunSessionProfileInfo);

    public SessionSettings? SessionSettings => RunLocked(details => details.SessionSettings);

    public bool IsSessionActivated => RunLocked(details => details.IsSessionActivated);

    // public ComparisonResult? ComparisonResult => RunLocked(details => details.ComparisonResult);
    
    // public DataPartMapper? DataPartMapper => RunLocked(details => details.DataPartMapper);

    public string? CloudSessionPassword => RunLocked(details => details.SessionPassword);

    // public ObservableCollectionExtended<ComparisonItemViewModel> ComparisonItems
    // {
    //     get
    //     {
    //         lock (SyncRoot)
    //         {
    //             return SessionFullDetails?.ComparisonItems ?? new ObservableCollectionExtended<ComparisonItemViewModel>();
    //         }
    //     }
    // }

    public ObservableCollection<SharedAtomicAction>? SharedAtomicActions => RunLocked(details => details.SharedAtomicActions);

    public List<SharedActionsGroup>? SharedActionsGroups => RunLocked(details => details.SharedActionsGroups);
    
    public string? SessionId => RunLocked(details => details.Session?.SessionId);

    // public bool HasSynchronizationStarted => RunLocked(details => details.HasSynchronizationStarted);

    // public bool AreBaseInventoriesComplete => RunLocked(details => details.AreBaseInventoriesComplete);
    //
    // public bool AreFullInventoriesComplete => RunLocked(details => details.AreFullInventoriesComplete);

    public int ProgressActionsCount => RunLocked(details => details.ProgressActions.Count);
    
    public int ProgressActionsErrorsCount => RunLocked(details => details.ProgressActions.Count(pa => pa.IsError));

    // public bool IsSynchronizationEnded => RunLocked(details => details.IsSynchronizationEnded);

    // public bool IsSynchronizationRunning => RunLocked(_ => HasSynchronizationStarted && !IsSynchronizationEnded);

    public bool IsCloudSessionOnFatalError => RunLocked(details => details.IsCloudSessionFatalError);

    // public bool IsInventoriesComparisonDone => RunLocked(details => details.IsInventoriesComparisonDone);

    // public bool IsSynchronizationAbortRequested => RunLocked(details => details.IsSynchronizationAbortRequested);

    public bool IsProfileSession => RunSessionProfileInfo != null;

    // public InventoryProcessData InventoryProcessData => SessionFullDetails.InventoryProcessData;

    // public SynchronizationProcessData SynchronizationProcessData => SessionFullDetails.SynchronizationProcessData;
    
    // private List<SessionMemberInfo>? SessionMembers
    // {
    //     get { return SessionFullDetails?.SessionMembers; }
    // }

    // private HashSet<LocalSharedFile> OtherMembersInventories
    // {
    //     get { return SessionFullDetails?.OtherMembersInventories!; }
    // }

    // private HashSet<LocalSharedFile> OtherBaseMembersInventories
    // {
    //     get
    //     {
    //         return OtherMembersInventories
    //             .Where(i => i.SharedFileDefinition.SharedFileType == SharedFileTypes.BaseInventory)
    //             .ToList()
    //             .ToHashSet();
    //     }
    // }
    //
    // private HashSet<LocalSharedFile> OtherFullMembersInventories
    // {
    //     get
    //     {
    //         return OtherMembersInventories
    //             .Where(i => i.SharedFileDefinition.SharedFileType == SharedFileTypes.FullInventory)
    //             .ToList()
    //             .ToHashSet();
    //     }
    // }

    // private List<LocalSharedFile>? LocalBaseInventories
    // {
    //     get { return SessionFullDetails?.LocalBaseInventories; }
    // }
    //
    // private List<LocalSharedFile>? LocalFullInventories
    // {
    //     get { return SessionFullDetails?.LocalFullInventories; }
    // }
    
    
    public async Task SetLocalSession(LocalSession localSession, RunLocalSessionProfileInfo? runLocalSessionProfileInfo, SessionSettings sessionSettings)
    {
        lock (SyncRoot)
        {
            // if (SessionFullDetails != null)
            // {
            //     // ClearComparisonItems();
            //     SessionFullDetails.OnSessionEnd();
            // }
            
            

            SessionFullDetails.SessionMode = SessionModes.Local;
            SessionFullDetails.Session = localSession;
            SessionFullDetails.RunSessionProfileInfo = runLocalSessionProfileInfo;
            SessionFullDetails.SessionSettings = sessionSettings;
        

            SessionMemberInfo currentSessionMemberInfo = new SessionMemberInfo();
            currentSessionMemberInfo.Endpoint = _connectionManager.GetCurrentEndpoint()!;
            currentSessionMemberInfo.SessionId = localSession.SessionId;
            currentSessionMemberInfo.JoinedSessionOn = DateTimeOffset.UtcNow;
            currentSessionMemberInfo.PositionInList = 0;
            currentSessionMemberInfo.LocalInventoryGlobalStatus = LocalInventoryGlobalStatus.WaitingForStart;
            
            SessionFullDetails.SessionMembers = new List<SessionMemberInfo> {currentSessionMemberInfo};
            
            foreach (var sessionMemberInfo in SessionFullDetails.SessionMembers)
            {
                SessionFullDetails.PathItems.Add(sessionMemberInfo, new ObservableCollectionExtended<PathItemViewModel>());
            }

            if (runLocalSessionProfileInfo != null)
            {
                var myPathItems = runLocalSessionProfileInfo.GetMyPathItems();
                var pathItemsViewModels = GetMyPathItems()!;
                foreach (var pathItem in myPathItems)
                {
                    pathItemsViewModels.Add(new PathItemViewModel(pathItem));
                }
            }
            
            
        }
    }

    public async Task SetCloudSession(CloudSession cloudSession, RunCloudSessionProfileInfo? runCloudSessionProfileInfo, 
        SessionSettings sessionSettings, List<SessionMemberInfo> sessionMemberInfos)
    {
        lock (SyncRoot)
        {
            // if (SessionFullDetails != null)
            // {
            //     // ClearComparisonItems();
            //     SessionFullDetails.OnSessionEnd();
            // }

            

            SessionFullDetails.SessionMode = SessionModes.Cloud;
            SessionFullDetails.Session = cloudSession;
            SessionFullDetails.RunSessionProfileInfo = runCloudSessionProfileInfo;
            SessionFullDetails.SessionSettings = sessionSettings;
            SessionFullDetails.SessionMembers = sessionMemberInfos.ToList();

            foreach (var sessionMemberInfo in sessionMemberInfos)
            {
                SessionFullDetails.PathItems.Add(sessionMemberInfo, new ObservableCollectionExtended<PathItemViewModel>());
            }
        }
    }

    public void ClearCloudSession()
    {
        lock (SyncRoot)
        {
            if (SessionFullDetails != null)
            {
                SessionFullDetails.OnSessionEnd();
            
                SessionFullDetails = null;
            }
        }
    }
    
    public bool CheckCloudSession(CloudSession? cloudSession)
    {
        if (cloudSession == null)
        {
            return false;
        }

        lock (SyncRoot)
        {
            return Session != null && Session.Equals(cloudSession);
        }
    }

    public bool CheckCloudSession(string cloudSessionId)
    {
        if (cloudSessionId.IsNullOrEmpty())
        {
            return false;
        }

        lock (SyncRoot)
        {
            return Session != null && cloudSessionId.Equals(Session.SessionId, StringComparison.InvariantCultureIgnoreCase);
        }
    }

    public SessionMemberInfo? GetSessionMember(string clientInstanceId)
    {
        return Get(null, details => details.SessionMembers?.SingleOrDefault(mce => mce.HasInstanceId(clientInstanceId)));
    }

    public List<SessionMemberInfo> GetAllSessionMembers()
    {
        return Get(null, details =>
        {
            if (details.SessionMembers != null)
            {
                var result = new List<SessionMemberInfo>(details.SessionMembers);

                return result;
            }
            else
            {
                return new List<SessionMemberInfo>();
            }
        });
    }

    public List<SessionMemberInfo>? GetOtherSessionMembers()
    {
        var currentMachineEndpoint = _connectionManager.GetCurrentEndpoint();

        if (currentMachineEndpoint == null)
        {
            return null;
        }

        return Get(null, details =>
        {
            if (details.SessionMembers != null)
            {
                var result = new List<SessionMemberInfo>(details.SessionMembers);

                //result.RemoveWhere(m => m.Equals(CurrentMachineEndpoint));

                int index = result.FindIndex(smi => Equals(smi.Endpoint, currentMachineEndpoint));
                while (index != -1)
                {
                    result.RemoveAt(index);
                    index = result.FindIndex(smi => Equals(smi.Endpoint, currentMachineEndpoint));
                }

                return result;
            }
            else
            {
                return new List<SessionMemberInfo>();
            }
        });
    }

    public SessionMemberInfo? GetCurrentSessionMember()
    {
        return Get(null, details =>
        {
            var currentMachineEndpoint = _connectionManager.GetCurrentEndpoint();

            if (currentMachineEndpoint == null)
            {
                return null;
            }

            return details.SessionMembers?.FirstOrDefault(smi => Equals(smi.Endpoint, currentMachineEndpoint));
        });
    }

    public bool AddSessionMember(CloudSessionResult cloudSessionResult)
    {
        return Get(cloudSessionResult.SessionId, details =>
        {
            if (SessionFullDetails != null && details.SessionMembers != null)
            {
                if (!details.SessionMembers.Contains(cloudSessionResult.SessionMemberInfo))
                {
                    details.SessionMembers.Add(cloudSessionResult.SessionMemberInfo);
                    SessionFullDetails.PathItems.Add(cloudSessionResult.SessionMemberInfo, new ObservableCollectionExtended<PathItemViewModel>());

                    _cloudSessionEventsHub.RaiseMemberJoinedSession(cloudSessionResult);

                    return true;
                }
            }

            return false;
        });
    }

    public Task RemoveSessionMember(CloudSessionResult cloudSessionResult)
    {
        return RunAsync(cloudSessionResult.SessionId, details =>
            {
                if (details.SessionMembers != null)
                {
                    if (details.SessionMembers.Contains(cloudSessionResult.SessionMemberInfo))
                    {
                        details.SessionMembers.Remove(cloudSessionResult.SessionMemberInfo);
                        details.PathItems.Remove(cloudSessionResult.SessionMemberInfo);

                        _cloudSessionEventsHub.RaiseMemberQuittedSession(cloudSessionResult);
                    }
                }
            }
        );
        
        // lock (SyncRoot)
        // {
        //     if (SessionFullDetails != null && SessionMembers != null)
        //     {
        //         if (SessionMembers.Contains(cloudSessionResult.SessionMemberInfo))
        //         {
        //             SessionMembers.Remove(cloudSessionResult.SessionMemberInfo);
        //             SessionFullDetails.PathItems.Remove(cloudSessionResult.SessionMemberInfo);
        //
        //             _cloudSessionEventsHub.RaiseMemberQuittedSession(cloudSessionResult);
        //
        //             return true;
        //         }
        //     }
        //
        //     return false;
        // }
    }

    public async Task AddPathItem(string clientInstanceId, PathItem pathItem)
    {
        ObservableCollectionExtended<PathItemViewModel>? pathItems = null;

        await RunAsync(null, details =>
        {
            if (SessionFullDetails != null && details.SessionMembers != null)
            {
                var sessionMemberInfo = GetSessionMember(clientInstanceId);

                if (sessionMemberInfo != null)
                {
                    pathItems = SessionFullDetails.PathItems[sessionMemberInfo];
                }
            }
        });

        if (pathItems != null)
        {
            await _uiHelper.AddOnUI(pathItems, new PathItemViewModel(pathItem));
        }
    }

    public async Task RemovePathItem(string clientInstanceId, PathItem pathItem)
    {
        ObservableCollectionExtended<PathItemViewModel>? pathItems = null;

        await RunAsync(null, details =>
        {
            if (SessionFullDetails != null && details.SessionMembers != null)
            {
                var sessionMemberInfo = GetSessionMember(clientInstanceId);

                if (sessionMemberInfo != null)
                {
                    pathItems = SessionFullDetails.PathItems[sessionMemberInfo];

                    // CloudSessionLocalDetails.PathItems[sessionMemberInfo].RemoveAll(pivm => pivm.PathItem.Equals(pathItem));

                    // _cloudSessionEventsHub.RaisePathItemRemoved(sessionMemberInfo, pathItem);
                }
            }
        });
        
        if (pathItems != null)
        {
            await _uiHelper.RemoveAllOnUI(pathItems, pivm => pivm.PathItem.Equals(pathItem));
        }
    }

    public ObservableCollectionExtended<PathItemViewModel> GetPathItems(SessionMemberInfo sessionMemberInfo)
    {
        lock (SyncRoot)
        {
            if (SessionFullDetails != null)
            {
                if (SessionFullDetails.PathItems.ContainsKey(sessionMemberInfo))
                {
                    return SessionFullDetails.PathItems[sessionMemberInfo];
                }
            }
        }

        throw new ArgumentOutOfRangeException(nameof(sessionMemberInfo));
    }
    
    public ObservableCollectionExtended<PathItemViewModel>? GetMyPathItems()
    {
        lock (SyncRoot)
        {
            if (SessionFullDetails != null)
            {
                var currentSessionMember = GetCurrentSessionMember();

                if (currentSessionMember != null && SessionFullDetails.PathItems.ContainsKey(currentSessionMember))
                {
                    return SessionFullDetails.PathItems[currentSessionMember];
                }
            }
        }

        return null;
    }

    public ObservableCollection<SynchronizationRuleSummaryViewModel>? SynchronizationRules
    {
        get
        {
            lock (SyncRoot)
            {
                return SessionFullDetails?.SynchronizationRules;
            }
        }
    }

    public bool IsCurrentInstanceId(string clientInstanceId)
    {
        lock (SyncRoot)
        {
            var currentSessionMemer = GetCurrentSessionMember();

            return currentSessionMemer != null && currentSessionMemer.HasInstanceId(clientInstanceId);
        }
    }
    
    public bool IsCloudSession
    {
        get
        {
            lock (SyncRoot)
            {
                return Session is CloudSession;
            }
        }
    }
    
    public bool IsLocalSession
    {
        get
        {
            lock (SyncRoot)
            {
                return Session is LocalSession;
            }
        }
    }

    public bool IsCloudSessionCreatedByMe
    {
        get
        {
            lock (SyncRoot)
            {
                return IsSessionCreatedByMe && IsCloudSession;
                
                // var currentSessionMember = GetCurrentSessionMember();
                //
                // return currentSessionMember != null && Session is CloudSession &&
                //        SessionMembers != null && SessionMembers.IndexOf(currentSessionMember) == 0;
            }
        }
    }
    
    public bool IsSessionCreatedByMe
    {
        get
        {
            return Get(null, details =>
            {
                var currentSessionMember = GetCurrentSessionMember();

                return currentSessionMember != null && details.SessionMembers != null && details.SessionMembers.IndexOf(currentSessionMember) == 0;
            });
        }
    }
    
    public bool IsLobbyCloudSessionCreatedByMe
    {
        get
        {
            return Get(null, details =>
            {
                var currentSessionMember = GetCurrentSessionMember();

                return currentSessionMember != null && Session is CloudSession &&
                       details.SessionMembers != null && details.SessionMembers.IndexOf(currentSessionMember) == 0
                       && RunSessionProfileInfo != null;
            });
        }
    }

    public bool HasSessionBeenRestarted
    {
        get
        {
            lock (SyncRoot)
            {
                return SessionFullDetails?.HasSessionBeenRestarted ?? false;
            }
        }
    }

    public void GeneratePassword()
    {
        lock (SyncRoot)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < 5; i++)
            {
                sb.Append(RandomUtils.GetRandomLetter(true));
            }

            SessionFullDetails!.SessionPassword = sb.ToString();
        }
    }

    public Task SetSessionSettings(string sessionId, SessionSettings sessionSettings)
    {
        return RunAsync(sessionId, cloudSessionLocalDetails =>
        {
            cloudSessionLocalDetails.SessionSettings = sessionSettings;

            _cloudSessionEventsHub.RaiseSessionSettingsUpdated(sessionSettings);
        });
    }

    public void SetCloudSessionDetails(CloudSessionDetails cloudSessionDetails)
    {
        lock (SyncRoot)
        {
            if (SessionFullDetails?.Session != null
                && SessionFullDetails.Session.Equals(cloudSessionDetails.CloudSession))
            {
                SessionFullDetails.SessionMode = SessionFullDetails.Session is CloudSession
                    ? SessionModes.Cloud
                    : SessionModes.Local;
                
                SessionFullDetails?.Fill(cloudSessionDetails);
            }
        }
    }

    public Task SetSessionActivated(string sessionId)
    {
        return RunAsync(sessionId, cloudSessionLocalDetails =>
        {
            // todo 040423 : on ferait pas plutôt ça dans le Reset uniquement ???
            // cloudSessionLocalDetails.InventoryProcessData.Reset();
            
            cloudSessionLocalDetails.IsSessionActivated = true;
            
            _cloudSessionEventsHub.RaiseSessionActivated();
        });
    }

    public Task SetSessionOnFatalError(CloudSessionFatalError cloudSessionFatalError)
    {
        return RunAsync(cloudSessionFatalError.SessionId, cloudSessionLocalDetails =>
        {
            if (!cloudSessionLocalDetails.IsCloudSessionFatalError)
            {
                cloudSessionLocalDetails.CloudSessionFatalError = cloudSessionFatalError;
                
                // cloudSessionLocalDetails.InventoryProcessData.RequestInventoryAbort();

                _cloudSessionEventsHub.RaiseSessionOnFatalError(cloudSessionFatalError);
            }
        });
    }
    
    

    // private async Task CheckInventoriesReady()
    // {
    //     bool areFullInventoriesCompleteNow = false;
    //
    //     await RunAsync(null, details =>
    //     {
    //         var otherSessionMembers = GetOtherSessionMembers();
    //
    //         if (otherSessionMembers == null)
    //         {
    //             return;
    //         }
    //
    //         bool areBaseInventoriesComplete = // otherSessionMembers.Count >= 1 &&
    //             OtherBaseMembersInventories.Count == otherSessionMembers.Count &&
    //             LocalBaseInventories != null;
    //
    //         bool areFullInventoriesComplete = // otherSessionMembers.Count >= 1 &&
    //             OtherFullMembersInventories.Count == otherSessionMembers.Count &&
    //             LocalFullInventories != null;
    //
    //         // Log.Debug("CloudSessionDataHolder.CheckInventoriesReady: " +
    //         //           "BaseInventories:{AreBaseInventoriesComplete}, FullInventories:{AreFullInventoriesComplete}",
    //         //     areBaseInventoriesComplete, areFullInventoriesComplete);
    //         //
    //         // if (areBaseInventoriesComplete && !SessionFullDetails.AreBaseInventoriesComplete)
    //         // {
    //         //     // avec ce contrôle, on ne lance l'évènement qu'une seule fois
    //         //     _cloudSessionEventsHub.RaiseAllInventoriesReady(LocalInventoryModes.Base);
    //         // }
    //         //     
    //         // if (areFullInventoriesComplete && !SessionFullDetails.AreFullInventoriesComplete)
    //         // {
    //         //     // avec ce contrôle, on ne lance l'évènement qu'une seule fois
    //         //     _cloudSessionEventsHub.RaiseAllInventoriesReady(LocalInventoryModes.Full);
    //         //
    //         //     areFullInventoriesCompleteNow = true;
    //         // }
    //
    //         details.InventoryProcessData.AreBaseInventoriesComplete.OnNext(areBaseInventoriesComplete);
    //         details.InventoryProcessData.AreFullInventoriesComplete.OnNext(areFullInventoriesComplete);
    //         
    //         
    //         // if (SessionFullDetails != null)
    //         // {
    //         //     if (areBaseInventoriesComplete && !SessionFullDetails.AreBaseInventoriesComplete)
    //         //     {
    //         //         // avec ce contrôle, on ne lance l'évènement qu'une seule fois
    //         //         _cloudSessionEventsHub.RaiseAllInventoriesReady(LocalInventoryModes.Base);
    //         //     }
    //         //     
    //         //     if (areFullInventoriesComplete && !SessionFullDetails.AreFullInventoriesComplete)
    //         //     {
    //         //         // avec ce contrôle, on ne lance l'évènement qu'une seule fois
    //         //         _cloudSessionEventsHub.RaiseAllInventoriesReady(LocalInventoryModes.Full);
    //         //
    //         //         areFullInventoriesCompleteNow = true;
    //         //     }
    //         //
    //         //     details.InventoryProcessData.AreBaseInventoriesComplete.OnNext(areBaseInventoriesComplete);
    //         //     details.InventoryProcessData.AreFullInventoriesComplete.OnNext(areFullInventoriesComplete);
    //         //     
    //         //     // SessionFullDetails.AreBaseInventoriesComplete = areBaseInventoriesComplete;
    //         //     // SessionFullDetails.AreFullInventoriesComplete = areFullInventoriesComplete;
    //         //     //
    //         //     // if (SessionFullDetails.AreBaseInventoriesComplete)
    //         //     // {
    //         //     //     SessionFullDetails.InventoryProcessData!.AllBaseInventoriesCompleteEvent.Set();
    //         //     // }
    //         //     //
    //         //     // if (SessionFullDetails.AreFullInventoriesComplete)
    //         //     // {
    //         //     //     SessionFullDetails.InventoryProcessData!.AllFullInventoriesCompleteEvent.Set();
    //         //     // }
    //         // }
    //     });
    // }

    // private async Task ComputeComparisonResult()
    // {
    //     using InventoryComparer inventoryComparer = new InventoryComparer(SessionSettings!);
    //             
    //     List<LocalSharedFile> inventoriesFiles = GetAllInventoriesFiles(LocalInventoryModes.Full);
    //     inventoryComparer.AddInventories(inventoriesFiles);
    //     var comparisonResult = inventoryComparer.Compare();
    //
    //     
    //     //////
    //     SetComparisonResult(comparisonResult);
    //
    //     lock (SyncRoot)
    //     {
    //         if (SessionFullDetails != null)
    //         {
    //             SessionFullDetails.IsInventoriesComparisonDone = true;
    //         }
    //     }
    //
    //     await _cloudSessionEventsHub.RaiseInventoriesComparisonDone(comparisonResult);
    //     //////
    // }

    public void SetPassword(string sessionPassword)
    {
        lock (SyncRoot)
        {
            if (SessionFullDetails != null)
            {
                SessionFullDetails.SessionPassword = sessionPassword;
            }
        }
    }



    // public LocalInventoryGlobalStatus? GetLocalInventoryStatus()
    // {
    //     lock (SyncRoot)
    //     {
    //         return SessionFullDetails?.LocalInventoryGlobalStatus;
    //     }
    // }

    // public void SetLocalInventoryStarted()
    // {
    //     _cloudSessionEventsHub.RaiseLocalInventoryStarted();
    //
    //     SetLocalInventoryStatus(LocalInventoryStatuses.RunningIdentification);
    // }

    // public void SetLocalInventoryFinished(LocalInventoryStatuses localInventoryStatus)
    // {
    //     // _cloudSessionEventsHub.RaiseLocalInventoryFinished();
    //
    //     SetLocalInventoryStatus(localInventoryStatus);
    // }

    // public async Task OnFileIsFullyDownloaded(LocalSharedFile localSharedFile)
    // {
    //     if (localSharedFile.SharedFileDefinition.IsInventory)
    //     {
    //         lock (SyncRoot)
    //         {
    //             OtherMembersInventories.Add(localSharedFile);
    //         }
    //         
    //         await CheckInventoriesReady();
    //     }
    // }

    // public async Task SetSynchronizationStarted(SynchronizationStart synchronizationStart)
    // {
    //     await RunAsync(synchronizationStart.SessionId, cloudSessionLocalDetails =>
    //     {
    //         cloudSessionLocalDetails.SynchronizationStart = synchronizationStart;
    //         cloudSessionLocalDetails.SynchronizationStarted.Set();
    //             
    //         _cloudSessionEventsHub.RaiseSynchronizationStarted(synchronizationStart, 
    //             cloudSessionLocalDetails.SharedAtomicActions, cloudSessionLocalDetails.SharedActionsGroups);
    //             
    //         // Log.Information("Synchronization started by {ClienInstanceId}", synchronizationStart.StartedBy);
    //     });
    // }
    
    // public async Task SetSynchronizationStartData(SharedSynchronizationStartData synchronizationStartData)
    // {
    //     await RunAsync(synchronizationStartData.SessionId, cloudSessionLocalDetails =>
    //     {
    //         if (cloudSessionLocalDetails is { HasSynchronizationStarted: false })
    //         {
    //             cloudSessionLocalDetails.SharedActionsGroups.Clear();
    //             cloudSessionLocalDetails.SharedActionsGroups.AddAll(synchronizationStartData.SharedActionsGroups);
    //
    //             _uiHelper.ClearAndAddOnUI(cloudSessionLocalDetails.SharedAtomicActions, synchronizationStartData.SharedAtomicActions)
    //                 .ContinueWith(_ =>
    //                 {
    //                     cloudSessionLocalDetails.RegisterActionsGroupsAndAtomicActionsLinks();
    //                 })
    //                 .ContinueWith(_ =>
    //                 {
    //                     cloudSessionLocalDetails.SynchronizationDataReady.Set();
    //                 });
    //             
    //             Log.Information("The Data Synchronization actions have been set:");
    //             if (synchronizationStartData.SharedAtomicActions.Count == 0)
    //             {
    //                 Log.Information(" - No action to perform");
    //             }
    //             else
    //             {
    //                 Log.Information(" - {Count} action(s) to perform", synchronizationStartData.SharedAtomicActions.Count);
    //             }
    //             foreach (var synchronizationRule in synchronizationStartData.LooseSynchronizationRules)
    //             {
    //                 var descriptionBuilder = new SynchronizationRuleDescriptionBuilder(synchronizationRule);
    //                 descriptionBuilder.BuildDescription(" | ");
    //                 var description = $"{descriptionBuilder.Mode} [{descriptionBuilder.Conditions}] {descriptionBuilder.Then} " +
    //                                   $"[{descriptionBuilder.Actions}]";
    //
    //                 Log.Information(" - Synchronization Rule: {Description}", description);
    //             }
    //             foreach (var sharedAtomicAction in synchronizationStartData.SharedAtomicActions.Where(a => !a.IsFromSynchronizationRule))
    //             {
    //                 var descriptionBuilder = new SharedAtomicActionDescriptionBuilder();
    //                 var description = $"{sharedAtomicAction.PathIdentity.LinkingData} ({sharedAtomicAction.PathIdentity.FileSystemType}) - " +
    //                                   $"{descriptionBuilder.GetDescription(sharedAtomicAction)}";
    //
    //                 Log.Information(" - Targeted Action: {LinkingData} ({FileSystemType}) - {Description}", 
    //                     sharedAtomicAction.PathIdentity.LinkingData, sharedAtomicAction.PathIdentity.FileSystemType, description);
    //             }
    //         }
    //     });
    //     
    //     await WaitForSynchronizationDataReadyAsync();
    // }

    public HashSet<string> GetAtomicActionsIds(string actionsGroupId) 
        => RunLocked(cloudSessionLocalDetails => cloudSessionLocalDetails.GetAtomicActionsIds(actionsGroupId)) ?? new HashSet<string>();

    public HashSet<string> GetActionsGroupIds(ICollection<string> atomicActionsIds)
    {
        lock (SyncRoot)
        {
            if (SessionFullDetails != null)
            {
                return SessionFullDetails.GetActionsGroupIds(atomicActionsIds);
            }
        }

        return new HashSet<string>();
    }

    public List<string>? GetActionsGroupIds(SharedFileDefinition sharedFileDefinition)
    {
        lock (SyncRoot)
        {
            if (SessionFullDetails != null)
            {
                return SessionFullDetails.GetActionsGroupIds(sharedFileDefinition);
            }
        }

        return null;
    }
    
    public void SetActionsGroupIds(SharedFileDefinition sharedFileDefinition, List<string> actionsGroupsIds)
    {
        lock (SyncRoot)
        {
            if (SessionFullDetails != null)
            {
                SessionFullDetails.SetActionsGroupIds(sharedFileDefinition, actionsGroupsIds);
            }
        }
    }

    // public void SetInventoryProcessData(InventoryProcessData inventoryProcessData)
    // {
    //     lock (SyncRoot)
    //     {
    //         if (SessionFullDetails != null)
    //         {
    //             SessionFullDetails.SetInventoryProcessData(inventoryProcessData);
    //         }
    //     }
    // }

    // private async Task<List<ComparisonItemViewModel>> BuildComparisonItemViewModelList()
    // {
    //     return await Task.Run(() =>
    //     {
    //         List<ComparisonItemViewModel> list = new List<ComparisonItemViewModel>();
    //         foreach (var resultComparisonItem in ComparisonResult!.ComparisonItems.OrderBy(c => c.PathIdentity.LinkingKeyValue))
    //         {
    //             var comparisonItemView = new ComparisonItemViewModel(resultComparisonItem, ComparisonResult.Inventories);
    //
    //             list.Add(comparisonItemView);
    //         }
    //
    //         return list;
    //     });
    // }

    // public async Task<bool> WaitForInventoryStartedAsync()
    // {
    //     return await Task.Run(WaitForInventoryStarted);
    // }
    //
    // private bool WaitForInventoryStarted()
    // {
    //     List<WaitHandle> waitHandles = new List<WaitHandle>();
    //     lock (SyncRoot)
    //     {
    //         if (SessionFullDetails != null)
    //         {
    //             waitHandles.Add(SessionFullDetails.InventoryStarted);
    //             waitHandles.Add(SessionFullDetails.SessionEnded);
    //         }
    //     }
    //
    //     if (waitHandles.Count > 0)
    //     {
    //         int result = WaitHandle.WaitAny(waitHandles.ToArray());
    //         return result == 0;
    //     }
    //     else
    //     {
    //         return false;
    //     }
    // }
    
    // // todo 040423
    // public async Task<bool> WaitForComparisonResultSetAsync()
    // {
    //     return await Task.Run(WaitForComparisonResultSet);
    // }
    //
    // // todo 040423
    // private bool WaitForComparisonResultSet()
    // {
    //     List<WaitHandle> waitHandles = new List<WaitHandle>();
    //     lock (SyncRoot)
    //     {
    //         if (SessionFullDetails != null)
    //         {
    //             waitHandles.Add(SessionFullDetails.ComparisonResultSet);
    //             // waitHandles.Add(SessionFullDetails.SessionEnded);
    //         }
    //     }
    //
    //     if (waitHandles.Count > 0)
    //     {
    //         int result = WaitHandle.WaitAny(waitHandles.ToArray());
    //         return result == 0;
    //     }
    //     else
    //     {
    //         return false;
    //     }
    // }
    
    // public async Task<bool> WaitForSynchronizationEndedAsync()
    // {
    //     return await Task.Run(WaitForSynchronizationEnded);
    // }
    //
    // private bool WaitForSynchronizationEnded()
    // {
    //     List<WaitHandle> waitHandles = new List<WaitHandle>();
    //     lock (SyncRoot)
    //     {
    //         if (SessionFullDetails != null)
    //         {
    //             waitHandles.Add(SessionFullDetails.SynchronizationEnded);
    //             waitHandles.Add(SessionFullDetails.SessionEnded);
    //         }
    //     }
    //
    //     if (waitHandles.Count > 0)
    //     {
    //         int result = WaitHandle.WaitAny(waitHandles.ToArray());
    //         return result == 0;
    //     }
    //     else
    //     {
    //         return false;
    //     }
    // }

    // todo 040423
    public async Task<bool> WaitForSynchronizationDataReadyAsync()
    {
        return await Task.Run(WaitForSynchronizationDataReady);
    }

    // todo 040423
    private bool WaitForSynchronizationDataReady()
    {
        List<WaitHandle> waitHandles = new List<WaitHandle>();
        lock (SyncRoot)
        {
            if (SessionFullDetails != null)
            {
                waitHandles.Add(SessionFullDetails.SynchronizationDataReady);
                // waitHandles.Add(SessionFullDetails.SessionEnded);
            }
        }

        if (waitHandles.Count > 0)
        {
            int result = WaitHandle.WaitAny(waitHandles.ToArray());
            return result == 0;
        }
        else
        {
            return false;
        }
    }

    // public WaitHandle GetSessionEndedEvent()
    //     => RunLocked(cloudSessionLocalDetails => cloudSessionLocalDetails.SessionEnded) ?? new ManualResetEvent(true);

    // public WaitHandle GetInventoryTransferErrorEvent() 
    //     => RunLocked(cloudSessionLocalDetails => cloudSessionLocalDetails.InventoryTransferError) ?? new ManualResetEvent(true);

    // public void SetSessionMode(SessionModes sessionMode)
    //     => RunLocked(_ => SessionMode = sessionMode);

    // public void SetSynchronizationAbortRequest(SynchronizationAbortRequest synchronizationAbortRequest)
    // {
    //     lock (SyncRoot)
    //     {
    //         if (SessionFullDetails is { IsSynchronizationAbortRequested: false })
    //         {
    //             SessionFullDetails.SynchronizationAbortRequest = synchronizationAbortRequest;
    //
    //             _cloudSessionEventsHub.RaiseSynchronizationAbortRequested(synchronizationAbortRequest);
    //         }
    //     }
    // }
    //
    // public void SetSynchronizationEnded(SynchronizationEnd synchronizationEnd)
    // {
    //     lock (SyncRoot)
    //     {
    //         if (SessionFullDetails is { IsSynchronizationEnded: false })
    //         {
    //             Log.Information("The Data Synchronization has ended with status: {Status}", synchronizationEnd.Status);
    //             
    //             SessionFullDetails.SynchronizationEnd = synchronizationEnd;
    //
    //             SessionFullDetails.SynchronizationEnded.Set();
    //
    //             _cloudSessionEventsHub.RaiseSynchronizationEnded(synchronizationEnd);
    //         }
    //     }
    // }

    public HashSet<ProgressActionInfo>? GetProgressActions(HashSet<string> actionIds)
    {
        lock (SyncRoot)
        {
            if (SessionFullDetails != null)
            {
                var result = SessionFullDetails.ProgressActions
                    .Where(pa => actionIds.Contains(pa.ActionsGroupId))
                    .ToList()
                    .ToHashSet();

                return result;
            }

            return null;
        }
    }

    // public void SetComparisonResult(ComparisonResult comparisonResult)
    // {
    //     lock (SyncRoot)
    //     {
    //         if (SessionFullDetails != null)
    //         {
    //             SessionFullDetails.ComparisonResult = comparisonResult;
    //
    //             SessionFullDetails.DataPartMapper = new DataPartMapper(SessionFullDetails.ComparisonResult?.Inventories);
    //             
    //             if (IsSessionCreatedByMe && ! HasSessionBeenRestarted && SessionFullDetails.RunSessionProfileInfo != null)
    //             {
    //                 // On reconvertit les SynchronizationRules
    //                 SynchronizationRulesConverter converter = new SynchronizationRulesConverter();
    //                 var synchronizationRuleViewModels = converter.ConvertToSynchronizationRuleViewModels(
    //                     SessionFullDetails.RunSessionProfileInfo.GetProfileDetails().SynchronizationRules,
    //                     SessionFullDetails.DataPartMapper);
    //             
    //                 SessionFullDetails.SynchronizationRules.AddAll(synchronizationRuleViewModels);
    //             }
    //
    //             SessionFullDetails.ComparisonResultSet.Set();
    //         }
    //     }
    // }

    public SharedActionsGroup? GetSharedActionsGroup(ActionsGroupDefinition? actionsGroupDefinition)
    {
        if (actionsGroupDefinition == null)
        {
            return null;
        }

        return GetSharedActionsGroup(actionsGroupDefinition.ActionsGroupId);
    }
    
    public SharedActionsGroup? GetSharedActionsGroup(string actionsGroupId)
    {
        lock (SyncRoot)
        {
            return SharedActionsGroups?.FirstOrDefault(ssa => ssa.ActionsGroupId.Equals(actionsGroupId));
        }
    }

    public async void OnSynchronizationProgressChanged(string synchronizationProgressInfosId)
    {
        try
        {
            List<SynchronizationProgressInfo>? synchronizationProgressInfos =
                await _connectionManager.HttpWrapper.GetSynchronizationProgressInfos(SessionId, synchronizationProgressInfosId);

            if (synchronizationProgressInfos != null && synchronizationProgressInfos.Count > 0)
            {
                OnSynchronizationProgressChanged(synchronizationProgressInfos);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "OnSynchronizationProgressChanged");
        }
    }

    public void OnSynchronizationProgressChanged(SynchronizationProgressInfo synchronizationProgressInfo)
    {
        OnSynchronizationProgressChanged(new List<SynchronizationProgressInfo> { synchronizationProgressInfo });
    }

    public void OnSynchronizationProgressChanged(List<SynchronizationProgressInfo> synchronizationProgressInfos)
    {
        Task.Run(() =>
        {
            try
            {
                if (synchronizationProgressInfos.Count == 0)
                {
                    return;
                }

                var sessionId = synchronizationProgressInfos
                    .Select(s => s.SessionId)
                    .Distinct()
                    .SingleOrDefault();
                
                if (sessionId != null && CheckCloudSession(sessionId))
                {
                    var waitResult = WaitForSynchronizationDataReady();
                    if (!waitResult)
                    {
                        return;
                    }

                    foreach (var synchronizationProgressInfo in synchronizationProgressInfos)
                    {
                        lock (SyncRoot)
                        {
                            if (SessionFullDetails != null)
                            {
                                if (synchronizationProgressInfo.LastProgressAction != null)
                                {
                                    // On remplace
                                    SessionFullDetails.ProgressActions.Remove(synchronizationProgressInfo.LastProgressAction);
                                    SessionFullDetails.ProgressActions.Add(synchronizationProgressInfo.LastProgressAction);
                                }
                            }
                        }

                        _cloudSessionEventsHub.RaiseSynchronizationProgressChanged(synchronizationProgressInfo);
                    }
                }
                else
                {
                    if (synchronizationProgressInfos.Count == 0)
                    {
                        Log.Warning("OnSynchronizationProgressChanged: synchronizationProgressInfos is empty");
                    }
                    else
                    {
                        Log.Warning("OnRunSynchronizationAction: unknown session ({@Session})", sessionId);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "OnSynchronizationProgressChanged");
            }
        });
    }

    public async Task ResetSession()
    {
        await Task.Run(() =>
        {
            lock (SyncRoot)
            {
                SessionFullDetails?.ResetSession();

                // todo 040423 : Reset => Clear
                // _uiHelper.ClearOnUI(ComparisonItems);
            }
            
            _cloudSessionEventsHub.RaiseSessionResetted();
        });
    }

    // public Task ApplySynchronizationRules()
    // {
    //     return Task.Run(async () =>
    //     {
    //         // Il faut remapper les règles existantes car les inventaires ont pu changer
    //         var synchronizationRules = SynchronizationRules;
    //     
    //         if (synchronizationRules != null)
    //         {
    //             DataPartMapper!.Remap(synchronizationRules);
    //
    //             await UpdateComparisonItemsActions();
    //     
    //             await UpdateSharedSynchronizationActions();
    //
    //             await _cloudSessionEventsHub.RaiseSynchronizationRulesApplied();
    //         }
    //     });
    // }
    
    public async Task UpdateSharedSynchronizationActions()
    {
        // todo 040423
        //
        if (HasSynchronizationStarted)
        {
            return;
        }
        //
        
        var sharedAtomicActions = SharedAtomicActions;
        if (sharedAtomicActions != null)
        {
            await _uiHelper.ExecuteOnUi(() =>
            {
                var sharedAtomicActionComputer = Locator.Current.GetService<ISharedAtomicActionComputer>()!;
                
                sharedAtomicActions.Clear();
                sharedAtomicActions.AddAll(sharedAtomicActionComputer.GetSharedAtomicActions());
            });
        }
    }

    // private async Task UpdateComparisonItemsActions()
    // {
    //     if (HasSynchronizationStarted)
    //     {
    //         return;
    //     }
    //     
    //     var allSynchronizationRules = SynchronizationRules!.Select(vm => vm.SynchronizationRule).ToList();
    //     await _uiHelper.ExecuteOnUi(() =>
    //     {
    //         SynchronizationRuleMatcher synchronizationRuleMatcher = new SynchronizationRuleMatcher(this);
    //         synchronizationRuleMatcher.MakeMatches(ComparisonItems, allSynchronizationRules);
    //     });
    // }

    /*
    private void SetLocked(Action func)
    {
        lock (SyncRoot)
        {
            func.Invoke();
        }
    }
    
    private T GetLocked<T>(Func<T> func)
    {
        lock (SyncRoot)
        {
            return func.Invoke();
        }
    }*/
    
/*
    private void RunLocked(Action<SessionFullDetails> func)
    {
        lock (SyncRoot)
        {
            if (SessionFullDetails != null)
            {
                func.Invoke(SessionFullDetails);
            }
            else
            {
                Log.Warning("SessionDataHolder.RunLocked: can not run func {@func}", func);
            }
        }
    }*/

    
    /*
    private async Task RunLockedTasked(Action<SessionFullDetails> func)
    {
        await Task.Run(() => RunLocked(func));
    }
    */
    
    
    /*
    private T? RunLocked<T>(Func<SessionFullDetails, T> func)
    {
        lock (SyncRoot)
        {
            if (SessionFullDetails != null)
            {
                return func.Invoke(SessionFullDetails);
            }
            else
            {

            }
        }

        return default;
    }

    protected override string GetDataId(SessionFullDetails data)
    {
        return data.Session?.SessionId ?? "";
    }

    protected override ManualResetEvent? GetEndEvent(SessionFullDetails data)
    {
        return null;
    }
}
*/