using System.Threading;
using System.Threading.Tasks;
using ByteSync.Business.Communications;
using ByteSync.Common.Business.Sessions.Cloud.Connections;
using ByteSync.Interfaces.Controls.Applications;
using ByteSync.Interfaces.Controls.Communications;
using ByteSync.Interfaces.Controls.Communications.Http;
using ByteSync.Interfaces.Controls.Sessions;
using ByteSync.Interfaces.Repositories;
using ByteSync.Interfaces.Services.Sessions.Connecting;

namespace ByteSync.Services.Sessions.Connecting;

public class CheckCloudSessionPasswordExchangeKeyService : ICheckCloudSessionPasswordExchangeKeyService
{
    private readonly IEnvironmentService _environmentService;
    private readonly ITrustProcessPublicKeysRepository _trustProcessPublicKeysRepository;
    private readonly IPublicKeysManager _publicKeysManager;
    private readonly ISessionService _sessionService;
    private readonly ICloudSessionConnectionRepository _cloudSessionConnectionRepository;
    private readonly ICloudSessionApiClient _cloudSessionApiClient;
    private readonly ILogger<CheckCloudSessionPasswordExchangeKeyService> _logger;
    
    private const string UNKNOWN_RECEIVED_SESSION_ID = "unknown received sessionId {sessionId}";
    private const string PUBLIC_KEY_IS_NOT_TRUSTED = "Public key is not trusted";
    
    public CheckCloudSessionPasswordExchangeKeyService(
        IEnvironmentService environmentService,
        ITrustProcessPublicKeysRepository trustProcessPublicKeysRepository,
        IPublicKeysManager publicKeysManager,
        ISessionService sessionService,
        ICloudSessionConnectionRepository cloudSessionConnectionRepository,
        ICloudSessionApiClient cloudSessionApiClient,
        ILogger<CheckCloudSessionPasswordExchangeKeyService> logger)
    {
        _environmentService = environmentService;
        _trustProcessPublicKeysRepository = trustProcessPublicKeysRepository;
        _publicKeysManager = publicKeysManager;
        _sessionService = sessionService;
        _cloudSessionConnectionRepository = cloudSessionConnectionRepository;
        _cloudSessionApiClient = cloudSessionApiClient;
        _logger = logger;
    }
    
    public async Task Process(AskJoinCloudSessionParameters request)
    {
        try
        {
            if (!_environmentService.ClientInstanceId.Equals(request.ValidatorInstanceId))
            {
                _logger.LogWarning("unexpected password check request received with ValidatorId {validatorId}", request.ValidatorInstanceId);
                return;
            }

            var keyCheckData = await _trustProcessPublicKeysRepository.GetLocalPublicKeyCheckData(request.SessionId, request.JoinerClientInstanceId);
            var publicKeyInfo = keyCheckData?.OtherPartyPublicKeyInfo;
            if (publicKeyInfo != null)
            {
                var isTrusted = _publicKeysManager.IsTrusted(publicKeyInfo);
                if (!isTrusted)
                {
                    throw new Exception(PUBLIC_KEY_IS_NOT_TRUSTED);
                }

                try
                {
                    var rawPassword = _publicKeysManager.DecryptString(request.EncryptedPassword);
                    ExchangePassword exchangePassword = new(rawPassword);
                    if (exchangePassword.IsMatch(request.SessionId, request.JoinerClientInstanceId, _sessionService.CloudSessionPassword!))
                    {
                        var encryptedAesKey = _publicKeysManager.EncryptBytes(publicKeyInfo,
                            _cloudSessionConnectionRepository.GetAesEncryptionKey()!);

                        ValidateJoinCloudSessionParameters outParameters = new(request, encryptedAesKey);

                        _logger.LogInformation("...Password successfully checked for client {clientId}", request.JoinerClientInstanceId);
                        
                        await _cloudSessionApiClient.ValidateJoinCloudSession(outParameters);
                    }
                    else
                    {
                        await _cloudSessionApiClient.InformPasswordIsWrong(request.SessionId, request.JoinerClientInstanceId);

                        _logger.LogInformation("...Password checked failed for client {clientId}", request.JoinerClientInstanceId);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "...Password checked failed with error for client {clientId}", request.JoinerClientInstanceId);
                }
            }
            else
            {
                _logger.LogWarning("...Can not find encryption key for client {clientId}", request.JoinerClientInstanceId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OnCheckCloudSessionPasswordExchangeKey");
        }
    }
}