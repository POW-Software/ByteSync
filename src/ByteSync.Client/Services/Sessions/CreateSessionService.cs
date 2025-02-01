using System.Threading.Tasks;
using ByteSync.Business.SessionMembers;
using ByteSync.Business.Sessions;
using ByteSync.Business.Sessions.Connecting;
using ByteSync.Common.Business.Sessions.Cloud.Connections;
using ByteSync.Interfaces.Controls.Applications;
using ByteSync.Interfaces.Controls.Communications;
using ByteSync.Interfaces.Controls.Communications.Http;
using ByteSync.Interfaces.Controls.Encryptions;
using ByteSync.Interfaces.Repositories;
using ByteSync.Interfaces.Services.Sessions;
using ByteSync.Interfaces.Services.Sessions.Connecting;

namespace ByteSync.Services.Sessions;

public class CreateSessionService : ICreateSessionService
{
    private readonly ICloudSessionConnectionRepository _cloudSessionConnectionRepository;
    private readonly IDataEncrypter _dataEncrypter;
    private readonly IEnvironmentService _environmentService;
    private readonly ICloudSessionApiClient _cloudSessionApiClient;
    private readonly IPublicKeysManager _publicKeysManager;
    private readonly ITrustProcessPublicKeysRepository _trustProcessPublicKeysRepository;
    private readonly IDigitalSignaturesRepository _digitalSignaturesRepository;
    private readonly IAfterJoinSessionService _afterJoinSessionService;
    private readonly ICloudSessionConnector _cloudSessionConnector;
    private readonly ILogger<CreateSessionService> _logger;

    public CreateSessionService(ICloudSessionConnectionRepository cloudSessionConnectionRepository, 
        IDataEncrypter dataEncrypter, 
        IEnvironmentService environmentService, 
        ICloudSessionApiClient cloudSessionApiClient, 
        IPublicKeysManager publicKeysManager, 
        ITrustProcessPublicKeysRepository trustProcessPublicKeysRepository, 
        IDigitalSignaturesRepository digitalSignaturesRepository,
        IAfterJoinSessionService afterJoinSessionService,
        ICloudSessionConnector cloudSessionConnector,
        ILogger<CreateSessionService> logger)
    {
        _cloudSessionConnectionRepository = cloudSessionConnectionRepository;
        _dataEncrypter = dataEncrypter;
        _environmentService = environmentService;
        _cloudSessionApiClient = cloudSessionApiClient;
        _publicKeysManager = publicKeysManager;
        _trustProcessPublicKeysRepository = trustProcessPublicKeysRepository;
        _digitalSignaturesRepository = digitalSignaturesRepository;
        _afterJoinSessionService = afterJoinSessionService;
        _cloudSessionConnector = cloudSessionConnector;
        _logger = logger;
    }
    
    public async Task<CloudSessionResult?> CreateCloudSession(CreateCloudSessionRequest request)
    {
        try
        {
            await _cloudSessionConnector.InitializeConnection(SessionConnectionStatus.CreatingSession);
            
            // await Task.Delay(5000, _cloudSessionConnectionRepository.CancellationToken);
            
            var createCloudSessionParameters = BuildCreateCloudSessionParameters(request);
            var cloudSessionResult = await _cloudSessionApiClient.CreateCloudSession(createCloudSessionParameters, 
                _cloudSessionConnectionRepository.CancellationToken);

            if (_cloudSessionConnectionRepository.CancellationToken.IsCancellationRequested)
            {
                throw new TaskCanceledException();
            }
            
            await _trustProcessPublicKeysRepository.Start(cloudSessionResult.SessionId);
            await _digitalSignaturesRepository.Start(cloudSessionResult.SessionId);
    
            await _afterJoinSessionService.Process(new AfterJoinSessionRequest(cloudSessionResult, request.RunCloudSessionProfileInfo, true));
            
            _cloudSessionConnectionRepository.SetConnectionStatus(SessionConnectionStatus.InSession);
            
            _logger.LogInformation("Created Cloud Session {CloudSession}", cloudSessionResult.SessionId);

            return cloudSessionResult;
        }
        catch (Exception)
        {
            await _cloudSessionConnector.InitializeConnection(SessionConnectionStatus.NoSession);

            throw;
        }
    }

    public Task CancelCreateCloudSession()
    {
        _logger.LogInformation("User requested to cancel Cloud Session creation");
        _cloudSessionConnectionRepository.CancellationTokenSource.Cancel();
        
        return Task.CompletedTask;
    }

    private CreateCloudSessionParameters BuildCreateCloudSessionParameters(CreateCloudSessionRequest request)
    {
        SessionSettings sessionSettings;
        if (request.RunCloudSessionProfileInfo == null)
        {
            sessionSettings = SessionSettings.BuildDefault();
        }
        else
        {
            sessionSettings = request.RunCloudSessionProfileInfo.ProfileDetails.Options.Settings;
        }
            
        var encryptedSessionSettings = _dataEncrypter.EncryptSessionSettings(sessionSettings);

        var sessionMemberPrivateData = new SessionMemberPrivateData
        {
            MachineName = _environmentService.MachineName
        };
        var encryptedSessionMemberPrivateData = _dataEncrypter.EncryptSessionMemberPrivateData(sessionMemberPrivateData);
            
        var parameters = new CreateCloudSessionParameters
        {
            LobbyId = request.RunCloudSessionProfileInfo?.LobbyId,
            CreatorProfileClientId = request.RunCloudSessionProfileInfo?.LocalProfileClientId,
            SessionSettings = encryptedSessionSettings,
            CreatorPublicKeyInfo = _publicKeysManager.GetMyPublicKeyInfo(),
            CreatorPrivateData = encryptedSessionMemberPrivateData
        };
        return parameters;
    }

    // private async Task ClearConnectionData()
    // {
    //     await Task.WhenAll(
    //         _cloudSessionConnectionRepository.ClearAsync(), 
    //         _trustProcessPublicKeysRepository.ClearAsync(), 
    //         _digitalSignaturesRepository.ClearAsync());
    // }
    
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
    //         if (!sessionMemberInfo.HasClientInstanceId(_environmentService.ClientInstanceId))
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
}