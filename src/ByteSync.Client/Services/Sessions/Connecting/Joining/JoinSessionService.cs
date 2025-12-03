using ByteSync.Business.Sessions;
using ByteSync.Business.Sessions.Connecting;
using ByteSync.Business.Sessions.RunSessionInfos;
using ByteSync.Common.Business.Sessions.Cloud.Connections;
using ByteSync.Interfaces.Controls.Communications;
using ByteSync.Interfaces.Controls.Communications.Http;
using ByteSync.Interfaces.Repositories;
using ByteSync.Interfaces.Services.Sessions;
using ByteSync.Interfaces.Services.Sessions.Connecting;
using ByteSync.Interfaces.Services.Sessions.Connecting.Joining;

namespace ByteSync.Services.Sessions.Connecting;

public class JoinSessionService : IJoinSessionService
{
    private readonly ISessionService _sessionService;
    private readonly ICloudSessionConnectionRepository _cloudSessionConnectionRepository;
    private readonly ITrustProcessPublicKeysRepository _trustProcessPublicKeysRepository;
    private readonly IDigitalSignaturesRepository _digitalSignaturesRepository;
    private readonly IPublicKeysTruster _publicKeysTruster;
    private readonly IPublicKeysManager _publicKeysManager;
    private readonly ICloudSessionApiClient _cloudSessionApiClient;
    private readonly ICloudSessionConnectionService _cloudSessionConnectionService;
    private readonly ILogger<JoinSessionService> _logger;
    
    public JoinSessionService(ISessionService sessionService, ICloudSessionConnectionRepository cloudSessionConnectionRepository,
        ITrustProcessPublicKeysRepository trustProcessPublicKeysRepository, IDigitalSignaturesRepository digitalSignaturesRepository,
        IPublicKeysTruster publicKeysTruster, IPublicKeysManager publicKeysManager, ICloudSessionApiClient cloudSessionApiClient,
        ICloudSessionConnectionService cloudSessionConnectionService, ILogger<JoinSessionService> logger)
    {
        _sessionService = sessionService;
        _cloudSessionConnectionRepository = cloudSessionConnectionRepository;
        _trustProcessPublicKeysRepository = trustProcessPublicKeysRepository;
        _digitalSignaturesRepository = digitalSignaturesRepository;
        _publicKeysTruster = publicKeysTruster;
        _publicKeysManager = publicKeysManager;
        _cloudSessionApiClient = cloudSessionApiClient;
        _cloudSessionConnectionService = cloudSessionConnectionService;
        _logger = logger;
    }
    
    public async Task JoinSession(string sessionId, string sessionPassword, RunCloudSessionProfileInfo? lobbySessionDetails)
    {
        try
        {
            await DoStartJoinSession(sessionId, sessionPassword, lobbySessionDetails);
        }
        catch (Exception ex)
        {
            await _cloudSessionConnectionService.HandleJoinSessionError(ex);
        }
    }
    
    public Task CancelJoinCloudSession()
    {
        _logger.LogInformation("User requested to cancel joining the Cloud Session");
        _cloudSessionConnectionRepository.CancellationTokenSource.Cancel();
        
        return Task.CompletedTask;
    }
    
    private async Task DoStartJoinSession(string sessionId, string sessionPassword, RunCloudSessionProfileInfo? lobbySessionDetails)
    {
        if (sessionId.IsNotEmpty(true) && sessionId.Equals(_sessionService.SessionId))
        {
            return;
        }
        
        await _cloudSessionConnectionService.InitializeConnection(SessionConnectionStatus.JoiningSession);
        
        // await Task.Delay(5000, _cloudSessionConnectionRepository.CancellationToken);
        
        await _trustProcessPublicKeysRepository.Start(sessionId);
        await _digitalSignaturesRepository.Start(sessionId);
        
        _logger.LogInformation("Start joining the Cloud Session {sessionId}: getting password exchange encryption key", sessionId);
        
        await _cloudSessionConnectionRepository.SetCloudSessionConnectionData(sessionId, sessionPassword, lobbySessionDetails);
        
        _logger.LogInformation(
            "[PROTOCOL_VERSION_DEBUG] JoinSessionService - About to call TrustAllMembersPublicKeys for SessionId={SessionId}", sessionId);
        var joinSessionResult =
            await _publicKeysTruster.TrustAllMembersPublicKeys(sessionId, _cloudSessionConnectionRepository.CancellationToken);
        _logger.LogInformation(
            "[PROTOCOL_VERSION_DEBUG] JoinSessionService - TrustAllMembersPublicKeys returned: IsOK={IsOK}, Status={Status}",
            joinSessionResult.IsOK, joinSessionResult.Status);
        
        if (!joinSessionResult.IsOK)
        {
            var joinSessionError = new JoinSessionError
            {
                Status = joinSessionResult.Status
            };
            
            await _cloudSessionConnectionService.OnJoinSessionError(joinSessionError);
            
            return;
        }
        
        var parameters = new AskCloudSessionPasswordExchangeKeyParameters(sessionId, _publicKeysManager.GetMyPublicKeyInfo());
        parameters.LobbyId = lobbySessionDetails?.LobbyId;
        parameters.ProfileClientId = lobbySessionDetails?.LocalProfileClientId;
        joinSessionResult = await _cloudSessionApiClient.AskPasswordExchangeKey(parameters,
            _cloudSessionConnectionRepository.CancellationToken);
        
        if (!joinSessionResult.IsOK)
        {
            var joinSessionError = new JoinSessionError
            {
                Status = joinSessionResult.Status
            };
            
            await _cloudSessionConnectionService.OnJoinSessionError(joinSessionError);
        }
        else
        {
            await _cloudSessionConnectionRepository.WaitOrThrowAsync(sessionId,
                data => data.WaitForPasswordExchangeKeyEvent, data => data.WaitTimeSpan, "Keys exchange failed: no key received",
                _cloudSessionConnectionRepository.CancellationToken);
        }
    }
}