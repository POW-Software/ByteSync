using System.Reactive.Linq;
using ByteSync.Business;
using ByteSync.Business.Communications;
using ByteSync.Business.DataNodes;
using ByteSync.Business.Inventories;
using ByteSync.Business.Sessions;
using ByteSync.Common.Business.Sessions.Cloud;
using ByteSync.Interfaces.Controls.Communications.Http;
using ByteSync.Interfaces.Controls.Inventories;
using ByteSync.Interfaces.Repositories;
using ByteSync.Interfaces.Services.Communications;
using ByteSync.Interfaces.Services.Sessions;

namespace ByteSync.Services.Inventories;

public class InventoryService : IInventoryService
{
    private readonly ISessionService _sessionService;
    private readonly IConnectionService _connectionService;
    private readonly IInventoryApiClient _inventoryApiClient;
    private readonly ISessionMemberRepository _sessionMemberRepository;
    private readonly IInventoryFileRepository _inventoryFileRepository;
    private readonly IDataNodeRepository _dataNodeRepository;
    private readonly ILogger<InventoryService> _logger;


    public InventoryService(ISessionService sessionService, IConnectionService connectionService, IInventoryApiClient inventoryApiClient,
        ISessionMemberRepository sessionMemberRepository, IInventoryFileRepository inventoryFileRepository, 
        IDataNodeRepository dataNodeRepository, ILogger<InventoryService> logger)
    {
        _sessionService = sessionService;
        _connectionService = connectionService;
        _inventoryApiClient = inventoryApiClient;
        _sessionMemberRepository = sessionMemberRepository;
        _inventoryFileRepository = inventoryFileRepository;
        _dataNodeRepository = dataNodeRepository;
        _logger = logger;

        InventoryProcessData = new InventoryProcessData();
        
        _sessionService.SessionStatusObservable
            .Where(x => x == SessionStatus.Preparation)
            .Subscribe(_ =>
            {
                InventoryProcessData.Reset();
            });
        
        
        // _sessionService.SessionStatusObservable.DistinctUntilChanged()
        //     .Where(ss => ss.In(SessionStatus.Preparation))
        //     .Subscribe(_ => _remainingTimeComputer.Stop());
        // todo, stopper également si session resettée #WI19
    }

    public InventoryProcessData InventoryProcessData { get; }
    
    public async Task SetLocalInventory(ICollection<InventoryFile> inventoriesFiles, LocalInventoryModes localInventoryMode)
    {
        _inventoryFileRepository.AddOrUpdate(inventoriesFiles);

        await CheckInventoriesReady();
    }
    
    public async Task OnFileIsFullyDownloaded(LocalSharedFile localSharedFile)
    {
        
        
        if (localSharedFile.SharedFileDefinition.IsInventory)
        {
            var inventoryFile = new InventoryFile(localSharedFile);
            
            _inventoryFileRepository.AddOrUpdate(inventoryFile);

            await CheckInventoriesReady();
        }
    }
    
    private async Task CheckInventoriesReady()
    {
        await Task.Run(() =>
        {
            var currentEndPoint = _connectionService.CurrentEndPoint!;
            var inventoriesFilesCache = _inventoryFileRepository.Elements.ToList();
            var allDataNodes = _dataNodeRepository.Elements.ToList();

            // Get all DataNodes from other session members
            var otherDataNodes = allDataNodes
                .Where(dataNode => !dataNode.ClientInstanceId.Equals(currentEndPoint.ClientInstanceId))
                .ToList();

            // Get all DataNodes from current member
            var currentMemberDataNodes = allDataNodes
                .Where(dataNode => dataNode.ClientInstanceId.Equals(currentEndPoint.ClientInstanceId))
                .ToList();

            // Check base inventories by DataNode
            var areBaseInventoriesComplete = CheckInventoriesCompleteByDataNode(
                inventoriesFilesCache, 
                otherDataNodes, 
                currentMemberDataNodes, 
                LocalInventoryModes.Base);

            // Check full inventories by DataNode
            var areFullInventoriesComplete = CheckInventoriesCompleteByDataNode(
                inventoriesFilesCache, 
                otherDataNodes, 
                currentMemberDataNodes, 
                LocalInventoryModes.Full);
        
            InventoryProcessData.AreBaseInventoriesComplete.OnNext(areBaseInventoriesComplete);
            InventoryProcessData.AreFullInventoriesComplete.OnNext(areFullInventoriesComplete);
        });
    }

    private bool CheckInventoriesCompleteByDataNode(
        List<InventoryFile> inventoriesFilesCache,
        List<DataNode> otherDataNodes,
        List<DataNode> currentMemberDataNodes,
        LocalInventoryModes inventoryMode)
    {
        // Check that each DataNode from other members has a corresponding inventory
        var otherDataNodesWithInventories = otherDataNodes
            .Where(dataNode => inventoriesFilesCache
                .Where(inventoryFile => inventoryFile.LocalInventoryMode == inventoryMode)
                .Any(inventoryFile => inventoryFile.SharedFileDefinition.AdditionalName.EndsWith($"_{dataNode.Code}")))
            .Count();

        // Check that current member has at least one inventory for their DataNodes
        var currentMemberHasInventories = currentMemberDataNodes
            .Any(dataNode => inventoriesFilesCache
                .Where(inventoryFile => inventoryFile.LocalInventoryMode == inventoryMode)
                .Any(inventoryFile => inventoryFile.SharedFileDefinition.AdditionalName.EndsWith($"_{dataNode.Code}")));

        return otherDataNodesWithInventories == otherDataNodes.Count && currentMemberHasInventories;
    }

    public Task AbortInventory()
    {
        _logger.LogInformation("inventory aborted on user request");

        InventoryProcessData.RequestInventoryAbort();

        return Task.CompletedTask;
    }
}