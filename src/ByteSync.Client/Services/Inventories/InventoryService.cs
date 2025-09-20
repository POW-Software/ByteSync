using System.Reactive.Linq;
using ByteSync.Business;
using ByteSync.Business.Communications;
using ByteSync.Business.DataNodes;
using ByteSync.Business.Inventories;
using ByteSync.Business.Sessions;
using ByteSync.Interfaces.Controls.Inventories;
using ByteSync.Interfaces.Repositories;
using ByteSync.Interfaces.Services.Sessions;

namespace ByteSync.Services.Inventories;

public class InventoryService : IInventoryService
{
    private readonly ISessionService _sessionService;
    private readonly IInventoryFileRepository _inventoryFileRepository;
    private readonly IDataNodeRepository _dataNodeRepository;
    private readonly ISessionMemberRepository? _sessionMemberRepository;
    private readonly ILogger<InventoryService> _logger;
    
    
    public InventoryService(ISessionService sessionService, IInventoryFileRepository inventoryFileRepository,
        IDataNodeRepository dataNodeRepository, ISessionMemberRepository sessionMemberRepository, ILogger<InventoryService> logger)
    {
        _sessionService = sessionService;
        _inventoryFileRepository = inventoryFileRepository;
        _dataNodeRepository = dataNodeRepository;
        _sessionMemberRepository = sessionMemberRepository;
        _logger = logger;
        
        InventoryProcessData = new InventoryProcessData();
        
        _sessionService.SessionStatusObservable
            .Where(x => x == SessionStatus.Preparation)
            .Subscribe(_ => { InventoryProcessData.Reset(); });
        
        // Compute and expose global aggregated status across all members
        _sessionMemberRepository?.SortedSessionMembersObservable
            .Select(changes => changes.SortedItems.Select(kv => kv.Value.SessionMemberGeneralStatus))
            .Select(InventoryGlobalStatusAggregator.Aggregate)
            .DistinctUntilChanged()
            .Subscribe(status => InventoryProcessData.GlobalMainStatus.OnNext(status));
    }
    
    // Backward-compatible overload for tests and callers not providing a sessionMemberRepository
    public InventoryService(ISessionService sessionService, IInventoryFileRepository inventoryFileRepository,
        IDataNodeRepository dataNodeRepository, ILogger<InventoryService> logger)
        : this(sessionService, inventoryFileRepository, dataNodeRepository, sessionMemberRepository: null!, logger)
    {
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
            var inventoriesFilesCache = _inventoryFileRepository.Elements.ToList();
            var allDataNodes = _dataNodeRepository.Elements.ToList();
            
            var areBaseInventoriesComplete = CheckInventoriesCompleteByDataNode(
                inventoriesFilesCache,
                allDataNodes,
                LocalInventoryModes.Base);
            
            var areFullInventoriesComplete = CheckInventoriesCompleteByDataNode(
                inventoriesFilesCache,
                allDataNodes,
                LocalInventoryModes.Full);
            
            InventoryProcessData.AreBaseInventoriesComplete.OnNext(areBaseInventoriesComplete);
            InventoryProcessData.AreFullInventoriesComplete.OnNext(areFullInventoriesComplete);
        });
    }
    
    private bool CheckInventoriesCompleteByDataNode(
        List<InventoryFile> inventoriesFilesCache,
        List<DataNode> allDataNodes,
        LocalInventoryModes inventoryMode)
    {
        return allDataNodes.All(dataNode =>
            inventoriesFilesCache
                .Where(inventoryFile => inventoryFile.LocalInventoryMode == inventoryMode)
                .Any(inventoryFile =>
                    inventoryFile.SharedFileDefinition.ClientInstanceId == dataNode.ClientInstanceId));
    }
    
    public Task AbortInventory()
    {
        _logger.LogInformation("inventory aborted on user request");
        
        InventoryProcessData.RequestInventoryAbort();
        
        return Task.CompletedTask;
    }
}