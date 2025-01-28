using System.Reactive.Linq;
using System.Threading.Tasks;
using ByteSync.Business.Inventories;
using ByteSync.Business.Profiles;
using ByteSync.Business.Synchronizations;
using ByteSync.Common.Business.Lobbies;
using ByteSync.Common.Helpers;
using ByteSync.Interfaces;
using ByteSync.Interfaces.Controls.Automating;
using ByteSync.Interfaces.Controls.Inventories;
using ByteSync.Interfaces.Controls.Sessions;
using ByteSync.Interfaces.Controls.Synchronizations;
using ByteSync.Interfaces.Lobbies;
using ByteSync.Interfaces.Profiles;

namespace ByteSync.Services.Automating;

public class ProfileAutoRunner : IProfileAutoRunner
{
    private readonly IApplicationSettingsRepository _applicationSettingsRepository;
    private ISessionProfileLocalDataManager _sessionProfileLocalDataManager;
    private ILobbyManager _lobbyManager;
    private IDataInventoryStarter _dataInventoryStarter;
    // private ISessionDataHolder _sessionDataHolder;
    // private ISynchronizationManager _synchronizationManager;
    private readonly ILobbyRepository _lobbyRepository;
    private readonly ICloudSessionConnector _cloudSessionConnector;
    private readonly IInventoryService _inventoryService;
    private readonly ISynchronizationService _synchronizationService;
    private readonly IComparisonItemsService _comparisonItemsService;
    private readonly ISessionService _sessionService;
    private readonly ISynchronizationStarter _synchronizationStarter;

    public const int RESULT_SUCCESS = 0;
    public const int RESULT_INVENTORY_ERROR = 1;
    public const int RESULT_INVENTORY_ERROR_NOT_STARTED = 2;
    public const int RESULT_UNKNOWN_PROFILE_OR_LOBBY_ERROR = 3;
    public const int RESULT_CONNECTION_PROBLEM = 4;
    public const int RESULT_SYNCHRONIZATION_NOT_ENDED = 5;
    public const int RESULT_COMPARISON_RESULT_NOT_SET = 6;

    public ProfileAutoRunner(IApplicationSettingsRepository applicationSettingsManager,
        ISessionProfileLocalDataManager sessionProfileLocalDataManager, ILobbyManager lobbyManager,
        IDataInventoryStarter dataInventoryStarter, ILobbyRepository lobbyRepository,
        ICloudSessionConnector cloudSessionConnector, IInventoryService inventoriesService,
        ISynchronizationService synchronizationService, IComparisonItemsService comparisonItemsService,
        ISessionService sessionService, ISynchronizationStarter synchronizationStarter)
    {
        _applicationSettingsRepository = applicationSettingsManager;
        _sessionProfileLocalDataManager = sessionProfileLocalDataManager;
        _lobbyManager = lobbyManager;
        _dataInventoryStarter = dataInventoryStarter;
        _lobbyRepository = lobbyRepository;
        _cloudSessionConnector = cloudSessionConnector;
        _inventoryService = inventoriesService;
        _synchronizationService = synchronizationService;
        _comparisonItemsService = comparisonItemsService;
        _sessionService = sessionService;
        _synchronizationStarter = synchronizationStarter;
    }
    
    public async Task<int> OperateRunProfile(string? profileName, JoinLobbyModes? joinLobbyMode)
    {
        var userSettings = _applicationSettingsRepository.GetCurrentApplicationSettings();
        
        // todo Call ConnectionService
        // var authenticateResponse = await _connectionManager.Connect();

        // if (!authenticateResponse.IsSuccess)
        // {
        //     return RESULT_CONNECTION_PROBLEM;
        // }

        var profiles = await _sessionProfileLocalDataManager.GetAllSavedProfiles();

        AbstractSessionProfile? profile = null;
        if (profileName.IsNotEmpty())
        {
            profile = profiles.FirstOrDefault(p => p.Name.Equals(profileName));
        }

        if (profile == null || joinLobbyMode == null)
        {
            return RESULT_UNKNOWN_PROFILE_OR_LOBBY_ERROR;
        }

        await _lobbyManager.StartLobbyAsync(profile, joinLobbyMode.Value);

        bool isInventoryOK;
        if (profile is CloudSessionProfile)
        {
            var lobbyId = (await _lobbyRepository.GetDataAsync())!.LobbyId;
            
            await _lobbyRepository.WaitAsync(lobbyId, details => details.SecurityCheckProcessEndedWithSuccess, TimeSpan.FromMinutes(5));
                
            // Ici, l'inventaire sera lancé automatiquement, mais comment savoir quand il est terminé ?


            // todo 040423
            var v = await _inventoryService.InventoryProcessData.MainStatus
                .CombineLatest(_sessionService.SessionEnded)
                .Where(tuple => tuple.First.In(LocalInventoryPartStatus.Success, LocalInventoryPartStatus.Error)
                                || tuple.Second)
                .FirstAsync().Timeout(TimeSpan.FromMinutes(1))
                .Select(tuple => (Status: tuple.First, IsSessionEnd: tuple.Second));
            
            
            
            

            // bool isInventoryStarted = await _sessionDataHolder.WaitForInventoryStartedAsync();
            // if (!isInventoryStarted)
            // {
            //     return RESULT_INVENTORY_ERROR_NOT_STARTED;
            // }

            // todo 040423
            _comparisonItemsService.ComparisonResult
                .Where(cr => cr != null)
                .FirstAsync().Timeout(TimeSpan.FromMinutes(1));
            
            // bool isComparisonResultSet = await _sessionDataHolder.WaitForComparisonResultSetAsync();
            // if (!isComparisonResultSet)
            // {
            //     return RESULT_COMPARISON_RESULT_NOT_SET;
            // }
                
            isInventoryOK = true;
        }
        else
        {
            var startInventoryResult = await _dataInventoryStarter.StartDataInventory(false);

            isInventoryOK = startInventoryResult.IsOK;
        }

        if (!isInventoryOK)
        {
            return RESULT_INVENTORY_ERROR;
        }
        
        // todo 040423
        // await _sessionDataHolder.InitializeComparisonItems();
        
        
        
        await _synchronizationStarter.StartSynchronization(false);
        
        // todo 040423
        var v2 = await _synchronizationService.SynchronizationProcessData.SynchronizationMainStatus
            .CombineLatest(_sessionService.SessionEnded)
            .Where(tuple => tuple.First.In(SynchronizationProcessStatuses.Success, SynchronizationProcessStatuses.Error)
                            || tuple.Second)
            .FirstAsync().Timeout(TimeSpan.FromMinutes(1))
            .Select(tuple => (Status: tuple.First, IsSessionEnd: tuple.Second));
        
        // bool isSynchronizationEnded = await _sessionDataHolder.WaitForSynchronizationEndedAsync();
        // if (!isSynchronizationEnded)
        // {
        //     return RESULT_SYNCHRONIZATION_NOT_ENDED;
        // }
        
        // if (profile is CloudSessionProfile)
        // {
        //     await _cloudSessionConnector.QuitSession();
        // }
        
        // On quitte la session systématiquement, qu'elle soit locale ou cloud
        // await _cloudSessionConnector.QuitSession();

        return RESULT_SUCCESS;
    }
}