using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using ByteSync.Business.PathItems;
using ByteSync.Business.Profiles;
using ByteSync.Business.SessionMembers;
using ByteSync.Business.Sessions;
using ByteSync.Business.Sessions.RunSessionInfos;
using ByteSync.Common.Business.Inventories;
using ByteSync.Common.Business.Misc;
using ByteSync.Common.Business.Sessions;
using ByteSync.Common.Business.Sessions.Cloud;
using ByteSync.Common.Business.Sessions.Local;
using ByteSync.Common.Helpers;
using ByteSync.Interfaces.Controls.Communications;
using ByteSync.Interfaces.Controls.Communications.Http;
using ByteSync.Interfaces.Controls.Encryptions;
using ByteSync.Interfaces.Controls.Inventories;
using ByteSync.Interfaces.Controls.Sessions;
using ByteSync.Interfaces.Repositories;
using DynamicData;
using Serilog;

namespace ByteSync.Services.Inventories;

public class DataInventoryStarter : IDataInventoryStarter
{
    private readonly ISessionService _sessionService;
    private readonly ICloudProxy _connectionManager;
    private readonly IDataEncrypter _dataEncrypter;
    private readonly IDataInventoryRunner _dataInventoryRunner;
    private readonly IInventoryApiClient _inventoryApiClient;
    private readonly IPathItemRepository _pathItemRepository;
    private readonly ISessionMemberRepository _sessionMemberRepository;

    public DataInventoryStarter(ISessionService sessionService, ICloudProxy connectionManager, 
        IDataEncrypter dataEncrypter, IDataInventoryRunner dataInventoryRunner, 
        IInventoryApiClient inventoryApiClient, IPathItemRepository pathItemRepository, ISessionMemberRepository sessionMemberRepository)
    {
        _sessionService = sessionService;
        _connectionManager = connectionManager;
        _dataEncrypter = dataEncrypter;
        _dataInventoryRunner = dataInventoryRunner;
        _inventoryApiClient = inventoryApiClient;
        _pathItemRepository = pathItemRepository;
        _sessionMemberRepository = sessionMemberRepository;
        
        _connectionManager.HubPushHandler2.InventoryStarted
            .Where(dto => _sessionService.CheckSession(dto.SessionId))
            .Subscribe(OnDataInventoryStarted);

        _sessionService.RunSessionProfileInfoObservable.CombineLatest(
                _sessionMemberRepository.SortedSessionMembersObservable
                    .QueryWhenChanged(),
                _pathItemRepository.ObservableCache.Connect()
                    .QueryWhenChanged())
            .Where(tuple => tuple.First is RunCloudSessionProfileInfo && tuple.First.AutoStartsInventory
                          && tuple.Second.Count == 
                          ((CloudSessionProfileDetails) tuple.First.GetProfileDetails()).Members.Count)
            .SelectMany(async tuple =>
            {
                try
                {
                    await CheckInventoryAutoStart(tuple);
                }
                catch (Exception e)
                {
                    Log.Error("An erro");
                    // handle the exception, e.g. log it
                }
                return Unit.Default;
            })
            .Subscribe();
    }

    private async Task CheckInventoryAutoStart((AbstractRunSessionProfileInfo? First, IQuery<SessionMemberInfo, string> Second, 
        IQuery<PathItem, string> Third) tuple)
    {
        var runCloudSessionProfileInfo = (RunCloudSessionProfileInfo)tuple.First!;
        var cloudSessionProfileDetails = runCloudSessionProfileInfo.ProfileDetails;
        var allSessionMembers = tuple.Second.Items;
        var allPathItems = tuple.Third.Items.ToList();

        var allOK = true;
        foreach (var sessionMemberInfo in allSessionMembers)
        {
            var pathItems = allPathItems
                .Where(pi => pi.BelongsTo(sessionMemberInfo))
                .ToList();
                            
            var expectedPathItems = cloudSessionProfileDetails
                .Members.Single(m => m.ProfileClientId.Equals(sessionMemberInfo.ProfileClientId))
                .PathItems.OrderBy(pi => pi.Code)
                .ToList();

            if (pathItems.Count == expectedPathItems.Count)
            {
                var sessionProfilesPathItems = new List<SessionProfilePathItem>();
                foreach (var pathItem in pathItems)
                {
                    sessionProfilesPathItems.Add(new SessionProfilePathItem(pathItem));
                }
                
                if (!sessionProfilesPathItems.HaveSameContent(expectedPathItems))
                {
                    allOK = false;
                }
            }
            else
            {
                allOK = false;
            }
        }

        if (allOK)
        {
            await Task.Delay(TimeSpan.FromSeconds(5));
                        
            await StartDataInventory(false);
        }
    }

    public async Task<StartInventoryResult> StartDataInventory(bool isLaunchedByUser)
    {
        var session = _sessionService.CurrentSession;
        
        if (session == null)
        {
            return StartInventoryResult.BuildFrom(StartInventoryStatuses.UndefinedSession);
        }

        if (isLaunchedByUser)
        {
            Log.Information("The current user has requested to start the Data Inventory");
        }
        else
        {
            Log.Information("The Data Inventory has been automatically started");
        }
        
        var sessionSettings = _sessionService.CurrentSessionSettings;
        if (sessionSettings == null)
        {
            return StartInventoryResult.BuildFrom(StartInventoryStatuses.UndefinedSettings);
        }
        
        FinalizeSessionSettings(sessionSettings);

        var result = CheckPathItems(session);
        if (result != null) return result;

        // todo : remonter également les autres paramètres pour contrôle par les aux parties de l'égalité des paramètres
        
        result = await SendSessionSettings(session, sessionSettings);

        if (result.IsOK)
        {
            await _sessionService.SetSessionSettings(sessionSettings);
            await _dataInventoryRunner.RunDataInventory();

            return result;
        }
        else
        {
            return result;
        }
    }

    public async void OnDataInventoryStarted(InventoryStartedDTO inventoryStartedDto)
    {
        try
        {
            Log.Information("The Data Inventory has been started by another client (ClientInstanceId:{ClientInstanceId})", 
                inventoryStartedDto.ClientInstanceId);
                
            var sessionSettings = _dataEncrypter.DecryptSessionSettings(inventoryStartedDto.EncryptedSessionSettings);

            await _sessionService.SetSessionSettings(sessionSettings);
            await _dataInventoryRunner.RunDataInventory();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "OnStartInventory");
        }
    }
    
    public IObservable<bool> CanCurrentUserStartInventory()
    {
        var observable = _sessionService.SessionObservable.CombineLatest(_sessionMemberRepository.IsCurrentUserFirstSessionMemberObservable,
                _sessionService.RunSessionProfileInfoObservable, _sessionService.HasSessionBeenResettedObservable)
            .Select(tuple => new
            {
                Session = tuple.First, 
                IsFirstSessionMember = tuple.Second,
                AutoStartsInventory = tuple.Third is { AutoStartsInventory: true },
                HasSessionBeenResetted = tuple.Fourth
            })
            .Select(tuple =>
            {
                // During this refactoring, it appears that this behavior is independent of the session type (Cloud/Local),
                // so we'll see if it's really OK
                
                if (!tuple.IsFirstSessionMember)
                {
                    // only the 1st Session Member can start the inventory
                    return false;
                }
                if (tuple.AutoStartsInventory && !tuple.HasSessionBeenResetted)
                {
                    // IsInventoryAutoStart has an impact as long as the session has not been reset
                    return false;
                }

                return true;
            });

        return observable;
    }

    private async Task<StartInventoryResult> SendSessionSettings(AbstractSession session, SessionSettings sessionSettings)
    {
        StartInventoryResult result;
        
        if (session is CloudSession)
        {
            var encryptedSessionSettings = _dataEncrypter.EncryptSessionSettings(sessionSettings);
        
            result = await _inventoryApiClient.StartInventory(session.SessionId, encryptedSessionSettings);
        }
        else
        {
            result = StartInventoryResult.BuildOK();
        }

        return result;
    }

    private void FinalizeSessionSettings(SessionSettings sessionSettings)
    {
        if (_sessionMemberRepository.Elements.Any(m => m.Endpoint.OSPlatform == OSPlatforms.Windows))
        {
            sessionSettings.LinkingCase = LinkingCases.Insensitive;
        }
        else
        {
            sessionSettings.LinkingCase = LinkingCases.Sensitive;
        }
    }

    private StartInventoryResult? CheckPathItems(AbstractSession session)
    {
        if (session is LocalSession)
        {
            var pathItems = _pathItemRepository.CurrentMemberPathItems.Items.ToList();
            if (pathItems.Count < 2)
            {
                return LogAndBuildStartInventoryResult(session, StartInventoryStatuses.LessThan2DataSources);
            }
            if (pathItems.Count > 5)
            {
                return LogAndBuildStartInventoryResult(session, StartInventoryStatuses.MoreThan5DataSources);
            }
        }

        return null;
    }
    
    private StartInventoryResult LogAndBuildStartInventoryResult(AbstractSession localSession, StartInventoryStatuses status)
    {
        Log.Information("StartInventory: session {@localSession} - {Status}", localSession.SessionId, status);
        return StartInventoryResult.BuildFrom(status);
    }
}