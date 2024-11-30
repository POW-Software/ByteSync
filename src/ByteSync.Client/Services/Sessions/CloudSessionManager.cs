// using System.Threading.Tasks;
// using ByteSync.Business.Events;
// using ByteSync.Common.Business.Sessions.Cloud;
// using ByteSync.Interfaces.Controls.Inventories;
// using ByteSync.Interfaces.Controls.Sessions;
// using ByteSync.Interfaces.EventsHubs;
// using Prism.Events;
// using Splat;
//
// namespace ByteSync.Services.Sessions;
//
// class CloudSessionManager : ICloudSessionManager
// {
//     private readonly IEventAggregator _eventAggregator;
//     private readonly ISessionService _sessionService;
//     private readonly ICloudSessionLocalDataManager _cloudSessionLocalDataManager;
//     private readonly ICloudSessionEventsHub _cloudSessionEventsHub;
//     private readonly INavigationEventsHub _navigationEventsHub;
//     private readonly IDataInventoryStarter _dataInventoryStarter;
//     private readonly IInventoryService _inventoryService;
//
//     public CloudSessionManager(IEventAggregator? eventAggregator = null,
//         ISessionService? cloudSessionDataHolder = null, ICloudSessionEventsHub? cloudSessionEventsHub = null, 
//         ICloudSessionLocalDataManager? cloudSessionLocalDataManager = null, INavigationEventsHub? navigationEventsHub = null, 
//         IDataInventoryStarter? dataInventoryStarter = null, IInventoryService? inventoriesService = null)
//     {
//         _eventAggregator = eventAggregator ?? Locator.Current.GetService<IEventAggregator>()!;
//         _sessionService = cloudSessionDataHolder ?? Locator.Current.GetService<ISessionService>()!;
//         _cloudSessionEventsHub = cloudSessionEventsHub ?? Locator.Current.GetService<ICloudSessionEventsHub>()!;
//         _cloudSessionLocalDataManager = cloudSessionLocalDataManager ?? Locator.Current.GetService<ICloudSessionLocalDataManager>()!;
//         _navigationEventsHub = navigationEventsHub ?? Locator.Current.GetService<INavigationEventsHub>()!;
//         _dataInventoryStarter = dataInventoryStarter ?? Locator.Current.GetService<IDataInventoryStarter>()!;
//         _inventoryService = inventoriesService ?? Locator.Current.GetService<IInventoryService>()!;
//
//         // _eventAggregator.GetEvent<OnServerMemberJoinedSession>().Subscribe(OnMemberJoinedSession);
//         // _eventAggregator.GetEvent<OnServerMemberQuittedSession>().Subscribe(OnMemberQuittedSession);
//         // _eventAggregator.GetEvent<OnServerStartInventory>().Subscribe(OnDataInventoryStarted);
//         // _eventAggregator.GetEvent<OnServerSessionSettingsUpdated>().Subscribe(OnSessionSettingsUpdated);
//         _eventAggregator.GetEvent<OnServerSessionOnFatalError>().Subscribe(OnSessionOnFatalError);
//         // _eventAggregator.GetEvent<OnServerPathItemAdded>().Subscribe(OnPathItemAdded);
//         // _eventAggregator.GetEvent<OnServerPathItemRemoved>().Subscribe(OnPathItemRemoved);
//         // _eventAggregator.GetEvent<OnServerFilePartUploaded>().Subscribe(OnFilePartUploadedByteOtherClient);
//         // _eventAggregator.GetEvent<OnServerUploadFinished>().Subscribe(OnUploadFinishedByOtherClient);
//         // _eventAggregator.GetEvent<OnServerSynchronizationStarted>().Subscribe(OnSynchronizationStarted);
//         // _eventAggregator.GetEvent<OnServerSynchronizationAbortRequested>().Subscribe(OnSynchronizationAbortRequested);
//         // _eventAggregator.GetEvent<OnServerSynchronizationEnded>().Subscribe(OnSynchronizationEnded);
//         _eventAggregator.GetEvent<OnServerLocalInventoryStatusChanged>().Subscribe(OnLocalInventoryGlobalStatusChanged);
//         // _eventAggregator.GetEvent<OnServerSessionResetted>().Subscribe(OnSessionResetted);
//         
//         _cloudSessionEventsHub.SynchronizationRulesApplied += CloudSessionEventsHub_OnSynchronizationRulesApplied;
//     }
//     // public async Task SetSessionSettings(SessionSettings sessionSettings)
//     // {
//     //     var session = _sessionService.CurrentSession;
//     //
//     //     if (session != null)
//     //     {
//     //         try
//     //         {
//     //             if (session is CloudSession)
//     //             {
//     //                 var dataEncrypter = Locator.Current.GetService<IDataEncrypter>()!;
//     //                 var encryptedSessionSettings = dataEncrypter.EncryptSessionSettings(sessionSettings);
//     //                 
//     //                 await _connectionManager.HubWrapper.SendUpdatedSessionSettings(session.SessionId, encryptedSessionSettings);
//     //             }
//     //
//     //             await _sessionService.SetSessionSettings(session.SessionId, sessionSettings);
//     //         }
//     //         catch (Exception ex)
//     //         {
//     //             Log.Error(ex, "CloudSessionManager.SendUpdatedSessionSettings");
//     //         }
//     //     }
//     //     else
//     //     {
//     //         Log.Error("CloudSessionManager.SendUpdatedSessionSettings: unknown session");
//     //     }
//     // }
//     
//     // private void OnDataInventoryStarted((string sessionId, string clientInstanceId, EncryptedSessionSettings encryptedSessionSettings) tuple)
//     // {
//     //     _dataInventoryStarter.OnDataInventoryStarted(tuple.sessionId, tuple.clientInstanceId, tuple.encryptedSessionSettings);
//     // }
//
//     // public async Task<bool> SetPathItemAdded(PathItem pathItem)
//     // {
//     //     var cloudSessionId = _sessionService.SessionId;
//     //
//     //     if (cloudSessionId.IsNotEmpty())
//     //     {
//     //         try
//     //         {
//     //             var dataEncrypter = Locator.Current.GetService<IDataEncrypter>()!;
//     //             var encryptedPathItem = dataEncrypter.EncryptPathItem(pathItem); 
//     //
//     //             return await _connectionManager.HubWrapper.SetPathItemAdded(cloudSessionId!, encryptedPathItem);
//     //         }
//     //         catch (Exception ex)
//     //         {
//     //             Log.Error(ex, "CloudSessionManager.SetPathItemAdded");
//     //         }
//     //     }
//     //     else
//     //     {
//     //         Log.Error("CloudSessionManager.SetPathItemAdded: unknown session");
//     //     }
//     //
//     //     return false;
//     // }
//
//     // public async Task<bool> SetPathItemRemoved(PathItem pathItem)
//     // {
//     //     var cloudSessionId = _sessionService.SessionId;
//     //
//     //     if (cloudSessionId.IsNotEmpty())
//     //     {
//     //         try
//     //         {
//     //             var dataEncrypter = Locator.Current.GetService<IDataEncrypter>()!;
//     //             var encryptedPathItem = dataEncrypter.EncryptPathItem(pathItem); 
//     //             
//     //             // PathItemEncrypter pathItemEncrypter = _sessionObjectsFactory.BuildPathItemEncrypter();
//     //             // var sharedPathItem = pathItemEncrypter.Encrypt(pathItem);
//     //
//     //             return await _connectionManager.HubWrapper.SetPathItemRemoved(cloudSessionId!, encryptedPathItem);
//     //         }
//     //         catch (Exception ex)
//     //         {
//     //             Log.Error(ex, "CloudSessionManager.SetPathItemRemoved");
//     //         }
//     //     }
//     //
//     //     return false;
//     // }
//     //
//     // public async Task<List<PathItem>?> GetPathItems(string clientInstanceId)
//     // {
//     //     var cloudSessionId = _sessionService.SessionId;
//     //
//     //     if (cloudSessionId != null)
//     //     {
//     //         var encryptedPathItems = await _connectionManager.HubWrapper.GetPathItems(cloudSessionId, clientInstanceId);
//     //
//     //         var dataEncrypter = Locator.Current.GetService<IDataEncrypter>()!;
//     //         
//     //         List<PathItem> pathItems = new List<PathItem>();
//     //         foreach (var encryptedPathItem in encryptedPathItems)
//     //         {
//     //             var pathItem = dataEncrypter.DecryptPathItem(encryptedPathItem);
//     //             pathItems.Add(pathItem);
//     //         }
//     //
//     //         return pathItems;
//     //     }
//     //     else
//     //     {
//     //         return null;
//     //     }
//     // }
//
// //     private void OnMemberJoinedSession(CloudSessionResult cloudSessionResult)
// //     {
// //         // todo 050523
// //         throw new NotImplementedException("todo 050523");
// //         
// //         /*
// //         try
// //         {
// //             if (_sessionService.CheckCloudSession(cloudSessionResult.CloudSession))
// //             {
// //                 bool isAdded = _sessionService.AddSessionMember(cloudSessionResult);
// //
// //                 if (isAdded)
// //                 {
// //                     Log.Information("Another member joined session: {ClientInstanceId} (MachineName: {MachineName})", 
// //                         cloudSessionResult.SessionMemberInfo.ClientInstanceId, cloudSessionResult.SessionMemberInfo.Endpoint.MachineName);
// //                 }
// //             }
// //         }
// //         catch (Exception ex)
// //         {
// //             Log.Error(ex, "OnMemberJoinedSession");
// //         }
// //         
// //         */
// //     }
//
//     // private async void OnMemberQuittedSession(CloudSessionResult cloudSessionResult)
//     // {
//     //     // todo 050523
//     //     throw new NotImplementedException("todo 050523");
//     //     
//     //     /*
//     //     try
//     //     {
//     //         if (_sessionService.CheckCloudSession(cloudSessionResult.CloudSession))
//     //         {
//     //             await _sessionService.RemoveSessionMember(cloudSessionResult).ConfigureAwait(false);
//     //         }
//     //     }
//     //     catch (Exception ex)
//     //     {
//     //         Log.Error(ex, "OnMemberQuittedSession");
//     //     }
//     //     
//     //     */
//     // }
//
//     // private async void OnSessionResetted(string sessionId)
//     // {
//     //     // todo 050523
//     //     //throw new NotImplementedException("todo 050523");
//     //     
//     //     
//     //     try
//     //     {
//     //         if (_sessionService.CheckCloudSession(sessionId))
//     //         {
//     //             await _cloudSessionLocalDataManager.BackupCurrentSessionFiles();
//     //             
//     //             await _sessionService.ResetSession();
//     //         }
//     //     }
//     //     catch (Exception ex)
//     //     {
//     //         Log.Error(ex, "OnSessionResetted");
//     //     }
//     //     
//     // }
//         
//     private async void OnSessionOnFatalError(CloudSessionFatalError cloudSessionFatalError)
//     {
//         // todo 050523
//         throw new NotImplementedException("todo 050523");
//         
//         /*
//         try
//         {
//             if (_sessionService.CheckCloudSession(cloudSessionFatalError.SessionId))
//             {
//                 await _sessionService.SetSessionOnFatalError(cloudSessionFatalError);
//                 await _inventoriesService.SetSessionOnFatalError(cloudSessionFatalError);
//             }
//         }
//         catch (Exception ex)
//         {
//             Log.Error(ex, "OnSessionOnFatalError");
//         }
//         */
//     }
//
// //     private async void OnPathItemAdded((string sessionId, string clientInstanceId, EncryptedPathItem sharedPathItem) tuple)
// //     {
// //         // todo 050523
// //         throw new NotImplementedException("todo 050523");
// //         
// //         /*
// //         try
// //         {
// //             if (_sessionService.CheckCloudSession(tuple.sessionId))
// //             {
// //                 var dataEncrypter = Locator.Current.GetService<IDataEncrypter>()!;
// //                 var pathItem = dataEncrypter.DecryptPathItem(tuple.sharedPathItem);
// //                 
// //                 await _sessionService.AddPathItem(tuple.clientInstanceId, pathItem);
// //                 
// //                 if (_sessionService.IsLobbyCloudSessionCreatedByMe &&
// //                     _sessionService.RunSessionProfileInfo!.LobbySessionMode.In(LobbySessionModes.RunInventory, LobbySessionModes.RunSynchronization))
// //                 {
// //                     // On doit contrôler si tous les PathItems sont présents, si oui, on peut démarrer l'inventaire
// //
// //                    
// //                     var members = _sessionService.GetAllSessionMembers();
// //                     CloudSessionProfileDetails cloudSessionProfileDetails = 
// //                         (CloudSessionProfileDetails)_sessionService.RunSessionProfileInfo.GetProfileDetails();
// //                     bool allOK = members.Count == cloudSessionProfileDetails.Members.Count;
// //                     if (allOK)
// //                     {
// //                         foreach (var sessionMemberInfo in members)
// //                         {
// //                             var pathItems = _sessionService.GetPathItems(sessionMemberInfo)!
// //                                 .Select(pivm => pivm.PathItem)
// //                                 .ToList();
// //                             
// //                             var expectedPathItems = cloudSessionProfileDetails
// //                                 .Members.Single(m => m.ProfileClientId.Equals(sessionMemberInfo.ProfileClientId))
// //                                 .PathItems.OrderBy(pi => pi.Code)
// //                                 .ToList();
// //
// //                             if (!pathItems.HaveSameContent(expectedPathItems))
// //                             {
// //                                 allOK = false;
// //                             }
// //                         }
// //                     }
// //
// //                     if (allOK)
// //                     {
// //                         await Task.Delay(TimeSpan.FromSeconds(5));
// //                         
// //                         await _dataInventoryStarter.StartDataInventory(false);
// //                     }
// //                 }
// //             }
// //         }
// //         catch (Exception ex)
// //         {
// //             Log.Error(ex, "OnPathItemAdded");
// //         }
// //         
// //         */
// //     }
//
//     // private async void OnPathItemRemoved((string sessionId, string clientInstanceId, EncryptedPathItem sharedPathItem) tuple)
//     // {
//     //     // todo 050523
//     //     throw new NotImplementedException("todo 050523");
//     //     
//     //     /*
//     //     try
//     //     {
//     //         if (_sessionService.CheckCloudSession(tuple.sessionId))
//     //         {
//     //             var dataEncrypter = Locator.Current.GetService<IDataEncrypter>()!;
//     //             
//     //             var pathItem = dataEncrypter.DecryptPathItem(tuple.sharedPathItem);
//     //                 
//     //             await _sessionService.RemovePathItem(tuple.clientInstanceId, pathItem);
//     //         }
//     //     }
//     //     catch (Exception ex)
//     //     {
//     //         Log.Error(ex, "OnPathItemRemoved");
//     //     }
//     //     */
//     // }
//
// //     private async void OnFilePartUploadedByteOtherClient((string sessionId, SharedFileDefinition sharedFileDefinition, int partNumber) tuple)
// //     {
// //         // todo 050523
// //         throw new NotImplementedException("todo 050523");
// //         
// //         /*
// //         
// //         if (tuple.sharedFileDefinition.ClientInstanceId == _connectionManager.ClientInstanceId)
// //         {
// //             // On recevoir un évènement qu'on a lancé en cas d'erreur de connexion
// //             // todo 31/03 : à mieux traiter ultérieurement en se posant des questions pour chacune des méthodes
// //             return;
// //         }
// //
// //         try
// //         {
// //             if (_sessionService.CheckCloudSession(tuple.sessionId))
// //             {
// //                 SharedFileDefinition sharedFileDefinition = tuple.sharedFileDefinition;
// //                 
// //                 if (sharedFileDefinition.IsSynchronization)
// //                 {
// //                     await WaitForSynchronizationDataReady();
// //                 }
// //                 
// //                 var downloadManager = Locator.Current.GetService<IDownloadManager>()!;
// //                 await downloadManager.OnFilePartReadyToDownload(sharedFileDefinition, tuple.partNumber);
// //             }
// //         }
// //         catch (Exception ex)
// //         {
// //             Log.Error(ex, "CloudSessionManager.OnFilePartUploaded sharedFileDefinition.Id : {id}", tuple.sharedFileDefinition.Id);
// //                 
// //             if (tuple.sharedFileDefinition.IsSynchronization)
// //             {
// //                 await _connectionManager.HubWrapper
// //                     .AssertSynchronizationActionError(tuple.sessionId, tuple.sharedFileDefinition);
// //             }
// //             else if (tuple.sharedFileDefinition.IsInventory)
// //             {
// //                 _inventoriesService.InventoryProcessData.InventoryTransferError.OnNext(true);
// //             }
// //         }
// //         */
// //     }
//
// //     private async void OnUploadFinishedByOtherClient((string sessionId, SharedFileDefinition sharedFileDefinition, int partsCount) tuple)
// //     {
// //         // todo 050523
// //         throw new NotImplementedException("todo 050523");
// //         
// //         /*
// //         
// //         if (tuple.sharedFileDefinition.ClientInstanceId == _connectionManager.ClientInstanceId)
// //         {
// //             // On recevoir un évènement qu'on a lancé en cas d'erreur de connexion
// //             // todo 31/03 : à mieux traiter ultérieurement en se posant des questions pour chacune des méthodes
// //             return;
// //         }
// //
// //         try
// //         {
// //             if (_sessionService.CheckCloudSession(tuple.sessionId))
// //             {
// //                 SharedFileDefinition sharedFileDefinition = tuple.sharedFileDefinition;
// //
// //                 if (sharedFileDefinition.IsSynchronization)
// //                 {
// //                     await WaitForSynchronizationDataReady();
// //                 }
// //
// //                 var downloadManager = Locator.Current.GetService<IDownloadManager>()!;
// //                 await downloadManager.OnFileReadyToFinalize(sharedFileDefinition, tuple.partsCount);
// //             }
// //         }
// //         catch (Exception ex)
// //         {
// //             Log.Error(ex, "CloudSessionManager.OnUploadFinished sharedFileDefinition.Id :{id}, uploadedBy:{UploaderClientInstanceId} ", 
// //                 tuple.sharedFileDefinition.Id, tuple.sharedFileDefinition.ClientInstanceId);
// //                 
// //             if (tuple.sharedFileDefinition.IsSynchronization)
// //             {
// //                 await _connectionManager.HubWrapper
// //                     .AssertSynchronizationActionError(tuple.sessionId, tuple.sharedFileDefinition);
// //             }
// //             else if (tuple.sharedFileDefinition.IsInventory)
// //             {
// //                 _inventoriesService.InventoryProcessData.InventoryTransferError.OnNext(true);
// //             }
// //         }
// //         */
// //     }
//
//     // public async Task<bool> WaitForSynchronizationDataReady()
//     // {
//     //     var result = await _sessionService.WaitForSynchronizationDataReadyAsync();
//     //     return result;
//     // }
//
//     /* todo 190523
//     public async Task StartLocalSession(RunLocalSessionProfileInfo? runLocalSessionProfileInfo)
//     {
//         var currentMachineEndpoint = _connectionManager.GetCurrentEndpoint();
//
//         if (currentMachineEndpoint != null)
//         {
//             string sessionId = $"LSID_{Guid.NewGuid()}";
//             LocalSession localSession = new LocalSession(sessionId, currentMachineEndpoint.ClientInstanceId);
//
//             var cloudSessionSettings = SessionSettings.BuildDefault();
//                 
//             await _sessionService.SetLocalSession(localSession, runLocalSessionProfileInfo, cloudSessionSettings);
//                 
//             Log.Information("Starting Local Session {SessionId}", sessionId);
//                 
//             _navigationEventsHub.RaiseNavigateToLocalSynchronizationRequested();
//         }
//     }
//     */
//
//     // public async Task ResetSession()
//     // {
//     //     var session = _sessionService.CurrentSession;
//     //
//     //     if (session != null)
//     //     {
//     //         Log.Information("Restarting session {SessionId}", session.SessionId);
//     //         
//     //         try
//     //         {
//     //             if (session is CloudSession)
//     //             {
//     //                 await _connectionManager.HubWrapper.ResetSession(session.SessionId);
//     //                 
//     //                 await _cloudSessionLocalDataManager.BackupCurrentSessionFiles();
//     //                 
//     //                 await _sessionService.ResetSession();
//     //             }
//     //             else if (session is LocalSession)
//     //             {
//     //                 await _cloudSessionLocalDataManager.BackupCurrentSessionFiles();
//     //                 
//     //                 await _sessionService.ResetSession();
//     //             }
//     //         }
//     //         catch (Exception ex)
//     //         {
//     //             Log.Error(ex, "CloudSessionManager.ResetSession");
//     //         }
//     //     }
//     //     else
//     //     {
//     //         Log.Error("CloudSessionManager.ResetSession: unknown session");
//     //     }
//     // }
//
//     // private async void OnSynchronizationStarted((string sessionId, string startedBy) tuple)
//     // {
//     //     try
//     //     {
//     //         if (_sessionDataHolder.CheckCloudSession(tuple.sessionId))
//     //         {
//     //             Log.Information("The Data Synchronization has been started by another client ({@StartedBy}). Retrieving data...", tuple.startedBy);
//     //
//     //             var synchronizationStart = await _connectionManager.HttpWrapper.GetSynchronizationStart(tuple.sessionId);
//     //
//     //             if (synchronizationStart == null)
//     //             {
//     //                 // todo log
//     //                 return;
//     //             }
//     //
//     //             await WaitForSynchronizationDataReady();
//     //
//     //             await _sessionDataHolder.SetSynchronizationStarted(synchronizationStart);
//     //
//     //             var actionsGroupDefinitions = new List<ActionsGroupDefinition>();
//     //             foreach (var sharedActionsGroup in _sessionDataHolder.SharedActionsGroups!)
//     //             {
//     //                 actionsGroupDefinitions.Add(sharedActionsGroup.GetDefinition());
//     //             }
//     //             
//     //             var synchronizationProgress =
//     //                 MiscBuilder.BuildSynchronizationProgress(_sessionDataHolder.Session!, actionsGroupDefinitions, synchronizationStart);
//     //             
//     //             var synchronizationManager = Locator.Current.GetService<ISynchronizationManager>()!;
//     //             await synchronizationManager.InitializeData();
//     //             await synchronizationManager.CloudSessionSynchronizationLoop(_sessionDataHolder.SharedActionsGroups!, synchronizationProgress);
//     //         }
//     //         else
//     //         {
//     //             Log.Warning("OnSynchronizationStarted: unknown session ({@Session})", tuple.sessionId);
//     //         }
//     //     }
//     //     catch (Exception ex)
//     //     {
//     //         Log.Error(ex, "OnSynchronizationStarted");
//     //     }
//     // }
//         
//     // private void OnSynchronizationAbortRequested(SynchronizationAbortRequest synchronizationAbortRequest)
//     // {
//     //     try
//     //     {
//     //         if (_sessionDataHolder.CheckCloudSession(synchronizationAbortRequest.SessionId))
//     //         {
//     //             Log.Information("Synchronization cancellation requested. Launched by client {@AbortedBy}", synchronizationAbortRequest.RequestedBy);
//     //
//     //             _sessionDataHolder.SetSynchronizationAbortRequest(synchronizationAbortRequest);
//     //
//     //
//     //             // _eventAggregator.GetEvent<SynchronizationStarted>().Publish(synchronizationStart);
//     //         }
//     //         else
//     //         {
//     //             Log.Warning("OnSynchronizationAbortRequested: unknown session {@Session}", synchronizationAbortRequest.SessionId);
//     //         }
//     //     }
//     //     catch (Exception ex)
//     //     {
//     //         Log.Error(ex, "OnSynchronizationAbortRequested");
//     //     }
//     // }
//         
//     // private void OnSynchronizationEnded(SynchronizationEnd synchronizationEnd)
//     // {
//     //     try
//     //     {
//     //         if (_sessionDataHolder.CheckCloudSession(synchronizationEnd.SessionId))
//     //         {
//     //             Log.Information("The Data Synchronization is now complete");
//     //
//     //             _sessionDataHolder.SetSynchronizationEnded(synchronizationEnd);
//     //         }
//     //         else
//     //         {
//     //             Log.Warning("OnSynchronizationEnded: unknown session {@Session}", synchronizationEnd.SessionId);
//     //         }
//     //     }
//     //     catch (Exception ex)
//     //     {
//     //         Log.Error(ex, "OnSynchronizationEnded");
//     //     }
//     // }
//
//     private void OnLocalInventoryGlobalStatusChanged(SetLocalInventoryStatusParameters parameters)
//     {
//         // todo 050523
//         throw new NotImplementedException("todo 050523");
//         
//         /*
//         try
//         {
//             if (_sessionService.CheckCloudSession(parameters.SessionId))
//             {
//                 bool isSet = _inventoriesService.HandleLocalInventoryGlobalStatusChanged(parameters);
//
//                 if (isSet)
//                 {
//                     var member = _sessionService.GetSessionMember(parameters.ClientInstanceId);
//                     _cloudSessionEventsHub.RaiseLocalInventoryGlobalStatusChanged(member!.Endpoint, false, parameters.LocalInventoryGlobalStatus, null);
//                 }
//             }
//         }
//         catch (Exception ex)
//         {
//             Log.Error(ex, "OnLocalInventoryStatusChanged");
//         }*/
//     }
//
//     private void CloudSessionEventsHub_OnSynchronizationRulesApplied(object? sender, EventArgs e)
//     {
//         _ = TryStartLobbyCloudSessionSynchronization();
//     }
//
//     private async Task TryStartLobbyCloudSessionSynchronization()
//     {
//         // todo 040423
//         /*
//         if (_sessionDataHolder.IsLobbyCloudSessionCreatedByMe && _sessionDataHolder.IsInventoriesComparisonDone &&
//             _sessionDataHolder.RunSessionProfileInfo!.LobbySessionMode.In(LobbySessionModes.RunSynchronization) &&
//             ! _sessionDataHolder.IsSynchronizationRunning && ! _sessionDataHolder.IsSynchronizationEnded)
//         {
//             await Task.Delay(TimeSpan.FromSeconds(5));
//
//             if (_sessionDataHolder.IsSynchronizationRunning || _sessionDataHolder.IsSynchronizationEnded)
//             {
//                 return;
//             }
//
//             var otherSessionMembers = _sessionDataHolder.GetOtherSessionMembers();
//             bool allInventoriesOK = _inventoriesService.LocalInventoryGlobalStatus == LocalInventoryGlobalStatus.Finished &&
//                 otherSessionMembers!.Select(m => m.LocalInventoryGlobalStatus)
//                 .All(s => s == LocalInventoryGlobalStatus.Finished);
//
//             if (allInventoriesOK)
//             {
//                 var synchronizationManager = Locator.Current.GetService<ISynchronizationManager>()!;
//                 await synchronizationManager.StartSynchronization(false);
//             }
//             else
//             {
//                 Log.Error("Unable to start the Data Synchronization automatically because one of the Local Inventories has failed");
//             }
//         }*/
//     }
//
//     // public async Task OnFileIsFullyDownloaded(LocalSharedFile localSharedFile)
//     // {
//     //     if (localSharedFile.SharedFileDefinition.IsSynchronizationStartData)
//     //     {
//     //         string synchronizationDataPath = _cloudSessionLocalDataManager.GetSynchronizationStartDataPath();
//     //         var synchronizationDataSaver = new SynchronizationDataSaver();
//     //         var synchronizationData = synchronizationDataSaver.Load(synchronizationDataPath);
//     //
//     //         await _sessionDataHolder.SetSynchronizationStartData(synchronizationData);
//     //         
//     //         // Log.Information("Synchronization actions retrieved and loaded: {Count}", 
//     //         //     synchronizationData.SharedActionsGroups.Count);
//     //     }
//     //     else if (localSharedFile.SharedFileDefinition.IsInventory)
//     //     {
//     //         await _inventoriesService.OnFileIsFullyDownloaded(localSharedFile);
//     //         // await _sessionDataHolder.OnFileIsFullyDownloaded(localSharedFile);
//     //
//     //         //await TryStartLobbyCloudSessionSynchronization();
//     //     }
//     // }
// }