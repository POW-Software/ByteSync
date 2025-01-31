using System.Threading.Tasks;
using ByteSync.Business.Lobbies;
using ByteSync.Business.Navigations;
using ByteSync.Business.Profiles;
using ByteSync.Business.Sessions;
using ByteSync.Business.Sessions.RunSessionInfos;
using ByteSync.Common.Business.Lobbies;
using ByteSync.Common.Business.Lobbies.Connections;
using ByteSync.Common.Business.Sessions.Cloud.Connections;
using ByteSync.Common.Controls.Json;
using ByteSync.Interfaces;
using ByteSync.Interfaces.Controls.Applications;
using ByteSync.Interfaces.Controls.Communications;
using ByteSync.Interfaces.Controls.Communications.Http;
using ByteSync.Interfaces.Controls.Navigations;
using ByteSync.Interfaces.Controls.Sessions;
using ByteSync.Interfaces.Lobbies;
using ByteSync.Interfaces.Profiles;
using ByteSync.Interfaces.Services.Sessions;
using ByteSync.Interfaces.Services.Sessions.Connecting;
using Serilog;

namespace ByteSync.Services.Lobbies;

public class LobbyManager : ILobbyManager
{
    private readonly IEnvironmentService _environmentService;
    private readonly ISessionProfileLocalDataManager _sessionProfileLocalDataManager;
    private readonly ILobbyRepository _lobbyRepository;
    private readonly IPublicKeysManager _publicKeysManager;
    private readonly IApplicationSettingsRepository _applicationSettingsRepository;
    private readonly IDigitalSignaturesRepository _digitalSignaturesRepository;
    private readonly INavigationService _navigationService;
    private readonly ISessionService _sessionService;
    private readonly ILobbyApiClient _lobbyApiClient;
    private readonly ICreateSessionService _createSessionService;
    private readonly IDigitalSignaturesChecker _digitalSignaturesChecker;
    private readonly IJoinSessionService _joinSessionService;

    public LobbyManager(IEnvironmentService environmentService, ISessionProfileLocalDataManager sessionProfileLocalDataManager,
        ILobbyRepository lobbyRepository,
        IPublicKeysManager publicKeysManager, IApplicationSettingsRepository applicationSettingsManager,
        IDigitalSignaturesRepository digitalSignaturesRepository, INavigationService navigationService,
        ISessionService sessionService, ILobbyApiClient lobbyApiClient, ICreateSessionService createSessionService,
        IDigitalSignaturesChecker digitalSignaturesChecker, IJoinSessionService joinSessionService)   
    {
        _environmentService = environmentService;
        _sessionProfileLocalDataManager = sessionProfileLocalDataManager;
        _lobbyRepository = lobbyRepository;
        _publicKeysManager = publicKeysManager;
        _applicationSettingsRepository = applicationSettingsManager;
        _digitalSignaturesRepository = digitalSignaturesRepository;
        _navigationService = navigationService;
        _sessionService = sessionService;
        _lobbyApiClient = lobbyApiClient;
        _createSessionService = createSessionService;
        _digitalSignaturesChecker = digitalSignaturesChecker;
        _joinSessionService = joinSessionService;
    }

    public TimeSpan GeneralWaitTimeSpan
    {
        get
        {
        #if DEBUG
            return TimeSpan.FromSeconds(3);
        #endif
            
            // ReSharper disable once HeuristicUnreachableCode
#pragma warning disable CS0162
            return TimeSpan.FromSeconds(3);
#pragma warning restore CS0162
        }
    }

    public async Task StartLobbyAsync(AbstractSessionProfile sessionProfile, JoinLobbyModes joinLobbyMode)
    {
        await Task.Run(() => StartLobby(sessionProfile, joinLobbyMode));
    }

    private async Task StartLobby(AbstractSessionProfile sessionProfile, JoinLobbyModes joinLobbyMode)
    {
        if (sessionProfile is CloudSessionProfile cloudSessionProfile)
        {
            var joinLobbyParameters = new JoinLobbyParameters();
            joinLobbyParameters.CloudSessionProfileId = cloudSessionProfile.ProfileId;
            joinLobbyParameters.ProfileClientId = cloudSessionProfile.ProfileClientId;
            joinLobbyParameters.PublicKeyInfo = _publicKeysManager.GetMyPublicKeyInfo();
            joinLobbyParameters.JoinMode = joinLobbyMode;

            if (joinLobbyMode == JoinLobbyModes.Join)
            {
                Log.Information("Joining Cloud Lobby for Profile {ProfileName} (Id: {ProfileId})", sessionProfile.Name, cloudSessionProfile.ProfileId);
            }
            else
            {
                Log.Information("Starting Cloud Lobby for Profile {ProfileName} (Id: {ProfileId})", sessionProfile.Name, cloudSessionProfile.ProfileId);
            }

            var joinLobbyResult = await _lobbyApiClient.JoinLobby(joinLobbyParameters);

            if (joinLobbyResult.IsOK)
            {
                var lobbyInfo = joinLobbyResult.LobbyInfo!;

                Log.Information("LobbyId: {lobbyId} ", lobbyInfo.LobbyId);

                await _digitalSignaturesRepository.Start(lobbyInfo.LobbyId);

                var sessionProfileDetails = await _sessionProfileLocalDataManager.LoadCloudSessionProfileDetails(lobbyInfo);
                await _lobbyRepository.SetCloudSessionProfileDetails(cloudSessionProfile, sessionProfileDetails, lobbyInfo);

                var areAllConnected = _lobbyRepository.Get(lobbyInfo.LobbyId, details => details.LobbyMembersViewModels)
                    .All(lmvm => lmvm.LobbyMember.LobbyMemberInfo != null);

                // Si tout s'est bien passé, on peut désormais afficher le Lobby
                // _navigationEventsHub.RaiseNavigateToLobbyRequested();
                _navigationService.NavigateTo(NavigationPanel.ProfileSessionLobby);

                if (areAllConnected)
                {
                    await RunSecurityCheckAsync(lobbyInfo.LobbyId);
                }
            }
            else
            {
                Log.Error("Can not start or join Lobby. Error: {Error}", joinLobbyResult.Status);
            }
        }
        else if (sessionProfile is LocalSessionProfile localSessionProfile)
        {
            Log.Information("Starting Local Session for Profile {ProfileName} (Id: {ProfileId})", sessionProfile.Name, localSessionProfile.ProfileId);

            var sessionProfileDetails = 
                await _sessionProfileLocalDataManager.LoadLocalSessionProfileDetails(localSessionProfile);

            LobbySessionModes lobbySessionMode;

            if (joinLobbyMode == JoinLobbyModes.RunInventory)
            {
                lobbySessionMode = LobbySessionModes.RunInventory;
            }
            else if (joinLobbyMode == JoinLobbyModes.RunSynchronization)
            {
                lobbySessionMode = LobbySessionModes.RunSynchronization;
            }
            else
            {
                throw new Exception("Unexpected joinLobbyMode");
            }

            var runLocalSessionProfileInfo =
                new RunLocalSessionProfileInfo(localSessionProfile, sessionProfileDetails, lobbySessionMode);
            
            await _sessionService.StartLocalSession(runLocalSessionProfileInfo);
        }
    }

    public async Task ShowProfileDetails(AbstractSessionProfile sessionProfile)
    {
        if (sessionProfile is CloudSessionProfile cloudSessionProfile)
        {
            Log.Information("Loading details for Profile {ProfileName} (Id: {ProfileId})", sessionProfile.Name, cloudSessionProfile.ProfileId);
            
            var sessionProfileDetails = 
                await _sessionProfileLocalDataManager.LoadCloudSessionProfileDetails(cloudSessionProfile);

            if (sessionProfileDetails != null)
            {
                await _lobbyRepository.SetCloudSessionProfileDetails(cloudSessionProfile, sessionProfileDetails, null);

                // Si tout s'est bien passé, on peut désormais afficher le Lobby
                _navigationService.NavigateTo(NavigationPanel.ProfileSessionDetails);
                // await _navigationEventsHub.RaiseNavigateToProfileDetailsRequested();
            }
            else
            {
                Log.Error("Can not load details for Profile {ProfileName} (Id: {ProfileId}). Profile may have been deleted on the server", 
                    sessionProfile.Name, cloudSessionProfile.ProfileId);
            }
        }
        else if (sessionProfile is LocalSessionProfile localSessionProfile)
        {
            Log.Information("Starting Local Session for Profile {ProfileName} (Id: {ProfileId})", 
                sessionProfile.Name, localSessionProfile.ProfileId);
            
            var sessionProfileDetails = 
                await _sessionProfileLocalDataManager.LoadLocalSessionProfileDetails(localSessionProfile);

            var runLocalSessionProfileInfo =
                new RunLocalSessionProfileInfo(localSessionProfile, sessionProfileDetails, LobbySessionModes.LoadOnly);
            
            await _sessionService.StartLocalSession(runLocalSessionProfileInfo);
        }
    }

    public async Task ExitLobby(string lobbyId)
    {
        await DoExitLobby(lobbyId);
        
        // on revient à l'écran d'acceuil
        _navigationService.NavigateTo(NavigationPanel.Home);
    }

    private async Task DoExitLobby(string lobbyId)
    {
        Log.Information("Exiting Lobby {lobbyId}", lobbyId);
        
        try
        {
            if (!lobbyId.StartsWith(LobbyRepository.LOCAL_LOBBY_PREFIX, StringComparison.InvariantCultureIgnoreCase))
            {
                // On n'est pas sur un "lobby local", on est sur un lobby créé par le serveur
                await _lobbyApiClient.QuitLobby(lobbyId);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error during exit lobby request");
        }
        
        await _lobbyRepository.ClearAsync();
    }

    public Task RunSecurityCheckAsync(string lobbyId)
    {
        return Task.Run(() => RunSecurityCheck(lobbyId));
    }
    
    public async Task RunSecurityCheck(string lobbyId)
    {
        Log.Information("Beginning of the Lobby security checks");
        
        LobbyDetails? details = null;
        
        try
        {
            details = await _lobbyRepository.GetDataAsync(lobbyId);
            
            await Task.Delay(GeneralWaitTimeSpan);
            if (details.HasEnded) return;

            await _lobbyRepository.UpdateLobbyMemberStatus(lobbyId, LobbyMemberStatuses.SecurityChecksInProgress);
            if (details.HasEnded) return;

            var otherLobbyMembers = await CheckAndGetOtherLobbyMembers(lobbyId);
            if (details.HasEnded) return;
            
            if (otherLobbyMembers == null) return; // Le contrôle a échoué

            var lobbyCheckInfo = BuildLobbyCheckInfo(lobbyId, otherLobbyMembers);
            await _lobbyApiClient.SendLobbyCheckInfos(lobbyCheckInfo);
            if (details.HasEnded) return;

            var isCheckOK = await CheckAllOtherMembersChecked(lobbyId);
            if (details.HasEnded) return;
            
            if (!isCheckOK)
            {
                // Le contrôle a échoué, possiblement car un des autres membres a échoué
                await _lobbyRepository.UpdateLobbyMemberStatus(lobbyId, LobbyMemberStatuses.CrossCheckError);
                return; 
            }
            
            await Task.Delay(GeneralWaitTimeSpan);
            if (details.HasEnded) return;

            var mustExitLobby = false;
            
            // Le premier membre du Lobby crée la session et fournit les informations aux autres membres
            if (await _lobbyRepository.IsFirstLobbyMember(lobbyId) && await _lobbyRepository.IsEverythingOKBeforeSession(lobbyId))
            {
                mustExitLobby = await StartSessionCreationAndConnectionProcess(lobbyId, otherLobbyMembers);
            }

            details.SecurityCheckProcessEndedWithSuccess.Set();

            if (mustExitLobby)
            {
                await DoExitLobby(lobbyId);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "RunSecurityCheck");

            if (details == null || details.HasEnded)
            {
                return;
            }

            try
            {
                await _lobbyRepository.UpdateLobbyMemberStatus(lobbyId, LobbyMemberStatuses.UnexpectedError);
            }
            catch (Exception ex2)
            {
                Log.Error(ex2, "RunSecurityCheck // UpdateLobbyMemberStatus");
            }
        }
    }

    private async Task<bool> CheckAllOtherMembersChecked(string lobbyId)
    {
        var result = await _lobbyRepository.WaitAsync(lobbyId, details => details.AllOtherMembersCheckedWaitHandle, TimeSpan.FromMinutes(1));
        if (!result)
        {
            return false;
        }

        result = await _lobbyRepository.AreAllOtherMembersCheckSuccess(lobbyId);
        return result;
    }

    private async Task<bool> StartSessionCreationAndConnectionProcess(string lobbyId, List<LobbyMember> otherLobbyMembers)
    {
        var lobbySessionDetails = await _lobbyRepository.BuildCloudProfileSessionDetails(lobbyId);
        var cloudSessionResult = await _createSessionService.CreateCloudSession(new CreateCloudSessionRequest(lobbySessionDetails));

        if (cloudSessionResult != null)
        {
            await _lobbyRepository.UpdateLobbyMemberStatus(lobbyId, LobbyMemberStatuses.CreatedSession);

            await Task.Delay(GeneralWaitTimeSpan);

            var lobbyCloudSessionInfoJson = BuildLobbyCloudSessionInfoJson(cloudSessionResult, lobbyId);
            foreach (var lobbyMember in otherLobbyMembers)
            {
                var lobbyMemberInfo = lobbyMember.LobbyMemberInfo;
                if (lobbyMemberInfo == null)
                {
                    Log.Warning("StartSessionCreationAndConnectionProcess: process has ended because a member left the Lobby");
                    return false;
                }

                var credentials = BuildLobbyCloudSessionCredentials(lobbyMemberInfo, lobbyCloudSessionInfoJson, lobbyId);

                var lobbySessionExpectedMember = new LobbySessionExpectedMember(lobbyMember, lobbyMemberInfo, lobbyId, cloudSessionResult.SessionId);
                await _lobbyRepository.SetExpectedMember(lobbySessionExpectedMember);

                await _lobbyApiClient.SendLobbyCloudSessionCredentials(credentials);
                
                var isWaitOK = await _lobbyRepository.WaitAsync(lobbyId, details => details.ExpectedMemberWaitHandler, TimeSpan.FromMinutes(1));
                if (!isWaitOK)
                {
                    return false;
                }
            }

            // Quand tous les membres ont rejoint la session, on peut rejoindre la session également
            _navigationService.NavigateTo(NavigationPanel.CloudSynchronization);
            
            return true;
        }
        else
        {
            return false;
        }
    }
    
    private async Task<List<LobbyMember>?> CheckAndGetOtherLobbyMembers(string lobbyId)
    {
        var otherLobbyMembers = new List<LobbyMember>();
        var isTrustSuccess = true;
        foreach (var lobbyMember in _lobbyRepository.Get(lobbyId, details => details.OtherLobbyMembers))
        {
            if (!_publicKeysManager.IsTrusted(lobbyMember.LobbyMemberInfo!.PublicKeyInfo))
            {
                Log.Error("Public Key {@PublicKey} of Lobby Member '{LobbyMember}' is not trusted!", lobbyMember.LobbyMemberInfo!.PublicKeyInfo, 
                    lobbyMember.MachineName);
                isTrustSuccess = false;
            }
            else
            {
                otherLobbyMembers.Add(lobbyMember);
            }
        }

        await Task.Delay(GeneralWaitTimeSpan);

        if (!isTrustSuccess)
        {
            await _lobbyRepository.SetTrustCheckError(lobbyId);
            Log.Error("TrustCheck failed!");

            return null;
        }
        else
        {
            var isAuthOK = await _digitalSignaturesChecker.CheckExistingMembersDigitalSignatures(lobbyId, 
                otherLobbyMembers.Select(lm => lm.LobbyMemberInfo!.ClientInstanceId).ToList());
            if (!isAuthOK)
            {
                Log.Error("Error during digital signatures chec!");
                return null;
            }
            
            await _lobbyRepository.SetTrustCheckSuccess(lobbyId);
        }

        return otherLobbyMembers;
    }
    
    private LobbyCheckInfo BuildLobbyCheckInfo(string lobbyId, List<LobbyMember> members)
    {
        var lobbyCheckInfo = new LobbyCheckInfo();
        lobbyCheckInfo.LobbyId = lobbyId;
        lobbyCheckInfo.SenderClientInstanceId = _environmentService.ClientInstanceId;

        var checkKeyComputer = new LobbyCheckKeyComputer(lobbyId, 
            _lobbyRepository.Get(lobbyId, details => details.LocalProfileClientId),
            _applicationSettingsRepository.GetCurrentApplicationSettings().ClientId);
        foreach (var lobbyMember in members)
        {
            var checkKey = checkKeyComputer.ComputeKey(lobbyMember.CloudSessionProfileMember);

            var recipient = new LobbyCheckRecipient();
            recipient.ClientInstanceId = lobbyMember.LobbyMemberInfo!.ClientInstanceId;
            recipient.CheckData = _publicKeysManager.EncryptString(lobbyMember.LobbyMemberInfo!.PublicKeyInfo, checkKey);
            lobbyCheckInfo.Recipients.Add(recipient);
        }

        return lobbyCheckInfo;
    }

    private LobbyCloudSessionCredentials BuildLobbyCloudSessionCredentials(LobbyMemberInfo lobbyMemberInfo, string lobbyCloudSessionInfoJson,
        string lobbyId)
    {
        var bytes = _publicKeysManager.EncryptString(lobbyMemberInfo.PublicKeyInfo, lobbyCloudSessionInfoJson);
        var credentials = new LobbyCloudSessionCredentials();
        credentials.LobbyId = lobbyId;
        credentials.Info = bytes;
        credentials.Recipient = lobbyMemberInfo.ClientInstanceId;
        return credentials;
    }

    private string BuildLobbyCloudSessionInfoJson(CloudSessionResult cloudSessionResult, string lobbyId)
    {
        var lobbyCloudSessionInfo = new LobbyCloudSessionInfo();
        lobbyCloudSessionInfo.SessionId = cloudSessionResult.SessionId;
        lobbyCloudSessionInfo.SessionPassword = _sessionService.CloudSessionPassword!;
        lobbyCloudSessionInfo.LobbyId = lobbyId;
        var lobbyCloudSessionInfoJson = JsonHelper.Serialize(lobbyCloudSessionInfo);
        return lobbyCloudSessionInfoJson;
    }

    public async Task OnLobbyCloudSessionCredentialsSent(LobbyCloudSessionCredentials lobbyCloudSessionCredentials)
    {
        // Exception potentielle catchée par l'appelant
        if (await _lobbyRepository.IsEverythingOKBeforeSession(lobbyCloudSessionCredentials.LobbyId))
        {
            var json = _publicKeysManager.DecryptString(lobbyCloudSessionCredentials.Info);
            var lobbyCloudSessionInfo = JsonHelper.Deserialize<LobbyCloudSessionInfo>(json);

            var lobbySessionDetails = await _lobbyRepository.BuildCloudProfileSessionDetails(lobbyCloudSessionInfo.LobbyId);
            
            await _joinSessionService.JoinSession(lobbyCloudSessionInfo.SessionId, lobbyCloudSessionInfo.SessionPassword,
                lobbySessionDetails);

            _navigationService.NavigateTo(NavigationPanel.CloudSynchronization);

            await DoExitLobby(lobbyCloudSessionInfo.LobbyId);
        }
        else
        {
            throw new Exception("The current state does not allow to join the session");
        }
    }
}