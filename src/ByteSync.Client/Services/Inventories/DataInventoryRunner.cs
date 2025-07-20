using System.Reactive.Linq;
using ByteSync.Business.Inventories;
using ByteSync.Business.Misc;
using ByteSync.Business.Sessions;
using ByteSync.Common.Business.Sessions;
using ByteSync.Interfaces.Controls.Inventories;
using ByteSync.Interfaces.Controls.TimeTracking;
using ByteSync.Interfaces.Factories;
using ByteSync.Interfaces.Repositories;
using ByteSync.Interfaces.Services.Sessions;

namespace ByteSync.Services.Inventories;

public class DataInventoryRunner : IDataInventoryRunner
{
    private readonly ISessionService _sessionService;
    private readonly IInventoryService _inventoryService;
    private readonly ITimeTrackingCache _timeTrackingCache;
    private readonly ISessionMemberService _sessionMemberService;
    private readonly IInventoryBuilderFactory _inventoryBuilderFactory;
    private readonly IBaseInventoryRunner _baseInventoryRunner;
    private readonly IFullInventoryRunner _fullInventoryRunner;
    private readonly IDataNodeRepository _dataNodeRepository;
    private readonly ILogger<DataInventoryRunner> _logger;

    public DataInventoryRunner(ISessionService sessionService, IInventoryService inventoryService, ITimeTrackingCache timeTrackingCache, 
        ISessionMemberService sessionMemberService, IInventoryBuilderFactory inventoryBuilderFactory, IBaseInventoryRunner baseInventoryRunner, 
        IFullInventoryRunner fullInventoryRunner, IDataNodeRepository dataNodeRepository, ILogger<DataInventoryRunner> logger)
    {
        _sessionService = sessionService;
        _inventoryService = inventoryService;
        _timeTrackingCache = timeTrackingCache;
        _sessionMemberService = sessionMemberService;
        _inventoryBuilderFactory = inventoryBuilderFactory;
        _baseInventoryRunner = baseInventoryRunner;
        _fullInventoryRunner = fullInventoryRunner;
        _dataNodeRepository = dataNodeRepository;
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

    private InventoryProcessData InventoryProcessData => _inventoryService.InventoryProcessData;

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

        await _baseInventoryRunner.RunBaseInventory();
        
        var baseInventoryResult = await Observable.CombineLatest(InventoryProcessData.AreBaseInventoriesComplete, 
                InventoryProcessData.InventoryAbortionRequested, InventoryProcessData.ErrorEvent, InventoryProcessData.InventoryTransferError, 
                _sessionService.SessionEnded)
            .Where(list => list.Any(e => e is true))
            .FirstAsync()
            .Select(list => (IsOK: list[0], IsCancellationRequested: list[1], IsInventoryError: list[2] || list[3],
                Details: list));
        
        if (!baseInventoryResult.IsOK)
        {
            await HandleInventoryProblem(baseInventoryResult);
            return;
        }


        await _fullInventoryRunner.RunFullInventory();
        
        var fullInventoryResult = await Observable.CombineLatest(InventoryProcessData.AreFullInventoriesComplete, 
                InventoryProcessData.InventoryAbortionRequested, InventoryProcessData.ErrorEvent, InventoryProcessData.InventoryTransferError, 
                _sessionService.SessionEnded)
            .Where(list => list.Any(e => e is true))
            .FirstAsync()
            .Select(list => (IsOK: list[0], IsCancellationRequested: list[1], IsInventoryError: list[2] || list[3],
                Details: list));

        if (!fullInventoryResult.IsOK)
        {
            await HandleInventoryProblem(baseInventoryResult);
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

    private async Task<bool> StartDataInventoryInitialization()
    {
        await _sessionMemberService.UpdateCurrentMemberGeneralStatus(SessionMemberGeneralStatus.InventoryRunningIdentification);
        
        try
        {
            InventoryProcessData.MainStatus.OnNext(LocalInventoryPartStatus.Running);
            InventoryProcessData.IdentificationStatus.OnNext(LocalInventoryPartStatus.Running);
            InventoryProcessData.AnalysisStatus.OnNext(LocalInventoryPartStatus.Pending);
            
            var localDataNodes = _dataNodeRepository.SortedCurrentMemberDataNodes
                .OrderBy(n => n.OrderIndex)
                .ToList();
            
            var inventoryBuilders = new List<IInventoryBuilder>();
            
            foreach (var dataNode in localDataNodes)
            {
                try
                {
                    var inventoryBuilder = _inventoryBuilderFactory.CreateInventoryBuilder(dataNode);
                    inventoryBuilders.Add(inventoryBuilder);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to create inventory builder for DataNode {NodeId}", dataNode.NodeId);
                    // Continue with other DataNodes as requested
                }
            }
            
            InventoryProcessData.InventoryBuilders = inventoryBuilders;

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "StartDataInventoryInitialization");

            InventoryProcessData.LastException = ex;
            
            await _sessionMemberService.UpdateCurrentMemberGeneralStatus(SessionMemberGeneralStatus.InventoryError);

            return false;
        }
    }
}