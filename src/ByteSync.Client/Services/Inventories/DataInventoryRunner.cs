﻿using System.Reactive.Linq;
using System.Threading.Tasks;
using ByteSync.Business;
using ByteSync.Business.Inventories;
using ByteSync.Business.Misc;
using ByteSync.Business.PathItems;
using ByteSync.Business.SessionMembers;
using ByteSync.Business.Sessions;
using ByteSync.Common.Business.EndPoints;
using ByteSync.Common.Business.Sessions;
using ByteSync.Common.Business.Sessions.Cloud;
using ByteSync.Common.Business.SharedFiles;
using ByteSync.Common.Helpers;
using ByteSync.Interfaces.Controls.Communications;
using ByteSync.Interfaces.Controls.Inventories;
using ByteSync.Interfaces.Controls.Sessions;
using ByteSync.Interfaces.Controls.TimeTracking;
using ByteSync.Interfaces.Factories;
using ByteSync.Interfaces.Repositories;
using ByteSync.Models.Inventories;
using ByteSync.Services.Comparisons;
using ByteSync.Services.Misc;

namespace ByteSync.Services.Inventories;

public class DataInventoryRunner : IDataInventoryRunner
{
    private readonly ISessionService _sessionService;
    private readonly ICloudSessionLocalDataManager _cloudSessionLocalDataManager;
    private readonly ICloudProxy _connectionManager;
    private readonly IInventoryService _inventoryService;
    private readonly IInventoryFileRepository _inventoryFileRepository;
    private readonly ISessionMemberRepository _sessionMemberRepository;
    private readonly IFileUploaderFactory _fileUploaderFactory;
    private readonly ITimeTrackingCache _timeTrackingCache;
    private readonly IPathItemRepository _pathItemRepository;
    private readonly ISessionMemberService _sessionMemberService;
    private readonly ILogger<DataInventoryRunner> _logger;

    public DataInventoryRunner(ISessionService sessionService, ICloudSessionLocalDataManager cloudSessionLocalDataManager,
        ICloudProxy connectionManager, IInventoryService inventoryService, IInventoryFileRepository inventoryFileRepository, 
        ISessionMemberRepository sessionMemberRepository, IFileUploaderFactory fileUploaderFactory, ITimeTrackingCache timeTrackingCache, 
        IPathItemRepository pathItemRepository, ISessionMemberService sessionMemberService, ILogger<DataInventoryRunner> logger)
    {
        _sessionService = sessionService;
        _cloudSessionLocalDataManager = cloudSessionLocalDataManager;
        _connectionManager = connectionManager;
        _inventoryService = inventoryService;
        _inventoryFileRepository = inventoryFileRepository;
        _sessionMemberRepository = sessionMemberRepository;
        _fileUploaderFactory = fileUploaderFactory;
        _timeTrackingCache = timeTrackingCache;
        _pathItemRepository = pathItemRepository;
        _sessionMemberService = sessionMemberService;
        _logger = logger;
        
        InventoryProcessData.MainStatus.DistinctUntilChanged()
            .Where(status => status != LocalInventoryPartStatus.Running)
            .Subscribe(_ => StopRemainingTimeComputer());

        _sessionService.SessionStatusObservable.DistinctUntilChanged()
            .Where(ss => ss.In(SessionStatus.Preparation))
            .Subscribe(_ => StopRemainingTimeComputer());
    }

    private void StopRemainingTimeComputer()
    {
        var sessionId = _sessionService.SessionId;

        if (sessionId != null)
        {
            var timeTrackingComputer = _timeTrackingCache
                .GetTimeTrackingComputer(sessionId, TimeTrackingComputerType.Inventory)
                .Result;
            timeTrackingComputer.Stop();
        }
    }

    private InventoryProcessData InventoryProcessData
    {
        get
        {
            return _inventoryService.InventoryProcessData;
        }
    }

    private string SessionId
    {
        get
        {
            return _sessionService.SessionId!;
        }
    }

    public async Task RunDataInventory()
    {
        await Task.Run(DoRunDataInventory);
    }

    private async Task DoRunDataInventory()
    {
        await _sessionService.SetSessionStatus(SessionStatus.Inventory);
        
        InventoryProcessData.InventoryStart = DateTimeOffset.Now;

        var isOK = await StartDataInventoryInitialization();
        if (!isOK)
        {
            await HandleInventoryError();
            
            return;
        }

        var timeTrackingComputer = await _timeTrackingCache
            .GetTimeTrackingComputer(_sessionService.SessionId!, TimeTrackingComputerType.Inventory);
        timeTrackingComputer.Start(InventoryProcessData.InventoryStart);

        // var inventories = InventoryBuilders.Select(ib => ib.Inventory!).ToList();

        // S'occupe de réaliser l'inventaire initial et de gérer le traitement post-inventaire dont upload
        // AllInventoriesReadyEvent.Reset();

        isOK = await RunBaseInventory();
        
        var baseInventoryResult = await Observable.CombineLatest(InventoryProcessData.AreBaseInventoriesComplete, 
                InventoryProcessData.InventoryAbortionRequested, InventoryProcessData.ErrorEvent, InventoryProcessData.InventoryTransferError, 
                _sessionService.SessionEnded)
            .Where(list => list.Any(e => e is true))
            .FirstAsync()
            .Select(list => (IsOK: list[0], IsCancellationRequested: list[1], IsInventoryError: list[2] || list[3],
                Details: list));
        
        // InventoryProcessData.AreBaseInventoriesComplete.CombineLatest(InventoryProcessData.InventoryAbortionRequested)

        // Attendre que tous les autres inventaires soit downloadés
        // mre.WaitOne();
        // int result = WaitHandle.WaitAny(new WaitHandle[] {InventoryProcessData.AllBaseInventoriesCompleteEvent, InventoryProcessData.InventoryAbortionRequested, 
        //     InventoryProcessData.ErrorEvent, 
        //     _sessionDataHolder.GetInventoryTransferErrorEvent(), _sessionDataHolder.GetSessionEndedEvent()});
        //
        // if (result == 3)
        // {
        //     isOK = false;
        // }

        if (baseInventoryResult.IsOK)
        {
            // InventoryProcessData.IdentificationStatus.OnNext(LocalInventoryPartStatus.Success);
        }
        else
        {
            await HandleInventoryProblem(baseInventoryResult);
            
            return;
        }

        isOK = await RunFullInventory();
        
        var fullInventoryResult = await Observable.CombineLatest(InventoryProcessData.AreFullInventoriesComplete, 
                InventoryProcessData.InventoryAbortionRequested, InventoryProcessData.ErrorEvent, InventoryProcessData.InventoryTransferError, 
                _sessionService.SessionEnded)
            .Where(list => list.Any(e => e is true))
            .FirstAsync()
            .Select(list => (IsOK: list[0], IsCancellationRequested: list[1], IsInventoryError: list[2] || list[3],
                Details: list));

        if (fullInventoryResult.IsOK)
        {
            
            
            // await SetLocalInventoryFinished(InventoryProcessData.Inventories!, LocalInventoryModes.Full);
        }
        else
        {
            await HandleInventoryProblem(baseInventoryResult);
            
            return;
        }
    }

    private async Task HandleInventoryProblem((bool IsOK, bool IsCancellationRequested, bool IsInventoryError, IList<bool> Details) inventoryResult)
    {
        if (inventoryResult.IsCancellationRequested)
        {
            await HandleInventoryCancelled();
        }
        else
        {
            await HandleInventoryError();
        }
    }

    private async Task HandleInventoryCancelled()
    {
        await _sessionMemberService.UpdateCurrentMemberGeneralStatus(SessionMemberGeneralStatus.InventoryCancelled);

        InventoryProcessData.MainStatus.OnNext(LocalInventoryPartStatus.Cancelled);
    }

    private async Task HandleInventoryError()
    {
        await _sessionMemberService.UpdateCurrentMemberGeneralStatus(SessionMemberGeneralStatus.InventoryError);

        InventoryProcessData.MainStatus.OnNext(LocalInventoryPartStatus.Error);
    }

    private async Task<bool> RunBaseInventory()
    {
        bool isOK;
        try
        {
            await Parallel.ForEachAsync(InventoryProcessData.InventoryBuilders!, 
                new ParallelOptions { MaxDegreeOfParallelism = 2, CancellationToken = InventoryProcessData.CancellationTokenSource.Token}, 
                async (builder, token) =>
            {
                var baseInventoryFullName = _cloudSessionLocalDataManager
                    .GetCurrentMachineInventoryPath(builder.InventoryLetter, LocalInventoryModes.Base);

                await builder.BuildBaseInventoryAsync(baseInventoryFullName, token);
            });
            
            // InventoryProcessData.IsIdentificationRunning = false;
            
            
            
            // foreach (var inventoryBuilder in InventoryProcessData.InventoryBuilders)
            // {
            //     string baseInventoryFullName = _cloudSessionLocalDataManager
            //         .GetCurrentMachineInventoryPath(inventoryBuilder.InventoryLetter!, LocalInventoryModes.Base);
            //
            //     await inventoryBuilder.BuildBaseInventoryAsync(baseInventoryFullName);
            //     if (InventoryProcessData.CancellationTokenSource!.Token.IsCancellationRequested)
            //     {
            //         break;
            //     }
            // }

            if (!InventoryProcessData.CancellationTokenSource.Token.IsCancellationRequested)
            {
                InventoryProcessData.IdentificationStatus.OnNext(LocalInventoryPartStatus.Success);
                
                await SetLocalInventoryFinished(InventoryProcessData.Inventories!, LocalInventoryModes.Base);
            }
            
            isOK = true;
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "DoStartInventory:Identification");
            isOK = true;
            // InventoryProcessData.ErrorEvent.Set();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DoStartInventory:Identification");
            isOK = false;

            InventoryProcessData.SetError(ex);
        }

        return isOK;
    }
    
    public async Task SetLocalInventoryFinished(List<Inventory> inventories, LocalInventoryModes localInventoryMode)
    {
        var inventoriesFiles = BuildInventoriesLocalSharedFiles(inventories, localInventoryMode);

        if (inventoriesFiles != null)
        {
            if (_sessionService.CurrentSession is CloudSession)
            {
                foreach (var localSharedFile in inventoriesFiles)
                {
                    var fileUploader = _fileUploaderFactory.Build(localSharedFile.FullName, localSharedFile.SharedFileDefinition);
                    await fileUploader.Upload();
                }
            }

            await _inventoryService.SetLocalInventory(inventoriesFiles, localInventoryMode);

            await _sessionMemberService.UpdateCurrentMemberGeneralStatus(localInventoryMode.ConvertFinishInventory());
        }
    }
    
    private List<InventoryFile>? BuildInventoriesLocalSharedFiles(List<Inventory> inventories, LocalInventoryModes localInventoryMode)
    {
        var session = _sessionService.CurrentSession;
        var endpoint = _connectionManager.CurrentEndPoint;

        if (session == null || endpoint == null)
        {
            return null;
        }

        List<InventoryFile> result = new List<InventoryFile>();
        foreach (var inventory in inventories)
        {
            var inventoryFullName = _cloudSessionLocalDataManager.GetCurrentMachineInventoryPath(inventory.Letter, localInventoryMode);
            
            var sharedFileDefinition = new SharedFileDefinition();

            if (localInventoryMode == LocalInventoryModes.Base)
            {
                sharedFileDefinition.SharedFileType = SharedFileTypes.BaseInventory;
            }
            else
            {
                sharedFileDefinition.SharedFileType = SharedFileTypes.FullInventory;
            }
            sharedFileDefinition.ClientInstanceId = endpoint.ClientInstanceId;
            sharedFileDefinition.SessionId = session.SessionId;
            sharedFileDefinition.AdditionalName = inventory.Letter;

            var inventoryFile = new InventoryFile(sharedFileDefinition, inventoryFullName);

            result.Add(inventoryFile);
        }

        return result;
    }
    
    private async Task<bool> RunFullInventory()
    {
        bool isOK;
        
        try
        {
            // InventoryProcessData.IsAnalysisRunning = true;
            // InventoryProcessData.HasAnalysisStarted = true;
            InventoryProcessData.AnalysisStatus.OnNext(LocalInventoryPartStatus.Running);

            var inventoriesBuildersAndItems = new List<Tuple<InventoryBuilder, HashSet<IndexedItem>>>();
            foreach (var inventoryBuilder in InventoryProcessData.InventoryBuilders!)
            {
                var inventoriesFiles = _inventoryFileRepository.GetAllInventoriesFiles(LocalInventoryModes.Base);
                using var inventoryComparer = new InventoryComparer(_sessionService.CurrentSessionSettings!);
                inventoryComparer.Indexer = inventoryBuilder.Indexer;
                inventoryComparer.AddInventories(inventoriesFiles);
                var comparisonResult = inventoryComparer.Compare();
                
                var filesIdentifier = new FilesIdentifier(inventoryBuilder.Inventory, inventoryBuilder.SessionSettings!, inventoryBuilder.Indexer);
                HashSet<IndexedItem> items = filesIdentifier.Identify(comparisonResult);
                InventoryProcessData.UpdateMonitorData(monitorData => monitorData.AnalyzableFiles += items.Count);
                
                inventoriesBuildersAndItems.Add(new (inventoryBuilder, items));
            }

            foreach (var tuple in inventoriesBuildersAndItems)
            {
                var inventoryBuilder = tuple.Item1;
                var items = tuple.Item2;

                var fullInventoryFullName = _cloudSessionLocalDataManager.GetCurrentMachineInventoryPath(inventoryBuilder.InventoryLetter, LocalInventoryModes.Full);
                await inventoryBuilder.RunAnalysisAsync(fullInventoryFullName, items, InventoryProcessData.CancellationTokenSource.Token);
            }
            
            if (!InventoryProcessData.CancellationTokenSource.Token.IsCancellationRequested)
            {
                InventoryProcessData.AnalysisStatus.OnNext(LocalInventoryPartStatus.Success);
                InventoryProcessData.MainStatus.OnNext(LocalInventoryPartStatus.Success);
                
                await SetLocalInventoryFinished(InventoryProcessData.Inventories!, LocalInventoryModes.Full);
            }
            
            isOK = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DoStartInventory:Analysis");
            isOK = false;
            
            InventoryProcessData.SetError(ex);
        }

        return isOK;
    }

    // private void OnInventoryCancelOrErrorDuringAnalysis()
    // {
    //     if (InventoryProcessData.CancellationTokenSource.IsCancellationRequested)
    //     {
    //         InventoryProcessData.MainStatus.OnNext(InventoryProcessStatuses.Cancelled);
    //         InventoryProcessData.AnalysisStatus.OnNext(InventoryProcessStatuses.Cancelled);
    //     }
    //     else 
    //     {
    //         InventoryProcessData.MainStatus.OnNext(InventoryProcessStatuses.Error);
    //         InventoryProcessData.AnalysisStatus.OnNext(InventoryProcessStatuses.Error);
    //     }
    //         
    //     if (InventoryProcessData.CancellationTokenSource.IsCancellationRequested)
    //     {
    //         _sessionDataHolder.SetLocalInventoryMainStatus(SessionId, LocalInventoryStatuses.InventoryCancelled);
    //     }
    //     else
    //     {
    //         _sessionDataHolder.SetLocalInventoryMainStatus(SessionId, LocalInventoryStatuses.InventoryError);
    //     }
    // }

    // private async Task OnInventoryCancelOrErrorDuringIdentification(bool isError)
    // {
    //     if (InventoryProcessData.CancellationTokenSource.Token.IsCancellationRequested)
    //     {
    //         InventoryProcessData.MainStatus.OnNext(InventoryProcessStatuses.Cancelled);
    //         InventoryProcessData.IdentificationStatus.OnNext(InventoryProcessStatuses.Cancelled); 
    //         InventoryProcessData.AnalysisStatus.OnNext(InventoryProcessStatuses.NotLaunched);
    //     }
    //     else
    //     {
    //         InventoryProcessData.MainStatus.OnNext(InventoryProcessStatuses.Error);
    //         InventoryProcessData.IdentificationStatus.OnNext(InventoryProcessStatuses.Error);
    //         InventoryProcessData.AnalysisStatus.OnNext(InventoryProcessStatuses.NotLaunched);
    //     }
    //
    //     if (isError)
    //     {
    //         await _sessionDataHolder.SetLocalInventoryMainStatus(SessionId, LocalInventoryStatuses.InventoryError);
    //     }
    //     else
    //     {
    //         await _sessionDataHolder.SetLocalInventoryMainStatus(SessionId, LocalInventoryStatuses.InventoryCancelled);
    //     }
    //
    //     OnInventoryEnd();
    // }

    private async Task<bool> StartDataInventoryInitialization()
    {
        await _sessionMemberService.UpdateCurrentMemberGeneralStatus(SessionMemberGeneralStatus.InventoryRunningIdentification);
        
        try
        {
            InventoryProcessData.MainStatus.OnNext(LocalInventoryPartStatus.Running);
            InventoryProcessData.IdentificationStatus.OnNext(LocalInventoryPartStatus.Running);
            InventoryProcessData.AnalysisStatus.OnNext(LocalInventoryPartStatus.Pending);

            SessionMemberInfo? currentSessionMemberInfo = null;
            if (_sessionService.IsCloudSession)
            {
                currentSessionMemberInfo = _sessionMemberRepository.GetCurrentSessionMember();
            }

            var endpoint = _connectionManager.CurrentEndPoint;
            if (endpoint == null)
            {
                throw new Exception("Current endpoint is null!");
            }

            // InventoryProcessData.HasInventoryStarted = true;
            // InventoryProcessData.IsInventoryRunning = true;
            // InventoryProcessData.IsIdentificationRunning = true;
            // InventoryProcessData.IsAnalysisRunning = false;
            // InventoryProcessData.HasAnalysisStarted = false;
            
            List<InventoryBuilder> inventoryBuilders = new List<InventoryBuilder>();
            var myPathItems = _pathItemRepository.CurrentMemberPathItems.Items.ToList();
            if (_sessionService.IsCloudSession)
            {
                _logger.LogInformation("Local Inventory parts:");
                foreach (var pathItem in myPathItems)
                {
                    _logger.LogInformation(" - {@letter:l}: {@path} ({type})", pathItem.Code, pathItem.Path, pathItem.Type);
                }

                var inventoryBuilder = BuildInventoryBuilder(currentSessionMemberInfo, endpoint,
                    myPathItems.ToList());
                inventoryBuilders.Add(inventoryBuilder);
            }
            // else
            // {
            //     _logger.LogInformation("Inventory parts:");
            //     var letter = 'A';
            //     foreach (var pathItem in myPathItems)
            //     {
            //         _logger.LogInformation(" - {@letter:l}: {@path} ({type})", pathItem.Code, pathItem.Path, pathItem.Type);
            //
            //         var inventoryBuilder = BuildInventoryBuilder(letter.ToString(), endpoint, new List<PathItem> { pathItem });
            //         inventoryBuilders.Add(inventoryBuilder);
            //
            //         letter = (char)(letter + 1);
            //     }
            // }
            
            InventoryProcessData.InventoryBuilders = inventoryBuilders;

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DoStartInventory:StartInventoryInitialize");

            InventoryProcessData.LastException = ex;
            
            await _sessionMemberService.UpdateCurrentMemberGeneralStatus(SessionMemberGeneralStatus.InventoryError);

            return false;
        }
    }
    
    private InventoryBuilder BuildInventoryBuilder(SessionMemberInfo sessionMemberInfo, ByteSyncEndpoint endpoint, ICollection<PathItem> pathItems)
    {
        var cloudSessionSettings = _sessionService.CurrentSessionSettings!;
        
        var inventoryBuilder = new InventoryBuilder(sessionMemberInfo.Letter, cloudSessionSettings, InventoryProcessData, 
            endpoint, sessionMemberInfo.MachineName, FingerprintModes.Rsync);
        foreach (var pathItem in pathItems)
        {
            inventoryBuilder.AddInventoryPart(pathItem);
        }

        return inventoryBuilder;
    }
    
    // private void OnInventoryEnd()
    // {
    //     //
    //     // InventoryProcessData.IsIdentificationRunning = false;
    //     // InventoryProcessData.IsAnalysisRunning = false;
    //     // InventoryProcessData.IsInventoryRunning = false;
    // }
}