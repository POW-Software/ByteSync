using System.Reactive.Linq;
using System.Threading.Tasks;
using ByteSync.Business;
using ByteSync.Business.Communications;
using ByteSync.Business.Inventories;
using ByteSync.Business.Sessions;
using ByteSync.Common.Business.EndPoints;
using ByteSync.Common.Business.Sessions;
using ByteSync.Common.Business.Sessions.Cloud;
using ByteSync.Common.Helpers;
using ByteSync.Interfaces.Controls.Communications.Http;
using ByteSync.Interfaces.Controls.Inventories;
using ByteSync.Interfaces.Controls.Sessions;
using ByteSync.Interfaces.Repositories;
using ByteSync.Interfaces.Services.Communications;
using DynamicData;

namespace ByteSync.Services.Inventories;

public class InventoryService : IInventoryService
{
    private readonly ISessionService _sessionService;
    private readonly IConnectionService _connectionService;
    private readonly IInventoryApiClient _inventoryApiClient;
    private readonly ISessionMemberRepository _sessionMemberRepository;
    private readonly IInventoryFileRepository _inventoryFileRepository;
    private readonly ILogger<InventoryService> _logger;


    public InventoryService(ISessionService sessionService, IConnectionService connectionService, IInventoryApiClient inventoryApiClient,
        ISessionMemberRepository sessionMemberRepository, IInventoryFileRepository inventoryFileRepository, ILogger<InventoryService> logger)
    {
        _sessionService = sessionService;
        _connectionService = connectionService;
        _inventoryApiClient = inventoryApiClient;
        _sessionMemberRepository = sessionMemberRepository;
        _inventoryFileRepository = inventoryFileRepository;
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
            var otherSessionMembersCount = _sessionMemberRepository.SortedOtherSessionMembers.Count();

            var currentEndPoint = _connectionService.CurrentEndPoint!;

            var inventoriesFilesCache = _inventoryFileRepository.Elements.ToList();

            var areBaseInventoriesComplete =
                inventoriesFilesCache
                    .Where(inventoryFile => inventoryFile.IsBaseInventory)
                    .Count(inventoryFile =>
                        !inventoryFile.SharedFileDefinition.IsCreatedBy(currentEndPoint)) == otherSessionMembersCount
                &&
                inventoriesFilesCache
                    .Where(inventoryFile => inventoryFile.IsBaseInventory)
                    .Count(inventoryFile =>
                        inventoryFile.SharedFileDefinition.IsCreatedBy(currentEndPoint)) > 0;
            
            var areFullInventoriesComplete =
                inventoriesFilesCache
                    .Where(inventoryFile => inventoryFile.IsFullInventory)
                    .Count(inventoryFile =>
                        !inventoryFile.SharedFileDefinition.IsCreatedBy(currentEndPoint)) == otherSessionMembersCount
                &&
                inventoriesFilesCache
                    .Where(inventoryFile => inventoryFile.IsFullInventory)
                    .Count(inventoryFile =>
                        inventoryFile.SharedFileDefinition.IsCreatedBy(currentEndPoint)) > 0;
        
        
            InventoryProcessData.AreBaseInventoriesComplete.OnNext(areBaseInventoriesComplete);
            InventoryProcessData.AreFullInventoriesComplete.OnNext(areFullInventoriesComplete);
        });
    }

    public Task SetSessionOnFatalError(CloudSessionFatalError cloudSessionFatalError)
    {
        InventoryProcessData.RequestInventoryAbort();

        return Task.CompletedTask;
    }
    
    public Task AbortInventory()
    {
        _logger.LogInformation("inventory aborted on user request");

        InventoryProcessData.RequestInventoryAbort();

        return Task.CompletedTask;
    }
}