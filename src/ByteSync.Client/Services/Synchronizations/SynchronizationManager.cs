/*
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ByteSync.Business.Actions.Shared;
using ByteSync.Business.Arguments;
using ByteSync.Business.Communications;
using ByteSync.Controls.Actions;
using ByteSync.Controls.Profiles;
using ByteSync.Interfaces;
using ByteSync.Interfaces.Communications;
using ByteSync.Interfaces.Controls.Actions;
using ByteSync.Interfaces.Controls.Sessions;
using ByteSync.Interfaces.Controls.Synchronizations;
using ByteSync.Interfaces.EventsHubs;
using ByteSyncCommon.Business.Actions;
using ByteSyncCommon.Business.EndPoints;
using ByteSyncCommon.Business.Sessions.Cloud;
using ByteSyncCommon.Business.Sessions.Local;
using ByteSyncCommon.Business.SharedFiles;
using ByteSyncCommon.Business.Synchronizations;
using ByteSyncCommon.Business.Synchronizations.Full;
using ByteSyncCommon.Business.Synchronizations.Light;
using ByteSyncCommon.Controls.Synchronizations;
using PowSoftware.Common.Helpers;
using Serilog;
using Splat;

namespace ByteSync.Controls.Synchronizations;

public class SynchronizationManager : ISynchronizationManager
{
    private readonly ISessionDataHolder _sessionDataHolder;
    private readonly IConnectionManager _connectionManager;
    private readonly ICloudSessionLocalDataManager _cloudSessionLocalDataManager;
    private readonly ICloudSessionEventsHub _cloudSessionEventsHub;
    private readonly ISynchronizationActionHandler _synchronizationActionHandler;
    private readonly ISynchronizationService _synchronizationService;

    internal SynchronizationManager(ISessionDataHolder? sessionDataHolder = null, IConnectionManager? connectionManager = null,
        ICloudSessionLocalDataManager? cloudSessionLocalDataManager = null,
        ICloudSessionEventsHub? cloudSessionEventsHub = null, ISynchronizationActionHandler? synchronizationActionHandler = null,
        ISynchronizationService? synchronizationService = null)
    {
        _sessionDataHolder = sessionDataHolder ?? Locator.Current.GetService<ISessionDataHolder>()!;
        _connectionManager = connectionManager ?? Locator.Current.GetService<IConnectionManager>()!;
        _cloudSessionLocalDataManager = cloudSessionLocalDataManager ?? Locator.Current.GetService<ICloudSessionLocalDataManager>()!;
        _cloudSessionEventsHub = cloudSessionEventsHub ?? Locator.Current.GetService<ICloudSessionEventsHub>()!;
        _synchronizationActionHandler = synchronizationActionHandler ?? Locator.Current.GetService<ISynchronizationActionHandler>()!;
        _synchronizationService = synchronizationService ?? Locator.Current.GetService<ISynchronizationService>()!;

        _cloudSessionEventsHub.SessionResetted += (_, _) =>
        {
            IsLocalSynchronizationAbortRequested = true;
        };
    }
    
    public bool IsLocalSynchronizationAbortRequested { get; private set; }
    
    public ByteSyncEndpoint CurrentMachineEndpoint { get; private set; } = null!;

    public CloudSession? CloudSession { get; private set; }
    
    public LocalSession? LocalSession { get; private set; }
    
    // public async Task StartSynchronization(bool isLaunchedByUser)
    // {
    //     await InitializeData();
    //
    //     if (CloudSession != null)
    //     {
    //         if (isLaunchedByUser)
    //         {
    //             Log.Information("The current user has requested to start the Data Synchronization");
    //         }
    //         else
    //         {
    //             Log.Information("The Data Synchronization has been automatically started");
    //         }
    //     }
    //     else
    //     {
    //         Log.Information("The Data Synchronization has started");
    //     }
    //
    //     if (CloudSession != null)
    //     {
    //         await Task.Run(StartCloudSessionSynchronization);
    //     }
    //     else if (LocalSession != null)
    //     {
    //         await Task.Run(StartLocalSessionSynchronization);
    //     }
    //     else
    //     {
    //         throw new ApplicationException("Unable to start synchronization");
    //     }
    // }
    //
    // public Task InitializeData()
    // {
    //     var session = _sessionDataHolder.Session;
    //     var currentMachineEndpoint = _connectionManager.GetCurrentEndpoint();
    //
    //     if (session == null || currentMachineEndpoint == null)
    //     {
    //         return Task.CompletedTask;
    //     }
    //
    //     CurrentMachineEndpoint = currentMachineEndpoint;
    //     
    //     if (session is CloudSession cloudSession)
    //     {
    //         CloudSession = cloudSession;
    //     }
    //     else if (session is LocalSession localSession)
    //     {
    //         LocalSession = localSession;
    //     }
    //     
    //     return Task.CompletedTask;
    // }

    // private async Task StartCloudSessionSynchronization()
    // {
    //     BuildSynchronizationStartData(out var sharedActionsGroups, out var synchronizationData);
    //
    //     // Log.Information("Starting the Data Synchronization");
    //     
    //     var localSharedFile = BuildSynchronizationStartDataLocalSharedFile(CloudSession!);
    //
    //     var synchronizationDataSaver = new SynchronizationDataSaver();
    //     synchronizationDataSaver.Save(localSharedFile.LocalPath, synchronizationData);
    //     var fileUploader = Locator.Current.GetService<IFileUploader>()!;
    //     await fileUploader.Upload(localSharedFile.LocalPath, localSharedFile.SharedFileDefinition);
    //
    //     var actionsGroupDefinitions = new List<ActionsGroupDefinition>();
    //     foreach (var sharedActionsGroup in sharedActionsGroups)
    //     {
    //         actionsGroupDefinitions.Add(sharedActionsGroup.GetDefinition());
    //     }
    //     
    //     var synchronizationStart = await _connectionManager.HttpWrapper.StartSynchronization(CloudSession!,
    //         actionsGroupDefinitions);
    //
    //     if (synchronizationStart != null)
    //     {
    //         try
    //         {
    //             await _sessionDataHolder.SetSynchronizationStartData(synchronizationData);
    //             await _sessionDataHolder.SetSynchronizationStarted(synchronizationStart);
    //
    //             var synchronizationProgress =
    //                 MiscBuilder.BuildSynchronizationProgress(CloudSession!, actionsGroupDefinitions, synchronizationStart);
    //
    //             await CloudSessionSynchronizationLoop(sharedActionsGroups, synchronizationProgress);
    //         }
    //         catch (Exception ex)
    //         {
    //             Log.Error(ex, "Error during synchronization loop");
    //         }
    //     }
    // }
    // private async Task StartLocalSessionSynchronization()
    // {
    //     try
    //     {
    //         IsLocalSynchronizationAbortRequested = false;
    //
    //         BuildSynchronizationStartData(out var sharedActionsGroups, out var synchronizationData);
    //         
    //         // On traite les fichiers en premier, puis les répertoires
    //         // False en premier == FileSystemTypes.File
    //         // True en second == FileSystemTypes.Directory
    //         sharedActionsGroups = sharedActionsGroups.OrderBy(g => g.IsDirectory).ToList();
    //         
    //         // Log.Information("Starting the Synchronization");
    //
    //         var synchronizationActionDefinitions = new List<ActionsGroupDefinition>();
    //         foreach (var sharedActionsGroup in sharedActionsGroups)
    //         {
    //             synchronizationActionDefinitions.Add(sharedActionsGroup.GetDefinition());
    //         }
    //
    //         var synchronizationStart = MiscBuilder.BuildSynchronizationStart(LocalSession!, CurrentMachineEndpoint);
    //         var synchronizationProgress =
    //             MiscBuilder.BuildSynchronizationProgress(LocalSession!, synchronizationActionDefinitions, synchronizationStart);
    //
    //         await _sessionDataHolder.SetSynchronizationStartData(synchronizationData);
    //         await _sessionDataHolder.SetSynchronizationStarted(synchronizationStart);
    //
    //         await LocalSessionSynchronizationLoop(sharedActionsGroups, synchronizationProgress);
    //         
    //         SynchronizationProgressInfo synchronizationProgressInfo = MiscBuilder.BuildSynchronizationProgressData(synchronizationProgress, null);
    //         _sessionDataHolder.OnSynchronizationProgressChanged(synchronizationProgressInfo);
    //
    //         SynchronizationEnd synchronizationEnd;
    //         if (IsLocalSynchronizationAbortRequested)
    //         {
    //             synchronizationEnd = MiscBuilder.BuildSynchronizationEnd(LocalSession!, SynchronizationEndStatuses.Abortion);
    //         }
    //         else
    //         {
    //             synchronizationEnd = MiscBuilder.BuildSynchronizationEnd(LocalSession!, SynchronizationEndStatuses.Regular);
    //         }
    //
    //         _sessionDataHolder.SetSynchronizationEnded(synchronizationEnd);
    //     }
    //     catch (Exception)
    //     {
    //         var synchronizationEnd = MiscBuilder.BuildSynchronizationEnd(LocalSession!, SynchronizationEndStatuses.Error);
    //         _sessionDataHolder.SetSynchronizationEnded(synchronizationEnd);
    //         
    //         throw;
    //     }
    // }
    
//     public async Task CloudSessionSynchronizationLoop(List<SharedActionsGroup> sharedActionsGroups, SynchronizationProgress synchronizationProgress)
//     {
//         var preparedSharedActionsGroups = PrepareCloudSharedActionsGroups(sharedActionsGroups);
//         
//         foreach (SharedActionsGroup sharedActionsGroup in preparedSharedActionsGroups)
//         {
//             if (_sessionDataHolder.IsSynchronizationAbortRequested)
//             {
//                 break;
//             }
//             
//         #if DEBUG
//             if (DebugArguments.ForceSlow)
//             {
//                 await DebugUtils.DebugTaskDelay(0.5);
//             }
//         #endif
//             
//             var progressAction = synchronizationProgress.GetProgressAction(sharedActionsGroup.ActionsGroupId)!;
//             if (progressAction == null)
//             {
//                 throw new ApplicationException($"progressAction is null for ActionsGroupId {sharedActionsGroup.ActionsGroupId}");
//             }
//             
//             try
//             {
//             #if DEBUG
//                 if (DebugArguments.ForceSlow)
//                 {
//                     await DebugUtils.DebugTaskDelay(0.3);
//                 }
//             #endif
//                 
//                 await _synchronizationActionHandler.RunSynchronizationAction(sharedActionsGroup);
//                 
//                 if (sharedActionsGroup.IsSynchronizeContent)
//                 {
//                     synchronizationProgress.ProcessedVolume += progressAction.Size ?? 0;
//                 }
//             }
//             catch (Exception ex)
//             {
//                 Log.Error(ex, "Synchronization exception");
//             }
//         }
//
//         try
//         {
//             await _synchronizationActionHandler.RunPendingSynchronizationActions();
//         }
//         catch (Exception ex)
//         {
//             Log.Error(ex, "SynchronizationManager.StartLocalSessionSynchronization");
//         }
//         
//         try
//         {
//             // On informe qu'on a terminé la synchro ici
//             await _connectionManager.HttpWrapper.InformSynchronizationIsFinished(CloudSession!);
//         }
//         catch (Exception ex)
//         {
//             Log.Error(ex, "Error while informing server");
//         }
//     }
//
//     private List<SharedActionsGroup> PrepareCloudSharedActionsGroups(List<SharedActionsGroup> sharedActionsGroups)
//     {
//         // On met en premier les actions de synchro de contenu dont je suis la source
//         // En les groupant par targets
//         // Puis on met les autres actions
//         List<SharedActionsGroup> sourceCopyActions = new List<SharedActionsGroup>();
//         List<SharedActionsGroup> otherActions = new List<SharedActionsGroup>();
//         foreach (SharedActionsGroup sharedActionsGroup in sharedActionsGroups)
//         {
//             if (sharedActionsGroup.IsSynchronizeContent &&
//                 sharedActionsGroup.Source != null && sharedActionsGroup.Source.ClientInstanceId.Equals(_connectionManager.ClientInstanceId))
//             {
//                 sourceCopyActions.Add(sharedActionsGroup);
//             }
//             else if (! sharedActionsGroup.IsSynchronizeContent && 
//                      sharedActionsGroup.Targets.Any(t => t.ClientInstanceId.Equals(_connectionManager.ClientInstanceId)))
//             {
//                 otherActions.Add(sharedActionsGroup);
//             }
//         }
//
//         // On groupe par targets, comme ça, lors des uploads, on pourra zipper les fichiers par cibles
//         Dictionary<string, List<SharedActionsGroup>> sourceCopyActionsDictionary = new Dictionary<string, List<SharedActionsGroup>>();
//         foreach (var sharedActionsGroup in sourceCopyActions)
//         {
//
//             string key = sharedActionsGroup.Key;
//
//             if (!sourceCopyActionsDictionary.ContainsKey(key))
//             {
//                 sourceCopyActionsDictionary.Add(key, new List<SharedActionsGroup>());
//             }
//             
//             sourceCopyActionsDictionary[key].Add(sharedActionsGroup);
//         }
//
//         List<SharedActionsGroup> result = new List<SharedActionsGroup>();
//         foreach (var sourceCopySharedActionsGroups in sourceCopyActionsDictionary.Values)
//         {
//             // On trie par taille croissante pour obtenir un meilleur rendement de la compression
//             // On trie aussi par type de synchronisation. Ce n'est pas forcément utile
//             var list = sourceCopySharedActionsGroups
//                 .OrderBy(sag => sag.SynchronizationType)
//                 .ThenBy(sag => sag.Size);
//
//             foreach (var sharedActionsGroup in list)
//             {
//                 result.Add(sharedActionsGroup);
//             }
//         }
//         
//         result.AddAll(otherActions);
//
//         return result;
//     }
//
//     private async Task LocalSessionSynchronizationLoop(List<SharedActionsGroup> sharedActionsGroups, SynchronizationProgress synchronizationProgress)
//     {
//         foreach (SharedActionsGroup sharedActionsGroup in sharedActionsGroups)
//         {
//             if (IsLocalSynchronizationAbortRequested)
//             {
//                 break;
//             }
//
// #if DEBUG
//             if (DebugArguments.ForceSlow)
//             {
//                 await DebugUtils.DebugTaskDelay(0.7d);
//             }
// #endif
//
//
//             var progressAction = synchronizationProgress.GetProgressAction(sharedActionsGroup.ActionsGroupId);
//
//             if (progressAction == null)
//             {
//                 throw new ApplicationException($"progressAction is null for ActionsGroupId {sharedActionsGroup.ActionsGroupId}");
//             }
//
//             try
//             {
//                 //synchronizationProgress.CurrentAction = sharedActionsGroup.GetDefinition();
//                 var synchronizationProgressInfo = MiscBuilder.BuildSynchronizationProgressData(synchronizationProgress, progressAction);
//                 _sessionDataHolder.OnSynchronizationProgressChanged(synchronizationProgressInfo);
//
// #if DEBUG
//                 if (DebugArguments.ForceSlow)
//                 {
//                     await DebugUtils.DebugTaskDelay(2, 3);
//                 }
// #endif
//     
//                 await _synchronizationActionHandler.RunSynchronizationAction(sharedActionsGroup);
//
//                 progressAction.SetSuccess();
//                 if (sharedActionsGroup.IsSynchronizeContent)
//                 {
//                     synchronizationProgress.ProcessedVolume += progressAction.Size ?? 0;
//                 }
//             }
//             catch (Exception ex)
//             {
//                 Log.Error(ex, "SynchronizationManager.StartLocalSessionSynchronization");
//                 progressAction.AddError(CurrentMachineEndpoint.ClientInstanceId);
//             }
//             finally
//             {
//                 var synchronizationProgressInfo = MiscBuilder.BuildSynchronizationProgressData(synchronizationProgress, progressAction);
//                 _sessionDataHolder.OnSynchronizationProgressChanged(synchronizationProgressInfo);
//             }
//         }
//     }

    // private void BuildSynchronizationStartData(out List<SharedActionsGroup> sharedActionsGroups,
    //     out SharedSynchronizationStartData synchronizationStartData)
    // {
    //     List<SharedAtomicAction> sharedAtomicActions =
    //         _sessionDataHolder.SharedAtomicActions?.ToList() ?? new List<SharedAtomicAction>();
    //     
    //     var sharedActionsGroupComputer = Locator.Current.GetService<ISharedActionsGroupComputer>()!;
    //     sharedActionsGroups = sharedActionsGroupComputer.Compute(sharedAtomicActions);
    //
    //     SynchronizationRulesConverter synchronizationRulesConverter = new SynchronizationRulesConverter();
    //     var synchronizationRules = synchronizationRulesConverter
    //         .ConvertLooseSynchronizationRules(_sessionDataHolder.SynchronizationRules!);
    //
    //     string sessionId = CloudSession?.SessionId ?? LocalSession!.SessionId;
    //     synchronizationStartData = new(sessionId, CurrentMachineEndpoint, sharedAtomicActions, sharedActionsGroups, synchronizationRules);
    // }
    //
    //
    //
    // private LocalSharedFile BuildSynchronizationStartDataLocalSharedFile(CloudSession cloudSession)
    // {
    //     string synchronizationDataPath = _cloudSessionLocalDataManager.GetSynchronizationStartDataPath();
    //
    //     SharedFileDefinition sharedFileDefinition = new SharedFileDefinition();
    //     sharedFileDefinition.SharedFileType = SharedFileTypes.SynchronizationStartData;
    //     sharedFileDefinition.ClientInstanceId = CurrentMachineEndpoint.ClientInstanceId;
    //     sharedFileDefinition.SessionId = cloudSession.SessionId;
    //
    //     LocalSharedFile localSharedFile = new LocalSharedFile(sharedFileDefinition, synchronizationDataPath);
    //
    //     return localSharedFile;
    // }
}
*/