using ByteSync.Business.Communications;
using ByteSync.Common.Business.Sessions.Cloud.Connections;
using ByteSync.Interfaces.Controls.Applications;
using ByteSync.Interfaces.Controls.Communications;
using ByteSync.Interfaces.Controls.Communications.Http;
using ByteSync.Interfaces.Repositories;
using ByteSync.Interfaces.Services.Sessions.Connecting;
using ByteSync.Interfaces.Services.Sessions.Connecting.Joining;

namespace ByteSync.Services.Sessions.Connecting;

public class CloudSessionPasswordExchangeKeyGivenService : ICloudSessionPasswordExchangeKeyGivenService
{
    private readonly ICloudSessionConnectionRepository _cloudSessionConnectionRepository;
    private readonly IEnvironmentService _environmentService;
    private readonly IPublicKeysManager _publicKeysManager;
    private readonly ICloudSessionApiClient _cloudSessionApiClient;
    private readonly ICloudSessionConnectionService _cloudSessionConnectionService;
    private readonly ILogger<CloudSessionPasswordExchangeKeyGivenService> _logger;
    
    private const string UNKNOWN_RECEIVED_SESSION_ID = "unknown received sessionId {sessionId}";
    private const string PUBLIC_KEY_IS_NOT_TRUSTED = "Public key is not trusted";

    public CloudSessionPasswordExchangeKeyGivenService(
        ICloudSessionConnectionRepository cloudSessionConnectionRepository,
        IEnvironmentService environmentService,
        IPublicKeysManager publicKeysManager,
        ICloudSessionApiClient cloudSessionApiClient,
        ICloudSessionConnectionService cloudSessionConnectionService,
        ILogger<CloudSessionPasswordExchangeKeyGivenService> logger)
    {
        _cloudSessionConnectionRepository = cloudSessionConnectionRepository;
        _environmentService = environmentService;
        _publicKeysManager = publicKeysManager;
        _cloudSessionApiClient = cloudSessionApiClient;
        _cloudSessionConnectionService = cloudSessionConnectionService;
        _logger = logger;
    }
    
    public async Task Process(GiveCloudSessionPasswordExchangeKeyParameters request)
    {
        try
        {
            if (!await _cloudSessionConnectionRepository.CheckConnectingCloudSession(request.SessionId))
            {
                _logger.LogError(UNKNOWN_RECEIVED_SESSION_ID, request.SessionId);
                return;
            }
            if (!_environmentService.ClientInstanceId.Equals(request.JoinerInstanceId))
            {
                _logger.LogWarning("Unexpected password provide request received with JoinerId {joinerId}", request.JoinerInstanceId);
                return;
            }
            
            await _cloudSessionConnectionRepository.SetPasswordExchangeKeyReceived(request.SessionId);
        
            var isTrusted = _publicKeysManager.IsTrusted(request.PublicKeyInfo);
            if (isTrusted)
            {
                var password = await _cloudSessionConnectionRepository.GetTempSessionPassword(request.SessionId);
                ExchangePassword exchangePassword = new(request.SessionId, _environmentService.ClientInstanceId, password!);

                var encryptedPassword = _publicKeysManager.EncryptString(request.PublicKeyInfo, exchangePassword.Data);
                AskJoinCloudSessionParameters outParameters = new (request, encryptedPassword);

                _logger.LogInformation("...Providing encrypted password to the validator");
                var joinSessionResult = await _cloudSessionApiClient.AskJoinCloudSession(outParameters,
                    _cloudSessionConnectionRepository.CancellationToken);

                if (!joinSessionResult.IsOK)
                {
                    await _cloudSessionConnectionService.OnJoinSessionError(joinSessionResult);
                }
                else
                {
                    await _cloudSessionConnectionRepository.WaitOrThrowAsync(request.SessionId, 
                        data => data.WaitForJoinSessionEvent, data => data.WaitTimeSpan, "Join session failed");
                }
            }
            else
            {
                throw new Exception(PUBLIC_KEY_IS_NOT_TRUSTED);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OnCloudSessionPasswordExchangeKeyGiven");
        }
    }
}