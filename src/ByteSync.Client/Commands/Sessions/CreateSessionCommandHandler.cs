using System.Threading;
using System.Threading.Tasks;
using System.Security.Cryptography;
using ByteSync.Business.SessionMembers;
using ByteSync.Business.Sessions;
using ByteSync.Common.Business.Sessions.Cloud.Connections;
using ByteSync.Services.Applications;
using ByteSync.Services.Communications;
using ByteSync.Services.Communications.Api;
using ByteSync.Services.Encryptions;
using ByteSync.Services.Sessions;
using MediatR;

namespace ByteSync.Commands.Sessions;

public class CreateSessionCommandHandler : IRequestHandler<CreateSessionRequest, CloudSessionResult?>
{
    private readonly CloudSessionConnectionRepository _cloudSessionConnectionRepository;
    private readonly DataEncrypter _dataEncrypter;
    private readonly EnvironmentService _environmentService;
    private readonly CloudSessionApiClient _cloudSessionApiClient;
    private readonly PublicKeysManager _publicKeysManager;
    private readonly TrustProcessPublicKeysRepository _trustProcessPublicKeysRepository;
    private readonly DigitalSignaturesRepository _digitalSignaturesRepository;
    private readonly SessionService _sessionService;
    private readonly IMediator _mediator;
    private readonly ILogger<CreateSessionCommandHandler> _logger;

    public CreateSessionCommandHandler(CloudSessionConnectionRepository cloudSessionConnectionRepository, 
        DataEncrypter dataEncrypter, 
        EnvironmentService environmentService, 
        CloudSessionApiClient cloudSessionApiClient, 
        PublicKeysManager publicKeysManager, 
        TrustProcessPublicKeysRepository trustProcessPublicKeysRepository, 
        DigitalSignaturesRepository digitalSignaturesRepository, 
        SessionService sessionService,
        IMediator mediator,
        ILogger<CreateSessionCommandHandler> logger)
    {
        _cloudSessionConnectionRepository = cloudSessionConnectionRepository;
        _dataEncrypter = dataEncrypter;
        _environmentService = environmentService;
        _cloudSessionApiClient = cloudSessionApiClient;
        _publicKeysManager = publicKeysManager;
        _trustProcessPublicKeysRepository = trustProcessPublicKeysRepository;
        _digitalSignaturesRepository = digitalSignaturesRepository;
        _sessionService = sessionService;
        _mediator = mediator;
        _logger = logger;
    }
    
    public async Task<CloudSessionResult?> Handle(CreateSessionRequest request, CancellationToken cancellationToken)
    {
        try
        {
            await ClearConnectionData();
            _cloudSessionConnectionRepository.SetConnectionStatus(ConnectionStatuses.CreatingSession);
            
            using var aes = Aes.Create();
            aes.GenerateKey();
            _cloudSessionConnectionRepository.SetAesEncryptionKey(aes.Key);
            
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
            var cloudSessionResult = await _cloudSessionApiClient.CreateCloudSession(parameters);
            
            await _trustProcessPublicKeysRepository.Start(cloudSessionResult.SessionId);
            await _digitalSignaturesRepository.Start(cloudSessionResult.SessionId);
    
            await _mediator.Send(new AfterCreateOrJoinSessionRequest(cloudSessionResult, request.RunCloudSessionProfileInfo, true), cancellationToken);

            _logger.LogInformation("Created Cloud Session {CloudSession}", cloudSessionResult.SessionId);

            return cloudSessionResult;
        }
        catch (Exception)
        {
            _sessionService.ClearCloudSession();

            throw;
        }
        finally
        {
            _cloudSessionConnectionRepository.SetConnectionStatus(ConnectionStatuses.None);
        }
    }
    
    private async Task ClearConnectionData()
    {
        await Task.WhenAll(
            _cloudSessionConnectionRepository.ClearAsync(), 
            _trustProcessPublicKeysRepository.ClearAsync(), 
            _digitalSignaturesRepository.ClearAsync());
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