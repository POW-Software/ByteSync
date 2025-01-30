using System.Reactive.Linq;
using System.Threading.Tasks;
using ByteSync.Business.Sessions;
using ByteSync.Business.Sessions.RunSessionInfos;
using ByteSync.Common.Business.Sessions.Cloud.Connections;
using ByteSync.Common.Helpers;
using ByteSync.Interfaces.Controls.Applications;
using ByteSync.Interfaces.Controls.Communications;
using ByteSync.Interfaces.Controls.Communications.Http;
using ByteSync.Interfaces.Controls.Encryptions;
using ByteSync.Interfaces.Controls.Inventories;
using ByteSync.Interfaces.Controls.Navigations;
using ByteSync.Interfaces.Controls.Sessions;
using ByteSync.Interfaces.Controls.Synchronizations;
using ByteSync.Interfaces.EventsHubs;
using ByteSync.Interfaces.Repositories;
using ByteSync.Interfaces.Services.Sessions.Connecting;
using ConnectionStatuses = ByteSync.Business.Sessions.ConnectionStatuses;

namespace ByteSync.Services.Sessions.Connecting;

class CloudSessionConnector : ICloudSessionConnector
{
    // private static Serilog.ILogger Log => Serilog.Log.Logger;
    private static Serilog.ILogger Log => Serilog.Log.ForContext<CloudSessionConnector>();
    
    private readonly ICloudProxy _connectionManager;
    private readonly ICloudSessionConnectionRepository _cloudSessionConnectionRepository;
    private readonly IPublicKeysManager _publicKeysManager;
    private readonly ICloudSessionEventsHub _cloudSessionEventsHub;
    private readonly IDigitalSignaturesRepository _digitalSignaturesRepository;
    private readonly ITrustProcessPublicKeysRepository _trustProcessPublicKeysRepository;
    private readonly ISessionService _sessionService;
    private readonly ISynchronizationService _synchronizationService;
    private readonly IPathItemsService _pathItemsService;
    private readonly INavigationService _navigationService;
    private readonly IDataEncrypter _dataEncrypter;
    private readonly ICloudSessionApiClient _cloudSessionApiClient;
    private readonly IPublicKeysTruster _publicKeysTruster;
    private readonly IDigitalSignaturesChecker _digitalSignaturesChecker;
    private readonly IInventoryApiClient _inventoryApiClient;
    private readonly ISessionMemberService _sessionMemberService;
    private readonly IEnvironmentService _environmentService;

    private const string UNKNOWN_RECEIVED_SESSION_ID = "unknown received sessionId {sessionId}";
    private const string PUBLIC_KEY_IS_NOT_TRUSTED = "Public key is not trusted";
    
    public CloudSessionConnector(ICloudProxy connectionManager, ICloudSessionConnectionRepository cloudSessionConnectionRepository, 
        IPublicKeysManager publicKeysManager, ICloudSessionEventsHub cloudSessionEventsHub, IDigitalSignaturesRepository digitalSignaturesRepository, 
        ITrustProcessPublicKeysRepository trustPublicKeysRepository, ISessionService sessionService, ISynchronizationService synchronizationService, 
        IPathItemsService pathItemsService, INavigationService navigationService, 
        IDataEncrypter dataEncrypter, ICloudSessionApiClient cloudSessionApiClient, IPublicKeysTruster publicKeysTruster,
        IDigitalSignaturesChecker digitalSignaturesChecker, IInventoryApiClient inventoryApiClient, ISessionMemberService sessionMemberService,
        IEnvironmentService environmentService)
    {
        _connectionManager = connectionManager;
        _cloudSessionConnectionRepository = cloudSessionConnectionRepository;
        _publicKeysManager = publicKeysManager;
        _cloudSessionEventsHub = cloudSessionEventsHub;
        _digitalSignaturesRepository = digitalSignaturesRepository;
        _trustProcessPublicKeysRepository = trustPublicKeysRepository;
        _sessionService = sessionService;
        _synchronizationService = synchronizationService;
        _pathItemsService = pathItemsService;
        _navigationService = navigationService;
        _dataEncrypter = dataEncrypter;
        _cloudSessionApiClient = cloudSessionApiClient;
        _publicKeysTruster = publicKeysTruster;
        _digitalSignaturesChecker = digitalSignaturesChecker;
        _inventoryApiClient = inventoryApiClient;
        _sessionMemberService = sessionMemberService;
        _environmentService = environmentService;

        // _connectionManager.HubPushHandler2.YouJoinedSession
        //     .Subscribe(OnYouJoinedSession);
        
        // _connectionManager.HubPushHandler2.YouGaveAWrongPassword
        //     .Subscribe(OnYouGaveAWrongPassword);
        //
        // _connectionManager.HubPushHandler2.AskCloudSessionPasswordExchangeKey
        //     .Subscribe(OnCloudSessionPasswordExchangeKeyAsked);
        //
        // _connectionManager.HubPushHandler2.GiveCloudSessionPasswordExchangeKey
        //     .Subscribe(OnCloudSessionPasswordExchangeKeyGiven);
        
        // _connectionManager.HubPushHandler2.CheckCloudSessionPasswordExchangeKey
        //     .Subscribe(OnCheckCloudSessionPasswordExchangeKey);
        
        // _connectionManager.HubPushHandler2.OnReconnected
        //     .Subscribe(OnReconnected);
    }

    // public async Task<CloudSessionResult?> CreateSession(RunCloudSessionProfileInfo? lobbySessionDetails)
    // {
    //     try
    //     {
    //         await ClearConnectionData();
    //         _cloudSessionConnectionRepository.SetConnectionStatus(ConnectionStatuses.CreatingSession);
    //         
    //         using var aes = Aes.Create();
    //         aes.GenerateKey();
    //         _cloudSessionConnectionRepository.SetAesEncryptionKey(aes.Key);
    //         
    //         SessionSettings sessionSettings;
    //         if (lobbySessionDetails == null)
    //         {
    //             sessionSettings = SessionSettings.BuildDefault();
    //         }
    //         else
    //         {
    //             sessionSettings = lobbySessionDetails.ProfileDetails.Options.Settings;
    //         }
    //         
    //         var encryptedSessionSettings = _dataEncrypter.EncryptSessionSettings(sessionSettings);
    //
    //         var sessionMemberPrivateData = new SessionMemberPrivateData
    //         {
    //             MachineName = _environmentService.MachineName
    //         };
    //         var encryptedSessionMemberPrivateData = _dataEncrypter.EncryptSessionMemberPrivateData(sessionMemberPrivateData);
    //         
    //         var parameters = new CreateCloudSessionParameters
    //         {
    //             LobbyId = lobbySessionDetails?.LobbyId,
    //             CreatorProfileClientId = lobbySessionDetails?.LocalProfileClientId,
    //             SessionSettings = encryptedSessionSettings,
    //             CreatorPublicKeyInfo = _publicKeysManager.GetMyPublicKeyInfo(),
    //             CreatorPrivateData = encryptedSessionMemberPrivateData
    //         };
    //         var cloudSessionResult = await _cloudSessionApiClient.CreateCloudSession(parameters);
    //         
    //         await _trustProcessPublicKeysRepository.Start(cloudSessionResult.SessionId);
    //         await _digitalSignaturesRepository.Start(cloudSessionResult.SessionId);
    //
    //         await AfterSessionCreatedOrJoined(cloudSessionResult, lobbySessionDetails, true);
    //
    //         Log.Information("Created Cloud Session {CloudSession}", cloudSessionResult.SessionId);
    //
    //         return cloudSessionResult;
    //     }
    //     catch (Exception)
    //     {
    //         _sessionService.ClearCloudSession();
    //
    //         throw;
    //     }
    //     finally
    //     {
    //         _cloudSessionConnectionRepository.SetConnectionStatus(ConnectionStatuses.None);
    //     }
    // }

    // public async Task QuitSession()
    // {
    //     var session = _sessionService.CurrentSession;
    //
    //     if (session == null)
    //     {
    //         Log.Information("Can not quit Session: unknown Session");
    //         return;
    //     }
    //
    //     if (session is CloudSession)
    //     {
    //         try
    //         {
    //             // Ici, le but est d'essayer de quitter la session, sans bloquer l'utilisateur pour autant si cela échoue
    //             await _cloudSessionApiClient.QuitCloudSession(session.SessionId);
    //         }
    //         catch (Exception ex)
    //         {
    //             Log.Here().Error(ex, "Error durant calling Hub... continuing exit of the session");
    //         }
    //     }
    //     
    //     await ClearConnectionData();
    //     _sessionService.ClearCloudSession();
    //
    //     if (session is CloudSession)
    //     {
    //         Log.Information("Quitted Cloud Session {CloudSession}", session.SessionId);
    //     }
    //     else
    //     {
    //         Log.Information("Quitted Local Session {CloudSession}", session.SessionId);
    //     }
    //
    //     
    //     _navigationService.NavigateTo(NavigationPanel.Home);
    //     // _cloudSessionEventsHub.RaiseCloudSessionQuitted();
    // }

    public async Task ClearConnectionData()
    {
        await Task.WhenAll(
            _cloudSessionConnectionRepository.ClearAsync(), 
            _trustProcessPublicKeysRepository.ClearAsync(), 
            _digitalSignaturesRepository.ClearAsync());
    }

    public IObservable<bool> CanLogOutOrShutdown 
    {
        get
        {
            return Observable.CombineLatest(_cloudSessionConnectionRepository.ConnectionStatusObservable,
                _sessionService.SessionObservable, _synchronizationService.SynchronizationProcessData.SynchronizationEnd, 
                _sessionService.SessionStatusObservable,
                (connectionStatus, session, synchronizationEnd, sessionStatus) =>
                    !connectionStatus.In(ConnectionStatuses.CreatingSession, ConnectionStatuses.JoiningSession) &&
                    (session == null || synchronizationEnd != null)
                    && sessionStatus.In(SessionStatus.FatalError, SessionStatus.None, SessionStatus.RegularEnd));
            
            
            // bool result = !_cloudSessionConnectionDataHolder.IsCreatingOrJoiningSession() &&
            //               (_sessionDataHolder.Session == null || _sessionDataHolder.IsSynchronizationEnded);
            //
            // return result;
        }
    }

    // /// <summary>
    // /// Après que l'on ait créé ou rejoint une session
    // /// </summary>
    // /// <param name="cloudSessionResult"></param>
    // /// <param name="runCloudSessionProfileInfo"></param>
    // /// <param name="isCreator"></param>
    // private async Task AfterSessionCreatedOrJoined(CloudSessionResult cloudSessionResult, RunCloudSessionProfileInfo? runCloudSessionProfileInfo, 
    //     bool isCreator)
    // {
    //     var sessionMemberInfoDtos = await _cloudSessionApiClient.GetMembers(cloudSessionResult.SessionId);
    //     
    //     // On contrôle que chacun des autres membres est Auth-Checked
    //     var areAllMemberAuthOK = true;
    //     foreach (var sessionMemberInfo in sessionMemberInfoDtos)
    //     {
    //         if (!sessionMemberInfo.HasClientInstanceId(_connectionManager.ClientInstanceId))
    //         {
    //             if (! await _digitalSignaturesRepository.IsAuthChecked(cloudSessionResult.SessionId, sessionMemberInfo))
    //             {
    //                 Log.Warning("Digital Signature not checked for Client {ClientInstanceId}", sessionMemberInfo.ClientInstanceId);
    //                 areAllMemberAuthOK = false;
    //             }
    //         }
    //     }
    //
    //     if (!areAllMemberAuthOK)
    //     {
    //         Log.Here().Warning("Auth check failed, quitting session");
    //         
    //         await ClearConnectionData();
    //         await QuitSession();
    //
    //         throw new Exception("Auth check failed, quitting session");
    //     }
    //     
    //     var sessionSettings = _dataEncrypter.DecryptSessionSettings(cloudSessionResult.SessionSettings);
    //
    //     await _sessionService.SetCloudSession(cloudSessionResult.CloudSession, runCloudSessionProfileInfo, sessionSettings);
    //     string password;
    //     if (isCreator)
    //     {
    //         password = GeneratePassword();
    //     }
    //     else
    //     {
    //         password = (await _cloudSessionConnectionRepository.GetTempSessionPassword(cloudSessionResult.SessionId))!;
    //     }
    //     _sessionService.SetPassword(password.ToUpper());
    //
    //     
    //     
    //     _sessionMemberService.AddOrUpdate(sessionMemberInfoDtos);
    //     
    //     if (runCloudSessionProfileInfo != null)
    //     {
    //         var myPathItems = runCloudSessionProfileInfo.GetMyPathItems();
    //
    //         // var pathItemsViewModels = _pathItemsService.GetMyPathItems()!;
    //         foreach (var pathItem in myPathItems)
    //         {
    //             await _pathItemsService.CreateAndAddPathItem(pathItem.Path, pathItem.Type);
    //             
    //             // pathItemsViewModels.Add(new PathItemViewModel(pathItem));
    //             
    //             // var encryptedPathItem = dataEncrypter.EncryptPathItem(pathItem); 
    //             //
    //             // // PathItemEncrypter pathItemEncrypter = _sessionObjectsFactory.BuildPathItemEncrypter();
    //             // // var sharedPathItem = pathItemEncrypter.Encrypt(pathItem);
    //             // await _connectionManager.HubWrapper.SetPathItemAdded(cloudSessionResult.SessionId, encryptedPathItem);
    //         }
    //         
    //         // await _connectionManager.
    //     }
    //
    //     foreach (var sessionMemberInfo in sessionMemberInfoDtos)
    //     {
    //         if (!sessionMemberInfo.HasClientInstanceId(_connectionManager.ClientInstanceId))
    //         {
    //             var encryptedPathItems = await _inventoryApiClient.GetPathItems(cloudSessionResult.SessionId, sessionMemberInfo.ClientInstanceId);
    //
    //             if (encryptedPathItems != null)
    //             {
    //                 foreach (var encryptedPathItem in encryptedPathItems)
    //                 {
    //                     var pathItem = _dataEncrypter.DecryptPathItem(encryptedPathItem);
    //                     await _pathItemsService.AddPathItem(pathItem);
    //                 }
    //             }
    //         }
    //     }
    // }

    // private string GeneratePassword()
    // {
    //     var sb = new StringBuilder();
    //     for (var i = 0; i < 5; i++)
    //     {
    //         sb.Append(RandomUtils.GetRandomLetter(true));
    //     }
    //     
    //     return sb.ToString();
    // }

    public async Task JoinSession(string sessionId, string sessionPassword, RunCloudSessionProfileInfo? lobbySessionDetails)
    {
        try
        {
            await DoStartJoinSession(sessionId, sessionPassword, lobbySessionDetails);
        }
        catch (Exception ex)
        {
            var joinSessionResult = JoinSessionResult.BuildFrom(JoinSessionStatuses.UnexpectedError);
            await OnJoinSessionError(joinSessionResult);

            throw;
        }
    }

    private async Task DoStartJoinSession(string sessionId, string sessionPassword, RunCloudSessionProfileInfo? lobbySessionDetails)
    {
        if (sessionId.IsNotEmpty(true) && sessionId.Equals(_sessionService.SessionId))
        {
            return;
        }

        _cloudSessionConnectionRepository.SetConnectionStatus(ConnectionStatuses.JoiningSession);
        await ClearConnectionData();

        await _trustProcessPublicKeysRepository.Start(sessionId);
        await _digitalSignaturesRepository.Start(sessionId);

        Log.Information("Start joining the Cloud Session {sessionId}: getting password exchange encryption key", sessionId);

        await _cloudSessionConnectionRepository.SetCloudSessionConnectionData(sessionId, sessionPassword, lobbySessionDetails);

        JoinSessionResult joinSessionResult;
        // On Fait un processus de Trust pour les clés qui ne sont pas trustées

        joinSessionResult = await _publicKeysTruster.TrustAllMembersPublicKeys(sessionId);
        if (!joinSessionResult.IsOK)
        {
            await OnJoinSessionError(joinSessionResult);
            return;
        }

        // Quand tout est trusté, on peut contrôler les clés
        // Contruction digital signature : sessionId, monClientInstanceId, 
        // Protection: mix clientInstanceId / InstallationId / SessionId en SHA 256

        var parameters = new AskCloudSessionPasswordExchangeKeyParameters(sessionId, _publicKeysManager.GetMyPublicKeyInfo());
        parameters.LobbyId = lobbySessionDetails?.LobbyId;
        parameters.ProfileClientId = lobbySessionDetails?.LocalProfileClientId;
        joinSessionResult = await _cloudSessionApiClient.AskPasswordExchangeKey(parameters);

        if (!joinSessionResult.IsOK)
        {
            await OnJoinSessionError(joinSessionResult);
        }
        else
        {
            await _cloudSessionConnectionRepository.WaitOrThrowAsync(sessionId,
                data => data.WaitForPasswordExchangeKeyEvent, data => data.WaitTimeSpan, "Keys exchange failed: no key received");
        }
    }

    public async Task OnJoinSessionError(JoinSessionResult joinSessionResult)
    {
        _cloudSessionConnectionRepository.SetConnectionStatus(ConnectionStatuses.None);
        await ClearConnectionData();
            
        Log.Error("Can not join the Cloud Session. Reason: {Reason}", joinSessionResult.Status);
        await _cloudSessionEventsHub.RaiseJoinCloudSessionFailed(joinSessionResult);
    }

    // private async void OnYouJoinedSession((CloudSessionResult cloudSessionResult, ValidateJoinCloudSessionParameters parameters) tuple)
    // {
    //     try
    //     {
    //         if (!await _cloudSessionConnectionRepository.CheckConnectingCloudSession(tuple.cloudSessionResult.CloudSession.SessionId))
    //         {
    //             Log.Here().Error(UNKNOWN_RECEIVED_SESSION_ID, tuple.cloudSessionResult.CloudSession.SessionId);
    //             return;
    //         }
    //
    //         if (!_connectionManager.ClientInstanceId.Equals(tuple.parameters.JoinerClientInstanceId))
    //         {
    //             Log.Here().Warning("unexpected session event received with JoinerId {joinerId}", tuple.parameters.JoinerClientInstanceId);
    //             return;
    //         }
    //
    //         if (_cloudSessionConnectionRepository.CurrentConnectionStatus != ConnectionStatuses.JoiningSession)
    //         {
    //             Log.Here().Warning("no longer trying to join session");
    //             return;
    //         }
    //         
    //         var isAuthOK = false;
    //         var cpt = 0;
    //         while (! isAuthOK)
    //         {
    //             cpt += 1;
    //             if (cpt == 5)
    //             {
    //                 Log.Here().Warning($"can not check auth. Too many tries");
    //                 return;
    //             }
    //             
    //             var sessionMembersClientInstanceIds = await _publicKeysTruster.TrustMissingMembersPublicKeys(tuple.cloudSessionResult.CloudSession.SessionId);
    //             if (sessionMembersClientInstanceIds == null)
    //             {
    //                 Log.Here().Warning($"can not check trust");
    //                 return;
    //             }
    //             
    //             isAuthOK = await _digitalSignaturesChecker.CheckExistingMembersDigitalSignatures(tuple.cloudSessionResult.CloudSession.SessionId, 
    //                 sessionMembersClientInstanceIds);
    //             if (!isAuthOK)
    //             {
    //                 Log.Here().Warning($"can not check auth");
    //                 return;
    //             }
    //
    //             var sessionMemberPrivateData = new SessionMemberPrivateData
    //             {
    //                 MachineName = _environmentService.MachineName
    //             };
    //             
    //             var aesEncryptionKey = _publicKeysManager.DecryptBytes(tuple.parameters.EncryptedAesKey);
    //             _cloudSessionConnectionRepository.SetAesEncryptionKey(aesEncryptionKey);
    //             
    //             var encryptedSessionMemberPrivateData = _dataEncrypter.EncryptSessionMemberPrivateData(sessionMemberPrivateData);
    //             var finalizeParameters = new FinalizeJoinCloudSessionParameters(tuple.parameters, encryptedSessionMemberPrivateData);
    //
    //             var finalizeJoinSessionResult = await _cloudSessionApiClient.FinalizeJoinCloudSession(finalizeParameters);
    //
    //             if (finalizeJoinSessionResult.Status == FinalizeJoinSessionStatuses.AuthIsNotChecked)
    //             {
    //                 isAuthOK = false;
    //                 await Task.Delay(TimeSpan.FromSeconds(1));
    //             }
    //             else if (!finalizeJoinSessionResult.IsOK)
    //             {
    //                 Log.Here().Warning($"error during join session finalization");
    //                 return;
    //             }
    //         }
    //
    //         try
    //         {
    //             var aesEncryptionKey = _publicKeysManager.DecryptBytes(tuple.parameters.EncryptedAesKey);
    //             _cloudSessionConnectionRepository.SetAesEncryptionKey(aesEncryptionKey);
    //
    //             Log.Here().Debug("...EncryptionKey received successfully");
    //         }
    //         catch (Exception ex)
    //         {
    //             Log.Here().Error(ex, "...Error during EncryptionKey reception");
    //             throw;
    //         }
    //
    //         var lobbySessionDetails = await _cloudSessionConnectionRepository
    //             .GetTempLobbySessionDetails(tuple.cloudSessionResult.CloudSession.SessionId);
    //         
    //         await AfterSessionCreatedOrJoined(tuple.cloudSessionResult, lobbySessionDetails, false);
    //         
    //         await _cloudSessionConnectionRepository.SetJoinSessionResultReceived(tuple.cloudSessionResult.CloudSession.SessionId);
    //
    //         // ReSharper disable once PossibleNullReferenceException
    //         Log.Information("JoinSession: {CloudSession}", tuple.cloudSessionResult.SessionId);
    //     }
    //     catch (Exception ex)
    //     {
    //         Log.Error(ex, "OnYouJoinedSession");
    //         
    //         _sessionService.ClearCloudSession();
    //     }
    //     finally
    //     {
    //         _cloudSessionConnectionRepository.SetConnectionStatus(ConnectionStatuses.None);
    //     }
    // }

    // private async void OnYouGaveAWrongPassword(string sessionId)
    // {
    //     try
    //     {
    //         if (!await _cloudSessionConnectionRepository.CheckConnectingCloudSession(sessionId))
    //         {
    //             Log.Here().Error(UNKNOWN_RECEIVED_SESSION_ID, sessionId);
    //         }
    //
    //         _cloudSessionConnectionRepository.SetConnectionStatus(ConnectionStatuses.None);
    //         
    //         await _cloudSessionEventsHub.RaiseJoinCloudSessionFailed(JoinSessionResult.BuildFrom(JoinSessionStatuses.WrondPassword));
    //     }
    //     catch (Exception ex)
    //     {
    //         Log.Error(ex, "OnYouGaveAWrongPassword");
    //     }
    // }

    // private async void OnCloudSessionPasswordExchangeKeyAsked(AskCloudSessionPasswordExchangeKeyPush askCloudSessionPasswordExchangeKeyPush)
    // {
    //     try
    //     {
    //         await OnCloudSessionPasswordExchangeKeyAskedAsync(askCloudSessionPasswordExchangeKeyPush);
    //     }
    //     catch (Exception ex)
    //     {
    //         Log.Error(ex, "OnCloudSessionPasswordExchangeKeyAsked");
    //     }
    // }

    // private async Task OnCloudSessionPasswordExchangeKeyAskedAsync(AskCloudSessionPasswordExchangeKeyPush pushData)
    // {
    //     var isTrusted = _publicKeysManager.IsTrusted(pushData.PublicKeyInfo);
    //     if (!isTrusted)
    //     {
    //         throw new Exception(PUBLIC_KEY_IS_NOT_TRUSTED);
    //     }
    //             
    //     var parameters = new GiveCloudSessionPasswordExchangeKeyParameters(pushData.SessionId, 
    //         pushData.RequesterInstanceId, _connectionManager.ClientInstanceId, 
    //         _publicKeysManager.GetMyPublicKeyInfo());
    //     await _cloudSessionApiClient.GiveCloudSessionPasswordExchangeKey(parameters);
    // }

    // private async void OnCloudSessionPasswordExchangeKeyGiven(GiveCloudSessionPasswordExchangeKeyParameters parameters)
    // {
    //     try
    //     {
    //         await OnCloudSessionPasswordExchangeKeyGivenAsync(parameters);
    //     }
    //     catch (Exception ex)
    //     {
    //         Log.Error(ex, "OnCloudSessionPasswordExchangeKeyGiven");
    //     }
    // }

    // /// <summary>
    // /// Appelé lorsqu'on reçoit les paramètres d'échange de clé
    // /// </summary>
    // /// <param name="parameters"></param>
    // private async Task OnCloudSessionPasswordExchangeKeyGivenAsync(GiveCloudSessionPasswordExchangeKeyParameters parameters)
    // {
    //     if (!await _cloudSessionConnectionRepository.CheckConnectingCloudSession(parameters.SessionId))
    //     {
    //         Log.Here().Error(UNKNOWN_RECEIVED_SESSION_ID, parameters.SessionId);
    //         return;
    //     }
    //     if (!_connectionManager.ClientInstanceId.Equals(parameters.JoinerInstanceId))
    //     {
    //         Log.Here().Warning("Unexpected password provide request received with JoinerId {joinerId}", parameters.JoinerInstanceId);
    //         return;
    //     }
    //         
    //     await _cloudSessionConnectionRepository.SetPasswordExchangeKeyReceived(parameters.SessionId);
    //     
    //     var isTrusted = _publicKeysManager.IsTrusted(parameters.PublicKeyInfo);
    //     if (isTrusted)
    //     {
    //         var password = await _cloudSessionConnectionRepository.GetTempSessionPassword(parameters.SessionId);
    //         ExchangePassword exchangePassword = new(parameters.SessionId, _connectionManager.ClientInstanceId, password!);
    //
    //         var encryptedPassword = _publicKeysManager.EncryptString(parameters.PublicKeyInfo, exchangePassword.Data);
    //         AskJoinCloudSessionParameters outParameters = new (parameters, encryptedPassword);
    //
    //         Log.Information("...Providing encrypted password to the validator");
    //         var joinSessionResult = await _cloudSessionApiClient.AskJoinCloudSession(outParameters);
    //
    //         if (!joinSessionResult.IsOK)
    //         {
    //             await OnJoinSessionError(joinSessionResult);
    //         }
    //         else
    //         {
    //             await _cloudSessionConnectionRepository.WaitOrThrowAsync(parameters.SessionId, 
    //                 data => data.WaitForJoinSessionEvent, data => data.WaitTimeSpan, "Join session failed");
    //         }
    //     }
    //     else
    //     {
    //         throw new Exception(PUBLIC_KEY_IS_NOT_TRUSTED);
    //     }
    // }

    // private async void OnCheckCloudSessionPasswordExchangeKey(AskJoinCloudSessionParameters parameters)
    // {
    //     try
    //     {
    //         await Task.Run(() => HandleOnCheckCloudSessionPasswordExchangeKey(parameters));
    //     }
    //     catch (Exception ex)
    //     {
    //         Log.Error(ex, "OnCheckCloudSessionPasswordExchangeKey");
    //     }
    // }
        
    // private async void OnReconnected(string sessionId)
    // {
    //     
    // }

    // private async void HandleOnCheckCloudSessionPasswordExchangeKey(AskJoinCloudSessionParameters parameters)
    // {
    //     if (!_connectionManager.ClientInstanceId.Equals(parameters.ValidatorInstanceId))
    //     {
    //         Log.Here().Warning("unexpected password check request received with ValidatorId {validatorId}", parameters.ValidatorInstanceId);
    //         return;
    //     }
    //
    //     var keyCheckData = await _trustProcessPublicKeysRepository.GetLocalPublicKeyCheckData(parameters.SessionId, parameters.JoinerClientInstanceId);
    //     var publicKeyInfo = keyCheckData?.OtherPartyPublicKeyInfo;
    //     if (publicKeyInfo != null)
    //     {
    //         var isTrusted = _publicKeysManager.IsTrusted(publicKeyInfo);
    //         if (!isTrusted)
    //         {
    //             throw new Exception(PUBLIC_KEY_IS_NOT_TRUSTED);
    //         }
    //         
    //         try
    //         {
    //             var rawPassword = _publicKeysManager.DecryptString(parameters.EncryptedPassword);
    //             ExchangePassword exchangePassword = new(rawPassword);
    //             if (exchangePassword.IsMatch(parameters.SessionId, parameters.JoinerClientInstanceId, _sessionService.CloudSessionPassword!))
    //             {
    //                 var encryptedAesKey = _publicKeysManager.EncryptBytes(publicKeyInfo, 
    //                     _cloudSessionConnectionRepository.GetAesEncryptionKey()!);
    //
    //                 ValidateJoinCloudSessionParameters outParameters = new (parameters, encryptedAesKey);
    //                     
    //                 Log.Information("...Password successfully checked for client {clientId}", parameters.JoinerClientInstanceId);
    //                     
    //                 // On informe le serveur que c'est OK
    //                 await _cloudSessionApiClient.ValidateJoinCloudSession(outParameters);
    //             }
    //             else
    //             {
    //                 // On informe le serveur que le mot de passe est erroné
    //                 await _cloudSessionApiClient.InformPasswordIsWrong(parameters.SessionId, parameters.JoinerClientInstanceId);
    //
    //                 Log.Information("...Password checked failed for client {clientId}", parameters.JoinerClientInstanceId);
    //             }
    //         }
    //         catch (Exception ex)
    //         {
    //             Log.Warning(ex, "...Password checked failed with error for client {clientId}", parameters.JoinerClientInstanceId);
    //         }
    //     }
    //     else
    //     {
    //         Log.Warning("...Can not find encryption key for client {clientId}", parameters.JoinerClientInstanceId);
    //     }
    // }
}