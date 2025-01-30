using System.Text;
using System.Threading.Tasks;
using ByteSync.Business.Sessions.Connecting;
using ByteSync.Common.Helpers;
using ByteSync.Interfaces.Controls.Applications;
using ByteSync.Interfaces.Controls.Communications;
using ByteSync.Interfaces.Controls.Communications.Http;
using ByteSync.Interfaces.Controls.Encryptions;
using ByteSync.Interfaces.Controls.Inventories;
using ByteSync.Interfaces.Controls.Sessions;
using ByteSync.Interfaces.Repositories;
using ByteSync.Interfaces.Services.Sessions.Connecting;

namespace ByteSync.Services.Sessions.Connecting;

public class AfterJoinSessionService : IAfterJoinSessionService
{
    private readonly ICloudSessionApiClient _cloudSessionApiClient;
    private readonly ISessionService _sessionService;
    private readonly ISessionMemberService _sessionMemberService;
    private readonly IPathItemsService _pathItemsService;
    private readonly IDataEncrypter _dataEncrypter;
    private readonly ICloudSessionConnectionRepository _cloudSessionConnectionRepository;
    private readonly IInventoryApiClient _inventoryApiClient;
    private readonly IDigitalSignaturesRepository _digitalSignaturesRepository;
    private readonly IEnvironmentService _environmentService;
    private readonly ICloudSessionConnector _cloudSessionConnector;
    private readonly ILogger<AfterJoinSessionService> _logger;
    private readonly IQuitSessionService _quitSessionService;

    public AfterJoinSessionService(
        ICloudSessionApiClient cloudSessionApiClient,
        ISessionService sessionService,
        ISessionMemberService sessionMemberService,
        IPathItemsService pathItemsService,
        IDataEncrypter dataEncrypter,
        ICloudSessionConnectionRepository cloudSessionConnectionRepository,
        IInventoryApiClient inventoryApiClient,
        IDigitalSignaturesRepository digitalSignaturesRepository,
        IEnvironmentService environmentService,
        ICloudSessionConnector cloudSessionConnector,
        IQuitSessionService quitSessionService,
        ILogger<AfterJoinSessionService> logger)
    {
        _cloudSessionApiClient = cloudSessionApiClient;
        _sessionService = sessionService;
        _sessionMemberService = sessionMemberService;
        _pathItemsService = pathItemsService;
        _dataEncrypter = dataEncrypter;
        _cloudSessionConnectionRepository = cloudSessionConnectionRepository;
        _inventoryApiClient = inventoryApiClient;
        _digitalSignaturesRepository = digitalSignaturesRepository;
        _environmentService = environmentService;
        _cloudSessionConnector = cloudSessionConnector;
        _quitSessionService = quitSessionService;
        _logger = logger;
    }
    
    public async Task Process(AfterJoinSessionRequest request)
    {
        var sessionMemberInfoDtos = await _cloudSessionApiClient.GetMembers(request.CloudSessionResult.SessionId);
        
        // On contrôle que chacun des autres membres est Auth-Checked
        var areAllMemberAuthOK = true;
        foreach (var sessionMemberInfo in sessionMemberInfoDtos)
        {
            if (!sessionMemberInfo.HasClientInstanceId(_environmentService.ClientInstanceId))
            {
                if (! await _digitalSignaturesRepository.IsAuthChecked(request.CloudSessionResult.SessionId, sessionMemberInfo))
                {
                    _logger.LogWarning("Digital Signature not checked for Client {ClientInstanceId}", sessionMemberInfo.ClientInstanceId);
                    areAllMemberAuthOK = false;
                }
            }
        }

        if (!areAllMemberAuthOK)
        {
            _logger.LogWarning("Auth check failed, quitting session");
            
            await _cloudSessionConnector.ClearConnectionData();
            
            await _quitSessionService.Process();
            
            throw new Exception("Auth check failed, quitting session");
        }
        
        var sessionSettings = _dataEncrypter.DecryptSessionSettings(request.CloudSessionResult.SessionSettings);

        await _sessionService.SetCloudSession(request.CloudSessionResult.CloudSession, request.RunCloudSessionProfileInfo, sessionSettings);
        string password;
        if (request.IsCreator)
        {
            password = GeneratePassword();
        }
        else
        {
            password = (await _cloudSessionConnectionRepository.GetTempSessionPassword(request.CloudSessionResult.SessionId))!;
        }
        _sessionService.SetPassword(password.ToUpper());

        
        
        _sessionMemberService.AddOrUpdate(sessionMemberInfoDtos);
        
        if (request.RunCloudSessionProfileInfo != null)
        {
            var myPathItems = request.RunCloudSessionProfileInfo.GetMyPathItems();

            // var pathItemsViewModels = _pathItemsService.GetMyPathItems()!;
            foreach (var pathItem in myPathItems)
            {
                await _pathItemsService.CreateAndAddPathItem(pathItem.Path, pathItem.Type);
                
                // pathItemsViewModels.Add(new PathItemViewModel(pathItem));
                
                // var encryptedPathItem = dataEncrypter.EncryptPathItem(pathItem); 
                //
                // // PathItemEncrypter pathItemEncrypter = _sessionObjectsFactory.BuildPathItemEncrypter();
                // // var sharedPathItem = pathItemEncrypter.Encrypt(pathItem);
                // await _connectionManager.HubWrapper.SetPathItemAdded(cloudSessionResult.SessionId, encryptedPathItem);
            }
            
            // await _connectionManager.
        }

        foreach (var sessionMemberInfo in sessionMemberInfoDtos)
        {
            if (!sessionMemberInfo.HasClientInstanceId(_environmentService.ClientInstanceId))
            {
                var encryptedPathItems = await _inventoryApiClient.GetPathItems(request.CloudSessionResult.SessionId, sessionMemberInfo.ClientInstanceId);

                if (encryptedPathItems != null)
                {
                    foreach (var encryptedPathItem in encryptedPathItems)
                    {
                        var pathItem = _dataEncrypter.DecryptPathItem(encryptedPathItem);
                        await _pathItemsService.AddPathItem(pathItem);
                    }
                }
            }
        }
    }
    
    private string GeneratePassword()
    {
        var sb = new StringBuilder();
        for (var i = 0; i < 5; i++)
        {
            sb.Append(RandomUtils.GetRandomLetter(true));
        }
        
        return sb.ToString();
    }
}