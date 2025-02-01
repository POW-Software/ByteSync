using ByteSync.Business.Sessions;
using ByteSync.Business.Sessions.RunSessionInfos;
using ByteSync.Common.Business.Sessions.Cloud.Connections;
using ByteSync.Interfaces.Controls.Communications;
using ByteSync.Interfaces.Controls.Communications.Http;
using ByteSync.Interfaces.Controls.Sessions;
using ByteSync.Interfaces.Repositories;
using ByteSync.Interfaces.Services.Sessions.Connecting;

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
    private readonly ICloudSessionConnector _cloudSessionConnector;
    private readonly ILogger<JoinSessionService> _logger;

    public JoinSessionService(ISessionService sessionService, ICloudSessionConnectionRepository cloudSessionConnectionRepository,
        ITrustProcessPublicKeysRepository trustProcessPublicKeysRepository, IDigitalSignaturesRepository digitalSignaturesRepository,
        IPublicKeysTruster publicKeysTruster, IPublicKeysManager publicKeysManager, ICloudSessionApiClient cloudSessionApiClient,
        ICloudSessionConnector cloudSessionConnector, ILogger<JoinSessionService> logger)
    {
        _sessionService = sessionService;
        _cloudSessionConnectionRepository = cloudSessionConnectionRepository;
        _trustProcessPublicKeysRepository = trustProcessPublicKeysRepository;
        _digitalSignaturesRepository = digitalSignaturesRepository;
        _publicKeysTruster = publicKeysTruster;
        _publicKeysManager = publicKeysManager;
        _cloudSessionApiClient = cloudSessionApiClient;
        _cloudSessionConnector = cloudSessionConnector;
        _logger = logger;
    }
    
    public async Task JoinSession(string sessionId, string sessionPassword, RunCloudSessionProfileInfo? lobbySessionDetails)
    {
        try
        {
            await DoStartJoinSession(sessionId, sessionPassword, lobbySessionDetails);
        }
        catch (Exception)
        {
            var joinSessionResult = JoinSessionResult.BuildFrom(JoinSessionStatuses.UnexpectedError);
            await _cloudSessionConnector.OnJoinSessionError(joinSessionResult);

            throw;
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

        await _cloudSessionConnector.InitializeConnection(SessionConnectionStatus.JoiningSession);
        
        // await Task.Delay(5000, _cloudSessionConnectionRepository.CancellationToken);

        await _trustProcessPublicKeysRepository.Start(sessionId);
        await _digitalSignaturesRepository.Start(sessionId);

        _logger.LogInformation("Start joining the Cloud Session {sessionId}: getting password exchange encryption key", sessionId);

        await _cloudSessionConnectionRepository.SetCloudSessionConnectionData(sessionId, sessionPassword, lobbySessionDetails);

        var joinSessionResult = await _publicKeysTruster.TrustAllMembersPublicKeys(sessionId);
        if (!joinSessionResult.IsOK)
        {
            await _cloudSessionConnector.OnJoinSessionError(joinSessionResult);
            return;
        }

        var parameters = new AskCloudSessionPasswordExchangeKeyParameters(sessionId, _publicKeysManager.GetMyPublicKeyInfo());
        parameters.LobbyId = lobbySessionDetails?.LobbyId;
        parameters.ProfileClientId = lobbySessionDetails?.LocalProfileClientId;
        joinSessionResult = await _cloudSessionApiClient.AskPasswordExchangeKey(parameters);

        if (!joinSessionResult.IsOK)
        {
            await _cloudSessionConnector.OnJoinSessionError(joinSessionResult);
        }
        else
        {
            await _cloudSessionConnectionRepository.WaitOrThrowAsync(sessionId,
                data => data.WaitForPasswordExchangeKeyEvent, data => data.WaitTimeSpan, "Keys exchange failed: no key received");
        }
    }
}